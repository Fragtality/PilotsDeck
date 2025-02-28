using CFIT.AppFramework.UI.Validations;
using CFIT.AppFramework.UI.ValueConverter;
using CFIT.AppFramework.UI.ViewModels;
using CFIT.AppTools;
using PilotsDeck.Actions.Advanced.SettingsModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace PilotsDeck.UI.ActionDesignerUI.ViewModels.Elements
{
    public class ViewModelGaugeMarkers(ViewModelElement parent, Func<object, bool> validator = null)
               : ViewModelCollection<IMarkerDefinition, IMarkerDefinition>(null, (v) => v, validator ?? ((m) => m != null))
    {
        public override ICollection<IMarkerDefinition> Source => GetSource();
        public ViewModelElement ModelElement { get; } = parent;
        public List<MarkerDefinition> MarkerDefinitions => ModelElement.Source.GaugeMarkers;
        public List<MarkerRangeDefinition> MarkerRangeDefinitions => ModelElement.Source.GaugeRangeMarkers;

        protected override void InitializeMemberBindings()
        {
            CreateMemberBinding<string, string>(nameof(IMarkerDefinition.Position), new NoneConverter(), new ValidationRuleMarkerValue());
            CreateMemberNumberBinding<float>(nameof(IMarkerDefinition.Size));
            CreateMemberNumberBinding<float>(nameof(IMarkerDefinition.Height));
            CreateMemberNumberBinding<float>(nameof(IMarkerDefinition.Offset));
            CreateMemberBinding<string, System.Windows.Media.SolidColorBrush>(nameof(IMarkerDefinition.Color), new StringColorConverter(), new ValidationRuleNull());
        }

        public override IMarkerDefinition BuildItemFromBindings()
        {
            if (!HasBindingErrors()
                && HasBinding(nameof(IMarkerDefinition.Position), out var bindingPosition)
                && HasBinding(nameof(IMarkerDefinition.Size), out var bindingSize)
                && HasBinding(nameof(IMarkerDefinition.Height), out var bindingHeight)
                && HasBinding(nameof(IMarkerDefinition.Offset), out var bindingOffset)
                && HasBinding(nameof(IMarkerDefinition.Color), out var bindingColor))
            {
                string position = bindingPosition.ConvertFromTarget<string>();
                float numSize = bindingSize.ConvertFromTarget<float>();
                float numHeight = bindingHeight.ConvertFromTarget<float>();
                float numOffset = bindingOffset.ConvertFromTarget<float>();
                string color = bindingColor.ConvertFromTarget<string>();

                if (position.StartsWith('$') && MarkerRangeDefinition.GetRangeDefinition(position, out float start, out float stop, out float step))
                    return new MarkerRangeDefinition(start, stop, step, numSize, numHeight, numOffset, color);
                else if (Conversion.IsNumberF(position, out float numPos))
                    return new MarkerDefinition(numPos, numSize, numHeight, numOffset, color);
                else
                    return null;
            }
            else
                return null;
        }

        protected virtual ICollection<IMarkerDefinition> GetSource()
        {
            List<IMarkerDefinition> itemsSource = [];

            foreach (var item in MarkerDefinitions)
                itemsSource.Add(item);
            foreach (var item in MarkerRangeDefinitions)
                itemsSource.Add(item);

            return itemsSource;
        }

        public override bool Contains(IMarkerDefinition item)
        {
            try { return MarkerDefinitions?.Contains(item) == true || MarkerRangeDefinitions?.Contains(item) == true; }
            catch { return false; }
        }

        protected override void AddSource(IMarkerDefinition item)
        {
            if (item is MarkerDefinition marker)
            {
                MarkerDefinitions.Add(marker);
                SortMarkers();
            }
            else if (item is MarkerRangeDefinition markerRange)
                MarkerRangeDefinitions.Add(markerRange);
        }

        protected override bool RemoveSource(IMarkerDefinition item)
        {
            if (item is MarkerDefinition marker)
            {
                MarkerDefinitions.Remove(marker);
                return true;
            }
            else if (item is MarkerRangeDefinition markerRange)
            {
                MarkerRangeDefinitions.Remove(markerRange);
                return true;
            }
            else
                return false;
        }

        protected virtual void SortMarkers()
        {
            MarkerDefinitions.Sort(delegate (MarkerDefinition x, MarkerDefinition y)
            {
                if (x?.ValuePosition > y?.ValuePosition)
                    return 1;
                else if (x?.ValuePosition < y?.ValuePosition)
                    return -1;
                else
                    return 0;
            });
        }

        public override void Clear()
        {
            MarkerDefinitions.Clear();
            MarkerRangeDefinitions.Clear();
            NotifyCollectionChanged(new(NotifyCollectionChangedAction.Reset));
        }

        public override IEnumerator GetEnumerator()
        {
            return new MergeEnumerator(ModelElement.Source);
        }
    }
}
