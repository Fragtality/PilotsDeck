using Serilog;
using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace PilotsDeck
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
        public static ConcurrentQueue<string> Messages { get; set; } = [];

        public static void CreateLogger()
        {
            LoggerConfiguration loggerConfiguration = new LoggerConfiguration().WriteTo.File(App.Configuration.LogFile, rollingInterval: RollingInterval.Day, retainedFileCountLimit: App.Configuration.LogCount,
                                                    outputTemplate: App.Configuration.LogTemplate);
            if (App.Configuration.LogLevel == LogLevel.Warning)
                loggerConfiguration.MinimumLevel.Warning();
            else if (App.Configuration.LogLevel == LogLevel.Debug)
                loggerConfiguration.MinimumLevel.Debug();
            else if (App.Configuration.LogLevel == LogLevel.Verbose)
                loggerConfiguration.MinimumLevel.Verbose();
            else
                loggerConfiguration.MinimumLevel.Information();
            Serilog.Log.Logger = loggerConfiguration.CreateLogger();
        }

        public static void WriteLog(LogLevel level, string message, string classFile = "", string classMethod = "")
        {
            if (level < (App.Configuration?.LogLevel ?? LogLevel.Verbose))
                return;

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
            if (level > LogLevel.Debug)
                Messages.Enqueue(message);
        }

        public static void Log(LogLevel level, string message, [CallerFilePath] string classFile = "", [CallerMemberName] string classMethod = "")
        {
            WriteLog(level, message, classFile, classMethod);
        }

        public static void Error(string message, [CallerFilePath] string classFile = "", [CallerMemberName] string classMethod = "")
        {
            WriteLog(LogLevel.Error, message, classFile, classMethod);
        }

        public static void Warning(string message, [CallerFilePath] string classFile = "", [CallerMemberName] string classMethod = "")
        {
            WriteLog(LogLevel.Warning, message, classFile, classMethod);
        }

        public static void Information(string message, [CallerFilePath] string classFile = "", [CallerMemberName] string classMethod = "")
        {
            WriteLog(LogLevel.Information, message, classFile, classMethod);
        }

        public static void Debug(string message, [CallerFilePath] string classFile = "", [CallerMemberName] string classMethod = "")
        {
            WriteLog(LogLevel.Debug, message, classFile, classMethod);
        }

        public static void Verbose(string message, [CallerFilePath] string classFile = "", [CallerMemberName] string classMethod = "")
        {
            WriteLog(LogLevel.Verbose, message, classFile, classMethod);
        }

        public static void WriteLogException(Exception ex, string message = "", string classFile = "", string classMethod = "")
        {
            string context = GetContext(classFile, classMethod);
            message = message.Replace("\n", "").Replace("\r", "");
            if (!string.IsNullOrEmpty(message))
                message = $"{message}: '{ex.GetType()}' - '{ex.Message}' - '{ex.TargetSite}' - {ex.StackTrace}";
            else
                message = $"Exception catched: '{ex.GetType()}' - '{ex.Message}' - '{ex.TargetSite}' - {ex.StackTrace}";

            Serilog.Log.Logger.ForContext("SourceContext", context).Error(message);
            Messages.Enqueue(message);
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
