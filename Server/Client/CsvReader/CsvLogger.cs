using System;
using System.IO;
using System.Text;

namespace Client.CsvReader
{
    public class CsvLogger : IDisposable
    {
        private bool _disposed;
        private StreamWriter _writer;

        public CsvLogger(string logPath)
        {
            string directory = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _writer = new StreamWriter(logPath, append: false, encoding: Encoding.UTF8);
            _writer.WriteLine("RowNumber,RawLine,Reason,Timestamp");
            _writer.Flush();
        }

        public void LogInvalidRow(int rowNumber, string rawLine, string reason)
        {
            CheckDisposed();

            _writer.WriteLine($"{rowNumber},\"{Escape(rawLine)}\",\"{Escape(reason)}\",{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            _writer.Flush();
        }

        public void LogExcessRow(int rowNumber, string rawLine)
        {
            LogInvalidRow(rowNumber, rawLine, "Red preskocen jer je ucitano maksimalnih 100 validnih uzoraka");
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty).Replace("\"", "\"\"");
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(CsvLogger));
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
            if (_disposed)
            {
                return;
            }

            if (disposing && _writer != null)
            {
                _writer.Dispose();
                _writer = null;
            }

            _disposed = true;
        }
    }
}
