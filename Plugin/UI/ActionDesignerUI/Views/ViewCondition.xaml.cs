using PilotsDeck.UI.ActionDesignerUI.ViewModels;
using PilotsDeck.UI.ActionDesignerUI.ViewModels.Address;
using PilotsDeck.UI.ActionDesignerUI.Views.Controls;
using System.Windows;
using System.Windows.Controls;

namespace PilotsDeck.UI.ActionDesignerUI.Views
{
    public partial class ViewCondition : UserControl
    {
        public ViewModelCondition ModelCondition { get; set; }
        public Window ParentWindow { get; set; }

        public ViewCondition(ViewModelCondition model, Window parent)
        {
            InitializeComponent();
            ModelCondition = model;
            this.DataContext = ModelCondition;
            ParentWindow = parent;

            VariableControl.Content = new ControlAddress(new ViewModelVariableAddress(ModelCondition, "Variable Address"));

            ModelCondition[nameof(ModelCondition.Value)].BindElement(InputValue);
            ModelCondition[nameof(ModelCondition.Name)].BindElement(InputName);
        }
    }
}
