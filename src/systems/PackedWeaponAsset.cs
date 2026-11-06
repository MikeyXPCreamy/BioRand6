namespace MikeyXPCreamy.BioRand6
{
    public sealed class PackedWeaponAsset
    {
        public string Name;
        public string RawSha256;
        public uint Type;
        public byte[] Header;
        public byte[] Packed;

        public string Key { get { return Name + "|" + Type.ToString("X8"); } }
    }
}
