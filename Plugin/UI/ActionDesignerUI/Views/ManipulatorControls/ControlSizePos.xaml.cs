using PilotsDeck.UI.ActionDesignerUI.ViewModels.Address;
using PilotsDeck.UI.ActionDesignerUI.ViewModels.Manipulators;
using PilotsDeck.UI.ActionDesignerUI.Views.Controls;
using System.Windows.Controls;

namespace PilotsDeck.UI.ActionDesignerUI.Views.ManipulatorControls
{
    public partial class ControlSizePos : UserControl
    {
        protected virtual ViewModelManipulator ParentModel { get; }
        protected virtual ViewModelSizePos ViewModel { get; }

        public ControlSizePos(ViewModelManipulator model)
        {
            InitializeComponent();
            ParentModel = model;
            ViewModel = new(model);
            this.DataContext = ViewModel;

            VariableControl.Content = new ControlAddress(new ViewModelVariableAddress(ViewModel, "Monitor Address"));

            ViewModel[nameof(ViewModel.ValueX)].BindElement(InputValueX);
            ViewModel[nameof(ViewModel.ValueY)].BindElement(InputValueY);
            ViewModel[nameof(ViewModel.ValueW)].BindElement(InputValueW);
            ViewModel[nameof(ViewModel.ValueH)].BindElement(InputValueH);
            ViewModel[nameof(ViewModel.SizePosMinValue)].BindElement(InputSizePosMinValue);
            ViewModel[nameof(ViewModel.SizePosMaxValue)].BindElement(InputSizePosMaxValue);
        }
    }
}
