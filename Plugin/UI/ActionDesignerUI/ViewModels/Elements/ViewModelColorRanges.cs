using CFIT.AppFramework.UI.Validations;
using CFIT.AppFramework.UI.ValueConverter;
using CFIT.AppFramework.UI.ViewModels;
using PilotsDeck.Actions.Advanced.SettingsModel;
using System;
using System.Collections.Generic;

namespace PilotsDeck.UI.ActionDesignerUI.ViewModels.Elements
{
    public class ViewModelColorRanges(ICollection<ColorRange> source, Func<ColorRange, ColorRange> transformator = null, Func<ColorRange, bool> validator = null)
               : ViewModelCollection<ColorRange, ColorRange>(source, transformator ?? ((r) => r), validator ?? ((r) => r != null))
    {
        protected override void InitializeMemberBindings()
        {
            CreateMemberIndexBinding<float, string>(nameof(ColorRange.Range), 0, new RealInvariantConverter(), new ValidationRuleStringNumber());
            CreateMemberIndexBinding<float, string>(nameof(ColorRange.Range), 1, new RealInvariantConverter(), new ValidationRuleStringNumber());
            CreateMemberBinding<string, System.Windows.Media.SolidColorBrush>(nameof(ColorRange.Color), new StringColorConverter(), new ValidationRuleNull());
        }

        public override ColorRange BuildItemFromBindings()
        {
            if (!HasBindingErrors()
                && HasBindingIndex(nameof(ColorRange.Range), 0, out var bindingStart)
                && HasBindingIndex(nameof(ColorRange.Range), 1, out var bindingEnd)
                && HasBinding(nameof(ColorRange.Color), out var bindingColor))
            {
                float valStart = bindingStart.ConvertFromTarget<float>();
                float valEnd = bindingEnd.ConvertFromTarget<float>();
                string color = bindingColor.ConvertFromTarget<string>();
                if (valStart < valEnd)
                    return new ColorRange(valStart, valEnd, color);
                else
                    return new ColorRange(valEnd, valStart, color);
            }
            else
                return null;
        }

        protected override void AddSource(ColorRange item)
        {
            base.AddSource(item);
            SortRanges();
        }

        protected virtual void SortRanges()
        {
            (Source as List<ColorRange>)?.Sort(delegate (ColorRange x, ColorRange y)
            {
                if (x?.Range[0] > y?.Range[0])
                    return 1;
                else if (x?.Range[0] < y?.Range[0])
                    return -1;
                else
                    return 0;
            });
        }
    }
}
