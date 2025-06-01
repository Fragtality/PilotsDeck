using PilotsDeck.Actions.Advanced;
using PilotsDeck.UI.ActionDesignerUI.ViewModels.Manipulators;
using PilotsDeck.UI.ActionDesignerUI.Views.Controls;
using PilotsDeck.UI.ActionDesignerUI.Views.ManipulatorControls;
using System.Windows;
using System.Windows.Controls;

namespace PilotsDeck.UI.ActionDesignerUI.Views
{
    public partial class ViewManipulator : UserControl
    {
        public ViewModelManipulator ModelManipulator { get; set; }
        public Window ParentWindow { get; set; }

        public ViewManipulator(ViewModelManipulator model, Window parent)
        {
            InitializeComponent();
            ModelManipulator = model;
            this.DataContext = ModelManipulator;
            ParentWindow = parent;

            ModelManipulator[nameof(ModelManipulator.Name)].BindElement(InputName);
            InitializeContentControl();
        }

        protected virtual void InitializeContentControl()
        {
            if (ModelManipulator.ManipulatorType == ELEMENT_MANIPULATOR.VISIBLE)
                ManipulatorView.Content = new ControlVisible(ModelManipulator);
            else if (ModelManipulator.ManipulatorType == ELEMENT_MANIPULATOR.COLOR)
                ManipulatorView.Content = new ControlColor(ModelManipulator);
            else if (ModelManipulator.ManipulatorType == ELEMENT_MANIPULATOR.ROTATE)
                ManipulatorView.Content = new ControlRotate(ModelManipulator);
            else if (ModelManipulator.ManipulatorType == ELEMENT_MANIPULATOR.TRANSPARENCY)
                ManipulatorView.Content = new ControlTransparency(ModelManipulator);
            else if (ModelManipulator.ManipulatorType == ELEMENT_MANIPULATOR.SIZEPOS)
                ManipulatorView.Content = new ControlSizePos(ModelManipulator);
            else if (ModelManipulator.ManipulatorType == ELEMENT_MANIPULATOR.INDICATOR)
                ManipulatorView.Content = new ControlIndicator(ModelManipulator);
            else if (ModelManipulator.ManipulatorType == ELEMENT_MANIPULATOR.FORMAT)
                ManipulatorView.Content = new ControlFormat(ModelManipulator);
            else if (ModelManipulator.ManipulatorType == ELEMENT_MANIPULATOR.FLASH)
                ManipulatorView.Content = new ControlFlash(ModelManipulator);
        }
    }
}
