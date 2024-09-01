using PilotsDeck.Actions;
using PilotsDeck.Tools;
using PilotsDeck.UI.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PilotsDeck.UI
{
    public partial class ViewActionDelay : UserControl
    {
        protected ViewModelAction ModelAction { get; set; }
        protected StreamDeckCommand DeckType { get; set; }

        public ViewActionDelay(ViewModelAction model, StreamDeckCommand type)
        {
            ModelAction = model;
            DeckType = type;
            InitializeComponent();
            InputActionDelay.Text = ModelAction.GetInterActionDelay(DeckType);
        }

        private void InputActionDelay_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelAction.SetInterActionDelay(DeckType, InputActionDelay.Text);
        }

        private void InputActionDelay_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelAction.SetInterActionDelay(DeckType, InputActionDelay.Text);
        }
    }
}
