using Serilog;
using System;
using System.Runtime.CompilerServices;

namespace ProfileManager
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
        //public static Queue MessageQueue = new();

        public static void CreateLogger()
        {
            LoggerConfiguration loggerConfiguration = new LoggerConfiguration().WriteTo.File("log/ProfileManager.log", rollingInterval: RollingInterval.Infinite, retainedFileCountLimit: 1, fileSizeLimitBytes: 1048576, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] [{SourceContext}] {Message} {NewLine}");
                loggerConfiguration.MinimumLevel.Debug();
            Serilog.Log.Logger = loggerConfiguration.CreateLogger();
        }

        public static void WriteLog(LogLevel level, string message, string classFile = "", string classMethod = "")
        {
            string context = GetContext(classFile, classMethod);
            message = message.Replace("\n", "").Replace("\r", "");
            switch (level)
            {
                case LogLevel.Critical:
                    Serilog.Log.Logger.ForContext("SourceContext", context).Fatal(message);
                    break;
                case LogLevel.Error:
                    Serilog.Log.Logger.ForContext("SourceContext", context).Error(message);
                    break;
                case LogLevel.Warning:
                    Serilog.Log.Logger.ForContext("SourceContext", context).Warning(message);
                    break;
                case LogLevel.Information:
                    Serilog.Log.Logger.ForContext("SourceContext", context).Information(message);
                    break;
                case LogLevel.Debug:
                    Serilog.Log.Logger.ForContext("SourceContext", context).Debug(message);
                    break;
                case LogLevel.Verbose:
                    Serilog.Log.Logger.ForContext("SourceContext", context).Verbose(message);
                    break;
                default:
                    Serilog.Log.Logger.ForContext("SourceContext", context).Debug(message);
                    break;
            }
            //if (level > LogLevel.Debug)
            //{
            //    if (message.Length > 128)
            //        MessageQueue.Enqueue(message[0..128]);
            //    else
            //        MessageQueue.Enqueue(message);
            //}
        }

        public static void Log(LogLevel level, string message, [CallerFilePath] string classFile = "", [CallerMemberName] string classMethod = "")
        {            
            WriteLog(level, message, classFile, classMethod);
        }
        
        public static void WriteLogException(Exception ex, string message = "", string classFile = "", string classMethod = "")
        {
            string context = GetContext(classFile, classMethod);
            message = message.Replace("\n", "").Replace("\r", "");
            if (!string.IsNullOrEmpty(message))
                Serilog.Log.Logger.ForContext("SourceContext", context).Error($"{message}: '{ex.GetType()}' - '{ex.Message}' - '{ex.TargetSite}' - {ex.StackTrace}");
            else
                Serilog.Log.Logger.ForContext("SourceContext", context).Error($"Exception catched: '{ex.GetType()}' - '{ex.Message}' - '{ex.TargetSite}' - {ex.StackTrace}");
        }

        public static void LogException(Exception ex, string message = "", [CallerFilePath] string classFile = "", [CallerMemberName] string classMethod = "")
        {
            WriteLogException(ex, message, classFile, classMethod);
        }

        private static string GetContext(string classFile, string classMethod)
        {
            string context = System.IO.Path.GetFileNameWithoutExtension(classFile) + ":" + classMethod;
            if (context.Length > 32)
                context = context[0..32];

            return string.Format(" {0,-32} ", context);
        }
    }
}
