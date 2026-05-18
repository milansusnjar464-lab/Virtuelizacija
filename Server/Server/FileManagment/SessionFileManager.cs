using System;
using System.Globalization;
using System.IO;
using System.Text;
using Common.Models;

namespace Server.FileManagement
{
    public class SessionFileManager : IDisposable
    {
        private bool _disposed;
        private FileStream _measurementsStream;
        private FileStream _rejectsStream;
        private StreamWriter _measurementsWriter;
        private StreamWriter _rejectsWriter;

        public string SessionFilePath { get; }
        public string RejectsFilePath { get; }

        public SessionFileManager(string directoryPath, string sessionId)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                ResourceTracker.LogOpen($"Direktorijum kreiran: {directoryPath}");
            }

            SessionFilePath = Path.Combine(directoryPath, $"measurements_{sessionId}.csv");
            RejectsFilePath = Path.Combine(directoryPath, $"rejects_{sessionId}.csv");

            _measurementsStream = new FileStream(SessionFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
            _rejectsStream = new FileStream(RejectsFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
            _measurementsWriter = new StreamWriter(_measurementsStream, Encoding.UTF8);
            _rejectsWriter = new StreamWriter(_rejectsStream, Encoding.UTF8);

            ResourceTracker.LogOpen($"FileStream -> {SessionFilePath}");
            ResourceTracker.LogOpen($"FileStream -> {RejectsFilePath}");
            WriteHeaders();
        }

        public void WriteMeasurement(MotorSample sample)
        {
            CheckDisposed();

            _measurementsWriter.WriteLine(ToCsvLine(sample));
            _measurementsWriter.Flush();
        }

        public void WriteReject(MotorSample sample, string reason)
        {
            CheckDisposed();

            string rawSample = sample == null ? string.Empty : ToCsvLine(sample);
            _rejectsWriter.WriteLine($"{rawSample},\"{Escape(reason)}\"");
            _rejectsWriter.Flush();
        }

        private void WriteHeaders()
        {
            _measurementsWriter.WriteLine("I_q,I_d,Coolant,Profile_Id,Ambient,Torque");
            _measurementsWriter.Flush();

            _rejectsWriter.WriteLine("I_q,I_d,Coolant,Profile_Id,Ambient,Torque,Reason");
            _rejectsWriter.Flush();
        }

        private static string ToCsvLine(MotorSample sample)
        {
            return string.Join(",",
                sample.I_q.ToString(CultureInfo.InvariantCulture),
                sample.I_d.ToString(CultureInfo.InvariantCulture),
                sample.Coolant.ToString(CultureInfo.InvariantCulture),
                sample.Profile_Id.ToString(CultureInfo.InvariantCulture),
                sample.Ambient.ToString(CultureInfo.InvariantCulture),
                sample.Torque.ToString(CultureInfo.InvariantCulture)
            );
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty).Replace("\"", "\"\"");
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SessionFileManager));
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~SessionFileManager()
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
                if (_measurementsWriter != null)
                {
                    _measurementsWriter.Dispose();
                    _measurementsWriter = null;
                    ResourceTracker.LogClose("StreamWriter measurements");
                }

                if (_rejectsWriter != null)
                {
                    _rejectsWriter.Dispose();
                    _rejectsWriter = null;
                    ResourceTracker.LogClose("StreamWriter rejects");
                }

                if (_measurementsStream != null)
                {
                    _measurementsStream.Dispose();
                    _measurementsStream = null;
                    ResourceTracker.LogClose("FileStream measurements");
                }

                if (_rejectsStream != null)
                {
                    _rejectsStream.Dispose();
                    _rejectsStream = null;
                    ResourceTracker.LogClose("FileStream rejects");
                }
            }

            _disposed = true;
        }
    }
}
