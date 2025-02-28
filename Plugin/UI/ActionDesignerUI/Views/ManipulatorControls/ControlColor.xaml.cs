using PilotsDeck.UI.ActionDesignerUI.Clipboard;
using PilotsDeck.UI.ActionDesignerUI.ViewModels.Manipulators;
using System.Windows.Controls;

namespace PilotsDeck.UI.ActionDesignerUI.Views.ManipulatorControls
{
    public partial class ControlColor : UserControl
    {
        protected virtual ViewModelManipulator ParentModel { get; }
        protected virtual ViewModelColor ViewModel { get; }
        protected virtual SettingButtonManager ButtonManager { get; }

        public ControlColor(ViewModelManipulator model)
        {
            InitializeComponent();
            ParentModel = model;
            ViewModel = new(model);
            this.DataContext = ViewModel;

            ButtonManager = new(ViewModel);
            ButtonManager.BindButton(ButtonColorClipboard, nameof(ViewModel.Color), SettingType.COLOR);
            ButtonManager.BindParent(this);

            ColorStore.BindColorLabel(LabelColor, (c) => ViewModel.Color = c);
        }
    }
}
