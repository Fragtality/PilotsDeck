using CFIT.AppFramework.UI.Validations;
using CFIT.AppTools;
using PilotsDeck.Actions.Advanced.SettingsModel;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Controls;

namespace PilotsDeck.UI.ActionDesignerUI.ViewModels.Elements
{
    public class ValidationRuleMarkerValue : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            return BaseRule.Validate(() =>
            {
                if (value is not string text || string.IsNullOrWhiteSpace(text))
                    return false;

                if (!text.StartsWith('$'))
                    return Conversion.IsNumber(text, out _);
                else
                    return MarkerRangeDefinition.GetRangeDefinition(value, out _, out _, out _);
            }
            , "Not a valid Gauge Marker Value!");
        }
    }

    public class MergeEnumerator(ModelDisplayElement modelElement) : IEnumerator<object>
    {
        protected object _current = default;
        public virtual object Current => _current;
        object IEnumerator<object>.Current => _current;
        public virtual ModelDisplayElement ModelElement { get; } = modelElement;
        protected virtual IEnumerator<MarkerDefinition> MarkerEnumerator { get; } = modelElement.GaugeMarkers.GetEnumerator();
        protected virtual IEnumerator<MarkerRangeDefinition> RangeEnumerator { get; } = modelElement.GaugeRangeMarkers.GetEnumerator();

        public virtual bool MoveNext()
        {
            if (MarkerEnumerator.MoveNext())
            {
                _current = MarkerEnumerator.Current;
                return true;
            }
            else if (RangeEnumerator.MoveNext())
            {
                _current = RangeEnumerator.Current;
                return true;
            }
            else
                return false;
        }

        public virtual void Reset()
        {
            RangeEnumerator.Reset();
            MarkerEnumerator.Reset();
        }

#pragma warning disable
        public virtual void Dispose()
#pragma warning enable
        {
            MarkerEnumerator.Dispose();
            RangeEnumerator.Dispose();
        }
    }
}
