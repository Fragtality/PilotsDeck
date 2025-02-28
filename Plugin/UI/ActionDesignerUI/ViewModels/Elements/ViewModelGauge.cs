using PilotsDeck.Actions.Advanced.SettingsModel;
using PilotsDeck.UI.ActionDesignerUI.Clipboard;
using System.Collections.Generic;

namespace PilotsDeck.UI.ActionDesignerUI.ViewModels.Elements
{
    public partial class ViewModelGauge : ViewModelElement
    {
        protected virtual ViewModelElement ParentModel { get; }

        public ViewModelGauge(ViewModelElement viewModel) : base(viewModel.Source, viewModel.ModelAction)
        {
            ParentModel = viewModel;
            ColorRanges = new(Source.GaugeColorRanges);
            GaugeMarkers = new(ParentModel);
            SubscribeCollection(ColorRanges);
            SubscribeCollection(GaugeMarkers);
            CopyPasteInterface.BindProperty(nameof(ColorRangesCopy), SettingType.GAUGEMAP);
            CopyPasteInterface.BindProperty(nameof(GaugeMarkersCopy), SettingType.GAUGEMARKER);
            InitializeMemberBindings();
        }

        protected override void InitializeMemberBindings()
        {
            CreateMemberNumberBinding<float>(nameof(GaugeValueMin), "0");
            CreateMemberNumberBinding<float>(nameof(GaugeValueMax), "100");
            CreateMemberNumberBinding<float>(nameof(GaugeValueScale), "1");
            CreateMemberNumberBinding<float>(nameof(GaugeAngleStart));
            CreateMemberNumberBinding<float>(nameof(GaugeAngleSweep));
        }

        public virtual bool GaugeIsArc { get => GetSourceValue<bool>(); set { SetModelValue<bool>(value); ModelAction.NotifyTreeRefresh(); } }
        public virtual float GaugeValueMin { get => GetSourceValue<float>(); set => SetModelValue<float>(value); }
        public virtual float GaugeValueMax { get => GetSourceValue<float>(); set => SetModelValue<float>(value); }
        public virtual float GaugeValueScale { get => GetSourceValue<float>(); set => SetModelValue<float>(value); }
        public virtual float GaugeAngleStart { get => GetSourceValue<float>(); set => SetModelValue<float>(value); }
        public virtual float GaugeAngleSweep { get => GetSourceValue<float>(); set => SetModelValue<float>(value); }
        public virtual bool UseGaugeDynamicSize { get => GetSourceValue<bool>(); set => SetModelValue<bool>(value); }
        public virtual bool GaugeFixedRanges { get => GetSourceValue<bool>(); set => SetModelValue<bool>(value); }
        public virtual bool GaugeFixedMarkers { get => GetSourceValue<bool>(); set => SetModelValue<bool>(value); }
        public virtual bool GaugeRevereseDirection { get => GetSourceValue<bool>(); set => SetModelValue<bool>(value); }

        public override string Address { get => Source.GaugeSizeAddress; set => SetModelValue<string>(value, null, null, nameof(Source.GaugeSizeAddress)); }


        public virtual ViewModelColorRanges ColorRanges { get; }
        public virtual ICollection<ColorRange> ColorRangesCopy
        {
            get { return Source.GaugeColorRanges; }
            set
            {
                CopyToModelList<ColorRange>(value, (r) => new(r), nameof(Source.GaugeColorRanges));
                NotifyPropertyChanged(nameof(ColorRanges));
                ColorRanges.NotifyCollectionChanged();
            }
        }

        public virtual ViewModelGaugeMarkers GaugeMarkers { get; }
        public virtual GaugeMarkerSettings GaugeMarkersCopy
        {
            get
            {
                return new GaugeMarkerSettings()
                {
                    GaugeMarkers = Source.GaugeMarkers,
                    GaugeRangeMarkers = Source.GaugeRangeMarkers
                };
            }
            set
            {
                CopyToModelList<MarkerDefinition>(value?.GaugeMarkers, (m) => new(m), nameof(Source.GaugeMarkers));
                CopyToModelList<MarkerRangeDefinition>(value?.GaugeRangeMarkers, (m) => new(m), nameof(Source.GaugeRangeMarkers));
                NotifyPropertyChanged(nameof(GaugeMarkers));
                GaugeMarkers.NotifyCollectionChanged();
            }
        }
    }
}
