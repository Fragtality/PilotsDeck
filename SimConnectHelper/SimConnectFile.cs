using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;

namespace SimConnectHelper
{
    public class SimConnectFile
    {
        public static string FolderAppDataRoaming()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }

        public static string FolderAppDataLocal()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }

        public static string SimConnectFileName { get; } = "SimConnect.xml";

        public static Dictionary<string, string> SimConnectPaths { get; } = new Dictionary<string, string>()
        {
            { "MSFS2020 (Microsoft Store)", $@"{FolderAppDataLocal()}\Packages\Microsoft.FlightSimulator_8wekyb3d8bbwe\LocalCache\{SimConnectFileName}" },
            { "MSFS2020 (Steam)", $@"{FolderAppDataRoaming()}\Microsoft Flight Simulator\{SimConnectFileName}" },
            { "MSFS2024 (Microsoft Store)", $@"{FolderAppDataLocal()}\Packages\Microsoft.Limitless_8wekyb3d8bbwe\LocalCache\{SimConnectFileName}" },
            { "MSFS2024 (Steam)", $@"{FolderAppDataRoaming()}\Microsoft Flight Simulator 2024\{SimConnectFileName}" },
        };

        public static List<string> DetectedSimulators { get; } = [];

        protected string FilePath { get; }
        protected XmlDocument XmlDocument { get; } = new();
        public List<int> IndicesGlobalScopes { get; } = [];
        public bool HasGlobalScopes => IndicesGlobalScopes.Count > 0;
        protected XmlElement SimBase => XmlDocument.ChildNodes[1] as XmlElement;

        public SimConnectFile(string file)
        {
            FilePath = file;
            Logger.Write("Parsing SimConnect File ...");
            XmlDocument.Load(file);
            EnumerateGlobalScopes();
        }

        protected void EnumerateGlobalScopes()
        {
            if (XmlDocument.ChildNodes.Count >= 2 && XmlDocument.ChildNodes[1] is XmlElement simBase && simBase.Name == "SimBase.Document")
            {
                Logger.Write($"SimBase found");
                int i = 0;
                foreach (var node in simBase.ChildNodes)
                {
                    if (node is not XmlElement elementNode)
                    {
                        Logger.Write($"Child is not a XmlElement: '{node}'");
                    }
                    else
                    {
                        Logger.Write($"Evaluating Child '{elementNode.Name}' ..");
                        if (elementNode.Name == "SimConnect.Comm" && elementNode.GetElementsByTagName("Scope").Count > 0)
                        {
                            if (elementNode.GetElementsByTagName("Scope")[0].InnerText == "global")
                            {
                                IndicesGlobalScopes.Add(i);
                                Logger.Write($"Found global Scope at Index {i}");
                            }
                        }
                    }
                    i++;
                }
            }
            else
                throw new Exception($"Unexpected Format in SimConnect.xml File '{FilePath}'");
        }

        public XmlElement GetGlobalNode(int index)
        {
            return SimBase.ChildNodes[index] as XmlElement;
        }

        public string GetGlobalNodeInfo(int index)
        {
            var node = GetGlobalNode(index);
            string result = $"Child #{index}";
            if (node.GetElementsByTagName("Descr").Count > 0)
                result = node.GetElementsByTagName("Descr")[0].InnerText;

            if (node.GetElementsByTagName("Address").Count > 0 && node.GetElementsByTagName("Port").Count > 0)
                result = $"{result} ({node.GetElementsByTagName("Address")[0].InnerText}:{node.GetElementsByTagName("Port")[0].InnerText})";

            return result;
        }

        public void AddRemoteEntry(string address, string port)
        {
            var element = XmlDocument.CreateElement("SimConnect.Comm");

            CreateChild(element, "Descr", "Remote IP4 Port");
            CreateChild(element, "Protocol", "IPv4");
            CreateChild(element, "Scope", "global");
            CreateChild(element, "Address", address);
            CreateChild(element, "Port", port);
            CreateChild(element, "MaxClients", "64");
            CreateChild(element, "MaxRecvSize", "41088");
            CreateChild(element, "DisableNagle", "True");

            SimBase.AppendChild(element);
            XmlDocument.Save(FilePath);
            IndicesGlobalScopes.Add(SimBase.ChildNodes.Count - 1);
        }

        protected void CreateChild(XmlElement parent, string tagName, string content)
        {
            var child = XmlDocument.CreateElement(tagName);
            child.InnerText = content;
            parent.AppendChild(child);
        }

        public void RemoveRemoteEntry(int index)
        {
            IndicesGlobalScopes.Remove(index);
            SimBase.RemoveChild(GetGlobalNode(index));
            XmlDocument.Save(FilePath);
        }

        public void UpdateRemoteEntry(int index, string address, string port)
        {
            var element = GetGlobalNode(index);

            AddUpdateChild(element, "Descr", "Remote IP4 Port");
            AddUpdateChild(element, "Protocol", "IPv4");
            AddUpdateChild(element, "Scope", "global");
            AddUpdateChild(element, "Address", address);
            AddUpdateChild(element, "Port", port);
            AddUpdateChild(element, "MaxClients", "64");
            AddUpdateChild(element, "MaxRecvSize", "41088");
            AddUpdateChild(element, "DisableNagle", "True");
            
            XmlDocument.Save(FilePath);
        }

        protected void AddUpdateChild(XmlElement parent, string tagName, string content)
        {
            var query = parent.GetElementsByTagName(tagName);
            if (query.Count > 0)
            {
                var child = query[0];
                child.InnerText = content;
            }
            else
            {
                var child = XmlDocument.CreateElement(tagName);
                child.InnerText = content;
                parent.AppendChild(child);
            }            
        }

        public static int Run()
        {
            Logger.Write("Register Encoding Provider");
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            SearchSimulators();
            if (DetectedSimulators.Count == 0)
            {
                Logger.Write("No Simulators detected - exiting App");
                return -1;
            }

            MenuSelectSimulator();
            return 0;
        }

        public static void SearchSimulators()
        {
            Logger.Write($"Searching for Simulators ...");
            DetectedSimulators.Clear();
            foreach (var sim in SimConnectPaths)
            {
                if (File.Exists(sim.Value))
                {
                    Logger.Write($"Simulator '{sim.Key}' detected!");
                    DetectedSimulators.Add(sim.Key);
                }
            }
        }

        public static string MenuSelectSimulator()
        {
            do
            {
                Console.Clear();
                Console.WriteLine($"Select Simulator to modify:");
                for (int i = 0; i < DetectedSimulators.Count; i++)
                    Console.WriteLine($"[{i}]\t{DetectedSimulators[i]}");
                Console.WriteLine($"[Q]\tExit Application");
                Console.Write(">> ");

                char input = Console.ReadKey().KeyChar;
                if (input == 'q' || input == 'Q')
                    return "quit";
                if (int.TryParse(input.ToString(), out int index) && index >= 0 && index < DetectedSimulators.Count)
                    ModifySimulator(DetectedSimulators[index]);
            }
            while (true);
        }

        public static void ModifySimulator(string simulator)
        {
            int operation = MenuSelectSimOperation();
            Console.WriteLine("");
            if (operation == -1)
            {
                Logger.Write("Quit selected - returning to previous Menu");
                return;
            }
            else
                Logger.Write($"Operation '{operation}' selected");

            var simConnectFile = new SimConnectFile(SimConnectPaths[simulator]);
            if (operation == 0)
            {
                if (simConnectFile.HasGlobalScopes)
                    MenuUpdateEntry(simConnectFile);
                else
                    MenuAddEntry(simConnectFile);
            }
            if (operation == 1)
            {
                MenuSelectRemoveEntry(simConnectFile);
            }
        }

        public static int MenuSelectSimOperation()
        {
            do
            {
                Console.Clear();
                Console.WriteLine($"Select which Modification should be performed:");
                Console.WriteLine($"[0]\tAdd/Update Entry for Remote Connection");
                Console.WriteLine($"[1]\tRemove Entry for Remote Connection");
                Console.WriteLine($"[Q]\tBack to Simulator Selection");
                Console.Write(">> ");

                char input = Console.ReadKey().KeyChar;
                if (input == 'q' || input == 'Q')
                    return -1;
                if (int.TryParse(input.ToString(), out int num) && (num == 0 || num == 1))
                    return num;
            }
            while (true);
        }

        public static void MenuSelectRemoveEntry(SimConnectFile simConnectFile)
        {
            if (!simConnectFile.HasGlobalScopes)
            {
                Logger.Write($"File has no Remote Entries - press any Key to exit");
                Console.ReadKey();
                return;
            }

            do
            {
                Console.Clear();
                Console.WriteLine($"Select which Entry to remove:");
                for (int i = 0; i < simConnectFile.IndicesGlobalScopes.Count; i++)
                    Console.WriteLine($"[{i}]\t{simConnectFile.GetGlobalNodeInfo(simConnectFile.IndicesGlobalScopes[i])}");
                Console.WriteLine($"[Q]\tBack to Simulator Selection");
                Console.Write(">> ");

                char input = Console.ReadKey().KeyChar;
                if (input == 'q' || input == 'Q')
                    return;
                if (int.TryParse(input.ToString(), out int num) && num >= 0 && num < simConnectFile.IndicesGlobalScopes.Count)
                {
                    Console.Clear();
                    var index = simConnectFile.IndicesGlobalScopes[num];
                    Logger.Write($"\nRemoving Entry #{index}");
                    simConnectFile.RemoveRemoteEntry(index);
                    Logger.Write($"Removed Entry #{index}!\nPress any Key to continue ...");
                    Console.ReadKey();
                    return;
                }
            }
            while (true);
        }

        public static void MenuAddEntry(SimConnectFile simConnectFile)
        {
            MenuInputHost(out string address, out string port);
            simConnectFile.AddRemoteEntry(address, port);
            Logger.Write($"Added Entry for Host {address}:{port}!\nPress any Key to continue ...");
            Console.ReadKey();
        }

        public static void MenuUpdateEntry(SimConnectFile simConnectFile)
        {
            int index;
            do
            {
                Console.Clear();
                Console.WriteLine($"Select which Entry to update:");
                for (int i = 0; i < simConnectFile.IndicesGlobalScopes.Count; i++)
                    Console.WriteLine($"[{i}]\t{simConnectFile.GetGlobalNodeInfo(simConnectFile.IndicesGlobalScopes[i])}");
                Console.WriteLine($"[Q]\tBack to Simulator Selection");
                Console.Write(">> ");

                char input = Console.ReadKey().KeyChar;
                if (input == 'q' || input == 'Q')
                    return;
                if (int.TryParse(input.ToString(), out int num) && num >= 0 && num < simConnectFile.IndicesGlobalScopes.Count)
                {
                    Console.Clear();
                    Logger.Write($"\nUpdating Entry #{simConnectFile.IndicesGlobalScopes[num]}");
                    index = simConnectFile.IndicesGlobalScopes[num];
                    break;
                }
            }
            while (true);

            MenuInputHost(out string address, out string port);
            Console.Clear();
            simConnectFile.UpdateRemoteEntry(index, address, port);
            Logger.Write($"Updated Entry #{index} to Host {address}:{port}!\nPress any Key to continue ...");
            Console.ReadKey();
        }

        public static void MenuInputHost(out string address, out string port)
        {
            var query = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
            List<string> addressList = ["0.0.0.0 (Listen on All)"];
            foreach (var entry in query)
                if (entry.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    addressList.Add(entry.ToString());

            do
            {
                Console.Clear();
                Console.WriteLine($"Select IP Address to use:");
                for (int i = 0; i < addressList.Count; i++)
                        Console.WriteLine($"[{i}]\t{addressList[i]}");
                Console.Write(">> ");

                char input = Console.ReadKey().KeyChar;
                if (int.TryParse(input.ToString(), out int num) && num >= 0 && num < addressList.Count)
                {
                    Logger.Write($"\nUsing Address {addressList[num]}");
                    if (num != 0)
                        address = addressList[num];
                    else
                        address = "0.0.0.0";
                    break;
                }
            }
            while (true);

            do
            {
                Console.Clear();
                Console.Write($"Enter Port to use >> ");

                string input = Console.ReadLine();
                if (int.TryParse(input, out _))
                {
                    Logger.Write($"\nUsing Port {input}");
                    port = input;
                    break;
                }
            }
            while (true);
        }
    }
}
