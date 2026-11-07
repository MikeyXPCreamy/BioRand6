using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MikeyXPCreamy.BioRand6
{
    [DataContract]
    public sealed class ArchiveMap
    {
        [DataMember] public string RelativePath;
        [DataMember] public string Sha256;
        [DataMember] public List<ResourceMap> Resources;
    }
}
