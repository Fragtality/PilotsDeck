using CFIT.AppFramework.UI.ViewModels;
using CFIT.AppTools;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PilotsDeck.Actions;
using PilotsDeck.Actions.Advanced;
using PilotsDeck.Actions.Advanced.SettingsModel;
using PilotsDeck.Tools;
using PilotsDeck.UI.ActionDesignerUI.Clipboard;
using PilotsDeck.UI.ActionDesignerUI.TreeViews;
using PilotsDeck.UI.ActionDesignerUI.ViewModels.Commands;
using PilotsDeck.UI.ActionDesignerUI.ViewModels.Elements;
using PilotsDeck.UI.Converter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PilotsDeck.UI.ActionDesignerUI.ViewModels
{
    public partial class ViewModelAction : ViewModelBase<SettingsModelMeta>
    {
        public virtual Window WindowInstance { get; }
        public virtual ActionMeta Action { get; }
        public virtual SettingsModelMeta Settings => Source;
        protected virtual bool IsTemplateDeclined { get; set; } = false;
        public virtual ViewCollectionElements Elements { get; }
        public virtual ViewCollectionCommands Commands { get; }
        protected virtual TypeIconConverter IconConverter { get; } = new();
        public virtual ActionClipboard Clipboard { get; }

        public virtual ObservableCollection<KeyValuePair<Enum, string>> ItemsType { get; } = [];

        public ViewModelAction(ActionMeta action, Window windowInstance) : base(action.Settings)
        {
            Action = action;
            Elements = new(this);
            Commands = new(this);
            Clipboard = new(this);
            WindowInstance = windowInstance;
        }

        protected override void InitializeModel()
        {
            
        }

        public virtual void UpdateAction()
        {
            Action.UpdateRessources(true);
        }

        public virtual void ReloadActionTree(bool updateResources = true)
        {
            if (updateResources)
                UpdateAction();
            Elements.NotifyCollectionChanged();
            Commands.NotifyCollectionChanged();
        }

        [ObservableProperty]
        protected bool _IsTreeRefreshNeeded;

        public virtual void NotifyTreeRefresh()
        {
            NotifyPropertyChanged(nameof(IsTreeRefreshNeeded));
        }

        protected virtual bool CheckStateManipulator()
        {
            return CurrentItem.IsElement();
        }

        protected virtual bool CheckStateCondition()
        {
            return CurrentItem.IsCommand() || CurrentItem.IsManipulator() || CurrentItem.IsCondition();
        }

        public virtual bool CheckStateTemplate()
        {
            return !CurrentItem.IsItem() && !CurrentItem.HasModel() && Elements.Count == 0 && Commands.Count == 0 && !IsTemplateDeclined;
        }

        public virtual int GetSelectionIndex()
        {
            if (CurrentItem.IsElement())
            {
                if (CurrentItem.ElementType == DISPLAY_ELEMENT.GAUGE || CurrentItem.ElementType == DISPLAY_ELEMENT.VALUE)
                    return 1;
                else
                    return 0;
            }
            else
                return 0;
        }

        protected virtual Visibility GetVisibilityType()
        {
            ItemsType.Clear();
            if (CheckStateTemplate())
            {
                ViewModelHelper.SetTemplateTypes(ItemsType);
                return Visibility.Visible;
            }
            else if (CurrentItem.IsElementAdd())
            {
                ViewModelHelper.SetElementTypes(ItemsType);
                return Visibility.Visible;
            }
            else if (CheckStateManipulator())
            {
                ViewModelHelper.SetManipulatorTypes(ItemsType, CurrentItem.ElementType);
                return Visibility.Visible;
            }
            else
                return Visibility.Collapsed;
        }

        protected virtual Visibility GetVisibilityAdd()
        {
            if (CheckStateTemplate())
                return Visibility.Visible;
            else if (CurrentItem.IsElementAdd())
                return Visibility.Visible;
            else if (CurrentItem.IsCommandType())
                return Visibility.Visible;
            else if (CheckStateManipulator())
                return Visibility.Visible;
            else if (CheckStateCondition())
                return Visibility.Visible;
            else
                return Visibility.Collapsed;
        }

        protected virtual string GetNameTypeAdd()
        {
            if (CheckStateTemplate())
                return "Template";
            else if (CurrentItem.IsElementAdd())
                return "Element";
            else if (CurrentItem.IsCommandType())
                return "Command";
            else if (CheckStateManipulator())
                return "Manipulator";
            else if (CheckStateCondition())
                return "Condition";
            else
                return "None";
        }

        protected virtual BitmapImage GetTypeIcon()
        {
            if (CheckStateCondition())
                return Img.GetAssemblyImage("Condition");
            else if (CurrentItem.IsCommandType())
                return IconConverter.Convert(CurrentItem.DeckCommandType, typeof(ImageSource), null, null) as BitmapImage;
            else
                return null;
        }

        protected virtual bool GetItemModifiable()
        {
            return CurrentItem?.IsModifiable() == true;
        }

        public virtual void SetElementRootSelection()
        {
            CurrentItem = TreeItemData.CreateElementRoot();
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(VisibilityType))]
        [NotifyPropertyChangedFor(nameof(VisibilityAdd))]
        [NotifyPropertyChangedFor(nameof(VisibilityTypeIcon))]
        [NotifyPropertyChangedFor(nameof(TypeIcon))]
        [NotifyPropertyChangedFor(nameof(IsVisibleAdd))]
        [NotifyPropertyChangedFor(nameof(NameTypeAdd))]
        [NotifyPropertyChangedFor(nameof(IsItemModifiable))]
        [NotifyCanExecuteChangedFor(nameof(AddItemCommand))]
        [NotifyCanExecuteChangedFor(nameof(RemoveCommand))]
        [NotifyCanExecuteChangedFor(nameof(MoveUpCommand))]
        [NotifyCanExecuteChangedFor(nameof(MoveDownCommand))]
        protected TreeItemData _CurrentItem = TreeItemData.CreateNone();

        public virtual Visibility VisibilityType => GetVisibilityType();

        public virtual Visibility VisibilityAdd => GetVisibilityAdd();

        public virtual Visibility VisibilityTypeIcon => (VisibilityType != Visibility.Visible && VisibilityAdd == Visibility.Visible ? Visibility.Visible : Visibility.Collapsed);

        public virtual BitmapImage TypeIcon => GetTypeIcon();

        public virtual bool IsVisibleAdd => VisibilityAdd == Visibility.Visible;

        public virtual string NameTypeAdd => GetNameTypeAdd();

        public virtual bool IsItemModifiable => GetItemModifiable();

        public virtual int AddElement(ModelDisplayElement model, TreeItemData currentItem)
        {
            return Action.AddDisplayElement(model.ElementType, model);
        }

        public virtual int AddManipulator(ModelManipulator model, TreeItemData currentItem)
        {
            if (currentItem.IsElementTree())
                return Action.DisplayElements[currentItem.ElementID].AddManipulator(model.ManipulatorType, model);
            else
                return -1;
        }

        public virtual int AddManipulatorCondition(ConditionHandler model, TreeItemData currentItem)
        {
            if (currentItem.IsManipulator() || currentItem.IsManipulatorCondition())
                return Action.DisplayElements[currentItem.ElementID].ElementManipulators[currentItem.ManipulatorID].AddCondition(model);
            else
                return -1;
        }

        public virtual int AddCommand(ModelCommand model, TreeItemData currentItem)
        {
            if (currentItem.IsCommandTree())
                return Action.AddCommand(new ActionCommand(model), currentItem.DeckCommandType);
            else
                return -1;
        }

        public virtual int AddCommandCondition(ConditionHandler model, TreeItemData currentItem)
        {
            if (currentItem.IsCommand() || currentItem.IsCommandCondition())
                return Action.AddActionCondition(currentItem.DeckCommandType, currentItem.CommandID, model);
            else
                return -1;
        }
        
        [RelayCommand(CanExecute = nameof(IsVisibleAdd))]
        protected virtual void AddItem(Enum? type)
        {
            if (CurrentItem.IsElementAdd() && type?.IsEnumType<DISPLAY_ELEMENT>() == true)
            {
                DoAdd<DISPLAY_ELEMENT, ModelDisplayElement>(
                    type.ToEnumValue<DISPLAY_ELEMENT>(),
                    null,
                    Elements,
                    (enumType) => new ModelDisplayElement(enumType),
                    AddElement,
                    (enumType, model, id) => TreeItemData.CreateElement(enumType, new(model, this), id)
                );
            }
            else if (CheckStateManipulator() && type?.IsEnumType<ELEMENT_MANIPULATOR>() == true)
            {
                DoAdd<ELEMENT_MANIPULATOR, ModelManipulator>(
                    type.ToEnumValue<ELEMENT_MANIPULATOR>(),
                    Elements.TreeItems[CurrentItem.ElementID],
                    Elements,
                    (enumType) => new ModelManipulator(enumType),
                    AddManipulator,
                    (enumType, model, id) => TreeItemData.CreateManipulator(CurrentItem, id, new(model, this))
                );
            }
            else if (CurrentItem.IsCommandType() && type != null)
            {
                DoAdd<StreamDeckCommand, ModelCommand>(
                    CurrentItem.DeckCommandType,
                    Commands.TreeItems[(int)CurrentItem.DeckCommandType],
                    Commands,
                    (enumType) => new ModelCommand(CurrentItem.DeckCommandType),
                    AddCommand,
                    (enumType, model, id) => TreeItemData.CreateCommand(CurrentItem, id, new(model, this))
                );
            }
            else if (CheckStateCondition())
            {
                if (CurrentItem.IsManipulator() || CurrentItem.IsManipulatorCondition())
                {
                    DoAdd<ELEMENT_MANIPULATOR, ConditionHandler>(
                        CurrentItem.ManipulatorType,
                        Elements.TreeItems[CurrentItem.ElementID]?.Children[CurrentItem.ManipulatorID],
                        Elements,
                        (enumType) => new ConditionHandler(),
                        AddManipulatorCondition,
                        (enumType, model, id) => TreeItemData.CreateManipulatorCondition(CurrentItem, id, new(model, this))
                    );
                }
                else if (CurrentItem.IsCommand() || CurrentItem.IsCommandCondition())
                {
                    DoAdd<StreamDeckCommand, ConditionHandler>(
                        CurrentItem.DeckCommandType,
                        Commands.TreeItems[(int)CurrentItem.DeckCommandType]?.Children[CurrentItem.CommandID],
                        Commands,
                        (enumType) => new ConditionHandler(),
                        AddCommandCondition,
                        (enumType, model, id) => TreeItemData.CreateCommandCondition(CurrentItem, id, new(model, this))
                    );
                }
            }
            else if (CheckStateTemplate() && type?.IsEnumType<ActionTemplate>() == true)
            {
                ActionTemplate actionTemplate = type.ToEnumValue<ActionTemplate>();
                if (actionTemplate != ActionTemplate.NONE)
                {
                    ElementTemplates.SetTemplate(actionTemplate, Action);
                    UpdateAction();

                    Elements.BuildTreeItemData();
                    Commands.BuildTreeItemData();                    
                }
                else
                    IsTemplateDeclined = true;

                CurrentItem = TreeItemData.CreateNone();
                NotifyPropertyChanged(nameof(VisibilityAdd));
                NotifyPropertyChanged(nameof(VisibilityType));
                NotifyPropertyChanged(nameof(CurrentItem));
            }
        }

        protected virtual void DoAdd<Tenum, Msetting>(Tenum type, TreeItemData target, IViewCollection collection,
                                                      Func<Tenum, Msetting> createModel, Func<Msetting, TreeItemData, int> addFunc,
                                                      Func<Tenum, Msetting, int, TreeItemData> createItem)
        {
            var model = createModel(type);
            var id = addFunc(model, CurrentItem);
            UpdateAction();

            var newItem = createItem(type, model, id);
            newItem.IsSelected = true;
            if (target?.Children != null)
            {
                target.Children.Add(newItem);
                target.IsExpanded = true;
            }
            else
                collection.TreeItems.Add(newItem);

            collection.RefreshFocus = true;
            collection.NotifyCollectionChanged();
        }

        [RelayCommand(CanExecute = nameof(IsItemModifiable))]
        protected virtual void Remove()
        {
            if (CurrentItem.IsElement())
            {
                DoRemove(Elements, nameof(TreeItemData.ElementID),
                         () => Action.RemoveDisplayElement(CurrentItem.ElementID), null);
            }
            else if (CurrentItem.IsManipulator())
            {
                DoRemove(Elements, nameof(TreeItemData.ManipulatorID),
                         () => Action.DisplayElements[CurrentItem.ElementID]?.RemoveManipulator(CurrentItem.ManipulatorID) == true,
                         Elements.TreeItems[CurrentItem.ElementID]);
            }
            else if (CurrentItem.IsManipulatorCondition())
            {
                DoRemove(Elements, nameof(TreeItemData.ConditionID),
                         () => Action.DisplayElements[CurrentItem.ElementID]?.ElementManipulators[CurrentItem.ManipulatorID]?.RemoveCondition(CurrentItem.ConditionID) == true,
                         Elements.TreeItems[CurrentItem.ElementID]?.Children[CurrentItem.ManipulatorID]);
            }
            else if (CurrentItem.IsCommand())
            {
                DoRemove(Commands, nameof(TreeItemData.CommandID),
                         () => Action.RemoveCommand(CurrentItem.DeckCommandType, CurrentItem.CommandID),
                         Commands.TreeItems[(int)CurrentItem.DeckCommandType]);
            }
            else if (CurrentItem.IsCommandCondition())
            {
                DoRemove(Commands, nameof(TreeItemData.ConditionID),
                         () => Action.RemoveActionCondition(CurrentItem.DeckCommandType, CurrentItem.CommandID, CurrentItem.ConditionID),
                         Commands.TreeItems[(int)CurrentItem.DeckCommandType]?.Children[CurrentItem.CommandID]);
            }
        }

        protected virtual void DoRemove(IViewCollection collection, string propertyId, Func<bool> removeAction, TreeItemData fallbackItem)
        {
            if (removeAction?.Invoke() == false)
                return;
            UpdateAction();

            CurrentItem = collection.RemoveItem(CurrentItem, propertyId);
            if (!CurrentItem.IsItem())
            {
                collection.WalkTree((i) => i.IsSelected = false);
                if (fallbackItem?.IsItem() == true)
                    fallbackItem.IsSelected = true;
            }
            collection.RefreshFocus = true;
        }

        [RelayCommand(CanExecute = nameof(IsItemModifiable))]
        protected virtual void MoveUp()
        {
            SwapElement(-1);
        }

        [RelayCommand(CanExecute = nameof(IsItemModifiable))]
        protected virtual void MoveDown()
        {
            SwapElement(1);
        }

        protected virtual void SwapElement(int offset)
        {
            if (CurrentItem.IsElement() && Action.SwapElement(CurrentItem.ElementID, CurrentItem.ElementID + offset))
                DoSwap(Elements, nameof(TreeItemData.ElementID), offset);
            else if (CurrentItem.IsManipulator() && Action.SwapManipulator(CurrentItem.ElementID, CurrentItem.ManipulatorID, CurrentItem.ManipulatorID + offset))
                DoSwap(Elements, nameof(TreeItemData.ManipulatorID), offset);
            else if (CurrentItem.IsManipulatorCondition() && Action.SwapManipulatorCondition(CurrentItem.ElementID, CurrentItem.ManipulatorID, CurrentItem.ConditionID, CurrentItem.ConditionID + offset))
                DoSwap(Elements, nameof(TreeItemData.ConditionID), offset);
            else if (CurrentItem.IsCommand() && Action.SwapCommand(CurrentItem.DeckCommandType, CurrentItem.CommandID, CurrentItem.CommandID + offset))
                DoSwap(Commands, nameof(TreeItemData.CommandID), offset);
            else if (CurrentItem.IsCommandCondition() && Action.SwapActionCondition(CurrentItem.DeckCommandType, CurrentItem.CommandID, CurrentItem.ConditionID, CurrentItem.ConditionID + offset))
                DoSwap(Commands, nameof(TreeItemData.ConditionID), offset);
        }

        protected virtual void DoSwap(IViewCollection collection, string propertyId, int offset)
        {
            CurrentItem = collection.MoveItem(CurrentItem, propertyId, offset);
            collection.RefreshFocus = true;
        }
    }
}
