using System;
using System.ServiceModel;
using Common.Faults;

namespace Common.Models
{
    public static class Validator
    {
        public static void ValidateSessionMeta(SessionMeta meta)
        {
            // provera da li je objekat null
            if (meta == null)
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("SessionMeta objekat je null", "SessionMeta"),
                    new FaultReason("Neispravan format sesije")
                );
            }

            // provera da li SessionId postoji
            if (string.IsNullOrWhiteSpace(meta.SessionId))
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("SessionId ne sme biti prazan", "SessionId"),
                    new FaultReason("Nedostaje obavezno polje")
                );
            }

            // SessionId ne sme biti duzi od 50 karaktera
            if (meta.SessionId.Length > 50)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("SessionId je predugacak", "SessionId", "max 50 karaktera"),
                    new FaultReason("Neispravna vrednost polja")
                );
            }
        }

        public static void ValidateMotorSample(MotorSample sample)
        {
            // provera da li je objekat null
            if (sample == null)
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("MotorSample objekat je null", "MotorSample"),
                    new FaultReason("Neispravan format podataka")
                );
            }

            // Coolant mora biti pozitivan (temperatura rashladne tecnosti)
            if (sample.Coolant <= 0)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault(
                        $"Coolant vrednost {sample.Coolant} nije dozvoljena",
                        "Coolant",
                        "Coolant > 0"
                    ),
                    new FaultReason("Vrednost van dozvoljenog opsega")
                );
            }

            // Ambient temperatura mora biti realna vrednost (izmedju -50 i 100 stepeni)
            if (sample.Ambient < -50 || sample.Ambient > 100)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault(
                        $"Ambient vrednost {sample.Ambient} nije u dozvoljenom opsegu",
                        "Ambient",
                        "-50 do 100"
                    ),
                    new FaultReason("Vrednost van dozvoljenog opsega")
                );
            }

            // Profile_Id mora biti pozitivan broj
            if (sample.Profile_Id <= 0)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault(
                        $"Profile_Id vrednost {sample.Profile_Id} nije dozvoljena",
                        "Profile_Id",
                        "Profile_Id > 0"
                    ),
                    new FaultReason("Vrednost van dozvoljenog opsega")
                );
            }

            // I_q mora biti u realnom opsegu struje motora
            if (sample.I_q < -300 || sample.I_q > 300)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault(
                        $"I_q vrednost {sample.I_q} nije u dozvoljenom opsegu",
                        "I_q",
                        "-300 do 300 A"
                    ),
                    new FaultReason("Vrednost van dozvoljenog opsega")
                );
            }

            // I_d mora biti u realnom opsegu struje motora
            if (sample.I_d < -300 || sample.I_d > 300)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault(
                        $"I_d vrednost {sample.I_d} nije u dozvoljenom opsegu",
                        "I_d",
                        "-300 do 300 A"
                    ),
                    new FaultReason("Vrednost van dozvoljenog opsega")
                );
            }

            // Torque mora biti u realnom opsegu momenta motora
            if (sample.Torque < -200 || sample.Torque > 200)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault(
                        $"Torque vrednost {sample.Torque} nije u dozvoljenom opsegu",
                        "Torque",
                        "-200 do 200 Nm"
                    ),
                    new FaultReason("Vrednost van dozvoljenog opsega")
                );
            }
        }
    }
}