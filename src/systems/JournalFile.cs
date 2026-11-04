using System.Runtime.Serialization;

namespace MikeyXPCreamy.BioRand6
{
    [DataContract]
    public sealed class JournalFile
    {
        [DataMember] public string RelativePath;
        [DataMember] public string OriginalSha256;
        [DataMember] public string PatchedSha256;
        [DataMember] public string BackupName;
    }
}
