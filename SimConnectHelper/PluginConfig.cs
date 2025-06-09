using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SimConnectHelper
{
    public class PluginConfig
    {
        public const string ConfigFileName = "PluginConfig.json";
        public static JsonSerializerOptions JsonWriteOptions { get; } = new JsonSerializerOptions()
        {
            WriteIndented = true
        };

        public JsonNode JsonDocument { get; }
        public bool UseRemoteConnection => JsonDocument["MsfsRemoteConnection"].GetValue<bool>();
        public string RemoteAddress => JsonDocument["MsfsRemoteHost"].GetValue<string>().Split(':')[0];
        public string RemotePort => JsonDocument["MsfsRemoteHost"].GetValue<string>().Split(':')[1];

        public PluginConfig()
        {
            Logger.Write("Parsing PluginConfig ...");
            JsonDocument = JsonNode.Parse(File.ReadAllText(ConfigFileName));
            
            Logger.Write("Checking PluginConfig ...");
            bool save = false;
            if (!JsonDocument.AsObject().ContainsKey("MsfsRemoteConnection"))
            {
                Logger.Write($"Config needs Correction - adding 'MsfsRemoteConnection'");
                JsonDocument.AsObject().Add(new KeyValuePair<string, JsonNode?>("MsfsRemoteConnection", false));
                save = true;
            }
            if (!JsonDocument.AsObject().ContainsKey("MsfsRemoteHost") || string.IsNullOrWhiteSpace(JsonDocument["MsfsRemoteHost"].GetValue<string>()) || JsonDocument["MsfsRemoteHost"].GetValue<string>().Split(':').Length != 2)
            {
                Logger.Write($"Config needs Correction - adding/updating 'MsfsRemoteHost'");
                if (!JsonDocument.AsObject().ContainsKey("MsfsRemoteHost"))
                    JsonDocument.AsObject().Add(new KeyValuePair<string, JsonNode?>("MsfsRemoteHost", "127.0.0.1:6969"));
                else
                    JsonDocument["MsfsRemoteHost"] = "127.0.0.1:6969";
                save = true;
            }

            if (save)
                Save();
        }

        public void Save()
        {
            File.WriteAllText(ConfigFileName, JsonSerializer.Serialize(JsonDocument, JsonWriteOptions));
        }

        public static int Run()
        {
            int operation = MenuSelectPluginOperation();
            if (operation == -1)
            {
                Logger.Write($"Quit selected - exiting Application");
                return 0;
            }
            else
                return -1;
        }

        public static int MenuSelectPluginOperation()
        {
            int num = -1;
            do
            {
                Console.Clear();
                Console.WriteLine($"Select which Modification should be performed:");
                Console.WriteLine($"[0]\tSetup Plugin for Remote Connection");
                Console.WriteLine($"[1]\tSetup Plugin for Local Connection");
                Console.WriteLine($"[2]\tDisplay current Plugin Configuration");
                Console.WriteLine($"[Q]\tExit Application");
                Console.Write(">> ");

                char input = Console.ReadKey().KeyChar;
                Console.WriteLine("");
                if (input == 'q' || input == 'Q')
                    return -1;
                if (int.TryParse(input.ToString(), out num) && num >= 0 && num <= 2)
                {
                    if (num == 0)
                        SetupRemoteConnection();
                    else if (num == 1)
                        RemoveRemoteConnection();
                    else if (num == 2)
                        DisplayPluginConfig();
                }
            }
            while (true);
        }

        public static void SetupRemoteConnection()
        {
            var pluginConfig = new PluginConfig();
            MenuInputHost(pluginConfig, out string address, out string port);
            
            pluginConfig.SetRemoteConnection(address, port);
        }

        public void SetRemoteConnection(string address, string port)
        {
            JsonDocument["MsfsRemoteConnection"].ReplaceWith<bool>(true);
            JsonDocument["MsfsRemoteHost"].ReplaceWith<string>($"{address}:{port}");
            Save();
            Console.Clear();
            Logger.Write($"Modified Configuration for Remote Connection to Host {address}:{port}!\nPress any Key to continue ...");
            Console.ReadKey();
        }

        public static void MenuInputHost(PluginConfig pluginConfig, out string address, out string port)
        {
            bool isValidAddress = IPAddress.TryParse(pluginConfig.RemoteAddress, out IPAddress ip) && ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
            bool isValidPort = int.TryParse(pluginConfig.RemotePort, out _);
            do
            {
                Console.Clear();
                if (isValidAddress)
                    Console.Write($"Enter IP Address to use [Enter to keep {pluginConfig.RemoteAddress}] >> ");
                else
                    Console.Write($"Enter IP Address to use >> ");

                string input = Console.ReadLine();
                if (isValidAddress && input == "")
                {
                    address = pluginConfig.RemoteAddress;
                    Logger.Write($"\nKeeping Address {address}");
                    break;
                }
                else if (IPAddress.TryParse(input, out IPAddress ipAddress) && ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    Logger.Write($"\nUsing Address {ipAddress}");
                    address = ipAddress.ToString();
                    break;
                }
            }
            while (true);

            do
            {
                Console.Clear();
                if (isValidPort)
                    Console.Write($"Enter Port to use [Enter to keep {pluginConfig.RemotePort}] >> ");
                else
                    Console.Write($"Enter Port to use >> ");

                string input = Console.ReadLine();
                if (isValidPort && input == "")
                {
                    port = pluginConfig.RemotePort;
                    Logger.Write($"\nKeeping Port {port}");
                    break;
                }
                else if (int.TryParse(input, out _))
                {
                    Logger.Write($"\nUsing Port {input}");
                    port = input;
                    break;
                }
            }
            while (true);
        }

        public static void RemoveRemoteConnection()
        {
            var pluginConfig = new PluginConfig();
            pluginConfig.RemoveRemote();
        }

        public void RemoveRemote()
        {
            JsonDocument["MsfsRemoteConnection"].ReplaceWith<bool>(false);
            Save();
            Console.Clear();
            Logger.Write($"Modified Configuration for local Connection!\nPress any Key to continue ...");
            Console.ReadKey();
        }

        public const string SimConnectTemplate =
"""
<SimConnect.Comm>
  <Descr>Remote IP4 Port</Descr>
  <Protocol>IPv4</Protocol>
  <Scope>global</Scope>
  <Address>{0}</Address>
  <Port>{1}</Port>
  <MaxClients>64</MaxClients>
  <MaxRecvSize>41088</MaxRecvSize>
  <DisableNagle>True</DisableNagle>
</SimConnect.Comm>
""";

        public static void DisplayPluginConfig()
        {
            var pluginConfig = new PluginConfig();
            Console.Clear();

            Console.WriteLine($"Use Remote:\t{pluginConfig.UseRemoteConnection}");
            Console.WriteLine($"Remote Address:\t{pluginConfig.RemoteAddress}");
            Console.WriteLine($"Remote Port:\t{pluginConfig.RemotePort}");

            if (pluginConfig.UseRemoteConnection)
            {
                Console.WriteLine("");
                Console.WriteLine("XML Code for manual SimConnect.xml Modification:");
                Console.WriteLine(string.Format(SimConnectTemplate, pluginConfig.RemoteAddress, pluginConfig.RemotePort));
            }

            Console.WriteLine("");
            Logger.Write($"Press any Key to continue ...");
            Console.ReadKey();
        }
    }
}
