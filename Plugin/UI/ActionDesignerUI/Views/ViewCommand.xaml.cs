using PilotsDeck.UI.ActionDesignerUI.ViewModels;
using PilotsDeck.UI.ActionDesignerUI.Views.Controls;
using System.Windows;
using System.Windows.Controls;

namespace PilotsDeck.UI.ActionDesignerUI.Views
{
    public partial class ViewCommand : UserControl
    {
        public ViewModelCommand ModelCommand { get; set; }
        public Window ParentWindow { get; set; }

        public ViewCommand(ViewModelCommand model, Window parent)
        {
            InitializeComponent();

            ModelCommand = model;
            this.DataContext = ModelCommand;
            ParentWindow = parent;

            VariableControl.Content = new ControlAddress(ModelCommand.ModelAddress);
            InitializeBindings();
        }

        protected virtual void InitializeBindings()
        {
            ModelCommand[nameof(ModelCommand.TimeAfterLastDown)].BindElement(InputTimeAfter);
            ModelCommand[nameof(ModelCommand.TickDelay)].BindElement(InputTickDelay);
            ModelCommand[nameof(ModelCommand.ResetDelay)].BindElement(InputResetDelay);
            ModelCommand[nameof(ModelCommand.CommandDelay)].BindElement(InputCommandDelay);
            ModelCommand[nameof(ModelCommand.WriteValue)].BindElement(InputWriteValue);
            ModelCommand[nameof(ModelCommand.ResetValue)].BindElement(InputResetValue);
            ModelCommand[nameof(ModelCommand.Name)].BindElement(InputName);
        }
    }
}
