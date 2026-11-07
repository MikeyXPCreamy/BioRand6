namespace MikeyXPCreamy.BioRand6
{
    public sealed class OutfitOption
    {
        public readonly byte Variant;
        public readonly string Name;

        public OutfitOption(byte variant, string name)
        {
            Variant = variant;
            Name = name;
        }
    }
}
