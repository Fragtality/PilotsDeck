using CFIT.AppFramework.UI.ViewModels;
using PilotsDeck.UI.ActionDesignerUI.Clipboard;
using PilotsDeck.UI.ActionDesignerUI.ViewModels;
using PilotsDeck.UI.ActionDesignerUI.ViewModels.Elements;
using PilotsDeck.UI.ActionDesignerUI.ViewModels.Manipulators;
using System.Collections.Generic;
using System.Windows.Controls;

namespace PilotsDeck.UI.ActionDesignerUI.Views.Controls
{
    public partial class ControlFormat : UserControl
    {
        public virtual ViewModelFormat ViewModel { get; }
        public virtual ViewModelSelector<KeyValuePair<string, string>, string> ViewModelSelector { get; }
        protected SettingButtonManager ButtonManager { get; }

        public ControlFormat(ViewModelElement parent)
        {
            InitializeComponent();
            ViewModel = new(parent.Source.ValueFormat, parent.ModelAction);
            ViewModelSelector = new(ListMappings, ViewModel.ValueMappings);
            ButtonManager = new(ViewModel);
            InitializeControl();
        }

        public ControlFormat(ViewModelManipulator parent)
        {
            InitializeComponent();
            ViewModel = new(parent.Source.ConditionalFormat, parent.ModelAction);
            ViewModelSelector = new(ListMappings, ViewModel.ValueMappings);
            ButtonManager = new(ViewModel);
            InitializeControl();
        }

        protected virtual void InitializeControl()
        {
            this.DataContext = ViewModel;
            ButtonManager.BindParent(this);

            ViewModel[nameof(ViewModel.Scalar)].BindElement(InputScalar);
            ViewModel[nameof(ViewModel.Offset)].BindElement(InputOffset);
            ViewModel[nameof(ViewModel.Round)].BindElement(InputRound);
            ViewModel[nameof(ViewModel.Digits)].BindElement(InputDigits);
            ViewModel[nameof(ViewModel.DigitsTrailing)].BindElement(InputDigitsTrailing);
            ViewModel[nameof(ViewModel.SubIndex)].BindElement(InputSubIndex);
            ViewModel[nameof(ViewModel.SubLength)].BindElement(InputSubLength);
            ViewModel[nameof(ViewModel.FormatString)].BindElement(InputFormat);

            ViewModelSelector.BindAddUpdateButton(ButtonAddMapping, ImageAddUpdateMapping);
            ViewModelSelector.BindRemoveButton(ButtonRemoveMapping);
            ViewModelSelector.BindMember(InputMappingValue, "Key");
            ViewModelSelector.BindMember(InputMappingString, "Value", null, null, true);

            ButtonManager.BindButton(ButtonCopyPasteMapping, nameof(ViewModel.ValueMappingsCopy), SettingType.VALUEMAP);
        }
    }
}
