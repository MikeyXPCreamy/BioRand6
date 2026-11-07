using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MikeyXPCreamy.BioRand6
{
    [DataContract]
    public sealed class ResourceMap
    {
        [DataMember] public int Index;
        [DataMember] public string Name;
        [DataMember] public string Sha256;
        [DataMember] public uint Type;
        [DataMember] public List<ItemSlot> Slots;
    }
}
