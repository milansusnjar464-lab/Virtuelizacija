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
        public const int MaxValidSamples = 100;

        private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;
        private readonly StreamReader _reader;
        private readonly CsvLogger _logger;
        private bool _disposed;

        public int TotalRowsRead { get; private set; }
        public int ValidRowsCount { get; private set; }
        public int InvalidRowsCount { get; private set; }
        public int ExcessRowsCount { get; private set; }

        public MotorCsvReader(string csvPath, string logPath)
        {
            if (!File.Exists(csvPath))
            {
                throw new FileNotFoundException($"CSV fajl nije pronadjen: {csvPath}", csvPath);
            }

            _reader = new StreamReader(csvPath, Encoding.UTF8);
            _logger = new CsvLogger(logPath);
        }

        public List<MotorSample> ReadSamples()
        {
            CheckDisposed();

            var samples = new List<MotorSample>(MaxValidSamples);
            string header = _reader.ReadLine();
            if (string.IsNullOrWhiteSpace(header))
            {
                return samples;
            }

            Dictionary<string, int> columns = ParseHeader(header);
            string line;
            int rowNumber = 1;

            while ((line = _reader.ReadLine()) != null)
            {
                rowNumber++;
                TotalRowsRead++;

                if (string.IsNullOrWhiteSpace(line))
                {
                    InvalidRowsCount++;
                    _logger.LogInvalidRow(rowNumber, line, "Prazan red");
                    continue;
                }

                if (samples.Count >= MaxValidSamples)
                {
                    ExcessRowsCount++;
                    _logger.LogExcessRow(rowNumber, line);
                    continue;
                }

                MotorSample sample;
                if (TryParseLine(line, rowNumber, columns, out sample))
                {
                    samples.Add(sample);
                    ValidRowsCount++;
                }
                else
                {
                    InvalidRowsCount++;
                }
            }

            return samples;
        }

        private Dictionary<string, int> ParseHeader(string header)
        {
            var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            string[] parts = SplitCsvLine(header);

            for (int i = 0; i < parts.Length; i++)
            {
                columns[parts[i].Trim()] = i;
            }

            string[] requiredColumns = { "i_q", "i_d", "coolant", "profile_id", "ambient", "torque" };
            foreach (string column in requiredColumns)
            {
                if (!columns.ContainsKey(column))
                {
                    throw new InvalidDataException($"CSV zaglavlje ne sadrzi obaveznu kolonu: {column}");
                }
            }

            return columns;
        }

        private bool TryParseLine(string line, int rowNumber, Dictionary<string, int> columns, out MotorSample sample)
        {
            sample = null;
            string[] values = SplitCsvLine(line);

            try
            {
                sample = new MotorSample
                {
                    I_q = ReadDouble(values, columns, "i_q"),
                    I_d = ReadDouble(values, columns, "i_d"),
                    Coolant = ReadDouble(values, columns, "coolant"),
                    Profile_Id = ReadInt(values, columns, "profile_id"),
                    Ambient = ReadDouble(values, columns, "ambient"),
                    Torque = ReadDouble(values, columns, "torque")
                };

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogInvalidRow(rowNumber, line, ex.Message);
                return false;
            }
        }

        private static double ReadDouble(string[] values, Dictionary<string, int> columns, string column)
        {
            string raw = ReadRaw(values, columns, column);
            double parsed;
            if (!double.TryParse(raw, NumberStyles.Float, Culture, out parsed))
            {
                throw new FormatException($"Kolona {column} nije validan decimalni broj: '{raw}'");
            }

            return parsed;
        }

        private static int ReadInt(string[] values, Dictionary<string, int> columns, string column)
        {
            string raw = ReadRaw(values, columns, column);
            int parsed;
            if (!int.TryParse(raw, NumberStyles.Integer, Culture, out parsed))
            {
                throw new FormatException($"Kolona {column} nije validan ceo broj: '{raw}'");
            }

            return parsed;
        }

        private static string ReadRaw(string[] values, Dictionary<string, int> columns, string column)
        {
            int index = columns[column];
            if (index >= values.Length)
            {
                throw new InvalidDataException($"Red nema vrednost za kolonu {column}");
            }

            return values[index].Trim();
        }

        private static string[] SplitCsvLine(string line)
        {
            var values = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            values.Add(current.ToString());
            return values.ToArray();
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(MotorCsvReader));
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
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _reader.Dispose();
                _logger.Dispose();
            }

            _disposed = true;
        }
    }
}
