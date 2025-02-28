using PilotsDeck.UI.ActionDesignerUI.ViewModels.Address;
using PilotsDeck.UI.ActionDesignerUI.ViewModels.Manipulators;
using PilotsDeck.UI.ActionDesignerUI.Views.Controls;
using System.Windows.Controls;

namespace PilotsDeck.UI.ActionDesignerUI.Views.ManipulatorControls
{
    public partial class ControlTransparency : UserControl
    {
        protected virtual ViewModelManipulator ParentModel { get; }
        protected virtual ViewModelTransparency ViewModel { get; }

        public ControlTransparency(ViewModelManipulator model)
        {
            InitializeComponent();
            ParentModel = model;
            ViewModel = new(model);
            this.DataContext = ViewModel;

            VariableControl.Content = new ControlAddress(new ViewModelVariableAddress(ViewModel, "Monitor Address"));

            ViewModel[nameof(ViewModel.TransparencySetValue)].BindElement(InputTransparencySetValue);
            ViewModel[nameof(ViewModel.TransparencyMinValue)].BindElement(InputTransparencyMinValue);
            ViewModel[nameof(ViewModel.TransparencyMaxValue)].BindElement(InputTransparencyMaxValue);
        }
    }
}
