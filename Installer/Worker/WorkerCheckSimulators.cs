using CFIT.AppLogger;
using CFIT.Installer.LibFunc;
using CFIT.Installer.Tasks;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Installer.Worker
{
    public class WorkerCheckSimulators : TaskWorker<Config>
    {
        public List<Simulator> SearchSimulators { get; protected set; } = new List<Simulator>();
        public Dictionary<Simulator, string[]> PackagePaths { get; protected set; } = new Dictionary<Simulator, string[]>();
        protected int CountSimulators { get; set; } = 0;
        protected List<string> SimulatorMessages { get; } = new List<string>();

        public WorkerCheckSimulators(Config config) : base(config, "Installed Simulators", "Checking installed Simulators ...")
        {
            Model.DisplayCompleted = true;
            Model.DisplayInSummary = true;
            IgnoreFailed = true;
        }

        protected override async Task<bool> DoRun()
        {
            //MSFS
            var msfsVersions = new List<string>();
            if (!Config.IgnoreMsfs2020 && CheckMsfs(Simulator.MSFS2020))
                msfsVersions.Add("2020");

            if (!Config.IgnoreMsfs2024 && CheckMsfs(Simulator.MSFS2024))
                msfsVersions.Add("2024");

            if (msfsVersions.Count > 0)
                SimulatorMessages.Add($"Found: MSFS {string.Join(", ", msfsVersions)}");

            //X-Plane
            var xpVersions = CheckXplane();
            if (xpVersions.Count > 0)
                SimulatorMessages.Add($"Found: X-Plane {string.Join(", ", xpVersions)}");

            //Prepar3D
            var p3dVersions = CheckPrepar3d();
            if (p3dVersions.Count > 0)
                SimulatorMessages.Add($"Found: Prepar3D {string.Join(", ", p3dVersions)}");

            Config.SetOption(Config.OptionSearchSimulators, SearchSimulators);
            Config.SetOption(Config.OptionPackagePaths, PackagePaths);

            if (CountSimulators == 0)
            {
                Model.SetSuccess("No Simulators found - Can not check for Requirements!");
                Model.State = TaskState.WAITING;
            }
            else
            {
                Model.SetSuccess(string.Join("\r\n", SimulatorMessages));
            }

            await Task.Delay(0);
            return CountSimulators > 0;
        }

        protected bool CheckMsfs(Simulator sim)
        {
            Model.Message = $"Searching Package Path for {sim} ...";
            if (FuncMsfs.CheckInstalledMsfs(sim, out string[] paths))
            {
                CountSimulators++;
                SearchSimulators.Add(sim);
                PackagePaths.Add(sim, paths);
                Logger.Debug($"Added {paths?.Count()} Paths for Simulator {sim}");
                return true;
            }

            return false;
        }

        private static readonly Regex rxXpPrefFile = new Regex(@"^X-Plane (\d+) Preferences$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        protected List<string> CheckXplane()
        {
            var versions = new List<string>();

            try
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string[] files = Directory.EnumerateFiles(path, "*.prf").ToArray();
                Logger.Debug($"Enumerated {files?.Length} PRF Files in {path}");

                foreach (var file in files)
                {
                    var match = rxXpPrefFile.Match(Path.GetFileNameWithoutExtension(file));
                    if (match.Success && match.Groups.Count == 2 && int.TryParse(match.Groups[1].Value, out int version))
                    {
                        CountSimulators++;
                        SearchSimulators.Add((Simulator)version);
                        versions.Add(match.Groups[1].Value);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return versions;
        }

        protected List<string> CheckPrepar3d()
        {
            var versions = new List<string>();
            List<int> prepareSims = new List<int>() { (int)Simulator.P3DV4, (int)Simulator.P3DV5, (int)Simulator.P3DV6 };
            foreach (int version in prepareSims)
            {
                try
                {
                    string path = $@"{Config.P3dRegPath}\{Config.P3dRegFolderPrefix}{version}";
                    int result = (int)Registry.GetValue(path, Config.P3dRegValueInstalled, null);
                    Logger.Debug($"Path '{path}' returned '{result}'");
                    if (result == 1)
                    {
                        CountSimulators++;
                        SearchSimulators.Add((Simulator)version);
                        versions.Add($"v{version}");
                    }
                }
                catch { }
            }

            return versions;
        }
    }
}
