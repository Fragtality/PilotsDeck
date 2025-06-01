using PilotsDeck.UI.ActionDesignerUI.ViewModels.Manipulators;
using System.Windows.Controls;

namespace PilotsDeck.UI.ActionDesignerUI.Views.ManipulatorControls
{
    public partial class ControlFlash : UserControl
    {
        protected virtual ViewModelManipulator ParentModel { get; }
        protected virtual ViewModelFlash ViewModel { get; }

        public ControlFlash(ViewModelManipulator model)
        {
            InitializeComponent();
            ParentModel = model;
            ViewModel = new(model);
            this.DataContext = ViewModel;

            ViewModel[nameof(ViewModel.FlashInterval)].BindElement(InputFlashInterval);
        }
    }
}
