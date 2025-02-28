using PilotsDeck.UI.ActionDesignerUI.ViewModels.Elements;
using System.Windows.Controls;

namespace PilotsDeck.UI.ActionDesignerUI.Views.ElementControls
{
    public partial class ControlPrimitive : UserControl
    {
        public virtual ViewModelPrimitive ViewModel { get; }

        public ControlPrimitive(ViewModelElement viewModel)
        {
            InitializeComponent();
            ViewModel = new ViewModelPrimitive(viewModel);
            this.DataContext = ViewModel;

            ViewModel[nameof(ViewModel.LineSize)].BindElement(InputLineSize);
        }

    }
}
