using System;
using System.IO;
using System.Text;

namespace Client.CsvReader
{
    public class CsvLogger : IDisposable
    {
        private bool _disposed = false;
        private StreamWriter _writer = null;
        private readonly string _logPath;

        public CsvLogger(string logPath)
        {
            _logPath = logPath;

            // kreiraj direktorijum ako ne postoji
            string dir = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // otvori StreamWriter za logovanje
            _writer = new StreamWriter(logPath, append: false, encoding: Encoding.UTF8);
            _writer.WriteLine("RowNumber,RawLine,Reason,Timestamp");
            _writer.Flush();

            Console.WriteLine($"[LOG] Log fajl kreiran: {logPath}");
        }

        public void LogInvalidRow(int rowNumber, string rawLine, string reason)
        {
            CheckDisposed();

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            _writer.WriteLine($"{rowNumber},\"{rawLine}\",\"{reason}\",{timestamp}");
            _writer.Flush();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[LOG] Red #{rowNumber} nevalidan: {reason}");
            Console.ResetColor();
        }

        public void LogExcessRow(int rowNumber, string rawLine)
        {
            CheckDisposed();

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            _writer.WriteLine($"{rowNumber},\"{rawLine}\",\"Red viska - preko 100\",{timestamp}");
            _writer.Flush();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[LOG] Red #{rowNumber} preskocen (visak)");
            Console.ResetColor();
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(
                    nameof(CsvLogger),
                    "CsvLogger je vec dispose-ovan!"
                );
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~CsvLogger()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_writer != null)
                    {
                        _writer.Flush();
                        _writer.Close();
                        _writer.Dispose();
                        _writer = null;
                        Console.WriteLine($"[LOG] Log fajl zatvoren: {_logPath}");
                    }
                }
                _disposed = true;
            }
        }
    }
}