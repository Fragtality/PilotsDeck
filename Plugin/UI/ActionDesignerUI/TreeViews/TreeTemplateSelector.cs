using System.Windows;
using System.Windows.Controls;

namespace PilotsDeck.UI.ActionDesignerUI.TreeViews
{
    public class TreeTemplateSelector : DataTemplateSelector
    {
        public virtual HierarchicalDataTemplate TemplateElement => (HierarchicalDataTemplate)Application.Current.Resources["TemplateElement"];
        public virtual HierarchicalDataTemplate TemplateManipulator => (HierarchicalDataTemplate)Application.Current.Resources["TemplateManipulator"];
        public virtual HierarchicalDataTemplate TemplateManipulatorCondition => (HierarchicalDataTemplate)Application.Current.Resources["TemplateManipulatorCondition"];
        public virtual HierarchicalDataTemplate TemplateCommandType => (HierarchicalDataTemplate)Application.Current.Resources["TemplateCommandType"];
        public virtual HierarchicalDataTemplate TemplateCommand => (HierarchicalDataTemplate)Application.Current.Resources["TemplateCommand"];
        public virtual HierarchicalDataTemplate TemplateCommandCondition => (HierarchicalDataTemplate)Application.Current.Resources["TemplateCommandCondition"];

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is TreeItemData itemData)
            {
                if (itemData.ItemType == ItemType.Element)
                    return TemplateElement;
                else if (itemData.ItemType == ItemType.Manipulator)
                    return TemplateManipulator;
                else if (itemData.ItemType == ItemType.Condition && itemData.IsManipulatorCondition())
                    return TemplateManipulatorCondition;
                else if (itemData.ItemType == ItemType.CommandType)
                    return TemplateCommandType;
                else if (itemData.ItemType == ItemType.Command)
                    return TemplateCommand;
                else if (itemData.ItemType == ItemType.Condition && itemData.IsCommandCondition())
                    return TemplateCommandCondition;
            }

            return base.SelectTemplate(item, container);
        }
    }
}
