using System;
using System.ServiceModel;
using Server.Services;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            ServiceHost host = null;

            try
            {
                host = new ServiceHost(typeof(MotorService));
                host.Open();
                Console.WriteLine("=== Elektricni motor - WCF server ===");
                Console.WriteLine("Server pokrenut na: net.tcp://localhost:4000/MotorService");
                Console.WriteLine("Pritisnite ENTER za gasenje servera...");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greska pri pokretanju servera: {ex.Message}");
                host?.Abort();
            }
            finally
            {
                if (host != null)
                {
                    if (host.State == CommunicationState.Faulted)
                    {
                        host.Abort();
                    }
                    else
                    {
                        host.Close();
                    }
                }

                Console.WriteLine("Server ugasen.");
            }
        }
    }
}
