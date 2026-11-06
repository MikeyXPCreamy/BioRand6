using System.Runtime.Serialization;

namespace MikeyXPCreamy.BioRand6
{
    [DataContract]
    public sealed class WeaponAssetResource
    {
        [DataMember] public int Index;
        [DataMember] public uint Type;
        [DataMember] public string Name;
        [DataMember] public string Sha256;
    }
}
