using PilotsDeck.Actions;
using PilotsDeck.Actions.Advanced.Manipulators;
using PilotsDeck.Resources.Variables;
using PilotsDeck.Tools;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PilotsDeck.UI.ViewModels.Manipulator
{
    public class ViewModelManipulatorIndicator(ElementManipulator manipulator, ViewModelAction action)
    {
        public ViewModelAction ModelAction { get; set; } = action;
        public ElementManipulator Manipulator { get; set; } = manipulator;
        public virtual ManagedVariable IndicatorVariable { get { return (Manipulator as ManipulatorIndicator)?.IndicatorVariable; } }
        public virtual string IndicatorAddress { get { return Manipulator.Settings.IndicatorAddress; } }
        public virtual string IndicatorScale { get { return Conversion.ToString(Manipulator.Settings.IndicatorScale); } }
        public bool IsValidAddress { get { return IndicatorVariable?.Type != SimValueType.NONE; } }
        public virtual IndicatorType IndicatorType { get { return Manipulator.Settings.IndicatorType; } }
        public virtual BitmapSource ImageSource
        {
            get
            {
                return Sys.GetBitmapFromFile(IndicatorImage) ?? BitmapSource.Create(2, 2, 96, 96, PixelFormats.Indexed1, new BitmapPalette([Colors.Transparent]), new byte[] { 0, 0, 0, 0 }, 1);
            }
        }
        public virtual string IndicatorImage { get { return Manipulator.Settings.IndicatorImage; } }
        public virtual Color IndicatorColor { get { return Color.FromArgb(Manipulator.Settings.GetIndicatorColor().A, Manipulator.Settings.GetIndicatorColor().R, Manipulator.Settings.GetIndicatorColor().G, Manipulator.Settings.GetIndicatorColor().B); } }
        public virtual System.Drawing.Color ColorForms { get { return Manipulator.Settings.GetIndicatorColor(); } }
        public virtual string IndicatorSize { get { return Conversion.ToString(Manipulator.Settings.IndicatorSize); } }
        public virtual string IndicatorLineSize { get { return Conversion.ToString(Manipulator.Settings.IndicatorLineSize); } }
        public virtual string IndicatorOffset { get { return Conversion.ToString(Manipulator.Settings.IndicatorOffset); } }
        public virtual bool IndicatorReverse { get { return Manipulator.Settings.IndicatorReverse; } }
        public virtual bool IndicatorFlip { get { return Manipulator.Settings.IndicatorFlip; } }

        public virtual void SetAddress(string input)
        {
            if (input != null)
            {
                Manipulator.Settings.IndicatorAddress = input;
                ModelAction.UpdateAction();
            }
        }

        public virtual void SetScale(string input)
        {
            if (Conversion.IsNumberF(input, out float numValue))
            {
                Manipulator.Settings.IndicatorScale = numValue;
                ModelAction.UpdateAction();
            }
        }

        public virtual void SetType(IndicatorType type)
        {
            Manipulator.Settings.IndicatorType = type;
            ModelAction.UpdateAction();
        }

        public virtual void SetImage(string input)
        {
            if (input != null)
            {
                Manipulator.Settings.IndicatorImage = input;
                ModelAction.UpdateAction();
            }
        }

        public void SetColor(System.Drawing.Color color)
        {
            if (Manipulator is ManipulatorIndicator i)
            {
                i.Settings.SetIndicatorColor(color);
                ModelAction.UpdateAction();
            }
        }

        public virtual void SetSize(string input)
        {
            if (Conversion.IsNumberF(input, out float numValue))
            {
                Manipulator.Settings.IndicatorSize = numValue;
                ModelAction.UpdateAction();
            }
        }

        public virtual void SetLineSize(string input)
        {
            if (Conversion.IsNumberF(input, out float numValue))
            {
                Manipulator.Settings.IndicatorLineSize = numValue;
                ModelAction.UpdateAction();
            }
        }

        public virtual void SetOffset(string input)
        {
            if (Conversion.IsNumberF(input, out float numValue))
            {
                Manipulator.Settings.IndicatorOffset = numValue;
                ModelAction.UpdateAction();
            }
        }

        public virtual void SetReverse(bool input)
        {
            Manipulator.Settings.IndicatorReverse = input;
            ModelAction.UpdateAction();
        }

        public virtual void SetFlip(bool input)
        {
            Manipulator.Settings.IndicatorFlip = input;
            ModelAction.UpdateAction();
        }
    }
}
