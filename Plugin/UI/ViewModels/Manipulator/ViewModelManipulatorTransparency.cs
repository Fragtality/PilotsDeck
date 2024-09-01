using PilotsDeck.Actions.Advanced.Manipulators;
using PilotsDeck.Resources.Variables;
using PilotsDeck.Tools;

namespace PilotsDeck.UI.ViewModels.Manipulator
{
    public class ViewModelManipulatorTransparency(ManipulatorTransparency manipulator, ViewModelAction action)
    {
        public ViewModelAction ModelAction { get; set; } = action;
        public ManipulatorTransparency Manipulator { get; set; } = manipulator;
        public bool DynamicTransparency { get { return Manipulator.Settings.DynamicTransparency; } }
        public string TransparencySetValue { get { return Conversion.ToString(Manipulator.Settings.TransparencySetValue); } }
        public string TransparencyAddress { get { return Manipulator.Settings.TransparencyAddress; } }
        public bool IsValidTransparencyAddress { get { return TransparencyVariable?.Type != SimValueType.NONE; } }
        public ManagedVariable TransparencyVariable { get { return Manipulator?.TransparencyVariable; } }
        public string TransparencyMinValue { get { return Conversion.ToString(Manipulator.Settings.TransparencyMinValue); } }
        public string TransparencyMaxValue { get { return Conversion.ToString(Manipulator.Settings.TransparencyMaxValue); } }

        public void SetDynamicTransparency(bool value)
        {
            Manipulator.Settings.DynamicTransparency = value;
            ModelAction.UpdateAction();
        }

        public virtual void SetTransparencySetValue(string input)
        {
            if (Conversion.IsNumberF(input, out float num))
            {
                Manipulator.Settings.TransparencySetValue = num;
                ModelAction.UpdateAction();
            }
        }

        public virtual void SetTransparencyAddress(string input)
        {
            if (input == null)
                return;

            Manipulator.Settings.TransparencyAddress = input;
            ModelAction.UpdateAction();
        }

        public virtual void SetTransparencyMinValue(string input)
        {
            if (Conversion.IsNumberF(input, out float num))
            {
                Manipulator.Settings.TransparencyMinValue = num;
                ModelAction.UpdateAction();
            }
        }

        public virtual void SetTransparencyMaxValue(string input)
        {
            if (Conversion.IsNumberF(input, out float num))
            {
                Manipulator.Settings.TransparencyMaxValue = num;
                ModelAction.UpdateAction();
            }
        }
    }
}
