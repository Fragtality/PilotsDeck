using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Navigation;

namespace Installer
{
    public partial class InstallerTaskView : UserControl
    {
        public InstallerTask InstallerTask { get; protected set; }
        protected string LastTitle { get; set; } = "";
        protected string LastMessageEntry { get; set; } = "";
        protected string LastUrl { get; set; } = "";
        protected TaskState LastState { get; set; } = TaskState.ACTIVE;
        protected Hyperlink CurrentHyperlink { get; set; } = null;

        public bool DisplayOnlyLastCompleted { get { return InstallerTask.DisplayOnlyLastCompleted; } set { InstallerTask.DisplayOnlyLastCompleted = value; } }
        public bool DisplayOnlyLastError { get { return InstallerTask.DisplayOnlyLastError; } set { InstallerTask.DisplayOnlyLastError = value; } }

        protected Brush BrushDefault { get; } = new SolidColorBrush(Colors.DarkGray);
        protected Brush BrushRed { get; } = new SolidColorBrush(Colors.Red);
        protected Brush BrushGreen { get; } = new SolidColorBrush(Colors.Green);
        protected Brush BrushOrange { get; } = new SolidColorBrush(Colors.Orange);
        protected Brush BrushBlue { get; } = SystemColors.HighlightBrush;
        protected Brush BrushCurrent { get; set; } = SystemColors.HighlightBrush;


        public InstallerTaskView(InstallerTask task)
        {
            InitializeComponent();

            InstallerTask = task;
            LastTitle = InstallerTask.Title;
            LastMessageEntry = InstallerTask.ListMessages.LastOrDefault();
            LastUrl = InstallerTask.Hyperlink;
            LastState = InstallerTask.State;
            UpdateTaskView(true);
        }

        

        public void UpdateTaskView(bool force = false)
        {
            if (LastTitle != InstallerTask.Title || force)
                Title.Content = InstallerTask.Title;

            if (LastMessageEntry != InstallerTask.ListMessages.LastOrDefault() || LastUrl != InstallerTask.Hyperlink || force)
            {
                Message.Inlines.Clear();
                if (DisplayOnlyLastError && InstallerTask.State == TaskState.ERROR)
                    Message.Text = string.Join("\r\n", InstallerTask.ListMessages.GetRange(InstallerTask.ListMessages.Count - 2, 2));  
                else if (DisplayOnlyLastCompleted && InstallerTask.State == TaskState.COMPLETED)
                    Message.Text = InstallerTask.ListMessages.LastOrDefault();
                else
                    Message.Text = string.Join("\r\n", InstallerTask.ListMessages);

                if (!string.IsNullOrEmpty(InstallerTask.HyperlinkURL) && !string.IsNullOrEmpty(InstallerTask.Hyperlink))
                {
                    var run = new Run($"\r\n{InstallerTask.Hyperlink}");
                    if (InstallerTask.SetUrlFontSize != -1)
                        run.FontSize = InstallerTask.SetUrlFontSize;
                    if (InstallerTask.SetUrlBold)
                        run.FontWeight = FontWeights.DemiBold;

                    CurrentHyperlink = new Hyperlink(run)
                    {
                        NavigateUri = new Uri($"{InstallerTask.HyperlinkURL}")
                    };

                    Message.Inlines.Add(CurrentHyperlink);
                    this.AddHandler(Hyperlink.RequestNavigateEvent, new RequestNavigateEventHandler(RequestNavigateHandler));
                }
            }

            if (LastState != InstallerTask.State || force)
            {
                if (InstallerTask.State == TaskState.ERROR)
                    BrushCurrent = BrushRed;
                else if (InstallerTask.State == TaskState.COMPLETED)
                    BrushCurrent = BrushGreen;
                else if (InstallerTask.State == TaskState.WAITING)
                    BrushCurrent = BrushOrange;
                else if (InstallerTask.State == TaskState.ACTIVE)
                    BrushCurrent = BrushBlue;
                else
                    BrushCurrent = BrushDefault;

                this.TaskBorder.BorderBrush = BrushCurrent;
            }

            LastTitle = InstallerTask.Title;
            LastMessageEntry = InstallerTask.ListMessages.LastOrDefault();
            LastUrl = InstallerTask.Hyperlink;
            LastState = InstallerTask.State;
        }

        private void RequestNavigateHandler(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                if (InstallerTask.IsUrlCallback)
                    InstallerTask.HyperlinkOnClick?.Invoke();
                else if (InstallerTask.HyperLinkArg == null)
                    Tools.OpenUri(sender, e);
                else
                    Tools.OpenUriArgs(InstallerTask.HyperlinkURL, InstallerTask.HyperLinkArg);

                if (InstallerTask.SetCompletedOnUrl && InstallerTask.State != TaskState.COMPLETED)
                {
                    InstallerTask.State = TaskState.COMPLETED;
                    UpdateTaskView(true);
                }

                if (!InstallerTask.IsUrlCallback)
                    InstallerTask.HyperlinkOnClick?.Invoke();

                if (InstallerTask.DisableLinkAfterClick && CurrentHyperlink != null)
                    CurrentHyperlink.IsEnabled = false;

                e.Handled = true;
            }
            catch (Exception ex)
            {
                InstallerTask.SetError(ex);
            }
        }
    }
}
