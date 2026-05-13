using System;
using System.ServiceModel;
using Common.Contracts;
using Common.Faults;
using Common.Models;

namespace Client
{
    class Program
    {
        static IMotorService _proxy = null;
        static ChannelFactory<IMotorService> _factory = null;

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