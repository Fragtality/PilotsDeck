using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Installer
{
    public enum LogLevel
    {
        Critical = 5,
        Error = 4,
        Warning = 3,
        Information = 2,
        Debug = 1,
        Verbose = 0,
    }

    public static class Logger
    {
        private static readonly string _filename = "PilotsDeck-Installer.log";
        private static readonly string _format = "{0:yyyy-MM-dd HH:mm:ss} [{1}] [ {2} ] {3}\r\n";
        private static LogLevel highestLevel = LogLevel.Verbose;

        public static void CreateLogger()
        {
            if (File.Exists(_filename))
            {
                if ((new FileInfo(_filename)).Length != 0)
                {
                    if (File.Exists(_filename + ".old"))
                        File.Delete(_filename + ".old");
                    File.Move(_filename, _filename + ".old");
                }
                else
                    File.Delete(_filename);
            }

            var stream = File.Create(_filename);
            stream.Close();
            stream.Dispose();
        }

        public static void DestroyLogger()
        {
            if (!App.DebugMode && File.Exists(_filename) && highestLevel <= LogLevel.Information)
                File.Delete(_filename);
        }

        private static void WriteFile(string message)
        {
            File.AppendAllText(_filename, message);
        }

        public static string GetLevelString(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Critical:
                    return "FTL";
                case LogLevel.Error:
                    return "ERR";
                case LogLevel.Warning:
                    return "WRN";
                case LogLevel.Information:
                    return "INF";
                case LogLevel.Debug:
                    return "DBG";
                case LogLevel.Verbose:
                    return "VRB";
                default:
                    return "DBG";
            }
        }

        public static void WriteLog(LogLevel level, string message, string classFile, string classMethod)
        {
            WriteFile(string.Format(_format, DateTime.Now, GetLevelString(level), GetContext(classFile, classMethod), message));
            if (level > highestLevel)
                highestLevel = level;
        }

        public static void Log(LogLevel level, string message, [CallerFilePath] string classFile = "", [CallerMemberName] string classMethod = "")
        {            
            WriteLog(level, message, classFile, classMethod);
        }
        
        public static void WriteLogException(Exception ex, string message = "", string classFile = "", string classMethod = "")
        {
            if (!string.IsNullOrEmpty(message))
                message = $"{message}: '{ex.GetType()}' - '{ex.Message}' - '{ex.TargetSite}' - {ex.StackTrace}";
            else
                message = $"Exception catched: '{ex.GetType()}' - '{ex.Message}' - '{ex.TargetSite}' - {ex.StackTrace}";

            WriteLog(LogLevel.Critical, message, classFile, classMethod);
        }

        public static void LogException(Exception ex, string message = "", [CallerFilePath] string classFile = "", [CallerMemberName] string classMethod = "")
        {
            WriteLogException(ex, message, classFile, classMethod);
        }

        private static string GetContext(string classFile, string classMethod)
        {
            string context = Path.GetFileNameWithoutExtension(classFile) + ":" + classMethod;
            if (context.Length > 32)
                context = context.Substring(0, 32);

            return string.Format("{0,-32}", context);
        }
    }
}
