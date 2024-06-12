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
        private static StreamWriter _stream;
        private static readonly string _filename = "PilotsDeck-Installer.log";
        private static readonly string _format = "{0:yyyy-MM-dd HH:mm:ss} [{1}] [ {2} ] {3}\r\n";

        public static void CreateLogger()
        {
            if (File.Exists(_filename))
                File.Delete(_filename);

            _stream = new StreamWriter(File.OpenWrite(_filename));
        }

        public static void DestroyLogger()
        {
            _stream.Flush();
            _stream.Close();
            _stream.Dispose();

            if (File.Exists(_filename) && (new FileInfo(_filename)).Length == 0)
                File.Delete(_filename);
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
            _stream.Write(string.Format(_format, DateTime.Now, GetLevelString(level), GetContext(classFile, classMethod), message));
            _stream.Flush();
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
