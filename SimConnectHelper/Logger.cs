using System;
using System.IO;

namespace SimConnectHelper
{
    public static class Logger
    {
        public const string LogFile = "SimConnectHelper.log";
        public static bool WriteFile { get; set; } = false;

        public static void Write(string message)
        {
            Console.WriteLine(message);
            if (WriteFile)
                File.AppendAllText(LogFile, $"{message}\r\n");
        }

        public static void WriteException(Exception ex)
        {
            Console.WriteLine($"Exception catched: '{ex.GetType()}' - '{ex.Message}' - '{ex.TargetSite}' - {ex.StackTrace}");
            if (WriteFile)
                File.AppendAllText(LogFile, $"Exception catched: '{ex.GetType()}' - '{ex.Message}' - '{ex.TargetSite}' - {ex.StackTrace}\r\n");
        }
    }
}
