using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MikeyXPCreamy.BioRand6
{
    public static class WeaponAssetBundle
    {
        private const string BasePath = "nativePC/arc/DX9/PlayerBase.arc";
        private const string EffectsPath = "nativePC/arc/DX9/coreEff.arc";
        private const string SoundPath = "nativePC/arc/DX9/Game.arc";
        private const int SoundArchiveIndex = 2;

        private const uint ArcMagic = 4411969; // "ARC\0"
        private const ushort ArcVersion = 7;
        private const int HeaderSize = 8;
        private const int EntrySize = 80;
        private const int NameSize = 64;
        private const int TypeOffset = 64;
        private const int PackedSizeOffset = 68;
        private const int FlagsOffset = 72;
        private const int DataOffset = 76;
        private const int DataAlignment = 2048;
        private const uint SoundBankType = 2117312876;

        private static readonly string[] CommonSounds =
        {
            "sound\\se\\pl\\pl_cmn\\snd_pl_cmn",
            "sound\\se\\sys\\dmg\\snd_dmg_cmn",
            "sound\\se\\wp\\snd_wp_cmn"
        };

        private static readonly int[] WeaponPacks =
        {
            1010, 1011, 1012, 1020, 1021, 1022, 1030, 1031, 1040, 1041,
            1050, 1060, 1061, 1062, 1070, 1071, 1072, 1080, 1110, 1111,
            0, 1112
        };

        private static List<PackedWeaponAsset> Read(byte[] arc, List<WeaponAssetResource> selection = null)
        {
            if (arc == null || arc.Length < HeaderSize || ArcCodec.U32(arc, 0) != ArcMagic || BitConverter.ToUInt16(arc, 4) != ArcVersion)
                throw new InvalidDataException("Unsupported weapon asset ARC.");
            int count = BitConverter.ToUInt16(arc, 6);
            if (count == 0 || HeaderSize + EntrySize * count > arc.Length)
                throw new InvalidDataException("Invalid weapon asset table.");
            if (selection != null && (selection.Count == 0
                || selection.Any(resource => resource.Index < 0 || resource.Index >= count)
                || selection.Select(resource => resource.Index).Distinct().Count() != selection.Count))
                throw new InvalidDataException("Invalid shared resource selection.");

            var assets = new List<PackedWeaponAsset>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            IEnumerable<int> indexes = selection == null ? Enumerable.Range(0, count) : selection.Select(resource => resource.Index);
            foreach (int index in indexes)
            {
                int entry = HeaderSize + EntrySize * index;
                string name = Encoding.UTF8.GetString(arc, entry, NameSize).Split('\0')[0];
                uint type = ArcCodec.U32(arc, entry + TypeOffset);
                if (string.IsNullOrEmpty(name) || !name.All(character => character >= ' ' && character < '\u007f'))
                    throw new InvalidDataException("Invalid asset resource name.");

                string rawSha256 = Hashes.Bytes(ArcCodec.UnpackResource(arc, index, name, type));
                if (selection != null)
                {
                    int selectedIndex = index;
                    WeaponAssetResource expected = selection.Single(resource => resource.Index == selectedIndex);
                    if (expected.Name != name || expected.Type != type || expected.Sha256 != rawSha256)
                        throw new InvalidDataException("Shared resource does not match the verified dependency map.");
                }

                int dataOffset = checked((int)ArcCodec.U32(arc, entry + DataOffset));
                int packedSize = checked((int)ArcCodec.U32(arc, entry + PackedSizeOffset));
                var asset = new PackedWeaponAsset
                {
                    Name = name,
                    Type = type,
                    RawSha256 = rawSha256,
                    Header = new byte[EntrySize],
                    Packed = new byte[packedSize]
                };
                Buffer.BlockCopy(arc, entry, asset.Header, 0, EntrySize);
                Buffer.BlockCopy(arc, dataOffset, asset.Packed, 0, packedSize);
                if (!seen.Add(asset.Key))
                    throw new InvalidDataException("Duplicate resource inside a weapon archive.");
                assets.Add(asset);
            }
            return assets;
        }

        private static byte[] Merge(IEnumerable<PackedWeaponAsset> resources)
        {
            var merged = new List<PackedWeaponAsset>();
            var byKey = new Dictionary<string, PackedWeaponAsset>(StringComparer.OrdinalIgnoreCase);
            foreach (PackedWeaponAsset resource in resources)
            {
                PackedWeaponAsset existing;
                if (byKey.TryGetValue(resource.Key, out existing))
                {
                    if (existing.RawSha256 != resource.RawSha256 || BitConverter.ToUInt32(existing.Header, FlagsOffset) != BitConverter.ToUInt32(resource.Header, FlagsOffset))
                        throw new InvalidDataException("Conflicting weapon dependency: " + resource.Name);
                }
                else
                {
                    byKey.Add(resource.Key, resource);
                    merged.Add(resource);
                }
            }
            if (merged.Count == 0 || merged.Count > ushort.MaxValue)
                throw new InvalidDataException("Weapon asset bundle size is unsupported.");

            byte[] arc;
            using (var memory = new MemoryStream())
            using (var writer = new BinaryWriter(memory))
            {
                writer.Write(ArcMagic);
                writer.Write(ArcVersion);
                writer.Write((ushort)merged.Count);
                memory.SetLength(HeaderSize + EntrySize * merged.Count);
                for (int i = 0; i < merged.Count; i++)
                {
                    memory.Position = memory.Length;
                    while (memory.Position % DataAlignment != 0)
                        writer.Write((byte)0);
                    uint dataOffset = checked((uint)memory.Position);
                    writer.Write(merged[i].Packed);

                    byte[] header = (byte[])merged[i].Header.Clone();
                    ArcCodec.Put32(header, DataOffset, dataOffset);
                    memory.Position = HeaderSize + EntrySize * i;
                    writer.Write(header);
                }
                writer.Flush();
                arc = memory.ToArray();
            }

            List<PackedWeaponAsset> check = Read(arc);
            if (check.Count != merged.Count)
                throw new InvalidDataException("Weapon asset count changed during packing.");
            for (int i = 0; i < merged.Count; i++)
            {
                if (check[i].Key != merged[i].Key || check[i].RawSha256 != merged[i].RawSha256 || !check[i].Packed.SequenceEqual(merged[i].Packed))
                    throw new InvalidDataException("Weapon asset round-trip failed.");
            }
            return arc;
        }

        public static FilePatch CreatePatch(string gameRoot, List<WeaponAssetArchive> profile)
        {
            string[] paths = new[] { BasePath, EffectsPath, SoundPath }
                .Concat(WeaponPacks.Select(pack => "nativePC/arc/DX9/uWp" + pack.ToString("D4") + ".arc"))
                .ToArray();
            if (profile == null || profile.Count != paths.Length)
                throw new InvalidDataException("Weapon asset profile is missing.");

            var store = new PatchStore(gameRoot, Path.GetTempPath(), () => false);
            var assets = new List<PackedWeaponAsset>();
            for (int i = 0; i < paths.Length; i++)
            {
                if (!string.Equals(profile[i].RelativePath.Replace('\\', '/'), paths[i], StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Unexpected weapon dependency archive.");

                List<WeaponAssetResource> selection = profile[i].Resources;
                if (i == SoundArchiveIndex)
                {
                    if (selection == null || selection.Count != CommonSounds.Length
                        || selection.Any(resource => resource.Type != SoundBankType)
                        || !selection.Select(resource => resource.Name).OrderBy(name => name, StringComparer.Ordinal).SequenceEqual(CommonSounds))
                        throw new InvalidDataException("Common sound dependency selection is incomplete.");
                }
                else if (selection != null)
                {
                    throw new InvalidDataException("A required complete asset pack was filtered.");
                }

                byte[] archive = File.ReadAllBytes(store.GamePath(paths[i]));
                if (Hashes.Bytes(archive) != profile[i].Sha256)
                    throw new InvalidDataException("Weapon assets differ from the supported originals. Restore the previous seed or remove conflicting mods: " + paths[i]);
                assets.AddRange(Read(archive, selection));
            }

            return new FilePatch
            {
                RelativePath = BasePath,
                OriginalSha256 = profile[0].Sha256,
                Content = Merge(assets)
            };
        }
    }
}
