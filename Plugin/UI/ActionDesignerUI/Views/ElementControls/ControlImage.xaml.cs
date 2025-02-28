using PilotsDeck.UI.ActionDesignerUI.ViewModels.Elements;
using System.Windows.Controls;

namespace PilotsDeck.UI.ActionDesignerUI.Views.ElementControls
{
    public partial class ControlImage : UserControl
    {
        public virtual ViewModelImage ViewModel { get; }

        public ControlImage(ViewModelElement viewModel)
        {
            InitializeComponent();
            ViewModel = new ViewModelImage(viewModel);
            this.DataContext = ViewModel;

            ViewModel.SetImageCommand.Bind(InputImage);
        }
    }
}
