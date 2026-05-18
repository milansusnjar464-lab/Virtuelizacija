using System;
using System.Configuration;
using System.ServiceModel;
using Common.Contracts;
using Common.Faults;
using Common.Models;
using Server.FileManagement;

namespace Server.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Single)]
    public class MotorService : IMotorService
    {
        private readonly object _syncRoot = new object();
        private readonly string _filePath;
        private bool _sessionActive;
        private SessionMeta _currentSession;
        private int _acceptedCount;
        private int _rejectedCount;
        private SessionFileManager _fileManager;

        public MotorService()
        {
            _filePath = ConfigurationManager.AppSettings["FilePath"] ?? "Files";
        }

        public ServiceResponse StartSession(SessionMeta meta)
        {
            Validator.ValidateSessionMeta(meta);

            lock (_syncRoot)
            {
                if (_sessionActive)
                {
                    throw new FaultException<ValidationFault>(
                        new ValidationFault(
                            "Sesija je vec aktivna. Prvo pozvati EndSession.",
                            "Session",
                            "Jedna aktivna sesija"
                        ),
                        new FaultReason("Sesija vec postoji")
                    );
                }

                _currentSession = meta;
                _acceptedCount = 0;
                _rejectedCount = 0;
                _fileManager = new SessionFileManager(_filePath, meta.SessionId);
                _sessionActive = true;

                Console.WriteLine($"[SERVER] Start sesije: {meta.SessionId}");
                Console.WriteLine($"[SERVER] Opis: {meta.Description}");

                return new ServiceResponse
                {
                    Status = StatusType.ACK,
                    SessionStatus = SessionStatus.IN_PROGRESS,
                    Message = $"Sesija {meta.SessionId} je pokrenuta."
                };
            }
        }

        public ServiceResponse PushSample(MotorSample sample)
        {
            lock (_syncRoot)
            {
                EnsureSessionActive();

                try
                {
                    Validator.ValidateMotorSample(sample);
                    _fileManager.WriteMeasurement(sample);
                    _acceptedCount++;

                    Console.WriteLine($"[SERVER] Primljen sample #{_acceptedCount}: I_q={sample.I_q}, I_d={sample.I_d}, Torque={sample.Torque}");

                    return new ServiceResponse
                    {
                        Status = StatusType.ACK,
                        SessionStatus = SessionStatus.IN_PROGRESS,
                        Message = $"Sample #{_acceptedCount} je sacuvan."
                    };
                }
                catch (FaultException<ValidationFault> ex)
                {
                    _rejectedCount++;
                    _fileManager.WriteReject(sample, ex.Detail.Message);
                    throw;
                }
                catch (FaultException<DataFormatFault> ex)
                {
                    _rejectedCount++;
                    _fileManager.WriteReject(sample, ex.Detail.Message);
                    throw;
                }
            }
        }

        public ServiceResponse EndSession()
        {
            lock (_syncRoot)
            {
                if (!_sessionActive)
                {
                    return new ServiceResponse
                    {
                        Status = StatusType.NACK,
                        SessionStatus = SessionStatus.COMPLETED,
                        Message = "Nema aktivne sesije."
                    };
                }

                string sessionId = _currentSession.SessionId;
                int accepted = _acceptedCount;
                int rejected = _rejectedCount;

                _fileManager.Dispose();
                _fileManager = null;
                _currentSession = null;
                _acceptedCount = 0;
                _rejectedCount = 0;
                _sessionActive = false;

                Console.WriteLine($"[SERVER] Kraj sesije: {sessionId}. Prihvaceno: {accepted}, odbijeno: {rejected}");

                return new ServiceResponse
                {
                    Status = StatusType.ACK,
                    SessionStatus = SessionStatus.COMPLETED,
                    Message = $"Sesija {sessionId} zavrsena. Prihvaceno: {accepted}, odbijeno: {rejected}."
                };
            }
        }

        private void EnsureSessionActive()
        {
            if (!_sessionActive)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault(
                        "Nema aktivne sesije. Prvo pozvati StartSession.",
                        "Session",
                        "StartSession pre PushSample"
                    ),
                    new FaultReason("Sesija nije aktivna")
                );
            }
        }
    }
}
