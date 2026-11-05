namespace MikeyXPCreamy.BioRand6
{
    public static class ItemOptions
    {
        public const string PoolVersion = "item-pool-v1-custom";

        public static readonly string[] AmmoLevelNames = { "Very low", "Low", "Normal", "High", "Very high" };

        private static readonly int[] QuartersOfBudget = { 2, 3, 4, 6, 8 };
        private const int QuartersPerBudget = 4;

        private static int Level(SeedSetup seed)
        {
            return seed.Items ? seed.AmmoLevel : SeedSetup.AmmoNormal;
        }

        public static int ScaleAmmo(SeedSetup seed, int budget)
        {
            int level = Level(seed);
            return level == SeedSetup.AmmoNormal ? budget : budget * QuartersOfBudget[level - 1] / QuartersPerBudget;
        }

        public static string RunFileText(SeedSetup seed)
        {
            return "RE6ITEMS1\nammo=" + Level(seed) + "\n";
        }

        public static void WriteRunFile(string savePath, SeedSetup seed)
        {
            DataFiles.WriteText(savePath + ".items", RunFileText(seed));
        }
    }
}
