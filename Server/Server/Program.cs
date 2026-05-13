using System;
using System.ServiceModel;
using Server.Services;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            ServiceHost host = new ServiceHost(typeof(MotorService));

            try
            {
                host.Open();
                Console.WriteLine("=== PMSM Motor Monitoring Server ===");
                Console.WriteLine("Server pokrenut na: net.tcp://localhost:4000/MotorService");
                Console.WriteLine("Pritisnite ENTER za gasenje servera...");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greska pri pokretanju servera: {ex.Message}");
            }
            finally
            {
                host.Close();
                Console.WriteLine("Server ugasen.");
            }
        }
    }
}