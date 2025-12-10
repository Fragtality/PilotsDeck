using PilotsDeck.UI.ActionDesignerUI.Clipboard;
using PilotsDeck.UI.ActionDesignerUI.ColorStore;
using PilotsDeck.UI.ActionDesignerUI.ViewModels.Elements;
using PilotsDeck.UI.ActionDesignerUI.Views.ElementControls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PilotsDeck.UI.ActionDesignerUI.Views
{
    public partial class ViewElement : UserControl
    {
        public ViewModelElement ModelElement { get; set; }
        public Window ParentWindow { get; set; }
        protected SettingButtonManager ButtonManager { get; }
        protected SolidColorBrush AlignmentBorderSelected { get; set; } = SystemColors.HighlightBrush;
        protected SolidColorBrush AlignmentBorder { get; set; } = SystemColors.WindowFrameBrush;

        public ViewElement(ViewModelElement model, Window parent)
        {
            InitializeComponent();

            ModelElement = model;
            this.DataContext = ModelElement;
            ParentWindow = parent;

            ButtonManager = new(ModelElement);
            ButtonManager.BindParent(this);

            InitializeBindings();
            InitializeSettingClipboard();
            InitializeContentControl();
        }

        protected virtual void InitializeBindings()
        {
            ColorStoreManager.BindColorLabel(LabelColorSelect, ParentWindow, (c) => ModelElement.Color = c);
            ModelElement[nameof(ModelElement.PosX)].BindElement(InputPosX);
            ModelElement[nameof(ModelElement.PosY)].BindElement(InputPosY);
            ModelElement[nameof(ModelElement.Width)].BindElement(InputWidth);
            ModelElement[nameof(ModelElement.Height)].BindElement(InputHeight);
            ModelElement[nameof(ModelElement.Rotation)].BindElement(InputRotation);
            ModelElement[nameof(ModelElement.Transparency)].BindElement(InputTransparency);
            ModelElement[nameof(ModelElement.Name)].BindElement(InputName);
        }

        protected virtual void InitializeSettingClipboard()
        {
            ButtonManager.BindButton(ButtonColorClipboard, nameof(ModelElement.Color), SettingType.COLOR);
            ButtonManager.BindButton(ButtonPosClipboard, nameof(ModelElement.Position), SettingType.POS);
            ButtonManager.BindButton(ButtonSizeClipboard, nameof(ModelElement.Size), SettingType.SIZE);
        }

        protected virtual void InitializeContentControl()
        {
            if (ModelElement.IsImage)
                OptionView.Content = new ControlImage(ModelElement);
            else if (ModelElement.IsPrimitive)
                OptionView.Content = new ControlPrimitive(ModelElement);
            else if (ModelElement.IsText)
                OptionView.Content = new ControlText(ModelElement);
            else if (ModelElement.IsValueElement)
                OptionView.Content = new ControlValue(ModelElement);
            else if (ModelElement.IsGauge)
                OptionView.Content = new ControlGauge(ModelElement);
        }
    }
}
