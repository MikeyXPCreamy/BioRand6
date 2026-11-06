using System.Runtime.Serialization;

namespace MikeyXPCreamy.BioRand6
{
    [DataContract]
    public sealed class WeaponReplacement
    {
        [DataMember] public int OriginalId;
        [DataMember] public int ReplacementId;
        [DataMember] public int AmmoId;
        [DataMember] public int PoolGroup;
        [DataMember] public string Original;
        [DataMember] public string Replacement;
    }
}
