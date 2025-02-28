using PilotsDeck.UI.ActionDesignerUI.ViewModels.Manipulators;
using System.Windows.Controls;

namespace PilotsDeck.UI.ActionDesignerUI.Views.ManipulatorControls
{
    public partial class ControlVisible : UserControl
    {
        protected virtual ViewModelManipulator ParentModel { get; }
        protected virtual ViewModelVisible ViewModel { get; }

        public ControlVisible(ViewModelManipulator model)
        {
            InitializeComponent();
            ParentModel = model;
            ViewModel = new(model);
            this.DataContext = ViewModel;

            ViewModel[nameof(ViewModel.ResetDelay)].BindElement(InputResetDelay);
        }
    }
}
