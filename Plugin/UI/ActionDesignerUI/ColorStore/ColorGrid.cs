using ColorPicker;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace PilotsDeck.UI.ActionDesignerUI.ColorStore
{
    public class ColorGrid
    {
        public virtual UniformGrid Grid { get; }
        public virtual PickerControlBase PickerControl { get; }
        public virtual IEnumerable<Color> GridColors { get; protected set; }
        public virtual LabelColor SelectedLabel { get; protected set; } = null;
        public virtual bool HasSelection => SelectedLabel != null;
        public virtual Color SelectedColor => SelectedLabel?.Color ?? Colors.White;

        public ColorGrid(UniformGrid grid, PickerControlBase pickerControl, IEnumerable<Color> colors = null)
        {
            Grid = grid;
            PickerControl = pickerControl;
            SetColors(colors);
        }

        public void SetColors(IEnumerable<Color> colors)
        {
            if (colors == null || colors?.Any() == false)
                return;

            ClearSelection();
            Grid.Children.Clear();
            GridColors = colors;

            foreach (var c in GridColors)
                Grid.Children.Add(new LabelColor(c, PickerControl, this));
        }

        public void SetSelection(LabelColor label)
        {
            ClearSelection();
            SelectedLabel = label;
            SelectedLabel.SetSelection();
        }

        public void ClearSelection()
        {
            if (HasSelection)
            {
                SelectedLabel.ClearSelection();
                SelectedLabel = null;
            }
        }
    }
}
