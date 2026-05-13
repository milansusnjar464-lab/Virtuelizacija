using System.Runtime.Serialization;

namespace Common.Faults
{
    [DataContract]
    public class ValidationFault
    {
        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public string Field { get; set; }

        [DataMember]
        public string ExpectedRange { get; set; }

        public ValidationFault(string message, string field, string expectedRange)
        {
            Message = message;
            Field = field;
            ExpectedRange = expectedRange;
        }
    }
}