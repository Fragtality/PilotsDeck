using CFIT.AppFramework.UI.Validations;
using CFIT.AppFramework.UI.ValueConverter;
using CFIT.AppFramework.UI.ViewModels.Commands;
using CFIT.AppTools;
using CommunityToolkit.Mvvm.Input;
using PilotsDeck.Actions.Advanced;
using PilotsDeck.Actions.Advanced.SettingsModel;
using PilotsDeck.Plugin.Render;
using PilotsDeck.UI.ActionDesignerUI.Clipboard;
using PilotsDeck.UI.ActionDesignerUI.ViewModels.Address;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;

namespace PilotsDeck.UI.ActionDesignerUI.ViewModels.Elements
{
    public partial class ViewModelElement : ViewModelBaseExtension<ModelDisplayElement>, IModelAddress
    {
        public virtual DISPLAY_ELEMENT ElementType { get => Source.ElementType; }
        public virtual RelayCommand<string> IncreaseCommand { get; protected set; }
        public virtual RelayCommand<string> DecreaseCommand { get; protected set; }
        public virtual CommandWrapper IncreaseTransparencyCommand { get; }
        public virtual CommandWrapper DecreaseTransparencyCommand { get; }
        protected virtual float StepTransparency { get; set; } = 0.1f;
        

        public ViewModelElement(ModelDisplayElement source, ViewModelAction parent) : base(source, parent)
        {
            IncreaseCommand = new RelayCommand<string>((propertyName) => StepModelProperty<float>(1, [], null, propertyName));
            DecreaseCommand = new RelayCommand<string>((propertyName) => StepModelProperty<float>(-1, [], null, propertyName));

            IncreaseTransparencyCommand = new(() => StepModelProperty<float>(StepTransparency, [0.0f, 1.0f], (value) => MathF.Round(value, 1), nameof(Transparency)),
                                              () => { return Transparency + StepTransparency <= 1.0f; });
            IncreaseTransparencyCommand.Subscribe(this);
            
            DecreaseTransparencyCommand = new(() => StepModelProperty<float>(StepTransparency * -1.0, [0.0f, 1.0f], (value) => MathF.Round(value, 1), nameof(Transparency)),
                                              () => { return Transparency - StepTransparency <= 1.0f; });
            DecreaseTransparencyCommand.Subscribe(this);
        }

        protected override void InitializeModel()
        {
            CopyPasteInterface.BindProperty(nameof(Color), SettingType.COLOR);
            CopyPasteInterface.BindProperty(nameof(Position), SettingType.POS, nameof(Position), nameof(PosX), nameof(PosY));
            CopyPasteInterface.BindProperty(nameof(Size), SettingType.SIZE, nameof(Size), nameof(Width), nameof(Height));
        }

        protected override void InitializeMemberBindings()
        {
            CreateMemberNumberBinding<float>(nameof(PosX));
            CreateMemberNumberBinding<float>(nameof(PosY));
            CreateMemberNumberBinding<float>(nameof(Width));
            CreateMemberNumberBinding<float>(nameof(Height));
            CreateMemberNumberBinding<float>(nameof(Rotation));
            CreateMemberBinding<float, string>(nameof(Transparency), new RealInvariantConverter("1"), new ValidationRuleRange<float>(0.0f, 1.0f));
            CreateMemberBinding<string, string>(nameof(Name), new NoneConverter(), new ValidationRuleNull());
        }

        protected virtual void SetColor(Color color)
        {
            Source.SetColor(color);
            UpdateAction();
            OnPropertyChanged(nameof(Color));
            OnPropertyChanged(nameof(HtmlColor));
        }

        [RelayCommand]
        protected virtual void ResetPosition()
        {
            ResetPoint(nameof(Source.Position));
            OnPropertyChanged(nameof(PosX));
            OnPropertyChanged(nameof(PosY));
        }

        [RelayCommand]
        protected virtual void ResetSize()
        {
            ResetPoint(nameof(Source.Size));
            OnPropertyChanged(nameof(Width));
            OnPropertyChanged(nameof(Height));
        }

        [RelayCommand]
        protected virtual void ResetRotation()
        {
            Rotation = 0;
        }

        [RelayCommand]
        protected virtual void ResetTransparency()
        {
            Transparency = 1;
        }

        protected virtual void ResetPoint(string propertyName)
        {
            float[] pos = [0.0f, 0.0f];
            SetModelValue<float[]>(pos, null, null, propertyName);
        }

        public virtual bool IsGauge { get => ElementType == DISPLAY_ELEMENT.GAUGE; }
        public virtual bool IsImage { get => ElementType == DISPLAY_ELEMENT.IMAGE; }
        public virtual bool IsPrimitive { get => ElementType == DISPLAY_ELEMENT.PRIMITIVE; }
        public virtual bool IsText { get => ElementType == DISPLAY_ELEMENT.TEXT; }
        public virtual bool IsValueElement { get => ElementType == DISPLAY_ELEMENT.VALUE; }
        public virtual bool ElementHasColor { get => ((IsImage && Source.DrawImageBackground) || !IsImage); }
        public virtual Color Color { get => Source.GetColor(); set => SetColor(value); }
        public virtual string HtmlColor => Source.Color;
        public virtual Visibility VisibilityPosition { get => !IsPrimitive || Source.PrimitiveType != PrimitiveType.LINE ? Visibility.Visible : Visibility.Collapsed; }
        public virtual Visibility VisibilityStartEnd { get => IsPrimitive && Source.PrimitiveType == PrimitiveType.LINE ? Visibility.Visible : Visibility.Collapsed; }
        public virtual float[] Position { get => GetSourceValue<float[]>(); set => SetModelValue<float[]>(value); }
        public virtual float PosX
        {
            get => Source.GetPosition().X;
            set => SetModelArray<float>(value, 0, null, null, nameof(ModelDisplayElement.Position));
        }
        public virtual float PosY
        {
            get => Source.GetPosition().Y;
            set => SetModelArray<float>(value, 1, null, null, nameof(ModelDisplayElement.Position));
        }

        public virtual CenterType Center { get => GetSourceValue<CenterType>(); set => SetModelValue<CenterType>(value); }
        public virtual Dictionary<CenterType, string> CenterTypes { get; } = ViewModelHelper.CenterTypes;
        public virtual float[] Size { get => GetSourceValue<float[]>(); set => SetModelValue<float[]>(value); }
        public virtual float Width
        {
            get => Source.GetSize().X;
            set => SetModelArray<float>(value, 0, null, null, nameof(ModelDisplayElement.Size));
        }
        public virtual float Height
        {
            get => Source.GetSize().Y;
            set => SetModelArray<float>(value, 1, null, null, nameof(ModelDisplayElement.Size));
        }
        public virtual string CanvasString { get => $"Canvas Size: {ModelAction.Action.CanvasSize.X} x {ModelAction.Action.CanvasSize.Y}"; }
        public virtual ScaleType Scale { get => GetSourceValue<ScaleType>(); set => SetModelValue<ScaleType>(value); }
        public virtual Dictionary<ScaleType, string> ScaleTypes { get; } = ViewModelHelper.ScaleTypes;
        public virtual float Rotation { get => GetSourceValue<float>(); set => SetModelValue<float>(value); }
        public virtual float Transparency { get => GetSourceValue<float>(); set => SetModelValue<float>(value); }

        public virtual string SelectName()
        {
            if (!string.IsNullOrWhiteSpace(Source.Name))
                return Source.Name;
            else if (ElementType == DISPLAY_ELEMENT.TEXT && !string.IsNullOrWhiteSpace(Source?.Text))
                return Source.Text.Compact();
            else if (ElementType == DISPLAY_ELEMENT.VALUE && !string.IsNullOrWhiteSpace(Source?.ValueAddress))
                return Source.ValueAddress.Compact();
            else if (ElementType == DISPLAY_ELEMENT.PRIMITIVE)
                return ViewModelHelper.PrimitiveTypes[Source.PrimitiveType];
            else if (ElementType == DISPLAY_ELEMENT.GAUGE)
                return $"{(Source.GaugeIsArc ? "Arc" : "Bar")}";
            else if (ElementType == DISPLAY_ELEMENT.IMAGE && !string.IsNullOrWhiteSpace(Source?.Image))
            {
                int idx = Source.Image.LastIndexOf('/');
                if (idx != -1 && idx + 1 < Source.Image.Length)
                    return Source.Image[(idx + 1)..].Replace(AppConfiguration.ImageExtension, "").Compact();
                else
                    return Source.Image.Replace(AppConfiguration.ImageExtension, "").Compact();
            }
            else
                return ViewModelHelper.ElementTypes[Source.ElementType];
        }

        public override string DisplayName { get => SelectName(); } 
        public override string Name { get => GetSourceValue<string>(); set { SetModelValue<string>(value); ModelAction.NotifyTreeRefresh(); } }
        public virtual string Address { get => Source.ValueAddress; set => SetModelValue<string>(value, null, null, nameof(Source.ValueAddress)); }
    }
}
