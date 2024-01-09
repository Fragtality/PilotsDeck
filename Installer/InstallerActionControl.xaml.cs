using System;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Documents;
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
        private InstallerTask installerTask;

        public InstallerActionControl(InstallerTask task)
        {
            InitializeComponent();

            installerTask = task;
            UpdateTask();
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

        public void UpdateTask()
        {
            Message.Inlines.Clear();
            SetImage(installerTask.ResultIcon);
            Title.Content = installerTask.Title;
            Message.Text = installerTask.Message;
            if (!string.IsNullOrEmpty(installerTask.HyperlinkURL) && !string.IsNullOrEmpty(installerTask.Hyperlink))
            {
                Hyperlink link = new Hyperlink(new Run($"\r\n{installerTask.Hyperlink}"))
                {
                    NavigateUri = new Uri($"{installerTask.HyperlinkURL}"),
                };
                Message.Inlines.Add(link);
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
