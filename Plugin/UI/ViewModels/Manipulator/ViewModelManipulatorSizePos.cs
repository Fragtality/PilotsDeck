using PilotsDeck.Actions.Advanced.Manipulators;
using PilotsDeck.Resources.Variables;
using PilotsDeck.Tools;

namespace PilotsDeck.UI.ViewModels.Manipulator
{
    public class ViewModelManipulatorSizePos(ManipulatorSizePos manipulator, ViewModelAction action)
    {
        public ViewModelAction ModelAction { get; set; } = action;
        public ManipulatorSizePos Manipulator { get; set; } = manipulator;
        public ManagedVariable SizePosVariable { get { return Manipulator.SizePosVariable; } }
        public bool IsValidSizePosAddress { get { return SizePosVariable?.Type != SimValueType.NONE; } }
        public bool ChangeX { get { return Manipulator.Settings.ChangeX; } }
        public string ValueX { get { return Conversion.ToString(Manipulator.Settings.ValueX); } }
        public bool ChangeY { get { return Manipulator.Settings.ChangeY; } }
        public string ValueY { get { return Conversion.ToString(Manipulator.Settings.ValueY); } }
        public bool ChangeW { get { return Manipulator.Settings.ChangeW; } }
        public string ValueW { get { return Conversion.ToString(Manipulator.Settings.ValueW); } }
        public bool ChangeH { get { return Manipulator.Settings.ChangeH; } }
        public string ValueH { get { return Conversion.ToString(Manipulator.Settings.ValueH); } }
        public bool ChangeSizePosDynamic { get { return Manipulator.Settings.ChangeSizePosDynamic; } }
        public string SizePosAddress { get { return Manipulator.Settings.SizePosAddress; } }
        public string SizePosMinValue { get { return Conversion.ToString(Manipulator.Settings.SizePosMinValue); } }
        public string SizePosMaxValue { get { return Conversion.ToString(Manipulator.Settings.SizePosMaxValue); } }

        public void SetValue(string component, string value)
        {
            if (!Conversion.IsNumberF(value, out float numValue))
                return;

            if (component == "X")
            {
                Manipulator.Settings.ValueX = numValue;
                ModelAction.UpdateAction();
            }
            else if (component == "Y")
            {
                Manipulator.Settings.ValueY = numValue;
                ModelAction.UpdateAction();
            }
            else if (component == "W")
            {
                Manipulator.Settings.ValueW = numValue;
                ModelAction.UpdateAction();
            }
            else if (component == "H")
            {
                Manipulator.Settings.ValueH = numValue;
                ModelAction.UpdateAction();
            }
        }

        public void SetChange(string component, bool value)
        {
            if (component == "X")
            {
                Manipulator.Settings.ChangeX = value;
                ModelAction.UpdateAction();
            }
            else if (component == "Y")
            {
                Manipulator.Settings.ChangeY = value;
                ModelAction.UpdateAction();
            }
            else if (component == "W")
            {
                Manipulator.Settings.ChangeW = value;
                ModelAction.UpdateAction();
            }
            else if (component == "H")
            {
                Manipulator.Settings.ChangeH = value;
                ModelAction.UpdateAction();
            }
        }

        public void SetDynamic(bool value)
        {
            Manipulator.Settings.ChangeSizePosDynamic = value;
            ModelAction.UpdateAction();
        }

        public void SetAddress(string address)
        {
            if (address == null)
                return;

            Manipulator.Settings.SizePosAddress = address;
            ModelAction.UpdateAction();
        }

        public void SetMinValue(string value)
        {
            if (!Conversion.IsNumberF(value, out float numValue))
                return;

            Manipulator.Settings.SizePosMinValue = numValue;
            ModelAction.UpdateAction();
        }

        public void SetMaxValue(string value)
        {
            if (!Conversion.IsNumberF(value, out float numValue))
                return;

            Manipulator.Settings.SizePosMaxValue = numValue;
            ModelAction.UpdateAction();
        }
    }
}
