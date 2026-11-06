using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MikeyXPCreamy.BioRand6
{
    // The rule names and draw order decide what a seed gives.
    public sealed class WeaponPlan
    {
        private const string PoolRule = "weapon-pool-v3-campaign-groups";
        private const string CustomPoolRule = "weapon-pool-v4-custom";
        private const string MeleeRule = "melee-pool-v2-all-campaigns";

        public const int GunLimit = 7;
        private const int SlotsPerCharacter = 15;
        private const int Characters = 8;
        private const int PoolGroups = 2;
        private const int Guns = 18;
        private const ushort FirstGun = 257;
        private const ushort LastGun = FirstGun + Guns - 1;
        private const int OrderRange = 100000;

        private const int RunFileMapSize = Guns * 2;
        private const int RunFileSize = RunFileMapSize + Characters * SlotsPerCharacter * 2;
        private const string RunFileMagic = "RE6WPN7\0";
        private const int RunFileVersion = 5;
        private const uint FnvOffset = 2166136261;
        private const uint FnvPrime = 16777619;

        private static readonly ushort[][] AmmoGroups =
        {
            new ushort[] { 513, 514, 515, 516 },
            new ushort[] { 526, 518, 519, 517, 520 }
        };

        private static readonly ushort[] MeleeChoices = { 256, 280, 281, 282 };
        private static readonly ushort[] KeptSpecials = { 256, 279, 280, 281, 282 };

        private static readonly ushort[][] StoryPickups =
        {
            new ushort[] { 260, 266, 273, 263 },
            new ushort[] { 261, 274, 266 },
            new ushort[] { 264, 265, 272, 274, 260 },
            new ushort[] { 261, 265, 272 }
        };

        private static readonly ushort[] NormalFirstGuns = { 259, 258, 257, 270, 257, 269, 268, 257 };

        private static readonly int[] NormalGunCounts = { 1, 2, 2, 3, 3 };
        private static readonly int[] RandomGunCounts = { 2, 2, 3, 3, 4 };
        private static readonly int[] RandomExtraGuns = { 0, 1, 1, 2, 2 };

        private readonly SeedSetup seed;
        private readonly ushort[] mapping;
        private readonly ushort[][] pools;

        private WeaponPlan(SeedSetup seed, ushort[] mapping, ushort[][] pools)
        {
            this.seed = seed;
            this.mapping = mapping;
            this.pools = pools;
        }

        public static WeaponPlan Create(SeedSetup seed)
        {
            seed.Validate();
            return seed.CustomWeapons ? CreateCustom(seed) : CreateStandard(seed);
        }

        public static int LogicalSlot(int position)
        {
            if (position < 0 || position >= SlotsPerCharacter)
                throw new ArgumentOutOfRangeException("position");
            return position < GunLimit ? position : 28 + position - GunLimit;
        }

        private static WeaponPlan CreateStandard(SeedSetup seed)
        {
            ushort[] chosenAmmo = AmmoGroups[0]
                .OrderBy(ammo => SeedChoice.Pick(seed, PoolRule, "ammo-group|" + ammo, OrderRange))
                .Take(3)
                .ToArray();
            ushort[] allAmmo = AmmoGroups.SelectMany(group => group).Distinct().ToArray();
            ushort[] otherAmmo = allAmmo.Except(chosenAmmo).ToArray();
            chosenAmmo = chosenAmmo.Concat(new[] { otherAmmo[SeedChoice.Pick(seed, PoolRule, "ammo-group-extra", otherAmmo.Length)] }).ToArray();
            chosenAmmo = chosenAmmo.Concat(allAmmo.Except(chosenAmmo)
                .OrderBy(ammo => SeedChoice.Pick(seed, PoolRule, "expanded-ammo|" + ammo, OrderRange))
                .Take(GunLimit - 4)).ToArray();

            var guns = new ushort[PoolGroups][];
            for (int group = 0; group < PoolGroups; group++)
            {
                guns[group] = new ushort[GunLimit];
                for (int slot = 0; slot < GunLimit; slot++)
                {
                    ushort ammo = chosenAmmo[slot];
                    ushort[] sameAmmo = Enumerable.Range(FirstGun, Guns)
                        .Select(id => (ushort)id)
                        .Where(id => FreshSaveCodec.Ammo(id) == ammo)
                        .ToArray();
                    int pick = SeedChoice.Pick(seed, PoolRule, "pool|" + group + "|" + ammo, sameAmmo.Length);
                    if (group == 1 && sameAmmo.Length > 1)
                    {
                        while (sameAmmo[pick] == guns[0][slot])
                            pick = (pick + 1) % sameAmmo.Length;
                    }
                    guns[group][slot] = sameAmmo[pick];
                }
            }

            ushort[] map = Enumerable.Range(0, Guns)
                .Select(gun => seed.Weapons ? guns[0][SeedChoice.Pick(seed, PoolRule, "map|0|" + gun, guns[0].Length)] : (ushort)(FirstGun + gun))
                .ToArray();
            if (!seed.Weapons)
                guns = new[] { new ushort[GunLimit], new ushort[GunLimit] };
            return new WeaponPlan(seed, map, guns);
        }

        private static WeaponPlan CreateCustom(SeedSetup seed)
        {
            ushort[] allowed = Enumerable.Range(FirstGun, Guns).Where(seed.GunAllowed).Select(id => (ushort)id).ToArray();
            ushort[] startWeapons = seed.StartWeapons.Select(id => (ushort)id).ToArray();
            int poolSize = Math.Min(GunLimit, allowed.Length);

            var guns = new ushort[PoolGroups][];
            for (int group = 0; group < PoolGroups; group++)
            {
                int currentGroup = group;
                List<ushort> pool = startWeapons.ToList();
                ushort[] ordered = allowed.Except(startWeapons)
                    .OrderBy(id => SeedChoice.Pick(seed, CustomPoolRule, "pool|" + currentGroup + "|" + id, OrderRange))
                    .ThenBy(id => id)
                    .ToArray();
                foreach (ushort id in ordered)
                {
                    if (pool.Count < poolSize && !pool.Any(gun => FreshSaveCodec.Ammo(gun) == FreshSaveCodec.Ammo(id)))
                        pool.Add(id);
                }
                foreach (ushort id in ordered)
                {
                    if (pool.Count < poolSize && !pool.Contains(id))
                        pool.Add(id);
                }
                guns[group] = pool.ToArray();
            }

            ushort[] map = Enumerable.Range(0, Guns)
                .Select(gun => guns[0][SeedChoice.Pick(seed, CustomPoolRule, "map|0|" + gun, guns[0].Length)])
                .ToArray();
            return new WeaponPlan(seed, map, guns);
        }

        public ushort Map(ushort id)
        {
            return id < FirstGun || id > LastGun ? id : mapping[id - FirstGun];
        }

        private ushort[] CampaignPool(int campaign)
        {
            if (campaign < 0 || campaign > 3)
                throw new ArgumentOutOfRangeException("campaign");
            return pools[campaign == 0 ? 0 : 1];
        }

        public ushort PickupGun(string location, int campaign)
        {
            ushort[] pool = CampaignPool(campaign);
            return pool[seed.Weapons ? SeedChoice.Pick(seed, PoolRule, "pickup|" + location, pool.Length) : 0];
        }

        public ushort StoryPickupGun(string location, int campaign, ushort original)
        {
            int position = Array.IndexOf(StoryPickups[campaign], original);
            if (position < 0)
                return PickupGun(location, campaign);

            ushort[] starting = StartingGuns(campaign * 2, 1).Concat(StartingGuns(campaign * 2 + 1, 1)).ToArray();
            ushort[] ordered = CampaignPool(campaign)
                .OrderBy(id => starting.Count(gun => gun == id))
                .ThenBy(id => SeedChoice.Pick(seed, PoolRule, "story-order|" + campaign + "|" + id, OrderRange))
                .ToArray();
            return ordered[position % ordered.Length];
        }

        public ushort[] RuntimeSlots(int character)
        {
            if (character < 0 || character >= Characters)
                throw new ArgumentOutOfRangeException("character");

            var slots = new ushort[SlotsPerCharacter];
            ushort[] normal = FreshSaveCodec.Weapons[character];
            if (!seed.Weapons)
            {
                for (int i = 0; i < normal.Length && i < slots.Length; i++)
                    slots[i] = Map(normal[i]);
                return slots;
            }

            ushort[] pool = CampaignPool(character / 2);
            Array.Copy(pool, slots, pool.Length);
            int used = pool.Length;
            slots[used++] = MeleeChoices[SeedChoice.Pick(seed, MeleeRule, "melee|" + character, MeleeChoices.Length)];
            foreach (ushort special in normal.Where(id => KeptSpecials.Contains(id)))
            {
                if (used < slots.Length && !slots.Contains(special))
                    slots[used++] = special;
            }
            return slots;
        }

        public ushort OwnedMelee(int character)
        {
            return seed.Weapons ? RuntimeSlots(character)[CampaignPool(character / 2).Length] : (ushort)0;
        }

        public ushort[] StartingGuns(int character, int chapter)
        {
            if (character < 0 || character >= Characters || chapter < 1 || chapter > 5)
                throw new ArgumentOutOfRangeException();

            int index = chapter - 1;
            List<ushort> available = (seed.Weapons
                ? CampaignPool(character / 2)
                : FreshSaveCodec.Weapons[character].Where(id => FreshSaveCodec.GunNames.ContainsKey(id)).Select(Map).ToArray()).ToList();
            int count = seed.Weapons
                ? RandomGunCounts[index] + SeedChoice.Pick(seed, PoolRule, character + "|chapter-" + chapter + "|count", RandomExtraGuns[index] + 1)
                : NormalGunCounts[index];

            if (!seed.Weapons)
            {
                ushort first = Map(NormalFirstGuns[character]);
                int position = available.IndexOf(first);
                if (position >= 0)
                {
                    available.RemoveAt(position);
                    available.Insert(0, first);
                }
            }

            var starting = new List<ushort>();
            if (seed.CustomWeapons)
            {
                foreach (ushort weapon in seed.StartWeapons.Select(id => (ushort)id))
                {
                    if (available.Remove(weapon))
                        starting.Add(weapon);
                }
            }

            count = Math.Min(Math.Max(count, starting.Count), available.Count + starting.Count);
            while (starting.Count < count)
            {
                int pick = seed.Weapons ? SeedChoice.Pick(seed, PoolRule, character + "|chapter-" + chapter + "|gun-" + starting.Count, available.Count) : 0;
                starting.Add(available[pick]);
                available.RemoveAt(pick);
            }
            return starting.ToArray();
        }

        public List<WeaponReplacement> Report()
        {
            string rule = seed.CustomWeapons ? CustomPoolRule : PoolRule;
            var report = new List<WeaponReplacement>();
            int groups = seed.Weapons ? PoolGroups : 1;
            for (int group = 0; group < groups; group++)
            {
                for (int gun = 0; gun < Guns; gun++)
                {
                    ushort original = (ushort)(FirstGun + gun);
                    ushort replacement = seed.Weapons
                        ? pools[group][SeedChoice.Pick(seed, rule, "map|" + group + "|" + gun, pools[group].Length)]
                        : original;
                    report.Add(new WeaponReplacement
                    {
                        OriginalId = original,
                        ReplacementId = replacement,
                        Original = FreshSaveCodec.GunNames[original],
                        Replacement = FreshSaveCodec.GunNames[replacement],
                        AmmoId = FreshSaveCodec.Ammo(replacement),
                        PoolGroup = group
                    });
                }
            }
            return report;
        }

        public static int AmmoCost(int ammoId)
        {
            switch (ammoId)
            {
                case 513:
                case 516:
                    return 1;
                case 514:
                case 515:
                case 524:
                case 526:
                    return 3;
                case 517:
                case 518:
                case 519:
                case 520:
                case 525:
                    return 6;
                default:
                    throw new InvalidDataException("Unknown firearm ammunition.");
            }
        }

        public static int LoadedRounds(ushort gun)
        {
            switch (AmmoCost(FreshSaveCodec.Ammo(gun)))
            {
                case 1:
                    return 5;
                case 3:
                    return 2;
                default:
                    return 1;
            }
        }

        public void Write(string path)
        {
            byte[] payload = new byte[RunFileSize];
            for (int gun = 0; gun < Guns; gun++)
                Buffer.BlockCopy(BitConverter.GetBytes(mapping[gun]), 0, payload, gun * 2, 2);
            for (int character = 0; character < Characters; character++)
            {
                ushort[] slots = RuntimeSlots(character);
                for (int slot = 0; slot < SlotsPerCharacter; slot++)
                    Buffer.BlockCopy(BitConverter.GetBytes(slots[slot]), 0, payload, RunFileMapSize + (character * SlotsPerCharacter + slot) * 2, 2);
            }

            uint checksum = FnvOffset;
            foreach (byte value in payload)
                checksum = (checksum ^ value) * FnvPrime;

            using (var writer = new BinaryWriter(new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None)))
            {
                writer.Write(Encoding.ASCII.GetBytes(RunFileMagic));
                writer.Write(RunFileVersion);
                writer.Write(payload.Length);
                writer.Write(checksum);
                writer.Write(payload);
            }
        }
    }
}
