using System.Runtime.Serialization;

namespace MikeyXPCreamy.BioRand6
{
    [DataContract]
    public sealed class GameInfo
    {
        [DataMember] public string GameRoot;
        [DataMember] public string Manifest;
        [DataMember] public string BuildId;
        [DataMember] public uint StateFlags;
        [DataMember] public bool Ready;
        [DataMember] public bool Downloading;
        [DataMember] public long Downloaded;
        [DataMember] public long DownloadTotal;
        [DataMember] public string ExecutableSha256;
        [DataMember] public string ExecutableVersion;
        [DataMember] public int ArchiveCount;
        [DataMember] public string Message;
    }
}
