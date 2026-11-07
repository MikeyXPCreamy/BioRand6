using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MikeyXPCreamy.BioRand6
{
    [DataContract]
    public sealed class FreshCharacter
    {
        [DataMember] public string Character;
        [DataMember] public int WeaponId;
        [DataMember] public string Weapon;
        [DataMember] public int LoadedRounds;
        [DataMember] public int AmmoId;
        [DataMember] public int ReserveRounds;
        [DataMember] public string RetainedSpecial;
        [DataMember] public List<FreshWeapon> Weapons = new List<FreshWeapon>();
        [DataMember] public int FirstAidSprays;
        [DataMember] public int HealthTablets;
        [DataMember] public int GrenadeId;
        [DataMember] public int Grenades;
        [DataMember] public int ExtraGrenadeId;
        [DataMember] public int ExtraGrenades;
        [DataMember] public int GreenHerbs;
        [DataMember] public int RedHerbs;
    }
}
