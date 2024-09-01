using PilotsDeck.Actions.Advanced.Elements;
using PilotsDeck.Actions.Advanced.SettingsModel;
using PilotsDeck.Plugin.Render;
using PilotsDeck.Resources.Variables;
using System.Drawing;

namespace PilotsDeck.Actions.Advanced.Manipulators
{
    public class ManipulatorSizePos(ModelManipulator model, DisplayElement parent) : ElementManipulator(model, parent)
    {
        public ManagedVariable SizePosVariable { get; set; } = null;

        public override bool HasChanges()
        {
            return base.HasChanges() || (Settings.ChangeSizePosDynamic && SizePosVariable?.IsChanged == true);
        }

        public override void ManipulateElement()
        {
            PointF pos = Element.Position;
            PointF size = Element.Size;

            bool empty = ConditionStore.Conditions.IsEmpty;
            bool compares = ConditionStore.Compare();

            pos.X = CalcSizePos(Settings.ChangeX, pos.X, Settings.ValueX, Element.Parent.CanvasSize.X, empty, compares);
            pos.Y = CalcSizePos(Settings.ChangeY, pos.Y, Settings.ValueY, Element.Parent.CanvasSize.Y, empty, compares);
            size.X = CalcSizePos(Settings.ChangeW, size.X, Settings.ValueW, Element.Parent.CanvasSize.X, empty, compares);
            if (Settings.ChangeW && size.X == 0)
                size.X = 0.25f;
            size.Y = CalcSizePos(Settings.ChangeH, size.Y, Settings.ValueH, Element.Parent.CanvasSize.Y, empty, compares);
            if (Settings.ChangeH && size.Y == 0)
                size.Y = 0.25f;

            if (pos != Element.Position)
            {
                Element.Position = pos;
                Logger.Verbose($"Position set to {Element.Position}");
            }
            if (size != Element.Size)
            {
                Element.Size = size;
                Logger.Verbose($"Size set to {Element.Size}");
            }
        }

        protected float CalcSizePos(bool change, float currentValue, float setValue, float maxValue, bool empty, bool compares)
        {
            if (change)
            {
                if (Settings.ChangeSizePosDynamic && (empty || (!empty && compares)) && SizePosVariable?.IsNumericValue == true)
                    return ToolsRender.NormalizedRatio((float)SizePosVariable.NumericValue, Settings.SizePosMinValue, Settings.SizePosMaxValue) * maxValue;
                else if (!Settings.ChangeSizePosDynamic && compares)
                    return setValue;
                else
                    return currentValue;
            }
            else
                return currentValue;
        }

        public override void RegisterRessources()
        {
            base.RegisterRessources();
            if (SizePosVariable == null)
            {
                Logger.Verbose($"Register Variable '{Settings.SizePosAddress}'");
                SizePosVariable = App.PluginController.VariableManager.RegisterVariable(Settings.SizePosAddress);
            }
        }

        public override void DeregisterRessources()
        {
            base.DeregisterRessources();
            if (SizePosVariable != null)
            {
                Logger.Verbose($"Deregister Variable '{SizePosVariable.Address}'");
                App.PluginController.VariableManager.DeregisterVariable(SizePosVariable.Address);
                SizePosVariable = null;
            }
        }
    }
}
