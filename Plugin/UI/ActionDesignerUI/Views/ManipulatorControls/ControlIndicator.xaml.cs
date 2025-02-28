using PilotsDeck.UI.ActionDesignerUI.Clipboard;
using PilotsDeck.UI.ActionDesignerUI.ViewModels.Address;
using PilotsDeck.UI.ActionDesignerUI.ViewModels.Manipulators;
using PilotsDeck.UI.ActionDesignerUI.Views.Controls;
using System.Windows.Controls;

namespace PilotsDeck.UI.ActionDesignerUI.Views.ManipulatorControls
{
    public partial class ControlIndicator : UserControl
    {
        protected virtual ViewModelManipulator ParentModel { get; }
        protected virtual ViewModelIndicator ViewModel { get; }
        protected virtual SettingButtonManager ButtonManager { get; }

        public ControlIndicator(ViewModelManipulator model)
        {
            InitializeComponent();
            ParentModel = model;
            ViewModel = new(model);
            this.DataContext = ViewModel;

            VariableControl.Content = new ControlAddress(new ViewModelVariableAddress(ViewModel, "Indicator Address"));

            ButtonManager = new(ViewModel);
            ButtonManager.BindButton(ButtonIndicatorColorClipboard, nameof(ViewModel.IndicatorColor), SettingType.COLOR);
            ButtonManager.BindParent(this);

            ColorStore.BindColorLabel(LabelColorSelect, (c) => ViewModel.IndicatorColor = c);
            ViewModel.SetImageCommand.Bind(InputImage);

            ViewModel[nameof(ViewModel.IndicatorScale)].BindElement(InputScale);
            ViewModel[nameof(ViewModel.IndicatorLineSize)].BindElement(InputLineSize);
            ViewModel[nameof(ViewModel.IndicatorSize)].BindElement(InputSize);
            ViewModel[nameof(ViewModel.IndicatorOffset)].BindElement(InputOffset);
        }
    }
}
