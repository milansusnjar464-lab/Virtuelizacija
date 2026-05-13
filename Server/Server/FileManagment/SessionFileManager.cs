using System;
using System.IO;
using System.Text;

namespace Server.FileManagement
{
    public class SessionFileManager : IDisposable
    {
        // flag da znamo da li je Dispose vec pozvan
        private bool _disposed = false;

        // resursi koje moramo da zatvorimo
        private FileStream _fileStream = null;
        private StreamWriter _writer = null;
        private StreamReader _reader = null;

        // putanja do fajla
        private readonly string _sessionFilePath;
        private readonly string _rejectsFilePath;
        private readonly string _directoryPath;

        public string SessionFilePath => _sessionFilePath;
        public string RejectsFilePath => _rejectsFilePath;

        public SessionFileManager(string directoryPath, string sessionId)
        {
            _directoryPath = directoryPath;
            _sessionFilePath = Path.Combine(directoryPath, $"measurements_{sessionId}.csv");
            _rejectsFilePath = Path.Combine(directoryPath, $"rejects_{sessionId}.csv");

            // kreiraj direktorijum ako ne postoji
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                ResourceTracker.LogOpen($"Direktorijum kreiran: {directoryPath}");
            }

            // otvori FileStream i StreamWriter za session fajl
            OpenWriter();
        }

        // otvara StreamWriter za pisanje u session fajl
        private void OpenWriter()
        {
            // FileStream sa append modom - dodaje na kraj fajla
            _fileStream = new FileStream(
                _sessionFilePath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.Read
            );

            _writer = new StreamWriter(_fileStream, Encoding.UTF8);

            ResourceTracker.LogOpen($"FileStream -> {_sessionFilePath}");
            ResourceTracker.LogOpen($"StreamWriter -> {_sessionFilePath}");
        }

        // upisuje zaglavlje CSV fajla
        public void WriteHeader()
        {
            CheckDisposed();

            // proveri da li je fajl prazan pa tek onda pisi zaglavlje
            if (new FileInfo(_sessionFilePath).Length == 0)
            {
                _writer.WriteLine("I_q,I_d,Coolant,Profile_Id,Ambient,Torque");
                _writer.Flush();
                Console.WriteLine("[FILE] Zaglavlje upisano u session fajl");
            }
        }

        // upisuje jedan red podataka u session fajl
        public void WriteLine(string line)
        {
            CheckDisposed();

            _writer.WriteLine(line);
            _writer.Flush();
        }

        // upisuje odbaceni red u rejects fajl
        public void WriteReject(string line, string reason)
        {
            CheckDisposed();

            // za rejects koristimo File.AppendAllText
            // jer se rejects pisu retko
            string rejectLine = $"{line},REASON:{reason}";

            // ako rejects fajl ne postoji, dodaj zaglavlje
            if (!File.Exists(_rejectsFilePath))
            {
                File.AppendAllText(_rejectsFilePath,
                    "I_q,I_d,Coolant,Profile_Id,Ambient,Torque,Reason\n",
                    Encoding.UTF8);
            }

            File.AppendAllText(_rejectsFilePath, rejectLine + "\n", Encoding.UTF8);
            Console.WriteLine($"[REJECT] Red odbacen: {reason}");
        }

        // cita ceo session fajl i vraca sadrzaj
        public string ReadSessionFile()
        {
            CheckDisposed();

            // flush writer pre citanja
            _writer?.Flush();

            // otvori reader za citanje
            _reader = new StreamReader(_sessionFilePath, Encoding.UTF8);
            ResourceTracker.LogOpen($"StreamReader -> {_sessionFilePath}");

            string content = _reader.ReadToEnd();

            // zatvori reader odmah nakon citanja
            _reader.Close();
            _reader.Dispose();
            _reader = null;
            ResourceTracker.LogClose($"StreamReader -> {_sessionFilePath}");

            return content;
        }

        // proverava da li je Dispose vec pozvan
        // ako jeste, baca ObjectDisposedException
        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(
                    nameof(SessionFileManager),
                    "SessionFileManager je vec dispose-ovan, resursi su zatvoreni!"
                );
            }
        }

        // ============================================
        // DISPOSE PATTERN - standardna implementacija
        // ============================================

        // javni Dispose - poziva ga korisnik ili using blok
        public void Dispose()
        {
            Dispose(true);
            // kazemo GC-u da ne treba da poziva Finalize
            // jer smo vec ocistili resurse
            GC.SuppressFinalize(this);
        }

        // destruktor - poziva ga GC ako korisnik zaboravi Dispose
        // ovo je sigurnosna mreza
        ~SessionFileManager()
        {
            Dispose(false);
            ResourceTracker.LogError(
                "SessionFileManager",
                "Destruktor pozvan! Dispose nije pozvan rucno - curenje resursa!"
            );
        }

        // centralna metoda za ciscenje resursa
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // cisti managed resurse (StreamWriter, FileStream, StreamReader)
                    if (_writer != null)
                    {
                        _writer.Flush();
                        _writer.Close();
                        _writer.Dispose();
                        _writer = null;
                        ResourceTracker.LogClose("StreamWriter");
                    }

                    if (_fileStream != null)
                    {
                        _fileStream.Close();
                        _fileStream.Dispose();
                        _fileStream = null;
                        ResourceTracker.LogClose("FileStream");
                    }

                    if (_reader != null)
                    {
                        _reader.Close();
                        _reader.Dispose();
                        _reader = null;
                        ResourceTracker.LogClose("StreamReader");
                    }
                }

                // oznaci da je Dispose pozvan
                _disposed = true;
            }
        }
    }
}