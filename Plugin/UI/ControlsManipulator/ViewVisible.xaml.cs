using PilotsDeck.Tools;
using PilotsDeck.UI.ViewModels.Manipulator;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PilotsDeck.UI.ControlsManipulator
{
    public partial class ViewVisible : UserControl
    {
        public ViewModelManipulator ModelManipulator { get; set; }
        
        public ViewVisible(ViewModelManipulator model)
        {
            InitializeComponent();
            ModelManipulator = model;
            InitializeControls();
        }

        private void InitializeControls()
        {
            CheckboxResetVisibility.IsChecked = ModelManipulator.ResetVisibility;
            InputResetDelay.Text = ModelManipulator.ResetDelay;
        }

        private void CheckboxResetVisibility_Click(object sender, RoutedEventArgs e)
        {
            ModelManipulator.SetResetVisibility(CheckboxResetVisibility.IsChecked == true);
        }

        private void InputResetDelay_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelManipulator.SetResetDelay(InputResetDelay.Text);
        }

        private void InputResetDelay_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelManipulator.SetResetDelay(InputResetDelay.Text);
        }
    }
}
