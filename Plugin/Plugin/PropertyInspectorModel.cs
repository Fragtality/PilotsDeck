using CFIT.AppLogger;
using PilotsDeck.Actions;
using PilotsDeck.Simulator;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;

namespace PilotsDeck.Plugin
{
    public class PropertyInspectorModel
    {
        public bool IsPropertyInspectorModel { get; set; } = true;
        public Dictionary<string, SimCommandType> ActionTypes { get; set; } = [];
        public Dictionary<string, GaugeOrientation> GaugeOrientations { get; set; } = [];
        public Dictionary<string, string> FontNames { get; set; } = [];
        public Dictionary<string, FontStyle> FontStyles { get; set; } = [];
        public Dictionary<string, Dictionary<string, string>> ImageDictionary { get; set; } = [];

        public PropertyInspectorModel()
        {
            try
            {
                GenerateActionTypes();
                GenerateGaugeOrientations();
                GenerateFontNames();
                GenerateFontStyles();
                GenerateImageDictionary();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected void GenerateActionTypes()
        {
            ActionTypes.Add("FSUIPC: Macro", SimCommandType.MACRO);
            ActionTypes.Add("FSUIPC: Script", SimCommandType.SCRIPT);
            ActionTypes.Add("FSUIPC: Control", SimCommandType.CONTROL);
            ActionTypes.Add("FSUIPC: Offset", SimCommandType.OFFSET);
            ActionTypes.Add("FSUIPC: vJoy", SimCommandType.VJOY);
            ActionTypes.Add("MSFS/FSUIPC: L-Var", SimCommandType.LVAR);
            ActionTypes.Add("MSFS: A-Var (SimVar)", SimCommandType.AVAR);
            ActionTypes.Add("MSFS: H-Var (HTMLEvent)", SimCommandType.HVAR);
            ActionTypes.Add("MSFS: K-Var (SimEvent)", SimCommandType.KVAR);
            ActionTypes.Add("MSFS: B-Var (InputEvent)", SimCommandType.BVAR);
            ActionTypes.Add("MSFS: Calculator/RPN Code", SimCommandType.CALCULATOR);
            ActionTypes.Add("X-Plane: DataRef", SimCommandType.XPWREF);
            ActionTypes.Add("X-Plane: CommandRef", SimCommandType.XPCMD);
            ActionTypes.Add("All Sims: vJoy Driver", SimCommandType.VJOYDRV);
            ActionTypes.Add("All Sims: Lua Function", SimCommandType.LUAFUNC);
            ActionTypes.Add("All Sims: Internal Variable", SimCommandType.INTERNAL);
        }

        protected void GenerateGaugeOrientations()
        {
            GaugeOrientations.Add("Up", GaugeOrientation.UP);
            GaugeOrientations.Add("Down", GaugeOrientation.DOWN);
            GaugeOrientations.Add("Left", GaugeOrientation.LEFT);
            GaugeOrientations.Add("Right", GaugeOrientation.RIGHT);
        }

        protected void GenerateFontNames()
        {
            InstalledFontCollection installedFontCollection = new();
            foreach (var family in installedFontCollection.Families)
                FontNames.Add(family.Name, family.Name);
        }

        protected void GenerateFontStyles()
        {
            foreach (FontStyle style in Enum.GetValues(typeof(FontStyle)))
                FontStyles.Add(Enum.GetName(typeof(FontStyle), style), style);

            FontStyles.Add(FontStyle.Bold.ToString() + " + " + FontStyle.Italic.ToString(), FontStyle.Bold | FontStyle.Italic);
            FontStyles.Add(FontStyle.Bold.ToString() + " + " + FontStyle.Underline.ToString(), FontStyle.Bold | FontStyle.Underline);
            FontStyles.Add(FontStyle.Italic.ToString() + " + " + FontStyle.Underline.ToString(), FontStyle.Italic | FontStyle.Underline);
            FontStyles.Add(FontStyle.Bold.ToString() + " + " + FontStyle.Strikeout.ToString(), FontStyle.Bold | FontStyle.Strikeout);
            FontStyles.Add(FontStyle.Italic.ToString() + " + " + FontStyle.Strikeout.ToString(), FontStyle.Italic | FontStyle.Strikeout);
        }

        public static EnumerationOptions GetEnumerationOptions()
        {
            return new EnumerationOptions()
            {
                IgnoreInaccessible = true,
                MatchCasing = MatchCasing.CaseInsensitive,
                RecurseSubdirectories = false
            };
        }

        protected void GenerateImageDictionary()
        {
            var enumOptions = GetEnumerationOptions();
            var baseDirInfo = new DirectoryInfo(AppConfiguration.DirImages);
            AddImageDirectory("/", [.. baseDirInfo.EnumerateFiles(AppConfiguration.ImageExtensionFilter, enumOptions)]);

            foreach (var dir in baseDirInfo.GetDirectories())
                AddImageDirectory("/" + dir.Name, [.. new DirectoryInfo(dir.FullName).EnumerateFiles(AppConfiguration.ImageExtensionFilter, enumOptions)]);
        }

        protected void AddImageDirectory(string key, List<FileInfo> fileList)
        {
            var dict = new Dictionary<string, string>();
            string prefix = "Images";
            if (key != "/")
                prefix = $"Images{key}";

            foreach (var file in fileList)
            {
                if (file.Name.Contains('@'))
                    continue;

                dict.Add(file.Name.Replace(AppConfiguration.ImageExtension, ""), $"{prefix}/{file.Name}");
            }
            
            ImageDictionary.Add(key, dict);
        }
    }
}
