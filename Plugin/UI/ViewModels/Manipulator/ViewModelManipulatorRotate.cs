using PilotsDeck.Actions.Advanced.Manipulators;
using PilotsDeck.Resources.Variables;
using PilotsDeck.Tools;

namespace PilotsDeck.UI.ViewModels.Manipulator
{
    public class ViewModelManipulatorRotate(ManipulatorRotate manipulator, ViewModelAction action)
    {
        public ViewModelAction ModelAction { get; set; } = action;
        public ManipulatorRotate Manipulator { get; set; } = manipulator;
        public string RotateToValue { get { return Conversion.ToString(Manipulator.Settings.RotateToValue); } }
        public bool RotateContinous { get { return Manipulator.Settings.RotateContinous; } }
        public ManagedVariable RotateVariable { get { return Manipulator?.RotateVariable; } }
        public bool IsValidRotateAddress { get { return RotateVariable?.Type != SimValueType.NONE; } }
        public string RotateAddress { get { return Manipulator.Settings.RotateAddress; } }
        public string RotateAngleStart { get { return Conversion.ToString(Manipulator.Settings.RotateAngleStart); } }
        public string RotateAngleSweep { get { return Conversion.ToString(Manipulator.Settings.RotateAngleSweep); } }
        public string RotateMinValue { get { return Conversion.ToString(Manipulator.Settings.RotateMinValue); } }
        public string RotateMaxValue { get { return Conversion.ToString(Manipulator.Settings.RotateMaxValue); } }

        public virtual void SetRotateToValue(string input)
        {
            if (Conversion.IsNumberF(input, out float num))
            {
                Manipulator.Settings.RotateToValue = num;
                ModelAction.UpdateAction();
            }
        }

        public void SetRotateContinous(bool value)
        {
            Manipulator.Settings.RotateContinous = value;
            ModelAction.UpdateAction();
        }

        public virtual void SetRotateAddress(string input)
        {
            if (input == null)
                return;

            Manipulator.Settings.RotateAddress = input;
            ModelAction.UpdateAction();
        }

        public virtual void SetRotateMinValue(string input)
        {
            if (Conversion.IsNumberF(input, out float num))
            {
                Manipulator.Settings.RotateMinValue = num;
                ModelAction.UpdateAction();
            }
        }

        public virtual void SetRotateMaxValue(string input)
        {
            if (Conversion.IsNumberF(input, out float num))
            {
                Manipulator.Settings.RotateMaxValue = num;
                ModelAction.UpdateAction();
            }
        }

        public virtual void SetRotateAngleStart(string input)
        {
            if (Conversion.IsNumberF(input, out float num))
            {
                Manipulator.Settings.RotateAngleStart = num;
                ModelAction.UpdateAction();
            }
        }

        public virtual void SetRotateAngleSweep(string input)
        {
            if (Conversion.IsNumberF(input, out float num))
            {
                Manipulator.Settings.RotateAngleSweep = num;
                ModelAction.UpdateAction();
            }
        }
    }
}
