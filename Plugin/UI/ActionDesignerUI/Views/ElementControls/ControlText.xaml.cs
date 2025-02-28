using PilotsDeck.UI.ActionDesignerUI.ViewModels.Elements;
using System.Windows.Controls;

namespace PilotsDeck.UI.ActionDesignerUI.Views.ElementControls
{
    public partial class ControlText : UserControl
    {
        public virtual ViewModelText ViewModel { get; }

        public ControlText(ViewModelElement viewModel)
        {
            InitializeComponent();
            ViewModel = new ViewModelText(viewModel);
            this.DataContext = ViewModel;

            ViewModel[nameof(ViewModel.Text)].BindElement(InputElementText);

            FontControl.Content = new ControlFont(viewModel);
        }
    }
}
