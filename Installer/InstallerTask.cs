namespace Installer
{
    public class InstallerTask
    {
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string Hyperlink { get; set; } = "";
        public string HyperlinkURL { get; set; } = "";
        public ActionIcon ResultIcon { get; set; } = ActionIcon.None;

        public InstallerTask(string title, string message)
        {
            Title = title;
            Message = message;
        }
    }
}
