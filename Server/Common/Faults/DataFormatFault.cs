using System.Runtime.Serialization;

namespace Common.Faults
{
    [DataContract]
    public class DataFormatFault
    {
        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public string Field { get; set; }

        public DataFormatFault(string message, string field)
        {
            Message = message;
            Field = field;
        }
    }
}