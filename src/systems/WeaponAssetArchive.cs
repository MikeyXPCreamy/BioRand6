using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MikeyXPCreamy.BioRand6
{
    [DataContract]
    public sealed class WeaponAssetArchive
    {
        [DataMember] public string RelativePath;
        [DataMember] public string Sha256;
        [DataMember(EmitDefaultValue = false)] public List<WeaponAssetResource> Resources;
    }
}
