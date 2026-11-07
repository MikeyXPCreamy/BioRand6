using System;
using System.Collections.Generic;

namespace MikeyXPCreamy.BioRand6
{
    // Sight ahead, sight around, hearing. 100 units = 1 m.
    internal sealed class EnemySenses
    {
        private const float UstanakSight = 1500F;
        private const float UstanakNear = 250F;

        private static readonly Dictionary<string, float[]> KindSenses = new Dictionary<string, float[]>
        {
            { EnemyKinds.Zombie, new[] { 2500F, 500F, 1500F } },
            { EnemyKinds.Catacomb, new[] { 2500F, 500F, 1500F } },
            { EnemyKinds.Bloodshot, new[] { 2800F, 600F, 1600F } },
            { EnemyKinds.Shrieker, new[] { 2300F, 500F, 1500F } },
            { EnemyKinds.Whopper, new[] { 2400F, 500F, 1400F } },
            { EnemyKinds.Dog, new[] { 3000F, 700F, 1400F } }
        };

        private readonly SeedSetup seed;
        private int number;

        public EnemySenses(SeedSetup seed)
        {
            this.seed = seed;
        }

        private static int FloatBits(float value)
        {
            return BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
        }

        private static void SetIfPresent(Xfs.Node info, string field, float value)
        {
            if (Xfs.Has(info, field))
                Xfs.SetInt(info, field, FloatBits(value));
        }

        public void Apply(Xfs.Node info, string kind)
        {
            float[] senses = KindSenses[kind];
            int current = number++;
            float vary = (80 + SeedChoice.Pick(seed, EnemyRandomizer.Version, "senses|" + current, 41)) / 100F;
            SetIfPresent(info, "mSearchForward", senses[0] * vary);
            SetIfPresent(info, "mSearchCircle", senses[1] * vary);
            SetIfPresent(info, "mHearingLength", senses[2] * vary);
        }

        public void Sight(Xfs.Node info)
        {
            Xfs.SetInt(info, "mIsFindPL", 0);
            Apply(info, EnemyKinds.Zombie);
        }

        public void Hunt(Xfs.Node info)
        {
            Sight(info);
            Xfs.SetInt(info, "mSearchForward", FloatBits(UstanakSight));
            Xfs.SetInt(info, "mSearchCircle", FloatBits(UstanakNear));
        }
    }
}
