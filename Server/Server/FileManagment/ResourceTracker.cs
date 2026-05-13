using System;

namespace Server.FileManagement
{
    // pomocna klasa koja biljezi kada se resursi otvaraju i zatvaraju
    // koristimo je da dokazemo da se Dispose poziva ispravno
    public static class ResourceTracker
    {
        public static void LogOpen(string resourceName)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[RESURS OTVOREN]  {resourceName} u {DateTime.Now:HH:mm:ss}");
            Console.ResetColor();
        }

        public static void LogClose(string resourceName)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[RESURS ZATVOREN] {resourceName} u {DateTime.Now:HH:mm:ss}");
            Console.ResetColor();
        }

        public static void LogError(string resourceName, string error)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[GRESKA]          {resourceName} -> {error}");
            Console.ResetColor();
        }
    }
}