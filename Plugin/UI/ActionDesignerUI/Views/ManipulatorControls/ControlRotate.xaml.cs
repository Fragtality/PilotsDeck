using PilotsDeck.UI.ActionDesignerUI.ViewModels.Address;
using PilotsDeck.UI.ActionDesignerUI.ViewModels.Manipulators;
using PilotsDeck.UI.ActionDesignerUI.Views.Controls;
using System.Windows.Controls;

namespace PilotsDeck.UI.ActionDesignerUI.Views.ManipulatorControls
{
    public partial class ControlRotate : UserControl
    {
        protected virtual ViewModelManipulator ParentModel { get; }
        protected virtual ViewModelRotate ViewModel { get; }

        public ControlRotate(ViewModelManipulator model)
        {
            InitializeComponent();
            ParentModel = model;
            ViewModel = new(model);
            this.DataContext = ViewModel;

            VariableControl.Content = new ControlAddress(new ViewModelVariableAddress(ViewModel, "Monitor Address"));

            ViewModel[nameof(ViewModel.RotateToValue)].BindElement(InputRotateToValue);
            ViewModel[nameof(ViewModel.RotateMinValue)].BindElement(InputRotateMinValue);
            ViewModel[nameof(ViewModel.RotateMaxValue)].BindElement(InputRotateMaxValue);
            ViewModel[nameof(ViewModel.RotateAngleStart)].BindElement(InputRotateAngleStart);
            ViewModel[nameof(ViewModel.RotateAngleSweep)].BindElement(InputRotateAngleSweep);
        }
    }
}
