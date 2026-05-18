using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel;
using Client.CsvReader;
using Common.Contracts;
using Common.Faults;
using Common.Models;

namespace Client
{
    class Program
    {
        private const string SessionId = "ELECTRIC_MOTOR_001";

        static void Main(string[] args)
        {
            Console.WriteLine("=== Elektricni motor - WCF klijent ===");

            List<MotorSample> samples = LoadSamples();
            if (samples.Count == 0)
            {
                Console.WriteLine("Nema validnih uzoraka za slanje.");
                WaitForExit();
                return;
            }

            ChannelFactory<IMotorService> factory = null;
            IMotorService proxy = null;

            try
            {
                factory = new ChannelFactory<IMotorService>("MotorServiceEndpoint");
                proxy = factory.CreateChannel();

                RunSession(proxy, samples);
                CloseProxy(proxy);
                factory.Close();
            }
            catch (EndpointNotFoundException)
            {
                Console.WriteLine("Server nije pokrenut. Prvo startovati Server projekat, pa zatim Client.");
                AbortProxy(proxy);
                factory?.Abort();
            }
            catch (CommunicationException ex)
            {
                Console.WriteLine($"WCF komunikaciona greska: {ex.Message}");
                AbortProxy(proxy);
                factory?.Abort();
            }
            catch (TimeoutException ex)
            {
                Console.WriteLine($"WCF timeout: {ex.Message}");
                AbortProxy(proxy);
                factory?.Abort();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greska: {ex.Message}");
                AbortProxy(proxy);
                factory?.Abort();
            }

            WaitForExit();
        }

        private static List<MotorSample> LoadSamples()
        {
            string csvPath = FindCsvFile();
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Dataset", "invalid_rows.log");

            Console.WriteLine();
            Console.WriteLine("CSV ulaz:");
            Console.WriteLine(csvPath == null ? "  CSV fajl nije pronadjen." : $"  {Path.GetFullPath(csvPath)}");

            if (csvPath == null)
            {
                return new List<MotorSample>();
            }

            using (var reader = new MotorCsvReader(csvPath, logPath))
            {
                List<MotorSample> samples = reader.ReadSamples();

                Console.WriteLine($"Ucitano validnih uzoraka: {reader.ValidRowsCount}");
                Console.WriteLine($"Nevalidnih redova:        {reader.InvalidRowsCount}");
                Console.WriteLine($"Preskocen visak redova:   {reader.ExcessRowsCount}");
                Console.WriteLine($"Log nevalidnih redova:    {Path.GetFullPath(logPath)}");

                return samples;
            }
        }

        private static string FindCsvFile()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string[] candidates =
            {
                Path.Combine("Dataset", "measures_v2.csv"),
                Path.Combine("Dataset", "measures.csv"),
                Path.Combine("Dataset", "measures_demo.csv"),
                Path.Combine(baseDirectory, "Dataset", "measures_v2.csv"),
                Path.Combine(baseDirectory, "Dataset", "measures.csv"),
                Path.Combine(baseDirectory, "Dataset", "measures_demo.csv"),
                Path.Combine("..", "..", "Dataset", "measures_v2.csv"),
                Path.Combine("..", "..", "Dataset", "measures.csv"),
                Path.Combine("..", "..", "Dataset", "measures_demo.csv")
            };

            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static void RunSession(IMotorService proxy, List<MotorSample> samples)
        {
            Console.WriteLine();
            Console.WriteLine("Pokretanje sesije i slanje uzoraka...");

            var meta = new SessionMeta
            {
                SessionId = SessionId,
                Description = "Elektricni motor - slanje CSV merenja"
            };

            ServiceResponse start = proxy.StartSession(meta);
            Console.WriteLine($"StartSession: {start.Status} | {start.Message}");

            int accepted = 0;
            int rejected = 0;

            foreach (MotorSample sample in samples)
            {
                try
                {
                    proxy.PushSample(sample);
                    accepted++;
                }
                catch (FaultException<ValidationFault> ex)
                {
                    rejected++;
                    Console.WriteLine($"Odbijen uzorak ({ex.Detail.Field}): {ex.Detail.Message}");
                }
                catch (FaultException<DataFormatFault> ex)
                {
                    rejected++;
                    Console.WriteLine($"Neispravan format ({ex.Detail.Field}): {ex.Detail.Message}");
                }
            }

            ServiceResponse end = proxy.EndSession();
            Console.WriteLine($"EndSession: {end.Status} | {end.Message}");
            Console.WriteLine($"Rezime klijenta: prihvaceno {accepted}, odbijeno {rejected}.");
        }

        private static void CloseProxy(IMotorService proxy)
        {
            var channel = proxy as IClientChannel;
            channel?.Close();
        }

        private static void AbortProxy(IMotorService proxy)
        {
            var channel = proxy as IClientChannel;
            channel?.Abort();
        }

        private static void WaitForExit()
        {
            Console.WriteLine();
            Console.WriteLine("Pritisnite ENTER za izlaz...");
            Console.ReadLine();
        }
    }
}
