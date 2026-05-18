using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel;
using Common.Contracts;
using Common.Faults;
using Common.Models;
using Client.CsvReader;

namespace Client
{
    class Program
    {
        static IMotorService _proxy = null;
        static ChannelFactory<IMotorService> _factory = null;



        static void TestDisposePattern()
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("   TEST DISPOSE PATTERN");
            Console.WriteLine("========================================");

            Console.WriteLine("\n--- Test 1: Normalan tok (using blok) ---");
            try
            {
                var meta = new SessionMeta { SessionId = "DISPOSE_TEST_01", Description = "Dispose test" };
                var response = _proxy.StartSession(meta);
                Console.WriteLine($"StartSession: {response.Status} | {response.Message}");

                // salji par validnih sample-ova
                for (int i = 1; i <= 3; i++)
                {
                    var sample = new MotorSample
                    {
                        I_q = 10.0 * i,
                        I_d = 5.0 * i,
                        Coolant = 25.0,
                        Ambient = 22.0,
                        Torque = 15.0,
                        Profile_Id = 1
                    };
                    var res = _proxy.PushSample(sample);
                    Console.WriteLine($"  Sample #{i}: {res.Status}");
                }

                // EndSession poziva Dispose interno
                var endResponse = _proxy.EndSession();
                Console.WriteLine($"EndSession: {endResponse.Status} | {endResponse.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greska: {ex.Message}");
            }

            Console.WriteLine("\n--- Test 2: Simulacija prekida (nevalidan sample) ---");
            try
            {
                var meta = new SessionMeta { SessionId = "DISPOSE_TEST_02", Description = "Prekid test" };
                _proxy.StartSession(meta);
                Console.WriteLine("Sesija pokrenuta, saljemo nevalidan sample...");

                // salji nevalidan sample - ovo ce baciti izuzetak
                var badSample = new MotorSample
                {
                    I_q = 999,      // NEISPRAVNO - van opsega
                    I_d = 5.0,
                    Coolant = -10,  // NEISPRAVNO - mora biti > 0
                    Ambient = 22.0,
                    Torque = 15.0,
                    Profile_Id = 1
                };
                _proxy.PushSample(badSample);
            }
            catch (FaultException<ValidationFault> ex)
            {
                Console.WriteLine($"Ocekivana greska: {ex.Detail.Message}");
                Console.WriteLine("Zatvaramo sesiju i resurse...");

                // cak i posle greske, EndSession zatvara resurse
                try
                {
                    var endResponse = _proxy.EndSession();
                    Console.WriteLine($"EndSession: {endResponse.Status} | {endResponse.Message}");
                    Console.WriteLine("Resursi uspesno zatvoreni!");
                }
                catch (Exception endEx)
                {
                    Console.WriteLine($"Greska pri zatvaranju: {endEx.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Neocekivana greska: {ex.Message}");
            }

            Console.WriteLine("\n--- Test 3: Pokusaj pisanja nakon Dispose ---");
            Console.WriteLine("(Ovo se testira interno na serveru)");
            Console.WriteLine("Nakon EndSession, FileStream i StreamWriter su zatvoreni.");
            Console.WriteLine("Svaki pokusaj pisanja bi bacio ObjectDisposedException.");
        }


        static void Main(string[] args)
        {
            Console.WriteLine("=== PMSM Motor Monitoring Client ===");

            try
            {
                // otvaranje kanala ka servisu
                _factory = new ChannelFactory<IMotorService>("MotorServiceEndpoint");
                _proxy = _factory.CreateChannel();

                Console.WriteLine("Konekcija sa serverom uspostavljena.");

                // testiranje validacije
                TestValidacija();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greska pri povezivanju: {ex.Message}");
            }
            finally
            {
                _factory?.Close();
            }

            Console.WriteLine("\nPritisnite ENTER za izlaz...");
            Console.ReadLine();

                Console.WriteLine("=== PMSM Motor Monitoring Client ===");

                try
                {
                    _factory = new ChannelFactory<IMotorService>("MotorServiceEndpoint");
                    _proxy = _factory.CreateChannel();
                    Console.WriteLine("Konekcija uspostavljena.");

                    TestValidacija();      // iz zadatka 3
                    TestDisposePattern();  // novi test
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Greska: {ex.Message}");
                }
                finally
                {
                    _factory?.Close();
                }

                Console.WriteLine("\nPritisnite ENTER za izlaz...");
                Console.ReadLine();
            }


        static List<MotorSample> _samples = null;

        static void UcitajCsv()
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("   UCITAVANJE CSV DATASETA");
            Console.WriteLine("========================================");

            // putanja relativna od bin/Debug foldera
            string csvPath = @"Dataset\measures_v2.csv";
            string logPath = @"Dataset\invalid_rows.log";

            if (!File.Exists(csvPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[GRESKA] CSV fajl nije pronadjen: {csvPath}");
                Console.WriteLine($"Apsolutna putanja: {Path.GetFullPath(csvPath)}");
                Console.ResetColor();
                return;
            }

            using (var csvReader = new MotorCsvReader(csvPath, logPath))
            {
                _samples = csvReader.ReadSamples();

                Console.WriteLine($"\n[CSV] Ucitano {_samples.Count} validnih sample-ova.");

                Console.WriteLine("\n--- Preview prvih 5 sample-ova ---");
                int preview = Math.Min(5, _samples.Count);
                for (int i = 0; i < preview; i++)
                {
                    var s = _samples[i];
                    Console.WriteLine(
                        $"  [{i + 1}] I_q={s.I_q:F4} | I_d={s.I_d:F4} | " +
                        $"Coolant={s.Coolant:F4} | Torque={s.Torque:F4}"
                    );
                }
            }
        }


        static void TestValidacija()
        {
            Console.WriteLine("\n--- Test 1: Ispravan StartSession ---");
            try
            {
                var meta = new SessionMeta
                {
                    SessionId = "SES_001",
                    Description = "Test sesija"
                };
                var response = _proxy.StartSession(meta);
                Console.WriteLine($"Status: {response.Status} | {response.Message}");
            }
            catch (FaultException<DataFormatFault> ex)
            {
                Console.WriteLine($"DataFormatFault: {ex.Detail.Message} | Polje: {ex.Detail.Field}");
            }
            catch (FaultException<ValidationFault> ex)
            {
                Console.WriteLine($"ValidationFault: {ex.Detail.Message} | Polje: {ex.Detail.Field}");
            }

            Console.WriteLine("\n--- Test 2: Ispravan PushSample ---");
            try
            {
                var sample = new MotorSample
                {
                    I_q = 10.5,
                    I_d = 5.2,
                    Coolant = 25.0,
                    Ambient = 22.0,
                    Torque = 15.0,
                    Profile_Id = 1
                };
                var response = _proxy.PushSample(sample);
                Console.WriteLine($"Status: {response.Status} | {response.Message}");
            }
            catch (FaultException<ValidationFault> ex)
            {
                Console.WriteLine($"ValidationFault: {ex.Detail.Message} | Polje: {ex.Detail.Field} | Opseg: {ex.Detail.ExpectedRange}");
            }

            Console.WriteLine("\n--- Test 3: Neispravan Coolant (= -5) ---");
            try
            {
                var sample = new MotorSample
                {
                    I_q = 10.5,
                    I_d = 5.2,
                    Coolant = -5,        // NEISPRAVNO
                    Ambient = 22.0,
                    Torque = 15.0,
                    Profile_Id = 1
                };
                var response = _proxy.PushSample(sample);
                Console.WriteLine($"Status: {response.Status} | {response.Message}");
            }
            catch (FaultException<ValidationFault> ex)
            {
                Console.WriteLine($"ValidationFault: {ex.Detail.Message} | Polje: {ex.Detail.Field} | Opseg: {ex.Detail.ExpectedRange}");
            }

            Console.WriteLine("\n--- Test 4: Neispravan I_q (= 999) ---");
            try
            {
                var sample = new MotorSample
                {
                    I_q = 999,           // NEISPRAVNO
                    I_d = 5.2,
                    Coolant = 25.0,
                    Ambient = 22.0,
                    Torque = 15.0,
                    Profile_Id = 1
                };
                var response = _proxy.PushSample(sample);
                Console.WriteLine($"Status: {response.Status} | {response.Message}");
            }
            catch (FaultException<ValidationFault> ex)
            {
                Console.WriteLine($"ValidationFault: {ex.Detail.Message} | Polje: {ex.Detail.Field} | Opseg: {ex.Detail.ExpectedRange}");
            }

            Console.WriteLine("\n--- Test 5: EndSession ---");
            try
            {
                var response = _proxy.EndSession();
                Console.WriteLine($"Status: {response.Status} | {response.Message}");
            }
            catch (FaultException<DataFormatFault> ex)
            {
                Console.WriteLine($"DataFormatFault: {ex.Detail.Message}");
            }
        }
    }
}