using System;
using System.ServiceModel;
using Common.Faults;

namespace Common.Models
{
    public static class Validator
    {
        public const double MinCurrent = -300.0;
        public const double MaxCurrent = 300.0;
        public const double MinCoolant = -10.0;
        public const double MaxCoolant = 110.0;
        public const double MinAmbient = -50.0;
        public const double MaxAmbient = 100.0;
        public const double MinTorque = -250.0;
        public const double MaxTorque = 250.0;

        public static void ValidateSessionMeta(SessionMeta meta)
        {
            if (meta == null)
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("SessionMeta objekat je null", "SessionMeta"),
                    new FaultReason("Neispravan format sesije")
                );
            }

            if (string.IsNullOrWhiteSpace(meta.SessionId))
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("SessionId ne sme biti prazan", "SessionId"),
                    new FaultReason("Nedostaje obavezno polje")
                );
            }

            if (!IsSafeFileToken(meta.SessionId))
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault(
                        "SessionId sme da sadrzi samo slova, cifre, '-', '_' i '.'",
                        "SessionId",
                        "A-Z, a-z, 0-9, '-', '_' i '.'"
                    ),
                    new FaultReason("Neispravna vrednost polja")
                );
            }

            if (meta.SessionId.Length > 50)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("SessionId je predugacak", "SessionId", "max 50 karaktera"),
                    new FaultReason("Neispravna vrednost polja")
                );
            }

            if (meta.Description != null && meta.Description.Length > 200)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("Description je predugacak", "Description", "max 200 karaktera"),
                    new FaultReason("Neispravna vrednost polja")
                );
            }
        }

        public static void ValidateMotorSample(MotorSample sample)
        {
            if (sample == null)
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("MotorSample objekat je null", "MotorSample"),
                    new FaultReason("Neispravan format podataka")
                );
            }

            ValidateFinite(sample.I_q, "I_q");
            ValidateFinite(sample.I_d, "I_d");
            ValidateFinite(sample.Coolant, "Coolant");
            ValidateFinite(sample.Ambient, "Ambient");
            ValidateFinite(sample.Torque, "Torque");

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

            ValidateRange(sample.I_q, "I_q", MinCurrent, MaxCurrent, "A");
            ValidateRange(sample.I_d, "I_d", MinCurrent, MaxCurrent, "A");
            ValidateRange(sample.Coolant, "Coolant", MinCoolant, MaxCoolant, "C");
            ValidateRange(sample.Ambient, "Ambient", MinAmbient, MaxAmbient, "C");
            ValidateRange(sample.Torque, "Torque", MinTorque, MaxTorque, "Nm");
        }

        private static bool IsSafeFileToken(string value)
        {
            foreach (char c in value)
            {
                if (!char.IsLetterOrDigit(c) && c != '-' && c != '_' && c != '.')
                {
                    return false;
                }
            }

            return true;
        }

        private static void ValidateFinite(double value, string field)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault($"{field} mora biti konacan broj", field),
                    new FaultReason("Neispravan format broja")
                );
            }
        }

        private static void ValidateRange(double value, string field, double min, double max, string unit)
        {
            if (value < min || value > max)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault(
                        $"{field} vrednost {value} nije u dozvoljenom opsegu",
                        field,
                        $"{min} do {max} {unit}"
                    ),
                    new FaultReason("Vrednost van dozvoljenog opsega")
                );
            }
        }
    }
}
