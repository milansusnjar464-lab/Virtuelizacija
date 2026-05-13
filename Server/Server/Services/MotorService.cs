using System;
using System.Configuration;
using System.ServiceModel;
using Common.Contracts;
using Common.Faults;
using Common.Models;
using Server.FileManagement;

namespace Server.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class MotorService : IMotorService
    {
        private bool _sessionActive = false;
        private SessionMeta _currentSession = null;
        private int _sampleCount = 0;
        private SessionFileManager _fileManager = null;
        private readonly string _filePath;

        public MotorService()
        {
            _filePath = ConfigurationManager.AppSettings["FilePath"] ?? "Files";
        }

        public ServiceResponse StartSession(SessionMeta meta)
        {
            Validator.ValidateSessionMeta(meta);

            if (_sessionActive)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault(
                        "Sesija je vec aktivna, pozovi EndSession prvo",
                        "Session",
                        "Jedna sesija u isto vreme"
                    ),
                    new FaultReason("Sesija vec postoji")
                );
            }

            _sessionActive = true;
            _currentSession = meta;
            _sampleCount = 0;

            _fileManager = new SessionFileManager(_filePath, meta.SessionId);
            _fileManager.WriteHeader();

            Console.WriteLine($"[SERVER] Sesija pokrenuta: {meta.SessionId}");
            Console.WriteLine($"[SERVER] Opis: {meta.Description}");

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
                    new ValidationFault(
                        "Nema aktivne sesije, pozovi StartSession prvo",
                        "Session",
                        "Sesija mora biti aktivna"
                    ),
                    new FaultReason("Sesija nije aktivna")
                );
            }

            try
            {
                Validator.ValidateMotorSample(sample);
            }
            catch (FaultException<ValidationFault> ex)
            {
                string rawLine = $"{sample.I_q},{sample.I_d},{sample.Coolant}," +
                                 $"{sample.Profile_Id},{sample.Ambient},{sample.Torque}";
                _fileManager.WriteReject(rawLine, ex.Detail.Message);
                throw;
            }

            string line = $"{sample.I_q},{sample.I_d},{sample.Coolant}," +
                          $"{sample.Profile_Id},{sample.Ambient},{sample.Torque}";
            _fileManager.WriteLine(line);

            _sampleCount++;

            Console.WriteLine($"[SERVER] Sample #{_sampleCount} primljen:");
            Console.WriteLine($"         I_q={sample.I_q} | I_d={sample.I_d} | Coolant={sample.Coolant}");
            Console.WriteLine($"         Ambient={sample.Ambient} | Torque={sample.Torque} | Profile_Id={sample.Profile_Id}");

            return new ServiceResponse
            {
                Status = StatusType.ACK,
                SessionStatus = SessionStatus.IN_PROGRESS,
                Message = $"Sample #{_sampleCount} uspesno primljen"
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

            string sessionId = _currentSession.SessionId;
            int count = _sampleCount;

            _fileManager?.Dispose();
            _fileManager = null;

            _sessionActive = false;
            _currentSession = null;
            _sampleCount = 0;

            Console.WriteLine($"[SERVER] Sesija zavrsena: {sessionId}");
            Console.WriteLine($"[SERVER] Ukupno primljenih sample-ova: {count}");

            return new ServiceResponse
            {
                Status = StatusType.ACK,
                SessionStatus = SessionStatus.COMPLETED,
                Message = $"Sesija uspesno zavrsena. Primljeno {count} sample-ova."
            };
        }
    }
}