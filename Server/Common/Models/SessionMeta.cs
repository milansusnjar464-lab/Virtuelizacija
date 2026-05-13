using System.Runtime.Serialization;

namespace Common.Models
{
    [DataContract]
    public class SessionMeta
    {
        [DataMember]
        public string SessionId { get; set; }

        [DataMember]
        public string Description { get; set; }
    }
}