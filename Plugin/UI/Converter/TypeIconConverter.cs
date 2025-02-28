using CFIT.AppTools;
using PilotsDeck.Actions;
using PilotsDeck.Actions.Advanced;
using PilotsDeck.Tools;
using PilotsDeck.UI.ActionDesignerUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PilotsDeck.UI.Converter
{
    public class TypeIconConverter : IValueConverter
    {
        public static Dictionary<ActionTemplate, BitmapImage> TemplateIcons { get; } = new()
        {
            { ActionTemplate.NONE, Img.GetAssemblyImage("slash-circle")  },
            { ActionTemplate.DISPLAY, Img.GetAssemblyImage("ElementValue")  },
            { ActionTemplate.SWITCH, Img.GetAssemblyImage("CommandKeyDown")  },
            { ActionTemplate.DYNAMIC, Img.GetAssemblyImage("ElementImage")  },
            { ActionTemplate.KORRY, Img.GetAssemblyImage("view-stacked")  },
            { ActionTemplate.RADIO, Img.GetAssemblyImage("broadcast-pin")  },
            { ActionTemplate.GAUGE, Img.GetAssemblyImage("ElementGauge")  },
        };

        public static Dictionary<DISPLAY_ELEMENT, BitmapImage> ElementIcons { get; } = new()
        {
            { DISPLAY_ELEMENT.GAUGE, Img.GetAssemblyImage("ElementGauge")  },
            { DISPLAY_ELEMENT.IMAGE, Img.GetAssemblyImage("ElementImage")  },
            { DISPLAY_ELEMENT.PRIMITIVE, Img.GetAssemblyImage("ElementPrimitive")  },
            { DISPLAY_ELEMENT.TEXT, Img.GetAssemblyImage("ElementText")  },
            { DISPLAY_ELEMENT.VALUE, Img.GetAssemblyImage("ElementValue")  },
        };

        public static Dictionary<ELEMENT_MANIPULATOR, BitmapImage> ManipulatorIcons { get; } = new()
        {
            { ELEMENT_MANIPULATOR.COLOR, Img.GetAssemblyImage("ManipulatorColor")  },
            { ELEMENT_MANIPULATOR.FORMAT, Img.GetAssemblyImage("ManipulatorFormat")  },
            { ELEMENT_MANIPULATOR.INDICATOR, Img.GetAssemblyImage("ManipulatorIndicator")  },
            { ELEMENT_MANIPULATOR.ROTATE, Img.GetAssemblyImage("ManipulatorRotate")  },
            { ELEMENT_MANIPULATOR.TRANSPARENCY, Img.GetAssemblyImage("ManipulatorTransparency")  },
            { ELEMENT_MANIPULATOR.VISIBLE, Img.GetAssemblyImage("ManipulatorVisible")  },
            { ELEMENT_MANIPULATOR.SIZEPOS, Img.GetAssemblyImage("ManipulatorSizePos")  },
        };

        public static Dictionary<StreamDeckCommand, BitmapImage> CommandIcons { get; } = new()
        {
            { StreamDeckCommand.KEY_DOWN, Img.GetAssemblyImage("CommandKeyDown")  },
            { StreamDeckCommand.KEY_UP, Img.GetAssemblyImage("CommandKeyUp")  },
            { StreamDeckCommand.DIAL_DOWN, Img.GetAssemblyImage("CommandDialDown")  },
            { StreamDeckCommand.DIAL_UP, Img.GetAssemblyImage("CommandDialUp")  },
            { StreamDeckCommand.DIAL_LEFT, Img.GetAssemblyImage("CommandDialLeft")  },
            { StreamDeckCommand.DIAL_RIGHT, Img.GetAssemblyImage("CommandDialRight")  },
            { StreamDeckCommand.TOUCH_TAP, Img.GetAssemblyImage("CommandTouchTap")  },
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (typeof(ImageSource) == targetType && value is Enum dataType)
            {
                if (dataType.IsEnumType<DISPLAY_ELEMENT>())
                    return ElementIcons[dataType.ToEnumValue<DISPLAY_ELEMENT>()];
                else if (dataType.IsEnumType<ELEMENT_MANIPULATOR>())
                    return ManipulatorIcons[dataType.ToEnumValue<ELEMENT_MANIPULATOR>()];
                else if (dataType.IsEnumType<StreamDeckCommand>())
                    return CommandIcons[dataType.ToEnumValue<StreamDeckCommand>()];
                else if (dataType.IsEnumType<ActionTemplate>())
                    return TemplateIcons[dataType.ToEnumValue<ActionTemplate>()];
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
