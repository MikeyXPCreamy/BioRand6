using System.Runtime.Serialization;

namespace MikeyXPCreamy.BioRand6
{
    [DataContract]
    public sealed class FreshWeapon
    {
        [DataMember] public int WeaponId;
        [DataMember] public int LoadedRounds;
        [DataMember] public int AmmoId;
        [DataMember] public int ReserveRounds;
        [DataMember] public string Weapon;
    }
}
