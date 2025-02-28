using PilotsDeck.UI.ActionDesignerUI.ViewModels.Address;
using PilotsDeck.UI.ActionDesignerUI.ViewModels.Elements;
using PilotsDeck.UI.ActionDesignerUI.Views.Controls;
using System.Windows.Controls;

namespace PilotsDeck.UI.ActionDesignerUI.Views.ElementControls
{
    public partial class ControlValue : UserControl
    {
        protected virtual ViewModelElement ParentModel { get; }

        public ControlValue(ViewModelElement viewModel)
        {
            InitializeComponent();
            ParentModel = viewModel;
            this.DataContext = ParentModel;

            VariableControl.Content = new ControlAddress(new ViewModelVariableAddress(ParentModel, "Variable Address"));
            FormatControl.Content = new ControlFormat(viewModel);
            FontControl.Content = new ControlFont(viewModel);
        }
    }
}
