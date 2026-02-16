using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.Installer.LibWorker;
using CFIT.Installer.Tasks;
using System;
using System.Threading.Tasks;

namespace Installer.Worker
{
    public class WorkerStreamDockStartStop : TaskWorker<Config>
    {
        public virtual DeckProcessOperation Operation { get; set; }
        public virtual int StartStopDelay { get; set; } = 1;
        public virtual bool IgnorePluginRunning { get; set; } = false;
        public virtual bool RefocusWindow { get; set; } = false;
        public virtual string RefocusWindowTitle { get; set; } = string.Empty;

        public WorkerStreamDockStartStop(Config config, DeckProcessOperation operation) : base(config, "StreamDock", "Processing StreamDock ...")
        {
            Model.DisplayCompleted = false;
            Model.DisplayInSummary = false;
            Operation = operation;

            // Set the correct title and message based on operation
            if (operation == DeckProcessOperation.STOP || operation == DeckProcessOperation.KILL)
            {
                Model.Title = "StreamDock";
                Model.Message = "Stopping StreamDock ...";
            }
            else if (operation == DeckProcessOperation.START)
            {
                Model.Title = "StreamDock";
                Model.Message = "Starting StreamDock ...";
            }
        }

        protected override async Task<bool> DoRun()
        {
            await Task.Delay(StartStopDelay * 1000);

            try
            {
                if (Operation == DeckProcessOperation.STOP || Operation == DeckProcessOperation.KILL)
                {
                    Logger.Debug("Stopping PilotsDeck Plugin for StreamDock");
                    Model.Message = "Stopping StreamDock ...";

                    if (Sys.GetProcessRunning(Config.PluginBinary))
                    {
                        Sys.KillProcess(Config.PluginBinary);
                        await Task.Delay(500);
                    }

                    // Also try to stop StreamDock software if it's running
                    Sys.KillProcess("StreamDock");
                    await Task.Delay(500);

                    Model.SetSuccess("StreamDock stopped.");
                    return true;
                }
                else if (Operation == DeckProcessOperation.START)
                {
                    Logger.Debug("Starting StreamDock Software");
                    Model.Message = "Starting StreamDock ...";

                    // Start StreamDock software - check for StreamDock executable
                    try
                    {
                        // Try common StreamDock locations
                        string[] streamDockPaths = new string[]
                        {
                            @"C:\Program Files\HotSpot\StreamDock\StreamDock.exe",
                            @"C:\Program Files (x86)\HotSpot\StreamDock\StreamDock.exe",
                            Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\HotSpot\StreamDock\StreamDock.exe")
                        };

                        bool started = false;
                        foreach (var path in streamDockPaths)
                        {
                            if (System.IO.File.Exists(path))
                            {
                                Sys.RunCommand($"\"{path}\"", out _);
                                started = true;
                                break;
                            }
                        }

                        if (!started)
                        {
                            // Fallback: try to start with protocol
                            Sys.RunCommand("start streamdock://", out _);
                        }
                    }
                    catch
                    {
                        // Final fallback
                        Sys.RunCommand("start streamdock://", out _);
                    }

                    await Task.Delay(2000);

                    if (RefocusWindow && !string.IsNullOrWhiteSpace(RefocusWindowTitle))
                    {
                        Logger.Debug($"Refocusing window '{RefocusWindowTitle}'");
                        Sys.SetForegroundWindow(RefocusWindowTitle);
                    }

                    Model.SetSuccess("StreamDock started.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                if (!IgnorePluginRunning)
                    Model.SetError($"Error with StreamDock: {ex.Message}");
                return !IgnorePluginRunning;
            }

            return false;
        }
    }
}