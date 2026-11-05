using System.Runtime.Serialization;

namespace MikeyXPCreamy.BioRand6
{
    [DataContract]
    public sealed class ItemSlot
    {
        [DataMember] public string Id;
        [DataMember] public string Stage;
        [DataMember] public bool Weapon;
        [DataMember] public int[] Offsets;
        [DataMember] public int[] Values;
    }
}
