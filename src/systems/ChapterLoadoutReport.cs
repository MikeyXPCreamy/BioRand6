using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MikeyXPCreamy.BioRand6
{
    [DataContract]
    public sealed class ChapterLoadoutReport
    {
        [DataMember] public string Version = "chapter-inventory-v7-new-kinds";
        [DataMember] public string Selection = "Detected from RE6 Chapter Select";
        [DataMember] public List<FreshSaveReport> Chapters = new List<FreshSaveReport>();
    }
}
