using System;
using System.ServiceModel;
using Common.Contracts;
using Common.Faults;
using Common.Models;

namespace Server.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class MotorService : IMotorService
    {
        private bool _sessionActive = false;
        private SessionMeta _currentSession = null;

        public ServiceResponse StartSession(SessionMeta meta)
        {
            if (meta == null || string.IsNullOrEmpty(meta.SessionId))
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("SessionMeta ne sme biti null", "SessionId"),
                    new FaultReason("Neispravan format sesije")
                );
            }

            _sessionActive = true;
            _currentSession = meta;

            Console.WriteLine($"[SERVER] Sesija pokrenuta: {meta.SessionId}");

            return new ServiceResponse
            {
                Status = StatusType.ACK,
                SessionStatus = SessionStatus.IN_PROGRESS,
                Message = $"Sesija {meta.SessionId} uspesno pokrenuta"
            };
        }

        public ServiceResponse PushSample(MotorSample sample)
        {
            if (!_sessionActive)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("Nema aktivne sesije", "Session", "Pozovi StartSession prvo"),
                    new FaultReason("Sesija nije aktivna")
                );
            }

            if (sample == null)
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("Sample ne sme biti null", "MotorSample"),
                    new FaultReason("Neispravan format podataka")
                );
            }

            Console.WriteLine($"[SERVER] Sample primljen -> I_q:{sample.I_q} I_d:{sample.I_d} Coolant:{sample.Coolant}");

            return new ServiceResponse
            {
                Status = StatusType.ACK,
                SessionStatus = SessionStatus.IN_PROGRESS,
                Message = "Sample uspesno primljen"
            };
        }

        public ServiceResponse EndSession()
        {
            if (!_sessionActive)
            {
                return new ServiceResponse
                {
                    Status = StatusType.NACK,
                    SessionStatus = SessionStatus.COMPLETED,
                    Message = "Nije bilo aktivne sesije"
                };
            }

            Console.WriteLine($"[SERVER] Sesija zavrsena: {_currentSession.SessionId}");

            _sessionActive = false;
            _currentSession = null;

            return new ServiceResponse
            {
                Status = StatusType.ACK,
                SessionStatus = SessionStatus.COMPLETED,
                Message = "Sesija uspesno zavrsena"
            };
        }
    }
}