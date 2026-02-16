using CFIT.Installer.Product;
using CFIT.Installer.UI.Behavior;
using CFIT.Installer.UI.Config;
using System.Collections.Generic;
using System.Windows;

namespace Installer
{
    public class ConfigPage : PageConfig
    {
        public Config Config { get { return BaseConfig as Config; } }

        public override void CreateConfigItems()
        {
            ConfigItemHelper.CreateCheckboxDesktopLink(Config, ConfigBase.OptionDesktopLink, Items, $"Create Link for {Config.ProfileManagerName} on Desktop");

            Items.Add(new ConfigItemCheckbox("FSUIPC7 Connector", "Use FSUIPC7 as Secondary Connector for MSFS 2020/2024 (recommended)", Config.OptionFsuipc7UseSecondary, Config));

            Items.Add(new ConfigItemCheckbox("vJoy Driver", "Install/Update vJoy Driver (recommended)", Config.OptionVjoyInstallUpdate, Config));

            var dict = new Dictionary<int, string>()
            {
                { 0, "Elegato - StreamDeck" },
                { 1, "MiraBox/HotSpot - StreamDock" }
            };
            Items.Add(new ConfigItemDropdown("Plugin Install Location", "Install/Update Plugin on the following Software:", dict, Config.OptionInstallTarget, Config));

            if (Config.Mode == SetupMode.UPDATE)
                Items.Add(new ConfigItemCheckbox("Reset Configuration", "Reset Plugin Configuration to Default (only for Troubleshooting)", Config.OptionResetConfiguration, Config));
        }

        protected override void SetFooter()
        {
            base.SetFooter();
            if (Config.IgnoreMsfs2020)
                AddIgnoreMsfsHint("2020");
            if (Config.IgnoreMsfs2024)
                AddIgnoreMsfsHint("2024");
        }

        protected virtual void AddIgnoreMsfsHint(string version)
        {
            var header = CreateTextBlock($"Requirements for MSFS {version} will be ignored", 10, FontWeights.DemiBold);
            AddFooter(header);
        }
    }
}