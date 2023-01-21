using System.Reflection;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Installer
{
    public enum ActionIcon
    {
        None,
        OK,
        Notice,
        Warn,
        Error
    }

    public partial class InstallerActionControl : UserControl
    {
        public InstallerActionControl(string text = null, ActionIcon actionIcon = ActionIcon.None)
        {
            InitializeComponent();
            
            if (text != null)
                Message.Text = text;
            SetImage(actionIcon);
        }

        public void SetImage(ActionIcon actionIcon)
        {
            if (actionIcon != ActionIcon.None)
            {
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Installer.{GetImageName(actionIcon)}.png"))
                {
                    var bi = BitmapFrame.Create(stream, BitmapCreateOptions.IgnoreImageCache, BitmapCacheOption.OnLoad);
                    Image.Source = bi;
                }
            }
        }

        private string GetImageName(ActionIcon actionIcon)
        {
            switch (actionIcon)
            {
                case ActionIcon.OK:
                    return "FlagGreen";
                case ActionIcon.Notice:
                    return "FlagBlue";
                case ActionIcon.Warn:
                    return "FlagYellow";
                case ActionIcon.Error:
                    return "FlagRed";
                default:
                    return "FlagRed";
            }
        }
    }
}
