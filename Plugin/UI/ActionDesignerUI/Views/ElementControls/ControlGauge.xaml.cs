using CFIT.AppFramework.UI.ViewModels;
using PilotsDeck.Actions.Advanced.SettingsModel;
using PilotsDeck.UI.ActionDesignerUI.Clipboard;
using PilotsDeck.UI.ActionDesignerUI.ColorStore;
using PilotsDeck.UI.ActionDesignerUI.ViewModels.Address;
using PilotsDeck.UI.ActionDesignerUI.ViewModels.Elements;
using PilotsDeck.UI.ActionDesignerUI.Views.Controls;
using System.Windows.Controls;
using System.Windows.Media;

namespace PilotsDeck.UI.ActionDesignerUI.Views.ElementControls
{
    public partial class ControlGauge : UserControl
    {
        public virtual ViewModelGauge ViewModel { get; }
        public virtual ViewModelSelector<ColorRange, ColorRange> ViewModelRanges { get; }
        public virtual ViewModelSelector<IMarkerDefinition, IMarkerDefinition> ViewModelMarkers { get; }
        protected SettingButtonManager ButtonManager { get; }

        public ControlGauge(ViewModelElement viewModel)
        {
            InitializeComponent();
            ViewModel = new ViewModelGauge(viewModel);
            ViewModelRanges = new(ListRanges, ViewModel.ColorRanges);
            ViewModelMarkers = new(ListMarker, ViewModel.GaugeMarkers) { GetTransformedSelection = false };
            this.DataContext = ViewModel;
            ButtonManager = new(ViewModel);

            VariableControl.Content = new ControlAddress(new ViewModelVariableAddress(ViewModel, "Monitor Address"));
            InitializeControl();
        }

        protected virtual void InitializeControl()
        {            
            this.Loaded += Control_Loaded;
            ButtonManager.BindParent(this);

            ViewModel[nameof(ViewModel.GaugeValueMin)]?.BindElement(InputValueMin);
            ViewModel[nameof(ViewModel.GaugeValueMax)]?.BindElement(InputValueMax);
            ViewModel[nameof(ViewModel.GaugeValueScale)]?.BindElement(InputValueScale);
            ViewModel[nameof(ViewModel.GaugeAngleStart)]?.BindElement(InputAngleStart);
            ViewModel[nameof(ViewModel.GaugeAngleSweep)]?.BindElement(InputAngleSweep);

            ViewModelRanges.BindAddUpdateButton(ButtonAddRange, ImageAddUpdateRange);
            ViewModelRanges.BindRemoveButton(ButtonRemoveRange);

            ViewModelRanges.BindMemberIndex(InputRangeStart, nameof(ColorRange.Range), 0);
            ViewModelRanges.BindMemberIndex(InputRangeEnd, nameof(ColorRange.Range), 1);
            ViewModelRanges.BindMember(LabelRangeColor, nameof(ColorRange.Color), Label.BackgroundProperty, Brushes.White);
            ColorStoreManager.BindColorLabel(LabelRangeColor, ViewModel.ModelAction.WindowInstance, (c) => ViewModel.ColorRanges[nameof(ColorRange.Color)].SetValueIn(System.Drawing.ColorTranslator.ToHtml(c)));
            ViewModelRanges.BindTextElement(LabelRangeColor, nameof(ColorRange.Color), "Color");
            ButtonManager.BindButton(ButtonCopyPasteRanges, nameof(ViewModel.ColorRangesCopy), SettingType.GAUGEMAP);

            ViewModelMarkers.BindAddUpdateButton(ButtonAddMarker, ImageAddUpdateMarker);
            ViewModelMarkers.BindRemoveButton(ButtonRemoveMarker);
            ViewModelMarkers.BindMember(InputMarkerPos, nameof(IMarkerDefinition.Position));
            ViewModelMarkers.BindMember(InputMarkerSize, nameof(IMarkerDefinition.Size));
            ViewModelMarkers.BindMember(InputMarkerHeight, nameof(IMarkerDefinition.Height));
            ViewModelMarkers.BindMember(InputMarkerOffset, nameof(IMarkerDefinition.Offset));
            ViewModelMarkers.BindMember(LabelMarkerColor, nameof(IMarkerDefinition.Color), Label.BackgroundProperty, Brushes.White);
            ColorStoreManager.BindColorLabel(LabelMarkerColor, ViewModel.ModelAction.WindowInstance, (c) => ViewModel.GaugeMarkers[nameof(IMarkerDefinition.Color)].SetValueIn(System.Drawing.ColorTranslator.ToHtml(c)));
            ViewModelMarkers.BindTextElement(LabelMarkerColor, nameof(IMarkerDefinition.Color), "Color");
            ButtonManager.BindButton(ButtonCopyPasteMarker, nameof(ViewModel.GaugeMarkersCopy), SettingType.GAUGEMARKER);
        }

        protected virtual void Control_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            ComboGaugeType.SelectionChanged += (_, _) => SetGaugeType();
        }

        protected virtual void SetGaugeType()
        {
            if (ComboGaugeType?.SelectedIndex == 1)
                ViewModel.GaugeIsArc = true;
            else
                ViewModel.GaugeIsArc = false;
        }
    }
}
