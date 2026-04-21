using System.Windows.Controls;

namespace PilotsDeck.UI.DeveloperUI
{
    public partial class ViewSettings : UserControl, IView
    {
        public virtual ModelConfig ViewModel { get; } = new(App.Configuration);

        public ViewSettings()
        {
            InitializeComponent();
            this.DataContext = ViewModel;
        }

        public void Start()
        {

        }

        public void Stop()
        {

        }
    }
}
