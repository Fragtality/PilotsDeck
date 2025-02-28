using PilotsDeck.Actions;
using PilotsDeck.UI.ActionDesignerUI.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace PilotsDeck.UI.ActionDesignerUI.Views
{
    public partial class ViewCommandType : UserControl
    {
        public ViewModelCommandType ModelCommandType { get; set; }
        public Window ParentWindow { get; set; }

        public ViewCommandType(StreamDeckCommand type, ViewModelAction modelAction, Window parent)
        {
            InitializeComponent();

            ModelCommandType = new(type, modelAction);
            this.DataContext = ModelCommandType;
            ParentWindow = parent;

            ModelCommandType[nameof(ModelCommandType.Delay)].BindElement(InputDelay);
        }
    }
}
