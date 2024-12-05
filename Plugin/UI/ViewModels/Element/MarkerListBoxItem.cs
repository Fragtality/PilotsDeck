using PilotsDeck.Actions.Advanced.SettingsModel;
using PilotsDeck.Tools;
using System.Windows.Controls;
using System.Windows.Media;

namespace PilotsDeck.UI.ViewModels.Element
{
    public class MarkerListBoxItem : ListBoxItem
    {
        public bool IsRange { get; set; } = false;
        public int Index { get; set; } = -1;

        public MarkerListBoxItem(MarkerDefinition marker, int index) : base()
        {
            IsRange = false;
            Index = index;
            IsHitTestVisible = false;
            BorderBrush = new SolidColorBrush(Color.FromArgb(marker.GetColor().A, marker.GetColor().R, marker.GetColor().G, marker.GetColor().B));
            Content = $"Value: {Conversion.ToString(marker.ValuePosition)} / Thickness: {Conversion.ToString(marker.Size)} / Height: {Conversion.ToString(marker.Height)} / Offset: {Conversion.ToString(marker.Offset)}";
        }

        public MarkerListBoxItem(MarkerRangeDefinition range, int index) : base()
        {
            IsRange = true;
            Index = index;
            IsHitTestVisible = false;
            BorderBrush = new SolidColorBrush(Color.FromArgb(range.GetColor().A, range.GetColor().R, range.GetColor().G, range.GetColor().B));
            Content = GetRangeLabel(range);
        }

        public static string GetRangeLabel(MarkerRangeDefinition range)
        {
            return $"Range: {Conversion.ToString(range.Start)} - {Conversion.ToString(range.Stop)} / Step: {Conversion.ToString(range.Step)} / Thickness: {Conversion.ToString(range.Size)} / Height: {Conversion.ToString(range.Height)} / Offset: {Conversion.ToString(range.Offset)}";
        }

        public static string GetRangeValueText(MarkerRangeDefinition range)
        {
            return $"${Conversion.ToString(range.Step)}:{Conversion.ToString(range.Start)}:{Conversion.ToString(range.Stop)}";
        }
    }
}
