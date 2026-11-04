using System.Collections.Generic;

namespace MikeyXPCreamy.BioRand6
{
    public sealed class PatchPlan
    {
        public string SeedId;
        public string BackendVersion;
        public List<FilePatch> Files = new List<FilePatch>();
    }
}
