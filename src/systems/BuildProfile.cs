using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MikeyXPCreamy.BioRand6
{
    [DataContract]
    public sealed class BuildProfile
    {
        [DataMember] public string BuildId;
        [DataMember] public string ExecutableSha256;
        [DataMember] public List<ArchiveMap> Archives;
        [DataMember] public List<StageContext> StageContexts;
        [DataMember] public List<WeaponAssetArchive> WeaponAssets;
    }
}
