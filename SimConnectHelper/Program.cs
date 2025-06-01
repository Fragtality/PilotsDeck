using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace SimConnectHelper
{
    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                if (args?.Length > 0)
                {
                    if (args.Length == 1 && args[0].Equals("--debug", StringComparison.InvariantCultureIgnoreCase))
                        Logger.WriteFile = true;
                    if (args.Length >= 2 && (args[0].Equals("--debug", StringComparison.InvariantCultureIgnoreCase) || args[1].Equals("--debug", StringComparison.InvariantCultureIgnoreCase)))
                        Logger.WriteFile = true;

                    if (Logger.WriteFile && File.Exists(Logger.LogFile))
                        File.Delete(Logger.LogFile);

                    if (Logger.WriteFile)
                        Logger.Write("Debug Mode - writing Log File");
                }
                else if (!Logger.WriteFile && File.Exists(Logger.LogFile))
                {
                    Logger.Write("Delete old Logfile");
                    File.Delete(Logger.LogFile);
                }

                Logger.Write($"Testing Mode ...");
                bool pluginConfig = File.Exists(PluginConfig.ConfigFileName) && File.Exists("PilotsDeck.exe");
                if (pluginConfig)
                    Logger.Write($"Client-Mode: Modify PluginConfig");
                else
                    Logger.Write($"Server-Mode: Modify Simulator");

                if (pluginConfig)
                    return PluginConfig.Run();
                else
                    return SimConnectFile.Run();
            }
            catch (Exception ex)
            {
                Logger.WriteException(ex);
                return -1;
            }
        }

        
    }
}
