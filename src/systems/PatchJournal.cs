using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MikeyXPCreamy.BioRand6
{
    [DataContract]
    public sealed class PatchJournal
    {
        public const int CurrentSchema = 1;

        [DataMember] public int Schema = CurrentSchema;
        [DataMember] public string GameRoot;
        [DataMember] public string SeedId;
        [DataMember] public string BackendVersion;
        [DataMember] public string State;
        [DataMember] public List<JournalFile> Files = new List<JournalFile>();
    }
}
