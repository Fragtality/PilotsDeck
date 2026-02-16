using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.Installer.LibFunc;
using CFIT.Installer.LibWorker;
using Installer.Tools;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Installer
{
    public class WorkerInstallUpdate : WorkerAppInstall<Config>
    {
        public bool ResetConfiguration { get; set; } = false;
        public bool Fsuipc7UseSecondary { get; set; } = true;

        public WorkerInstallUpdate(Config config) : base(config)
        {
            Model.DisplayCompleted = true;
            Model.DisplayInSummary = true;

            Model.Title = "PilotsDeck Plugin";
            SetPropertyFromOption<bool>(Config.OptionResetConfiguration);
            SetPropertyFromOption<bool>(Config.OptionFsuipc7UseSecondary);
        }

        protected override async Task<bool> DoRun()
        {
            Model.Message = "Waiting for Plugin to close ...";
            await Task.Delay(500);
            if (Sys.GetProcessRunning(Config.PluginBinary))
            {
                Logger.Debug($"Plugin still running - kill Process");
                Sys.KillProcess(Config.PluginBinary);
                await Task.Delay(750);
            }
            
            return await base.DoRun();
        }

        protected override void CreateFileExclusions()
        {
            FileExclusions.Add(Config.ProductConfigFile.ToLowerInvariant());
            FileExclusions.Add(Config.ProductColorFile.ToLowerInvariant());
        }

        protected override bool DeleteOldFiles()
        {
            if (!Directory.Exists(Config.ProductPath))
                return true;

            FuncIO.DeleteDirectory(Path.Combine(Config.ProductPath, "logs"), true, false);
            FuncIO.DeleteDirectory(Path.Combine(Config.ProductPath, "log"), true, true);
            FuncIO.DeleteDirectory(Path.Combine(Config.ProductPath, "Plugin"), true, false);
            FuncIO.DeleteDirectory(Path.Combine(Config.ProductPath, "previews"), true, false);

            string[] files = Directory.EnumerateFiles(Config.ProductPath).ToArray();
            foreach (var file in files)
            {
                if (!FileExclusions.Contains(Path.GetFileName(file).ToLowerInvariant()))
                {
                    Logger.Debug($"Deleting '{file}'");
                    FuncIO.DeleteFile(file);
                }
            }

            if (File.Exists(Config.ProductConfigPath) && ResetConfiguration)
            {
                Logger.Debug($"Deleting Config File '{Config.ProductConfigPath}'");
                FuncIO.DeleteFile(Config.ProductConfigPath);
            }

            if (File.Exists(Config.ProductColorFile) && ResetConfiguration)
            {
                Logger.Debug($"Deleting Color File '{Config.ProductColorFile}'");
                FuncIO.DeleteFile(Config.ProductColorFile);
            }

            if (ResetConfiguration)
                InstallerRunCreateConfig = false;

            if (Fsuipc7UseSecondary != Config.Fsuipc7UseSecondaryConfig)
                InstallerRunCreateConfig = true;

            return Directory.EnumerateFiles(Config.ProductPath).Count() <= FileExclusions.Count;
        }

        protected override bool CreateDefaultConfig()
        {
            using (var stream = GetAppConfig())
            {
                var confStream = File.Create(Config.ProductConfigPath);
                stream.Seek(0, SeekOrigin.Begin);
                stream.CopyTo(confStream);
                confStream.Flush(true);
                confStream.Close();
            }
            Thread.Sleep(250);
            return Config.HasConfigFile;
        }

        protected override bool FinalizeSetup()
        {
            return CreateDirectories() && ChangeConfig();
        }

        protected virtual bool CreateDirectories()
        {
            Logger.Debug("Create log Dir");
            string logDir = Path.Combine(Config.ProductPath, "log");
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            Logger.Debug("Create Profiles Dir");
            if (!Directory.Exists(Config.ProductPathProfiles))
                Directory.CreateDirectory(Config.ProductPathProfiles);

            Logger.Debug("Create Scripts Dir");
            if (!Directory.Exists(Config.ProductPathScripts))
                Directory.CreateDirectory(Config.ProductPathScripts);

            Logger.Debug("Create Scripts-global Dir");
            string dir = Path.Combine(Config.ProductPathScripts, "global");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            Logger.Debug("Create Scripts-image Dir");
            dir = Path.Combine(Config.ProductPathScripts, "image");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            return Directory.Exists(logDir) && Directory.Exists(Config.ProductPathProfiles) && Directory.Exists(Config.ProductPathScripts);
        }

        protected virtual bool ChangeConfig()
        {
            try
            {
                if (Fsuipc7UseSecondary != Config.Fsuipc7UseSecondaryConfig)
                {
                    Logger.Debug($"Changing Config for FSUIPC7 Use Secondary {Config.Fsuipc7UseSecondaryConfig} => {Fsuipc7UseSecondary}");
                    var json = JsonNode.Parse(File.ReadAllText(Config.ProductConfigPath));
                    var option = json[Config.AppConfigUseFsuipcForMSFS];
                    if (option != null)
                    {
                        json[Config.AppConfigUseFsuipcForMSFS] = Fsuipc7UseSecondary;
                        string output = JsonSerializer.Serialize(json, Json.GetSerializerOptions());
                        File.WriteAllText(Config.ProductConfigPath, output);
                    }
                    return (File.Exists(Config.ProductConfigPath) && (new FileInfo(Config.ProductConfigPath)).Length > 1);
                }
                else
                {
                    Logger.Debug("Check Config exists");
                    return (File.Exists(Config.ProductConfigPath) && (new FileInfo(Config.ProductConfigPath)).Length > 1) || Config.GetOption<bool>(Config.OptionResetConfiguration);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return false;
        }
    }
}
