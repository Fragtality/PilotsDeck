using PilotsDeck.UI.ActionDesignerUI.Clipboard;
using PilotsDeck.UI.ActionDesignerUI.ViewModels.Elements;
using System.Windows.Controls;

namespace PilotsDeck.UI.ActionDesignerUI.Views.ElementControls
{
    public partial class ControlFont : UserControl
    {
        public virtual ViewModelFont ViewModel { get; }
        protected SettingButtonManager ButtonManager { get; }

        public ControlFont(ViewModelElement viewModel)
        {
            InitializeComponent();
            ViewModel = new ViewModelFont(viewModel);
            this.DataContext = ViewModel;

            ButtonManager = new(ViewModel);
            ButtonManager.BindParent(this);

            ViewModel.SelectFontCommand.Bind(LabelFontSelect);
            ButtonManager.BindButton(ButtonFontClipboard, nameof(ViewModel.FontSettings), SettingType.FONT);
        }
    }
}
