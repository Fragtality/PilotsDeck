using PilotsDeck.Actions;
using PilotsDeck.Actions.Advanced;
using PilotsDeck.Tools;
using PilotsDeck.UI.ControlsElement;
using PilotsDeck.UI.ControlsManipulator;
using PilotsDeck.UI.ViewModels;
using PilotsDeck.UI.ViewModels.Element;
using PilotsDeck.UI.ViewModels.Manipulator;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

namespace PilotsDeck.UI
{
    public partial class ActionDesigner : Window
    {
        public ViewModelAction ModelAction { get; set; }
        protected DispatcherTimer ShutdownCopyMonitor { get; set; }
        protected int LastSelectedElement { get; set; } = -1;
        protected int LastSelectedManipulator { get; set; } = -1;
        protected int LastSelectedCondition { get; set; } = -1;
        protected StreamDeckCommand LastSelectedCommandType { get; set; } = (StreamDeckCommand)(-1);
        protected int LastSelectedCommand { get; set; } = -1;
        protected static ISelectableItem CopiedItem { get; set; } = null;
        protected bool Refreshing { get; set; } = false;
        protected bool DeclinedTemplate { get; set; } = false;

        public ActionDesigner(ActionMeta action)
        {
            InitializeComponent();
            ModelAction = new(action, this);
            ShutdownCopyMonitor = new()
            {
                Interval = TimeSpan.FromMilliseconds(App.Configuration.IntervalCheckUiClose)
            };
            ShutdownCopyMonitor.Tick += CheckShutdownCopy;
            ShutdownCopyMonitor.Start();

            if (!string.IsNullOrWhiteSpace(action.Title))
                this.Title = $"Action Designer - '{action.Title}' ({action.Context.ToUpperInvariant()})";
            else
                this.Title = $"Action Designer - {action.Context.ToUpperInvariant()}";
        }

        protected void SetManipulatorList(ISelectableItem item)
        {
            ViewModel.SetComboBox(ComboManipulator, ViewModel.GetManipulatorTypes(item), (item?.IsTypeElementGauge() == true ? ELEMENT_MANIPULATOR.INDICATOR : ELEMENT_MANIPULATOR.VISIBLE));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Refreshing = true;
            ViewModel.SetComboBox(ComboTemplate, ViewModel.GetActionTemplates(), ActionTemplate.NONE);
            ViewModel.SetComboBox(ComboElement, ViewModel.GetElementTypes(), DISPLAY_ELEMENT.IMAGE);
            RefreshControls();
            Refreshing = false;
        }

        public void CheckShutdownCopy(object sender, EventArgs e)
        {
            if (App.CancellationToken.IsCancellationRequested || !App.ActiveDesigner.ContainsKey(ModelAction.Context))
            {
                RemoveDesigner();
                this.Close();
            }
            else
            {
                SetButtonDuplicate(GetItem());
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            RemoveDesigner();
        }

        private void RemoveDesigner()
        {
            ShutdownCopyMonitor.Stop();
            App.ActiveDesigner.Remove(ModelAction.Context);
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            Topmost = true;
            Focus();
            Topmost = false;
        }

        public void RefreshControls()
        {
            Refreshing = true;
            RefreshElements();
            RefreshCommands();
            Refreshing = false;
            ElementTree_SelectedItemChanged(null, null);
        }

        private void RefreshElements()
        {
            ElementTree.Items.Clear();
            ElementView.Content = null;
            TreeViewItem selected = null;

            var modelGroup = new StaticItem() { Header = "Elements" };
            var groupElements = new TreeViewItem() { Header = modelGroup.Header, Tag = modelGroup, IsExpanded = true, FontWeight = FontWeights.UltraBold, FontSize = 14 };
            foreach (var element in ModelAction.DisplayElements)
            {
                var modelElement = new ViewModelElement(element.Value, ModelAction, element.Key);
                var itemElement = new TreeViewItem() { Header = $"[{element.Key}] {modelElement.Name}", Tag = modelElement, IsExpanded = true, FontWeight = FontWeights.Bold };
                if (LastSelectedElement == element.Key && LastSelectedManipulator == -1 && LastSelectedCondition == -1)
                    selected = itemElement;

                if (!element.Value.ElementManipulators.IsEmpty)
                {
                    modelGroup = new StaticItem() { Header = "Manipulators", ElementID = element.Key };
                    itemElement.Items.Add(new TreeViewItem() { Header = modelGroup.Header, Tag = modelGroup, IsExpanded = true, FontWeight = FontWeights.DemiBold });
                    foreach (var manipulator in element.Value.ElementManipulators)
                    {
                        var modelManipulator = new ViewModelManipulator(manipulator.Value, ModelAction, element.Key, manipulator.Key);
                        var itemManipulator = new TreeViewItem() { Header = $"[{manipulator.Key}] {modelManipulator.Name}", Tag = modelManipulator, IsExpanded = true, FontWeight = FontWeights.Regular };
                        if (LastSelectedElement == element.Key && LastSelectedManipulator == manipulator.Key && LastSelectedCondition == -1)
                            selected = itemManipulator;

                        if (!manipulator.Value.ConditionStore.Conditions.IsEmpty)
                        {
                            modelGroup = new StaticItem() { Header = "Conditions", ElementID = element.Key, ManipulatorID = manipulator.Key };
                            itemManipulator.Items.Add(new TreeViewItem() { Header = modelGroup.Header, Tag = modelGroup, IsExpanded = true, FontWeight = FontWeights.Regular });
                            foreach (var condition in manipulator.Value.ConditionStore.Conditions)
                            {
                                var modelCondition = new ViewModelCondition(condition.Value, ModelAction, condition.Key, element.Key, manipulator.Key);
                                var itemCondition = new TreeViewItem() { Header = $"[{condition.Key}] {modelCondition.Name}", Tag = modelCondition, IsExpanded = true, FontWeight = FontWeights.Regular };
                                if (LastSelectedElement == element.Key && LastSelectedManipulator == manipulator.Key && LastSelectedCondition == condition.Key)
                                    selected = itemCondition;

                                itemManipulator.Items.Add(itemCondition);
                            }
                        }
                        itemElement.Items.Add(itemManipulator);
                    }
                }
                groupElements.Items.Add(itemElement);
            }
            ElementTree.Items.Add(groupElements);

            if (selected != null)
            {
                selected.IsSelected = true;
                selected.Focus();
            }
        }

        private void RefreshCommands()
        {
            TreeViewItem selected = null;

            var modelGroup = new StaticItem() { Header = "Commands" };
            var groupCommands = new TreeViewItem() { Header = modelGroup.Header, Tag = modelGroup, IsExpanded = true, FontWeight = FontWeights.UltraBold, FontSize = 14 };
            foreach (var type in ModelAction.ActionCommands)
            {
                modelGroup = new StaticItem() { Header = $"{type.Key}", DeckCommandType = type.Key };
                var itemType = new TreeViewItem() { Header = modelGroup.Header, Tag = modelGroup, IsExpanded = true, FontWeight = FontWeights.Bold };
                foreach (var cmd in type.Value)
                {
                    var modelCmd = new ViewModelCommand(cmd.Value, ModelAction, cmd.Key);
                    var itemCmd = new TreeViewItem() { Header = $"[{cmd.Key}] {modelCmd.Name}", Tag = modelCmd, IsExpanded = true, FontWeight = FontWeights.DemiBold };
                    if (LastSelectedCommandType == cmd.Value.DeckCommandType && LastSelectedCommand == cmd.Key && LastSelectedCondition == -1)
                        selected = itemCmd;

                    if (!cmd.Value.Conditions.IsEmpty)
                    {
                        modelGroup = new StaticItem() { Header = "Conditions", DeckCommandType = type.Key, CommandID = cmd.Key };
                        itemCmd.Items.Add(new TreeViewItem() { Header = modelGroup.Header, Tag = modelGroup, IsExpanded = true, FontWeight = FontWeights.Regular });
                        foreach (var condition in cmd.Value.Conditions)
                        {
                            var modelCondition = new ViewModelCondition(condition.Value, ModelAction, condition.Key, -1, -1, cmd.Value.DeckCommandType, cmd.Key);
                            var itemCondition = new TreeViewItem() { Header = $"[{condition.Key}] {modelCondition.Name}", Tag = modelCondition, IsExpanded = true, FontWeight = FontWeights.Regular };
                            if (LastSelectedCommandType == cmd.Value.DeckCommandType && LastSelectedCommand == cmd.Key && LastSelectedCondition == condition.Key)
                                selected = itemCondition;

                            itemCmd.Items.Add(itemCondition);
                        }
                    }
                    itemType.Items.Add(itemCmd);
                }
                groupCommands.Items.Add(itemType);
            }
            ElementTree.Items.Add(groupCommands);

            if (selected != null)
            {
                selected.IsSelected = true;
                selected.Focus();
            }
        }

        public static ISelectableItem GetItem(object item)
        {
            if (item is TreeViewItem)
                return (item as TreeViewItem).Tag as ISelectableItem;
            else
                return null;
        }

        private ISelectableItem GetItem()
        {
            return GetItem(ElementTree?.SelectedValue);
        }

        private void SetLastSelected(ISelectableItem item)
        {
            if (item == null)
            {
                LastSelectedElement = -1;
                LastSelectedManipulator = -1;
                LastSelectedCondition = -1;
                LastSelectedCommandType = (StreamDeckCommand)(-1);
                LastSelectedCommand = -1;
            }
            else
            {
                LastSelectedElement = item.ElementID;
                LastSelectedManipulator = item.ManipulatorID;
                LastSelectedCondition = item.ConditionID;
                LastSelectedCommandType = item.DeckCommandType;
                LastSelectedCommand = item.CommandID;
            }
        }

        private void ElementTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (Refreshing)
                return;

            ElementView.Content = null;

            var item = GetItem();
            SetLastSelected(item);
            SetMenuButtons(item);

            if (item?.IsDisplayElement() == true)
                ElementView.Content = new ViewElement(item as ViewModelElement, this);
            else if (item?.IsElementManipulator() == true)
                ElementView.Content = new ViewManipulator(item as ViewModelManipulator, this);
            else if (item?.IsManipulatorCondition() == true)
                ElementView.Content = new ViewCondition(item as ViewModelCondition);
            else if (item?.IsHeaderActionType() == true)
                ElementView.Content = new ViewActionDelay(ModelAction, item.DeckCommandType);
            else if (item?.IsActionCommand() == true)
                ElementView.Content = new ViewCommand(item as ViewModelCommand);
            else if (item?.IsActionCondition() == true)
                ElementView.Content = new ViewCondition(item as ViewModelCondition);
        }

        private void SetGeneralButtons(bool state)
        {
            ButtonMoveUp.IsEnabled = state;
            ButtonMoveDown.IsEnabled = state;
            ButtonRemove.IsEnabled = state;
        }

        private void SetMenuButtons(ISelectableItem item)
        {
            SetManipulatorList(item);
            SetButtonDuplicate(item);
            if (item == null)
            {
                SetGeneralButtons(false);
                PanelNewElement.Visibility = Visibility.Collapsed;
                PanelSetTemplate.Visibility = Visibility.Collapsed;
                PanelNewManipulator.Visibility = Visibility.Collapsed;
                PanelNewCondition.Visibility = Visibility.Collapsed;
                PanelNewCommand.Visibility = Visibility.Collapsed;
            }
            else if (item.IsDisplayElement())
            {
                SetGeneralButtons(true);
                PanelNewElement.Visibility = Visibility.Collapsed;
                PanelSetTemplate.Visibility = Visibility.Collapsed;
                PanelNewManipulator.Visibility = Visibility.Visible;
                PanelNewCondition.Visibility = Visibility.Collapsed;
                PanelNewCommand.Visibility = Visibility.Collapsed;
            }
            else if (item.IsElementManipulator())
            {
                SetGeneralButtons(true);
                PanelNewElement.Visibility = Visibility.Collapsed;
                PanelSetTemplate.Visibility = Visibility.Collapsed;
                PanelNewManipulator.Visibility = Visibility.Collapsed;
                PanelNewCondition.Visibility = Visibility.Visible;
                PanelNewCommand.Visibility = Visibility.Collapsed;
            }
            else if (item.IsManipulatorCondition() || item.IsActionCondition())
            {
                SetGeneralButtons(true);
                PanelNewElement.Visibility = Visibility.Collapsed;
                PanelSetTemplate.Visibility = Visibility.Collapsed;
                PanelNewManipulator.Visibility = Visibility.Collapsed;
                PanelNewCondition.Visibility = Visibility.Visible;
                PanelNewCommand.Visibility = Visibility.Collapsed;
            }
            else if (item.IsActionCommand())
            {
                SetGeneralButtons(true);
                PanelNewElement.Visibility = Visibility.Collapsed;
                PanelSetTemplate.Visibility = Visibility.Collapsed;
                PanelNewManipulator.Visibility = Visibility.Collapsed;
                PanelNewCondition.Visibility = Visibility.Visible;
                PanelNewCommand.Visibility = Visibility.Collapsed;
            }
            else if (item.IsHeaderElement())
            {
                SetGeneralButtons(false);
                if (DeclinedTemplate || !ModelAction.DisplayElements.IsEmpty || ModelAction.ActionCommands.Where(d => !d.Value.IsEmpty).Any())
                {
                    PanelNewElement.Visibility = Visibility.Visible;
                    PanelSetTemplate.Visibility = Visibility.Collapsed;
                }
                else
                {
                    PanelNewElement.Visibility = Visibility.Collapsed;
                    PanelSetTemplate.Visibility = Visibility.Visible;
                }
                PanelNewManipulator.Visibility = Visibility.Collapsed;
                PanelNewCondition.Visibility = Visibility.Collapsed;
                PanelNewCommand.Visibility = Visibility.Collapsed;
            }
            else if (item.IsHeaderActionType() || item.IsHeaderCommands())
            {
                SetGeneralButtons(false);
                PanelNewElement.Visibility = Visibility.Collapsed;
                PanelSetTemplate.Visibility = Visibility.Collapsed;
                PanelNewManipulator.Visibility = Visibility.Collapsed;
                PanelNewCondition.Visibility = Visibility.Collapsed;
                PanelNewCommand.Visibility = Visibility.Visible;
            }
            else if (item.IsHeaderActionConditions() || item.IsHeaderManipulatorConditions())
            {
                SetGeneralButtons(false);
                PanelNewElement.Visibility = Visibility.Collapsed;
                PanelSetTemplate.Visibility = Visibility.Collapsed;
                PanelNewManipulator.Visibility = Visibility.Collapsed;
                PanelNewCondition.Visibility = Visibility.Visible;
                PanelNewCommand.Visibility = Visibility.Collapsed;
            }
            else if (item.IsHeaderManipulator())
            {
                SetGeneralButtons(false);
                PanelNewElement.Visibility = Visibility.Collapsed;
                PanelSetTemplate.Visibility = Visibility.Collapsed;
                PanelNewManipulator.Visibility = Visibility.Visible;
                PanelNewCondition.Visibility = Visibility.Collapsed;
                PanelNewCommand.Visibility = Visibility.Collapsed;
            }
            else
            {
                SetGeneralButtons(false);
                PanelNewElement.Visibility = Visibility.Collapsed;
                PanelSetTemplate.Visibility = Visibility.Collapsed;
                PanelNewManipulator.Visibility = Visibility.Collapsed;
                PanelNewCondition.Visibility = Visibility.Collapsed;
                PanelNewCommand.Visibility = Visibility.Collapsed;
            }
        }

        private void ButtonAddElement_Click(object sender, RoutedEventArgs e)
        {
            if (ComboElement.SelectedValue != null && ComboElement.SelectedIndex != -1)
            {
                SetLastSelected(null);
                LastSelectedElement = ModelAction.AddDisplayElement((DISPLAY_ELEMENT)ComboElement.SelectedValue);
                RefreshControls();
            }
        }

        private void ButtonAddCommand_Click(object sender, RoutedEventArgs e)
        {
            var item = GetItem();
            if (item.IsHeaderActionType())
            {
                SetLastSelected(null);
                LastSelectedCommandType = item.DeckCommandType;
                LastSelectedCommand = ModelAction.AddCommand(item.DeckCommandType);
                RefreshControls();
            }
            else if (item.IsHeaderCommands())
            {
                SetLastSelected(null);
                LastSelectedCommandType = ModelAction.IsEncoder ? StreamDeckCommand.DIAL_UP : StreamDeckCommand.KEY_UP;
                LastSelectedCommand = ModelAction.AddCommand(LastSelectedCommandType);
                RefreshControls();
            }
        }

        private void ButtonAddManipulator_Click(object sender, RoutedEventArgs e)
        {
            var item = GetItem();
            if (ComboManipulator.SelectedValue != null && ComboManipulator.SelectedIndex != -1 && (item?.IsDisplayElement() == true || item?.IsHeaderManipulator() == true))
            {
                SetLastSelected(null);
                LastSelectedElement = item.ElementID;
                LastSelectedManipulator = ModelAction.AddManipulator(item.ElementID, (ELEMENT_MANIPULATOR)ComboManipulator.SelectedValue);
                RefreshControls();
            }
        }

        private void ButtonAddCondition_Click(object sender, RoutedEventArgs e)
        {
            var item = GetItem();
            if (item == null)
                return;

            if (item.IsElementManipulator() || item.IsManipulatorCondition() || item.IsHeaderManipulatorConditions())
            {
                SetLastSelected(null);
                LastSelectedElement = item.ElementID;
                LastSelectedManipulator = item.ManipulatorID;
                LastSelectedCondition = ModelAction.AddManipulatorCondition(item.ElementID, item.ManipulatorID);
                RefreshControls();
            }
            else if (item.IsActionCommand() || item.IsActionCondition() || item.IsHeaderActionConditions())
            {
                SetLastSelected(null);
                LastSelectedCommandType = item.DeckCommandType;
                LastSelectedCommand = item.CommandID;
                LastSelectedCondition = ModelAction.AddCommandCondition(item.DeckCommandType, item.CommandID);
                RefreshControls();
            }
        }

        private void ButtonRemove_Click(object sender, RoutedEventArgs e)
        {
            var item = GetItem();
            if (item?.IsDisplayElement() == true)
            {
                ModelAction.RemoveDisplayElement(item.ElementID);
            }
            else if (item?.IsElementManipulator() == true)
            {
                ModelAction.RemoveManipulator(item.ElementID, item.ManipulatorID);
            }
            else if (item?.IsManipulatorCondition() == true)
            {
                ModelAction.RemoveManipulatorCondition(item.ElementID, item.ManipulatorID, item.ConditionID);
            }
            else if (item?.IsActionCommand() == true)
            {
                ModelAction.RemoveCommand(item.DeckCommandType, item.CommandID);
            }
            else if (item?.IsActionCondition() == true)
            {
                ModelAction.RemoveCommandCondition(item.DeckCommandType, item.CommandID, item.ConditionID);
            }
            SetLastSelected(null);
        }

        private void ButtonMoveUp_Click(object sender, RoutedEventArgs e)
        {
            SwapElement(-1);
        }

        private void ButtonMoveDown_Click(object sender, RoutedEventArgs e)
        {
            SwapElement(1);
        }

        private void SwapElement(int offset)
        {
            var item = GetItem();
            if (item?.IsDisplayElement() == true && ModelAction.SwapElement(LastSelectedElement, LastSelectedElement + offset))
            {
                LastSelectedElement += offset;
                RefreshControls();
            }
            else if (item?.IsElementManipulator() == true && ModelAction.SwapManipulator(LastSelectedElement, LastSelectedManipulator, LastSelectedManipulator + offset))
            {
                LastSelectedManipulator += offset;
                RefreshControls();
            }
            else if (item?.IsManipulatorCondition() == true && ModelAction.SwapManipulatorCondition(LastSelectedElement, LastSelectedManipulator, LastSelectedCondition, LastSelectedCondition + offset))
            {
                LastSelectedCondition += offset;
                RefreshControls();
            }
            else if (item?.IsActionCommand() == true && ModelAction.SwapCommand(LastSelectedCommandType, LastSelectedCommand, LastSelectedCommand + offset))
            {   
                LastSelectedCommand += offset;
                RefreshControls();
            }
            else if (item?.IsActionCondition() == true && ModelAction.SwapActionCondition(LastSelectedCommandType, LastSelectedCommand, LastSelectedCondition, LastSelectedCondition + offset))
            {
                LastSelectedCondition += offset;
                RefreshControls();
            }
        }

        private void SetButtonDuplicateState(string text, bool enabled)
        {
            ButtonDuplicate.IsEnabled = enabled;
            if (text == "Paste" && enabled)
                ButtonDuplicate.BorderBrush = new SolidColorBrush(Colors.Green);
        }

        private void SetButtonDuplicate(ISelectableItem item)
        {
            if (CopiedItem == null)
            {
                ButtonDuplicate.BorderBrush = SystemColors.WindowFrameBrush;
                ButtonDuplicate.BorderThickness = new Thickness(1);
            }
            else
            {
                ButtonDuplicate.BorderBrush = SystemColors.HighlightBrush;
                ButtonDuplicate.BorderThickness = new Thickness(1.5);
            }

            //Element
            if (item?.IsDisplayElement() == true && CopiedItem == null)
            {
                SetButtonDuplicateState("Copy", true);
            }
            else if ((item?.IsDisplayElement() == true || item?.IsHeaderElement() == true || item?.IsDisplayElement() == true) && CopiedItem is ViewModelElement)
            {
                SetButtonDuplicateState("Paste", true);
            }
            //Manipulator
            else if (item?.IsElementManipulator() == true && CopiedItem == null)
            {
                SetButtonDuplicateState("Copy", true);
            }
            else if ((item?.IsDisplayElement() == true || item?.IsHeaderManipulator() == true || item?.IsElementManipulator() == true) && CopiedItem is ViewModelManipulator)
            {
                SetButtonDuplicateState("Paste", true);
            }
            //Manipulator Condition
            else if (item?.IsManipulatorCondition() == true && CopiedItem == null)
            {
                SetButtonDuplicateState("Copy", true);
            }
            else if ((item?.IsElementManipulator() == true || item?.IsHeaderManipulatorConditions() == true || item?.IsManipulatorCondition() == true) && CopiedItem is ViewModelCondition)
            {
                SetButtonDuplicateState("Paste", true);
            }
            //Command
            else if (item?.IsActionCommand() == true && CopiedItem == null)
            {
                SetButtonDuplicateState("Copy", true);
            }
            else if ((item?.IsHeaderActionType() == true || item?.IsActionCommand() == true) && CopiedItem is ViewModelCommand)
            {
                SetButtonDuplicateState("Paste", true);
            }
            //Command Condition
            else if (item?.IsActionCondition() == true && CopiedItem == null)
            {
                SetButtonDuplicateState("Copy", true);
            }
            else if ((item?.IsActionCommand() == true || item?.IsHeaderActionConditions() == true || item?.IsActionCondition() == true) && CopiedItem is ViewModelCondition)
            {
                SetButtonDuplicateState("Paste", true);
            }
            else if (CopiedItem != null)
                SetButtonDuplicateState("Paste", false);
            else
                SetButtonDuplicateState("Copy", false);
        }

        private void ButtonDuplicate_Click(object sender, RoutedEventArgs e)
        {
            var item = GetItem();
            //Element
            if (item?.IsDisplayElement() == true && CopiedItem == null)
            {
                CopiedItem = item;                
            }
            else if ((item?.IsDisplayElement() == true || item?.IsHeaderElement() == true || item?.IsDisplayElement() == true) && CopiedItem is ViewModelElement)
            {
                var model = CopiedItem as ViewModelElement;
                LastSelectedElement = ModelAction.AddDisplayElement(model.ElementType, model.Element.Settings.Copy());
                CopiedItem = null;
                RefreshControls();
            }
            //Manipulator
            else if (item?.IsElementManipulator() == true && CopiedItem == null)
            {
                CopiedItem = item;
            }
            else if ((item?.IsDisplayElement() == true || item?.IsHeaderManipulator() == true || item?.IsElementManipulator() == true) && CopiedItem is ViewModelManipulator)
            {
                var model = CopiedItem as ViewModelManipulator;
                LastSelectedManipulator = ModelAction.AddManipulator(LastSelectedElement, model.ManipulatorType, model.Manipulator.Settings.Copy());
                CopiedItem = null;
                RefreshControls();
            }
            //Manipulator Condition
            else if (item?.IsManipulatorCondition() == true && CopiedItem == null)
            {
                CopiedItem = item;
            }
            else if ((item?.IsElementManipulator() == true || item?.IsHeaderManipulatorConditions() == true || item?.IsManipulatorCondition() == true) && CopiedItem is ViewModelCondition)
            {
                var model = CopiedItem as ViewModelCondition;
                LastSelectedCondition = ModelAction.AddManipulatorCondition(LastSelectedElement, LastSelectedManipulator, model.Condition.Copy());
                CopiedItem = null;
                RefreshControls();
            }
            //Command
            else if (item?.IsActionCommand() == true && CopiedItem == null)
            {
                CopiedItem = item;
            }
            else if ((item?.IsHeaderActionType() == true || item?.IsActionCommand() == true) && CopiedItem is ViewModelCommand)
            {
                var model = CopiedItem as ViewModelCommand;
                LastSelectedCommand = ModelAction.AddCommand(LastSelectedCommandType, model.Command.Settings.Copy());
                CopiedItem = null;
                RefreshControls();
            }
            //Command Condition
            else if (item?.IsActionCondition() == true && CopiedItem == null)
            {
                CopiedItem = item;
            }
            else if ((item?.IsActionCommand() == true || item?.IsHeaderActionConditions() == true || item?.IsActionCondition() == true) && CopiedItem is ViewModelCondition)
            {
                var model = CopiedItem as ViewModelCondition;
                LastSelectedCondition = ModelAction.AddCommandCondition(LastSelectedCommandType, LastSelectedCommand, model.Condition.Copy());
                CopiedItem = null;
                RefreshControls();
            }

            SetButtonDuplicate(item);
        }

        private void ButtonSetTemplate_Click(object sender, RoutedEventArgs e)
        {
            if (ComboTemplate.SelectedValue is ActionTemplate type)
                if (type != ActionTemplate.NONE)
                    ModelAction.SetTemplate(type);
                else
                {
                    DeclinedTemplate = true;
                    SetMenuButtons(GetItem());
                }
        }
    }

    public class TreeViewLineConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            TreeViewItem item = (TreeViewItem)value;
            ItemsControl ic = ItemsControl.ItemsControlFromItemContainer(item);
            return ic.ItemContainerGenerator.IndexFromContainer(item) == ic.Items.Count - 1;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new Exception("The method or operation is not implemented.");
        }
    }
}
