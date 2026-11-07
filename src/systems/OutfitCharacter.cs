namespace MikeyXPCreamy.BioRand6
{
    public sealed class OutfitCharacter
    {
        public readonly int Id;
        public readonly string Name;
        public readonly OutfitOption[] Options;

        public OutfitCharacter(int id, string name, params OutfitOption[] options)
        {
            Id = id;
            Name = name;
            Options = options;
        }
    }
}
