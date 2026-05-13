using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Common.Models;

namespace Client.CsvReader
{
    public class MotorCsvReader : IDisposable
    {
        private bool _disposed = false;
        private StreamReader _reader = null;
        private CsvLogger _logger = null;

        // maksimalan broj redova koji ucitavamo
        private const int MAX_ROWS = 100;

        // putanja do CSV fajla
        private readonly string _csvPath;

        // kultura za parsiranje - tacka kao decimalni separator
        private static readonly CultureInfo _culture = CultureInfo.InvariantCulture;

        // rezultati citanja
        public List<MotorSample> ValidSamples { get; private set; }
        public int TotalRowsRead { get; private set; }
        public int ValidRowsCount { get; private set; }
        public int InvalidRowsCount { get; private set; }
        public int ExcessRowsCount { get; private set; }

        public MotorCsvReader(string csvPath, string logPath)
        {
            if (!File.Exists(csvPath))
            {
                throw new FileNotFoundException(
                    $"CSV fajl nije pronadjen na putanji: {csvPath}"
                );
            }

            _csvPath = csvPath;
            ValidSamples = new List<MotorSample>(MAX_ROWS);

            // otvori StreamReader sa UTF8 enkodingom
            _reader = new StreamReader(csvPath, Encoding.UTF8);
            Console.WriteLine($"[CSV] Otvoren fajl: {csvPath}");

            // otvori logger za nevalidne redove
            _logger = new CsvLogger(logPath);
        }

        public List<MotorSample> ReadSamples()
        {
            CheckDisposed();

            TotalRowsRead = 0;
            ValidRowsCount = 0;
            InvalidRowsCount = 0;
            ExcessRowsCount = 0;

            Console.WriteLine("\n[CSV] Pocetak ucitavanja...");
            Console.WriteLine("----------------------------------------");

            // preskoci zaglavlje (prvi red)
            string header = _reader.ReadLine();
            if (header == null)
            {
                Console.WriteLine("[CSV] Fajl je prazan!");
                return ValidSamples;
            }

            Console.WriteLine($"[CSV] Zaglavlje: {header}");

            // indeksi kolona iz zaglavlja
            var columnIndex = ParseHeader(header);
            if (columnIndex == null)
            {
                Console.WriteLine("[CSV] Zaglavlje nije ispravno!");
                return ValidSamples;
            }

            string line;
            int rowNumber = 1;

            // citaj red po red
            while ((line = _reader.ReadLine()) != null)
            {
                rowNumber++;
                TotalRowsRead++;

                // preskoci prazne redove
                if (string.IsNullOrWhiteSpace(line))
                {
                    _logger.LogInvalidRow(rowNumber, line, "Prazan red");
                    InvalidRowsCount++;
                    continue;
                }

                // ako smo ucitali 100 validnih, ostale loguj kao visak
                if (ValidRowsCount >= MAX_ROWS)
                {
                    _logger.LogExcessRow(rowNumber, line);
                    ExcessRowsCount++;
                    continue;
                }

                // pokusaj parsiranja reda
                MotorSample sample = TryParseLine(line, rowNumber, columnIndex);

                if (sample != null)
                {
                    ValidSamples.Add(sample);
                    ValidRowsCount++;
                }
                else
                {
                    InvalidRowsCount++;
                }
            }

            Console.WriteLine("----------------------------------------");
            Console.WriteLine($"[CSV] Ucitavanje zavrseno!");
            Console.WriteLine($"[CSV] Ukupno redova: {TotalRowsRead}");
            Console.WriteLine($"[CSV] Validnih:      {ValidRowsCount}");
            Console.WriteLine($"[CSV] Nevalidnih:    {InvalidRowsCount}");
            Console.WriteLine($"[CSV] Visak:         {ExcessRowsCount}");

            return ValidSamples;
        }

        // parsira zaglavlje i vraca recnik sa indeksima kolona
        private Dictionary<string, int> ParseHeader(string header)
        {
            var columns = new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase
            );

            string[] parts = header.Split(',');

            for (int i = 0; i < parts.Length; i++)
            {
                columns[parts[i].Trim()] = i;
            }

            // proveri da li postoje sve obavezne kolone
            string[] required = { "i_q", "i_d", "coolant", "profile_id", "ambient", "torque" };

            foreach (string col in required)
            {
                if (!columns.ContainsKey(col))
                {
                    Console.WriteLine($"[CSV] Nedostaje kolona: {col}");
                    return null;
                }
            }

            Console.WriteLine("[CSV] Zaglavlje validno, sve kolone pronadjene.");
            return columns;
        }

        // pokusava da parsira jedan red CSV-a
        private MotorSample TryParseLine(
            string line,
            int rowNumber,
            Dictionary<string, int> columnIndex)
        {
            try
            {
                string[] parts = line.Split(',');

                // proveri da li ima dovoljno kolona
                if (parts.Length < columnIndex.Count)
                {
                    _logger.LogInvalidRow(
                        rowNumber,
                        line,
                        $"Nedovoljan broj kolona: {parts.Length}"
                    );
                    return null;
                }

                // parsiranje sa InvariantCulture - tacka kao decimalni separator
                double iq = ParseDouble(parts[columnIndex["i_q"]], "i_q", rowNumber, line);
                double id = ParseDouble(parts[columnIndex["i_d"]], "i_d", rowNumber, line);
                double coolant = ParseDouble(parts[columnIndex["coolant"]], "coolant", rowNumber, line);
                double ambient = ParseDouble(parts[columnIndex["ambient"]], "ambient", rowNumber, line);
                double torque = ParseDouble(parts[columnIndex["torque"]], "torque", rowNumber, line);

                // profile_id je int
                string profileStr = parts[columnIndex["profile_id"]].Trim();
                if (!int.TryParse(profileStr, out int profileId))
                {
                    _logger.LogInvalidRow(
                        rowNumber,
                        line,
                        $"Neispravan tip za profile_id: '{profileStr}'"
                    );
                    return null;
                }

                // proveri da li je neka vrednost NaN (greska pri parsiranju)
                if (double.IsNaN(iq) || double.IsNaN(id) ||
                    double.IsNaN(coolant) || double.IsNaN(ambient) ||
                    double.IsNaN(torque))
                {
                    _logger.LogInvalidRow(
                        rowNumber,
                        line,
                        "Jedna ili vise vrednosti nisu validni brojevi"
                    );
                    return null;
                }

                return new MotorSample
                {
                    I_q = iq,
                    I_d = id,
                    Coolant = coolant,
                    Profile_Id = profileId,
                    Ambient = ambient,
                    Torque = torque
                };
            }
            catch (Exception ex)
            {
                _logger.LogInvalidRow(rowNumber, line, $"Greska parsiranja: {ex.Message}");
                return null;
            }
        }

        // parsira double vrednost sa InvariantCulture
        // vraca NaN ako parsiranje nije uspelo
        private double ParseDouble(string value, string fieldName, int rowNumber, string rawLine)
        {
            string trimmed = value.Trim();

            if (double.TryParse(trimmed, NumberStyles.Any, _culture, out double result))
            {
                return result;
            }

            _logger.LogInvalidRow(
                rowNumber,
                rawLine,
                $"Neispravan format broja u koloni '{fieldName}': '{trimmed}'"
            );

            // vracamo NaN kao signal greske
            return double.NaN;
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(
                    nameof(MotorCsvReader),
                    "MotorCsvReader je vec dispose-ovan!"
                );
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~MotorCsvReader()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_reader != null)
                    {
                        _reader.Close();
                        _reader.Dispose();
                        _reader = null;
                        Console.WriteLine("[CSV] StreamReader zatvoren.");
                    }

                    if (_logger != null)
                    {
                        _logger.Dispose();
                        _logger = null;
                    }
                }
                _disposed = true;
            }
        }
    }
}