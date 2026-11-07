using System.Runtime.Serialization;

namespace MikeyXPCreamy.BioRand6
{
    [DataContract]
    public sealed class StageContext
    {
        [DataMember] public string Stage;
        [DataMember] public int Campaign;
        [DataMember] public int Chapter;
    }
}
