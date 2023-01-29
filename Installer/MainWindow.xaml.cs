using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace Installer
{
    public partial class MainWindow : Window
    {
        private int InstallerState { get; set; } = 0;

        public MainWindow()
        {
            InitializeComponent();

            descLabel.Text = "This Tool will install the PilotsDeck StreamDeck Plugin on your System.\r\nYour StreamDeck Software will be stopped during the Installation-Process.\r\nAdded/Changed Profiles and added Images will stay intact.";
        }

        private InstallerActionControl AddActionControl(string text = null, ActionIcon icon = ActionIcon.None)
        {
            var component = new InstallerActionControl(text, icon);
            actionPanel.Children.Add(component);

            component.AddHandler(Hyperlink.RequestNavigateEvent, new RequestNavigateEventHandler(RequestNavigateHandler));

            return component;
        }

        private void RequestNavigateHandler(object sender, RequestNavigateEventArgs e)
        {
            if (!e.Uri.ToString().Contains(Parameters.importBinary))
                Process.Start(e.Uri.ToString());
            else
            {
                var pProcess = new Process();
                pProcess.StartInfo.FileName = e.Uri.AbsolutePath;
                pProcess.StartInfo.UseShellExecute = true;
                pProcess.StartInfo.WorkingDirectory = Parameters.pluginDir;
                pProcess.Start();
            }
        }

        private void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            if (InstallerState == 0)
            {
                InstallButton.IsEnabled = false;
                if (RunInstaller())
                {
                    var label = new Label()
                    {
                        Content = "Please start the StreamDeck Software again!\r\nThe Plugin might be blocked by Security-Software, make sure it will not be blocked!",
                        FontSize = 12,
                        FontWeight = FontWeights.DemiBold,
                        Margin = new Thickness(24),
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                    };
                    actionPanel.Children.Add(label);
                }
                else
                {

                }
                InstallerState = 1;
                InstallButton.Content = "Close";
                InstallButton.IsEnabled = true;
                return;
            }
            else if (InstallerState == 1)
            {
                this.Close();
            }
        }

        private bool RunInstaller()
        {
            //.NET Runtime
            var control = AddActionControl("Checking .NET Runtime Version ...");
            if (InstallerFunctions.CheckDotNet())
            {
                control.SetImage(ActionIcon.OK);
                control.Message.Text = $"The .NET Runtimes are at Version {Parameters.netVersion} or greater.";
            }
            else
            {
                control.SetImage(ActionIcon.Error);
                control.Message.Text = $"The installed .NET Runtime does not match the Minimum Version {Parameters.netVersion}!\r\nPlease install the following Packages:\r\n";
                Hyperlink link = new Hyperlink(new Run(".NET Runtime\r\n"))
                {
                    NavigateUri = new Uri("https://download.visualstudio.microsoft.com/download/pr/df7da01f-1f17-4728-92b7-778e9607da8f/7c18246830f8c78591f02f25aa368dcf/dotnet-runtime-7.0.2-win-x64.exe"),
                };
                control.Message.Inlines.Add(link);
                link = new Hyperlink(new Run(".NET Desktop Runtime"))
                {
                    NavigateUri = new Uri("https://download.visualstudio.microsoft.com/download/pr/8d4ae76c-10d6-450c-b1c2-76b7b2156dc3/9207c5d5d0b608d8ec0622efa4419ed6/windowsdesktop-runtime-7.0.2-win-x64.exe\r\n")
                };
                control.Message.Inlines.Add(link);


                return false;
            }


            //StreamDeck Version
            control = AddActionControl("Checking StreamDeck Software Version ...");
            if (InstallerFunctions.CheckStreamDeckSW())
            {
                control.SetImage(ActionIcon.OK);
                control.Message.Text = $"The StreamDeck Software is at Version {Parameters.sdVersion} or greater.";
            }
            else
            {
                control.SetImage(ActionIcon.Error);
                control.Message.Text = $"The installed StreamDeck Software does not match the Minimum Version {Parameters.sdVersion}!\r\nPlease update your Software.";

                return false;
            }


            //Check MSFS Requirements
            control = AddActionControl("Checking MSFS Requirements ...");
            if (InstallerFunctions.CheckInstalledMSFS(out string packagePath) && !string.IsNullOrWhiteSpace(packagePath))
            {
                Hyperlink link = new Hyperlink(new Run("\r\nFSUIPC"))
                {
                    NavigateUri = new Uri("http://fsuipc.com/")
                };
                if (InstallerFunctions.CheckFSUIPC())
                {
                    if (InstallerFunctions.CheckPackageVersion(packagePath, Parameters.wasmIpcName, Parameters.wasmIpcVersion))
                    {
                        link = new Hyperlink(new Run("\r\nMobiFlight"))
                        {
                            NavigateUri = new Uri("https://github.com/MobiFlight/MobiFlight-WASM-Module/releases")
                        };
                        if (InstallerFunctions.CheckPackageVersion(packagePath, Parameters.wasmMobiName, Parameters.wasmMobiVersion))
                        {
                            control.SetImage(ActionIcon.OK);
                            control.Message.Text = "All MSFS Requirements met!";
                        }
                        else
                        {
                            control.SetImage(ActionIcon.Error);
                            control.Message.Text = $"The installed WASM Module from MobiFlight does not match the Minimum Version {Parameters.wasmMobiVersion}! Please install / update it.";
                            control.Message.Inlines.Add(link);
                            return false;
                        }
                    }
                    else
                    {
                        control.SetImage(ActionIcon.Error);
                        control.Message.Text = $"The installed WASM Module from FSUIPC does not match the Minimum Version {Parameters.wasmIpcVersion}! Please try to update it manually.";
                        control.Message.Inlines.Add(link);
                        return false;
                    }
                }
                else
                {
                    control.SetImage(ActionIcon.Error);
                    control.Message.Text = $"The installed FSUIPC Version does not match the Minimum Version {Parameters.ipcMajor}.{Parameters.ipcMinor}.{Parameters.ipcPatch}! Please install the latest Version.";
                    control.Message.Inlines.Add(link);
                    return false;
                }
            }
            else
            {
                control.SetImage(ActionIcon.Notice);
                control.Message.Text = "MSFS not found.";
            }



            //Stop Deck
            control = AddActionControl("Stopping StreamDeck Software ...");
            if (InstallerFunctions.StopStreamDeck())
            {
                control.SetImage(ActionIcon.OK);
                control.Message.Text = $"StreamDeck Software stopped.";
            }
            else
            {
                control.SetImage(ActionIcon.Error);
                control.Message.Text = $"The StreamDeck Software could not be stopped!\r\nPlease stop it manually and try again.";

                return false;
            }


            //Delete Old Binaries
            control = AddActionControl("Deleting old Binaries ...");
            if (InstallerFunctions.DeleteOldFiles())
            {
                control.SetImage(ActionIcon.OK);
                control.Message.Text = $"Old Binaries removed.";
            }
            else
            {
                control.SetImage(ActionIcon.Error);
                control.Message.Text = $"The old Binaries could not be removed!\r\nPlease remove them manually and try again.";

                return false;
            }


            //Extract Plugin
            control = AddActionControl("Extracting Plugin ...");
            if (InstallerFunctions.ExtractZip())
            {
                control.SetImage(ActionIcon.OK);
                control.Message.Text = $"Plugin extracted to:\r\n%appdata%{Parameters.sdPluginDir}\\{Parameters.pluginName}";
            }
            else
            {
                control.SetImage(ActionIcon.Error);
                control.Message.Text = $"The Plugin Archive could not be extracted!";

                return false;
            }

            //Check Profiles
            Hyperlink importer = new Hyperlink(new Run("Import Profiles"))
            {
                NavigateUri = new Uri(Parameters.pluginDir + "\\" + "ImportProfiles.exe"),
            };

            control = AddActionControl("Checking installed Profiles ...");
            if (InstallerFunctions.HasCustomProfiles(out bool oldDefault))
            {
                control.SetImage(ActionIcon.Warn);
                control.Message.Text = $"Custom Profiles where detected. They need to be imported to be used for Profile Switching!\r\n";
                if (oldDefault)
                    control.Message.Text += "The old default Profiles 'Whiskey', 'X-Ray', 'Yankee' or 'Zulu' seem to be installed. You can remove them, if you never used them.\r\n";
                control.Message.Inlines.Add(importer);

            }
            else if (oldDefault)
            {
                control.SetImage(ActionIcon.Notice);
                control.Message.Text = $"The old default Profiles 'Whiskey', 'X-Ray', 'Yankee' or 'Zulu' seem to be installed. You can remove them, if you never used them.\r\nIf you used them you need to import them:\r\n";
                control.Message.Inlines.Add(importer);
            }
            else
            {
                control.SetImage(ActionIcon.OK);
                control.Message.Text = $"No Custom Profiles installed - nothing todo.";
            }


            return true;
        }
    }
}
