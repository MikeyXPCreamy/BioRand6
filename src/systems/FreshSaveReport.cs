using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MikeyXPCreamy.BioRand6
{
    [DataContract]
    public sealed class FreshSaveReport
    {
        [DataMember] public string Version;
        [DataMember] public string SeedId;
        [DataMember] public string OriginalSha256;
        [DataMember] public string CandidateSha256;
        [DataMember] public string Status;
        [DataMember] public bool ExistingChapterHistoryPreserved = true;
        [DataMember] public bool UnlocksAdditionalChapters;
        [DataMember] public List<FreshCharacter> Characters = new List<FreshCharacter>();
        [DataMember] public int StartingChapter;
    }
}
