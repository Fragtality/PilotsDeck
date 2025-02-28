using PilotsDeck.UI.ActionDesignerUI.Clipboard;
using System.Drawing;

namespace PilotsDeck.UI.ActionDesignerUI.ViewModels.Manipulators
{
    public partial class ViewModelColor : ViewModelManipulator
    {
        protected virtual ViewModelManipulator ParentModel { get; }

        public ViewModelColor(ViewModelManipulator viewModel) : base(viewModel.Source, viewModel.ModelAction)
        {
            ParentModel = viewModel;
            CopyPasteInterface.BindProperty(nameof(Color), SettingType.COLOR);
        }

        protected virtual void SetColor(Color color)
        {
            Source.SetColor(color);
            UpdateAction();
            OnPropertyChanged(nameof(Color));
            OnPropertyChanged(nameof(HtmlColor));
            ModelAction.NotifyTreeRefresh();
        }

        public virtual Color Color { get => Source.GetColor(); set => SetColor(value); }
        public virtual string HtmlColor => Source.Color;
    }
}
