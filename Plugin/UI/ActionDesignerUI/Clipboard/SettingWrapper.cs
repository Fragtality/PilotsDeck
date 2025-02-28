using PilotsDeck.Actions.Advanced.SettingsModel;
using System.Collections.Generic;

namespace PilotsDeck.UI.ActionDesignerUI.Clipboard
{
    public class FontSetting
    {
        public System.Drawing.Font Font;
        public System.Drawing.StringAlignment HorizontalAlignment;
        public System.Drawing.StringAlignment VerticalAlignment;
    }

    public class GaugeMarkerSettings
    {
        public List<MarkerDefinition> GaugeMarkers = [];
        public List<MarkerRangeDefinition> GaugeRangeMarkers = [];
    }
}
