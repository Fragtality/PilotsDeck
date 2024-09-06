﻿using PilotsDeck.Actions.Advanced.Elements;
using PilotsDeck.Actions.Advanced.SettingsModel;
using PilotsDeck.Tools;
using System.Collections.Generic;
using System.Windows.Media;

namespace PilotsDeck.UI.ViewModels.Element
{
    public class ViewModelElementGauge(ElementGauge element, ViewModelAction action)
    {
        public virtual ElementGauge GaugeElement { get; set; } = element;
        public virtual ViewModelAction ModelAction { get; set; } = action;
        public virtual string ValueMin { get { return Conversion.ToString(GaugeElement.Settings.GaugeValueMin); } }
        public virtual string ValueMax { get { return Conversion.ToString(GaugeElement.Settings.GaugeValueMax); } }
        public virtual string ValueScale { get { return Conversion.ToString(GaugeElement.Settings.GaugeValueScale); } }
        public virtual string SizeAddress { get { return GaugeElement.Settings.GaugeSizeAddress; } }
        public bool IsValidAddress { get { return GaugeElement.GaugeSizeVariable?.Type != Resources.Variables.SimValueType.NONE; } }
        public virtual bool UseDynamicSize { get { return GaugeElement.Settings.UseGaugeDynamicSize; } }
        public virtual bool RevereseDirection { get { return GaugeElement.Settings.GaugeRevereseDirection; } }
        public virtual bool FixedRanges { get { return GaugeElement.Settings.GaugeFixedRanges; } }
        public virtual bool FixedMarkers { get { return GaugeElement.Settings.GaugeFixedMarkers; } }
        public virtual bool IsArc { get { return GaugeElement.Settings.GaugeIsArc; } }
        public virtual string ArcAngleStart { get { return Conversion.ToString(GaugeElement.Settings.GaugeAngleStart); } }
        public virtual string ArcAngleSweep { get { return Conversion.ToString(GaugeElement.Settings.GaugeAngleSweep); } }
        public virtual List<ColorRange> ColorRanges { get { return GaugeElement.Settings.GaugeColorRanges; } }
        public virtual List<MarkerDefinition> GaugeMarkers { get { return GaugeElement.Settings.GaugeMarkers; } }

        public virtual List<ColorRange> GetColorRanges()
        {
            return ColorRanges;
        }

        public virtual List<MarkerDefinition> GetGaugeMarkers()
        {
            return GaugeMarkers;
        }

        public virtual List<Color> GetRangeColors()
        {
            List<Color> colors = [];

            foreach (var color in GaugeElement.Settings.GaugeColorRanges)
                colors.Add(Color.FromArgb(color.GetColor().A, color.GetColor().R, color.GetColor().G, color.GetColor().B));

            return colors;
        }

        public virtual int[] GetCustomColors()
        {
            List<int> colors = [];

            foreach (var color in GaugeElement.Settings.GaugeColorRanges)
                colors.Add(System.Drawing.ColorTranslator.ToOle(color.GetColor()));

            return [.. colors];
        }

        public virtual List<string> GetRangeValues()
        {
            List<string> values = [];

            foreach (var range in GaugeElement.Settings.GaugeColorRanges)
                values.Add($"{Conversion.ToString(range.Range[0])} - {Conversion.ToString(range.Range[1])}");

            return values;
        }

        public virtual List<Color> GetMarkerColors()
        {
            List<Color> colors = [];

            foreach (var color in GaugeElement.Settings.GaugeMarkers)
                colors.Add(Color.FromArgb(color.GetColor().A, color.GetColor().R, color.GetColor().G, color.GetColor().B));

            return colors;
        }

        public virtual List<string> GetMarkerValues()
        {
            List<string> values = [];

            foreach (var marker in GaugeElement.Settings.GaugeMarkers)
                values.Add($"Pos: {Conversion.ToString(marker.ValuePosition)} - {Conversion.ToString(marker.Size)}px");

            return values;
        }

        public virtual void SetValueMin(string input)
        {
            if (Conversion.IsNumberF(input, out float numValue))
            {
                GaugeElement.Settings.GaugeValueMin = numValue;
                ModelAction.UpdateAction();
            }
        }

        public virtual void SetValueMax(string input)
        {
            if (Conversion.IsNumberF(input, out float numValue))
            {
                GaugeElement.Settings.GaugeValueMax = numValue;
                ModelAction.UpdateAction();
            }
        }

        public virtual void SetValueScale(string input)
        {
            if (Conversion.IsNumberF(input, out float numValue))
            {
                GaugeElement.Settings.GaugeValueScale = numValue;
                ModelAction.UpdateAction();
            }
        }

        public virtual void SetArc(bool isArc)
        {
            GaugeElement.Settings.GaugeIsArc = isArc;
            ModelAction.UpdateAction();
        }

        public virtual void SetAngleStart(string input)
        {
            if (Conversion.IsNumberF(input, out float numValue))
            {
                GaugeElement.Settings.GaugeAngleStart = numValue;
                ModelAction.UpdateAction();
            }
        }

        public virtual void SetAngleSweep(string input)
        {
            if (Conversion.IsNumberF(input, out float numValue))
            {
                GaugeElement.Settings.GaugeAngleSweep = numValue;
                ModelAction.UpdateAction();
            }
        }

        protected virtual void SortRanges()
        {
            GaugeElement.Settings.GaugeColorRanges.Sort(delegate (ColorRange x, ColorRange y)
            {
                if (x?.Range[0] > y?.Range[1])
                    return 1;
                else if (x?.Range[0] < y?.Range[1])
                    return -1;
                else
                    return 0;
            });
        }

        public virtual void CopyRanges(List<ColorRange> ranges)
        {
            if (ranges?.Count < 1)
                return;

            GaugeElement.Settings.GaugeColorRanges.Clear();
            foreach (var range in ranges)
                GaugeElement.Settings.GaugeColorRanges.Add(new ColorRange(range));
            ModelAction.UpdateAction();
        }

        public virtual void AddRange(string start, string end, System.Drawing.Color color)
        {
            if (Conversion.IsNumberF(start, out float valStart) && Conversion.IsNumberF(end, out float valEnd))
            {
                if (valStart < valEnd)
                    GaugeElement.Settings.GaugeColorRanges.Add(new ColorRange(valStart, valEnd, color));
                else
                    GaugeElement.Settings.GaugeColorRanges.Add(new ColorRange(valEnd, valStart, color));
                SortRanges();
                ModelAction.UpdateAction();
            }
        }

        public void RemoveRange(int index)
        {
            if (index >= 0 && index < GaugeElement.Settings.GaugeColorRanges.Count)
            {
                GaugeElement.Settings.GaugeColorRanges.RemoveAt(index);
                SortRanges();
                ModelAction.UpdateAction();
            }
        }

        public virtual void UpdateRange(int index, string start, string end, System.Drawing.Color color)
        {
            if (index >= 0 && index < GaugeElement.Settings.GaugeColorRanges.Count && Conversion.IsNumberF(start, out float valStart) && Conversion.IsNumberF(end, out float valEnd))
            {
                if (valStart < valEnd)
                    GaugeElement.Settings.GaugeColorRanges[index] = new ColorRange(valStart, valEnd, color);
                else
                    GaugeElement.Settings.GaugeColorRanges[index] = new ColorRange(valEnd, valStart, color);
                SortRanges();
                ModelAction.UpdateAction();
            }
        }

        public virtual void AddMarker(string pos, string size, System.Drawing.Color color)
        {
            if (!Conversion.IsNumberF(size, out float numSize))
                return;
            
            if (pos.StartsWith('$') && pos.Length > 1)
            {
                string[] parts = pos[1..].Split(':');
                if (parts.Length != 3)
                    return;

                if (!Conversion.IsNumberF(parts[0], out float step) || !Conversion.IsNumberF(parts[1], out float start) || !Conversion.IsNumberF(parts[2], out float end))
                    return;

                for (float markerPos = start; markerPos <= end; markerPos += step)
                    GaugeElement.Settings.GaugeMarkers.Add(new(markerPos, numSize, color));

                SortMarkers();
                ModelAction.UpdateAction();

            }
            else if (Conversion.IsNumberF(pos, out float numPos))
            {
                GaugeElement.Settings.GaugeMarkers.Add(new(numPos, numSize, color));
                SortMarkers();
                ModelAction.UpdateAction();
            }
        }

        public void RemoveMarker(int index)
        {
            if (index >= 0 && index < GaugeElement.Settings.GaugeMarkers.Count)
            {
                GaugeElement.Settings.GaugeMarkers.RemoveAt(index);
                SortMarkers();
                ModelAction.UpdateAction();
            }
        }

        public virtual void UpdateMarker(int index, string pos, string size, System.Drawing.Color color)
        {
            if (index >= 0 && index < GaugeElement.Settings.GaugeMarkers.Count && Conversion.IsNumberF(pos, out float valPos) && Conversion.IsNumberF(size, out float valSize))
            {
                GaugeElement.Settings.GaugeMarkers[index] = new MarkerDefinition(valPos, valSize, color);
                SortMarkers();
                ModelAction.UpdateAction();
            }
        }

        public virtual void CopyMarker(List<MarkerDefinition> markers)
        {
            if (markers?.Count < 1)
                return;

            GaugeElement.Settings.GaugeMarkers.Clear();
            foreach (var marker in markers)
                GaugeElement.Settings.GaugeMarkers.Add(new MarkerDefinition(marker));
            ModelAction.UpdateAction();
        }

        protected virtual void SortMarkers()
        {
            GaugeElement.Settings.GaugeMarkers.Sort(delegate (MarkerDefinition x, MarkerDefinition y)
            {
                if (x?.ValuePosition > y?.ValuePosition)
                    return 1;
                else if (x?.ValuePosition < y?.ValuePosition)
                    return -1;
                else
                    return 0;
            });
        }

        public virtual void SetDynamicSize(bool input)
        {
            GaugeElement.Settings.UseGaugeDynamicSize = input;
            ModelAction.UpdateAction();
        }

        public virtual void SetReverseDirection(bool input)
        {
            GaugeElement.Settings.GaugeRevereseDirection = input;
            ModelAction.UpdateAction();
        }

        public virtual void SetFixedRanges(bool input)
        {
            GaugeElement.Settings.GaugeFixedRanges = input;
            ModelAction.UpdateAction();
        }

        public virtual void SetFixedMarkers(bool input)
        {
            GaugeElement.Settings.GaugeFixedMarkers = input;
            ModelAction.UpdateAction();
        }

        public virtual void SetSizeAddress(string input)
        {
            if (input != null)
            {
                GaugeElement.Settings.GaugeSizeAddress = input;
                ModelAction.UpdateAction();
            }
        }
    }
}