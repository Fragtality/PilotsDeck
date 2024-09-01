using PilotsDeck.Actions;
using PilotsDeck.Actions.Advanced;
using PilotsDeck.Actions.Advanced.Manipulators;
using PilotsDeck.Tools;
using System.Windows.Media;

namespace PilotsDeck.UI.ViewModels.Manipulator
{
    public class ViewModelManipulator(ElementManipulator manipulator, ViewModelAction action, int elementID, int manipulatorID) : ISelectableItem
    {
        public ViewModelAction ModelAction { get; set; } = action;
        public ElementManipulator Manipulator { get; set; } = manipulator;
        public string Header { get { return ""; } }
        public ELEMENT_MANIPULATOR ManipulatorType { get { return Manipulator.Settings.ManipulatorType; } }
        public string Type { get { return $"{Manipulator.Settings.ManipulatorType}"; } }
        public int ElementID { get; set; } = elementID;
        public int ManipulatorID { get; set; } = manipulatorID;
        public int ConditionID { get { return -1; } }
        public StreamDeckCommand DeckCommandType { get { return (StreamDeckCommand)(-1); } }
        public int CommandID { get { return -1; } }
        public string Name { get { return Manipulator.GetType().Name.Replace("Manipulator", ""); } }
        public bool IsManipulatorColor { get { return Manipulator is ManipulatorColor; } }
        public bool IsManipulatorVisible { get { return Manipulator is ManipulatorVisible; } }
        public bool IsManipulatorIndicator { get { return Manipulator is ManipulatorIndicator; } }
        public bool IsManipulatorTransparency { get { return Manipulator is ManipulatorTransparency; } }
        public bool IsManipulatorRotate { get { return Manipulator is ManipulatorRotate; } }
        public bool IsManipulatorFormat { get { return Manipulator is ManipulatorFormat; } }
        public bool IsManipulatorSizePos { get { return Manipulator is ManipulatorSizePos; } }
        public bool AnyCondition { get { return Manipulator.Settings.AnyCondition; } }
        public bool ResetVisibility { get { return Manipulator.Settings.ResetVisibility; } }
        public string ResetDelay { get { return Conversion.ToString(Manipulator.Settings.ResetDelay); } }
        public Color Color { get { return Color.FromArgb((Manipulator as ManipulatorColor).ConditionalColor.A, (Manipulator as ManipulatorColor).ConditionalColor.R, (Manipulator as ManipulatorColor).ConditionalColor.G, (Manipulator as ManipulatorColor).ConditionalColor.B); } }
        public System.Drawing.Color ColorForms { get { return (Manipulator as ManipulatorColor).ConditionalColor; } }

        public void SetAnyCondition(bool value)
        {
            Manipulator.Settings.AnyCondition = value;
            ModelAction.UpdateAction();
        }

        public void SetColor(System.Drawing.Color color)
        {
            if (Manipulator is ManipulatorColor m)
            {
                m.Settings.SetColor(color);
                ModelAction.UpdateAction();
            }
        }

        public void SetResetVisibility(bool value)
        {
            Manipulator.Settings.ResetVisibility = value;
            ModelAction.UpdateAction();
        }

        public virtual void SetResetDelay(string input)
        {
            if (Conversion.IsNumberI(input, out int num))
            {
                Manipulator.Settings.ResetDelay = num;
                ModelAction.UpdateAction();
            }
        }
    }
}
