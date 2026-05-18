using System.ServiceModel;
using Common.Faults;
using Common.Models;

namespace Common.Contracts
{
    [ServiceContract]
    public interface IMotorService
    {
        [OperationContract]
        [FaultContract(typeof(DataFormatFault))]
        [FaultContract(typeof(ValidationFault))]
        ServiceResponse StartSession(SessionMeta meta);

        [OperationContract]
        [FaultContract(typeof(DataFormatFault))]
        [FaultContract(typeof(ValidationFault))]
        ServiceResponse PushSample(MotorSample sample);

        [OperationContract]
        ServiceResponse EndSession();
    }
}
