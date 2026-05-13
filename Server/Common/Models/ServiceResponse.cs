using System.Runtime.Serialization;

namespace Common.Models
{
    public enum StatusType
    {
        ACK,
        NACK
    }

    public enum SessionStatus
    {
        IN_PROGRESS,
        COMPLETED
    }

    [DataContract]
    public class ServiceResponse
    {
        [DataMember]
        public StatusType Status { get; set; }

        [DataMember]
        public SessionStatus SessionStatus { get; set; }

        [DataMember]
        public string Message { get; set; }
    }
}