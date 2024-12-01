using PilotsDeck.Tools;
using System;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace PilotsDeck.UI.DeveloperUI
{
    public partial class ViewReference : UserControl, IView
    {
        public ViewReference()
        {
            InitializeComponent();
            InitializeControls();
            this.AddHandler(Hyperlink.RequestNavigateEvent, new RequestNavigateEventHandler(Sys.RequestNavigateHandler));
        }

        protected static string FindLatestControlFile(string path)
        {
            if (Path.Exists(path))
                return Directory.EnumerateFiles(path, "Controls List for *.txt").FirstOrDefault();
            else
                return "";
        }

        protected void InitializeControls()
        {
            try
            {
                UrlHelper(LabelReadme, "Readme", "https://github.com/Fragtality/PilotsDeck/blob/master/README.md");
                UrlHelper(LabelReadmeSyntax, "Readme: Syntax", "https://github.com/Fragtality/PilotsDeck/blob/master/README.md#12---supported-sim-commands---variables");
                UrlHelper(LabelReadmeLua, "Readme: Lua Scripts", "https://github.com/Fragtality/PilotsDeck/blob/master/README.md#35---lua-scripts");
                UrlHelper(LabelReadmeMapping, "Readme: Profile Mapping", "https://github.com/Fragtality/PilotsDeck/blob/master/README.md#34---profile-switching");

                UrlHelper(LabelFolderLogs, "Log Folder", @$"{App.PLUGIN_PATH}\log");
                UrlHelper(LabelFolderImages, "Image Folder", @$"{App.PLUGIN_PATH}\Images");
                UrlHelper(LabelFolderScripts, "Script Folder", @$"{App.PLUGIN_PATH}\Scripts");

                UrlHelper(LabelBvars, "Detected B-Vars", @$"{App.PLUGIN_PATH}\{AppConfiguration.FILE_BVAR}");
                UrlHelper(LabelLvars, "Detected L-Vars (MSFS)", @$"{App.PLUGIN_PATH}\{AppConfiguration.FILE_LVAR}");
                LabelLimitHint.Text = "Output is limited to 1000 Results, use FSUIPC for a complete List!";



                UrlHelper(LabelMsfsAvar, "MSFS SDK - SimVars", "https://docs.flightsimulator.com/html/Programming_Tools/SimVars/Simulation_Variables.htm");
                UrlHelper(LabelMsfsKvar, "MSFS SDK - SimEvents", "https://docs.flightsimulator.com/html/Programming_Tools/Event_IDs/Event_IDs.htm");

                string path = @$"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\FSUIPC7\";
                if (Path.Exists(path))
                {
                    UrlHelper(LabelIpc7Controls, "FSUIPC7 Controls", $"{FindLatestControlFile(path)}");
                    UrlHelper(LabelIpc7Offsets, "FSUIPC7 Offsets", @$"{path}FSUIPC7 Offsets Status.pdf");
                }
                else
                {
                    LabelIpc7Controls.Text = "FSUIPC7 not found";
                    LabelIpc7Offsets.Text = "FSUIPC7 not found";
                }

                path = @$"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\FSUIPC6\";
                if (Path.Exists(path))
                {
                    UrlHelper(LabelIpc6Controls, "FSUIPC6 Controls", $"{FindLatestControlFile(path)}");
                    UrlHelper(LabelIpc6Offsets, "FSUIPC6 Offsets", @$"{path}FSUIPC Offsets Status.pdf");
                }
                else
                {
                    LabelIpc6Controls.Text = "FSUIPC6 not found";
                    LabelIpc6Offsets.Text = "FSUIPC6 not found";
                }

                UrlHelper(LabelXplaneRef, "DataRefs on x-plane.com", "https://developer.x-plane.com/datarefs/");
                UrlHelper(LabelXplaneCmdRef, "Commands by SimInnovations", "https://www.siminnovations.com/xplane/command/");

                UrlHelper(LabelMobi, "HubHop by MobiFlight", "https://hubhop.mobiflight.com/");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected static void UrlHelper(TextBlock label, string title, string url)
        {
            label.Inlines.Clear();
            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(title))
                return;

            var run = new Run(title);
            var hyperlink = new Hyperlink(run)
            {
                NavigateUri = new Uri(url)
            };
            label.Inlines.Add(hyperlink);
        }

        public void Start()
        {
            
        }

        public void Stop()
        {
            
        }
    }
}
