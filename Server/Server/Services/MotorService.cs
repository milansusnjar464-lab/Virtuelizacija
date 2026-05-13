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
        private int _sampleCount = 0;

        public ServiceResponse StartSession(SessionMeta meta)
        {
            try
            {
                // validacija ulaznih podataka
                Validator.ValidateSessionMeta(meta);

                // provera da li vec postoji aktivna sesija
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

                Console.WriteLine($"[SERVER] Sesija pokrenuta: {meta.SessionId}");
                Console.WriteLine($"[SERVER] Opis: {meta.Description}");

                return new ServiceResponse
                {
                    Status = StatusType.ACK,
                    SessionStatus = SessionStatus.IN_PROGRESS,
                    Message = $"Sesija {meta.SessionId} uspesno pokrenuta"
                };
            }
            catch (FaultException)
            {
                // FaultException-i se prosledjuju klijentu direktno
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SERVER] Greska u StartSession: {ex.Message}");
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault($"Neocekivana greska: {ex.Message}", "StartSession"),
                    new FaultReason("Interna greska servera")
                );
            }
        }

        public ServiceResponse PushSample(MotorSample sample)
        {
            try
            {
                // provera da li postoji aktivna sesija
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

                // validacija sample podataka
                Validator.ValidateMotorSample(sample);

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
            catch (FaultException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SERVER] Greska u PushSample: {ex.Message}");
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault($"Neocekivana greska: {ex.Message}", "PushSample"),
                    new FaultReason("Interna greska servera")
                );
            }
        }

        public ServiceResponse EndSession()
        {
            try
            {
                // provera da li postoji aktivna sesija
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
                Console.WriteLine($"[SERVER] Ukupno primljenih sample-ova: {_sampleCount}");

                _sessionActive = false;
                _currentSession = null;
                _sampleCount = 0;

                return new ServiceResponse
                {
                    Status = StatusType.ACK,
                    SessionStatus = SessionStatus.COMPLETED,
                    Message = $"Sesija uspesno zavrsena. Primljeno {_sampleCount} sample-ova."
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SERVER] Greska u EndSession: {ex.Message}");
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault($"Neocekivana greska: {ex.Message}", "EndSession"),
                    new FaultReason("Interna greska servera")
                );
            }
        }
    }
}