using CFIT.AppFramework.UI.ViewModels.Commands;
using CFIT.AppTools;
using CommunityToolkit.Mvvm.ComponentModel;
using PilotsDeck.Actions;
using PilotsDeck.Actions.Advanced;
using PilotsDeck.Actions.Advanced.SettingsModel;
using PilotsDeck.UI.ActionDesignerUI.Clipboard;
using PilotsDeck.UI.ActionDesignerUI.ViewModels;
using PilotsDeck.UI.ActionDesignerUI.ViewModels.Elements;
using PilotsDeck.UI.ActionDesignerUI.ViewModels.Manipulators;
using System;
using System.Collections.Generic;

namespace PilotsDeck.UI.ActionDesignerUI.TreeViews
{
    public enum ItemType
    {
        None = -1,
        Element = 1,
        Manipulator = 2,
        Condition = 3,
        CommandType = 4,
        Command = 5,
    }

    public partial class TreeItemData(ItemType itemType, object model, DISPLAY_ELEMENT elementType = (DISPLAY_ELEMENT)(-1), int elementID = -1,
                        ELEMENT_MANIPULATOR manipulatorType = (ELEMENT_MANIPULATOR)(-1), int manipulatorID = -1,
                        StreamDeckCommand commandType = (StreamDeckCommand)(-1), int commandID = -1,
                        int conditionID = -1) : ObservableObject
    {
        public virtual ItemType ItemType { get; protected set; } = itemType;
        public virtual string DisplayName => GetName();
        public virtual Enum DisplayType => GetEnum();
        public virtual object Model { get; set; } = model;
        public virtual List<TreeItemData> Children { get; protected set; } = [];

        [ObservableProperty]
        protected bool _IsExpanded = false;

        [ObservableProperty]
        protected bool _IsSelected = false;

        [ObservableProperty]
        protected bool _IsPasteActive = false;

        public virtual CommandWrapper<TreeItemData> PasteCommand { get; protected set; } = null;

        //Element
        public virtual DISPLAY_ELEMENT ElementType { get; protected set; } = elementType;
        public virtual int ElementID { get; protected set; } = elementID;
        public virtual ELEMENT_MANIPULATOR ManipulatorType { get; protected set; } = manipulatorType;
        public virtual int ManipulatorID { get; protected set; } = manipulatorID;

        //Command
        public virtual StreamDeckCommand DeckCommandType { get; protected set; } = commandType;
        public int CommandID { get; protected set; } = commandID;

        //Condition
        public virtual int ConditionID { get; protected set; } = conditionID;

        public static TreeItemData CreateNone()
        {
            return new TreeItemData(ItemType.None, null);
        }

        public static TreeItemData CreateElementRoot()
        {
            return new TreeItemData(ItemType.None, new object());
        }

        public static TreeItemData CreateElement(TreeItemData item, int id = -1, ViewModelElement model = null)
        {
            return CreateElement(item.ElementType, (model ?? item.Model) as ViewModelElement, (id == -1 ? item.ElementID : id));
        }

        public static TreeItemData CreateElement(DISPLAY_ELEMENT type, ViewModelElement model, int elementID)
        {
            var elementItem = new TreeItemData(ItemType.Element, model, type, elementID);

            foreach (var manipulator in model.Source.Manipulators)
                elementItem.Children.Add(CreateManipulator(type, new(manipulator.Value, model.ModelAction), elementID,
                                                           manipulator.Value.ManipulatorType, manipulator.Key));

            return elementItem;
        }

        public static TreeItemData CreateManipulator(TreeItemData item, int id = -1, ViewModelManipulator model = null)
        {
            model ??= item.Model as ViewModelManipulator;
            return CreateManipulator(item.ElementType, model, item.ElementID, model.ManipulatorType, (id == -1 ? item.ManipulatorID : id));
        }

        protected static TreeItemData CreateManipulator(DISPLAY_ELEMENT type, ViewModelManipulator model, int elementID, ELEMENT_MANIPULATOR manipulatorType, int manipulatorID)
        {
            var manipulatorItem = new TreeItemData(ItemType.Manipulator, model, type, elementID, manipulatorType, manipulatorID);

            foreach (var condition in model.Source.Conditions)
                manipulatorItem.Children.Add(CreateManipulatorCondition(type, new(condition.Value, model.ModelAction), elementID, manipulatorType, manipulatorID, condition.Key));

            return manipulatorItem;
        }

        public static TreeItemData CreateManipulatorCondition(TreeItemData item, int id = -1, ViewModelCondition model = null)
        {
            model ??= item.Model as ViewModelCondition;
            return CreateManipulatorCondition(item.ElementType, model, item.ElementID, item.ManipulatorType, item.ManipulatorID, (id == -1 ? item.ConditionID : id));
        }

        protected static TreeItemData CreateManipulatorCondition(DISPLAY_ELEMENT type, ViewModelCondition model, int elementID, ELEMENT_MANIPULATOR manipulatorType, int manipulatorID, int conditionID)
        {
            return new TreeItemData(ItemType.Condition, model, type, elementID, manipulatorType, manipulatorID, (StreamDeckCommand)(-1), -1, conditionID);
        }

        public static TreeItemData CreateCommandType(TreeItemData item, SortedDictionary<int, ModelCommand> commands = null, ViewModelAction parent = null)
        {
            return CreateCommandType(item.DeckCommandType, commands, parent);
        }

        public static TreeItemData CreateCommandType(StreamDeckCommand commandType, SortedDictionary<int, ModelCommand> commands = null, ViewModelAction parent = null)
        {
            var typeItem = new TreeItemData(ItemType.CommandType, null, (DISPLAY_ELEMENT)(-1), -1, (ELEMENT_MANIPULATOR)(-1), -1, commandType);

            if (commands != null && parent != null)
            {
                foreach (var command in commands)
                    typeItem.Children.Add(CreateCommand(commandType, new(command.Value, parent), command.Key));
            }

            return typeItem;
        }

        public static TreeItemData CreateCommand(TreeItemData item, int id = -1, ViewModelCommand model = null)
        {
            model ??= item.Model as ViewModelCommand;
            return CreateCommand(item.DeckCommandType, model, (id == -1 ? item.CommandID : id));
        }

        protected static TreeItemData CreateCommand(StreamDeckCommand commandType, ViewModelCommand model, int commandID)
        {
            var commandItem = new TreeItemData(ItemType.Command, model, (DISPLAY_ELEMENT)(-1), -1, (ELEMENT_MANIPULATOR)(-1), -1, commandType, commandID);

            foreach (var condition in model.Source.Conditions)
                commandItem.Children.Add(CreateCommandCondition(commandType, new(condition.Value, model.ModelAction), commandID, condition.Key));

            return commandItem;
        }

        public static TreeItemData CreateCommandCondition(TreeItemData item, int id = -1, ViewModelCondition model = null)
        {
            model ??= item.Model as ViewModelCondition;
            return CreateCommandCondition(item.DeckCommandType, model, item.CommandID, (id == -1 ? item.ConditionID : id));
        }

        protected static TreeItemData CreateCommandCondition(StreamDeckCommand commandType, ViewModelCondition model, int commandID, int conditionID)
        {
            return new TreeItemData(ItemType.Condition, model, (DISPLAY_ELEMENT)(-1), -1, (ELEMENT_MANIPULATOR)(-1), -1, commandType, commandID, conditionID);
        }

        public virtual TreeItemData Copy()
        {
            var root = new TreeItemData(ItemType, Model, ElementType, ElementID, ManipulatorType, ManipulatorID, DeckCommandType, CommandID, ConditionID)
            {
                IsSelected = IsSelected,
                IsExpanded = IsExpanded
            };

            foreach (var child in Children)
                root.Children.Add(child.Copy());

            return root;
        }

        public virtual void UpdatePasteState(ViewModelAction viewModel)
        {
            if (ActionClipboard.IsPasteableList(this))
            {
                PasteCommand = viewModel.Clipboard.PasteListCommand;
                OnPropertyChanged(nameof(PasteCommand));
                IsPasteActive = true;
            }
            else
            {
                PasteCommand = null;
                OnPropertyChanged(nameof(PasteCommand));
                IsPasteActive = false;
            }

            foreach (var child in Children)
                child.UpdatePasteState(viewModel);
        }

        public virtual int GetId(string propertyId)
        {
            return this.GetPropertyValue<int>(propertyId);
        }

        public virtual int UpdateId(string propertyId, int id)
        {
            int oldId = GetId(propertyId);

            ReplaceId(propertyId, id, this);

            return oldId;
        }

        protected virtual void ReplaceId(string propertyId, int id, TreeItemData itemData)
        {
            itemData.SetPropertyValue<int>(propertyId, id);
            foreach (var child in itemData.Children)
                ReplaceId(propertyId, id, child);
        }

        public virtual string GetName()
        {
            if (IsElement() && Model is ViewModelElement element)
                return element.DisplayName;
            else if (IsManipulator() && Model is ViewModelManipulator manipulator)
                return manipulator.DisplayName;
            else if (IsCondition() && Model is ViewModelCondition condition)
                return condition.DisplayName;
            else if (IsCommandType())
                return ViewModelHelper.DeckCommandTypes[DeckCommandType];
            else if (IsCommand() && Model is ViewModelCommand command)
                return command.DisplayName;
            else
                return "null";
        }

        public virtual Enum GetEnum()
        {
            if (IsElement())
                return ElementType;
            else if (IsManipulator())
                return ManipulatorType;
            else if (IsCommandType())
                return DeckCommandType;
            else
                return (ItemType)(-1);
        }

        public virtual M GetModel<M>()
        {
            return (M)Model;
        }

        public virtual bool IsItemSelected(TreeItemData currentItem)
        {
            if (currentItem == null)
                return false;

            return currentItem.ItemType == ItemType &&
                   currentItem.ElementType == ElementType &&
                   currentItem.ElementID == ElementID &&
                   currentItem.ManipulatorType == ManipulatorType &&
                   currentItem.ManipulatorID == ManipulatorID &&
                   currentItem.DeckCommandType == DeckCommandType &&
                   currentItem.CommandID == CommandID &&
                   currentItem.ConditionID == ConditionID;
        }

        public virtual bool SetItemSelection(TreeItemData currentItem)
        {
            IsSelected = IsItemSelected(currentItem);
            if (currentItem == null)
                return IsSelected;
            
            if (IsCommandType())
                IsExpanded = true;
            else if (!IsSelected && currentItem.IsElementTree() && IsElementTree())
            {
                if (IsElement() && ElementID == currentItem.ElementID)
                    IsExpanded = true;
                else if (IsManipulator() && ElementID == currentItem.ElementID && ManipulatorID == currentItem.ManipulatorID)
                    IsExpanded = true;
            }
            else if (!IsSelected && IsCommand() && currentItem.IsCommandCondition() && CommandID == currentItem.CommandID)
            {
                IsExpanded = true;
            }

            return IsSelected;
        }

        public override bool Equals(object? obj)
        {
            if (obj is not TreeItemData itemData)
                return false;
            {
                return itemData.ItemType == ItemType &&
                       Object.ReferenceEquals(itemData.Model, Model) &&
                       itemData.ElementType == ElementType &&
                       itemData.ElementID == ElementID &&
                       itemData.ManipulatorType == ManipulatorType &&
                       itemData.ManipulatorID == ManipulatorID &&
                       itemData.DeckCommandType == DeckCommandType &&
                       itemData.CommandID == CommandID &&
                       itemData.ConditionID == ConditionID;
            }
        }

        public override int GetHashCode()
        {
            return  ItemType.GetHashCode() ^
                    (Model?.GetHashCode() ?? 0) ^
                    ElementType.GetHashCode() ^
                    ElementID.GetHashCode() ^
                    ManipulatorType.GetHashCode() ^
                    ManipulatorID.GetHashCode() ^
                    DeckCommandType.GetHashCode() ^
                    CommandID.GetHashCode() ^
                    ConditionID.GetHashCode();
        }

        public virtual void NotifyNameChanged()
        {
            NotifyPropertyChanged(nameof(DisplayName));
        }

        public virtual void NotifyPropertyChanged(string propertyName)
        {
            OnPropertyChanged(propertyName);
        }

        public virtual bool IsItem()
        {
            return ItemType != ItemType.None;
        }

        public virtual bool IsElementAdd()
        {
            return ItemType == ItemType.None && HasModel();
        }

        public virtual void UpdateModel(object model)
        {
            Model = model;
        }

        public virtual bool HasModel()
        {
            return Model != null;
        }

        public virtual bool HasModel<T>(out T model) where T : class
        {
            model = Model as T;
            return model != null;
        }

        public virtual bool IsElementTree()
        {
            return ItemType != ItemType.None && ElementID != -1 && ElementType != (DISPLAY_ELEMENT)(-1);
        }

        public virtual bool IsCommandTree()
        {
            return ItemType != ItemType.None && DeckCommandType != (StreamDeckCommand)(-1);
        }

        public virtual bool IsElement()
        {
            return ItemType == ItemType.Element && HasModel() && ElementID != -1 && ElementType != (DISPLAY_ELEMENT)(-1);
        }

        public virtual bool IsManipulator()
        {
            return ItemType == ItemType.Manipulator && HasModel() && ElementID != -1 && ManipulatorID != -1;
        }

        public virtual bool IsManipulatorCondition()
        {
            return ItemType == ItemType.Condition && HasModel() && ElementID != -1 && ManipulatorID != -1 && ConditionID != -1;
        }

        public virtual bool IsCommandType()
        {
            return ItemType == ItemType.CommandType && DeckCommandType != (StreamDeckCommand)(-1) && CommandID == -1;
        }

        public virtual bool IsCommand()
        {
            return ItemType == ItemType.Command && HasModel() && DeckCommandType != (StreamDeckCommand)(-1) && CommandID != -1;
        }

        public virtual bool IsCommandCondition()
        {
            return ItemType == ItemType.Condition && HasModel() && DeckCommandType != (StreamDeckCommand)(-1) && CommandID != -1 && ConditionID != -1;
        }

        public virtual bool IsCondition()
        {
            return ItemType == ItemType.Condition && HasModel() && ConditionID != -1;
        }

        public virtual bool IsModifiable()
        {
            return IsItem() && HasModel();
        }
    }
}
