using System.Runtime.Serialization;

namespace Common.Models
{
    [DataContract]
    public class MotorSample
    {
        [DataMember]
        public double I_q { get; set; }

        [DataMember]
        public double I_d { get; set; }

        [DataMember]
        public double Coolant { get; set; }

        [DataMember]
        public int Profile_Id { get; set; }

        [DataMember]
        public double Ambient { get; set; }

        [DataMember]
        public double Torque { get; set; }
    }
}