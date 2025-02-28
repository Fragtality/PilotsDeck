using CFIT.AppFramework.UI.ViewModels.Commands;
using CommunityToolkit.Mvvm.ComponentModel;
using PilotsDeck.Actions;
using PilotsDeck.Actions.Advanced;
using PilotsDeck.Actions.Advanced.SettingsModel;
using PilotsDeck.UI.ActionDesignerUI.TreeViews;
using PilotsDeck.UI.ActionDesignerUI.ViewModels;
using PilotsDeck.UI.ActionDesignerUI.ViewModels.Elements;
using PilotsDeck.UI.ActionDesignerUI.ViewModels.Manipulators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace PilotsDeck.UI.ActionDesignerUI.Clipboard
{
    public enum PasteType
    {
        NotCompatible = 0,
        Element,
        Manipulator,
        Condition,
        Command,
        CommandList,
        ManipulatorList,
        ConditionList,
    }

    public partial class ActionClipboard : ObservableObject
    {
        public event Action<TreeItemData> ClipboardChanged;

        public virtual ViewModelAction ModelAction { get; }
        public virtual List<TreeItemData> ElementTree => ModelAction.Elements.TreeItems;
        public virtual List<TreeItemData> CommandTree => ModelAction.Commands.TreeItems;
        public virtual SortedDictionary<int, ModelDisplayElement> DisplayElements => ModelAction.Source.DisplayElements;
        public virtual SortedDictionary<StreamDeckCommand, SortedDictionary<int, ModelCommand>> ActionCommands => ModelAction.Source.ActionCommands;
        public static TreeItemData CopiedItem { get; set; } = null;
        protected virtual TreeItemData LastCopyState { get; set; } = null;
        public virtual TreeItemData CurrentItem => ModelAction.CurrentItem;

        protected virtual DispatcherTimer CopyMonitor { get; }
        public virtual bool IsCopyAllowed => IsCopyable();
        public virtual bool IsPasteAllowed => IsPasteable();
        public virtual bool IsCopyPasteAllowed => IsCopyAllowed || IsPasteAllowed;
        public virtual CommandWrapper<TreeItemData> CopyPasteCommand { get; }
        public virtual CommandWrapper<TreeItemData> PasteListCommand { get; }

        public virtual Brush BrushCopyPaste => GetBrush();
        public static Brush BrushPaste { get; } = new SolidColorBrush(Colors.Green);
        public virtual Thickness ThicknessCopyPaste => GetThickness();
        public static Thickness ThicknessPaste { get; } = new Thickness(1.5);
        public static Thickness ThicknessDefault { get; } = new Thickness(1);
        
        public ActionClipboard(ViewModelAction modelAction)
        {
            ModelAction = modelAction;
            LastCopyState = CopiedItem;

            CopyPasteCommand = new((item) => CopyPaste(IsPasteable(CopiedItem, item), item), (item) => IsCopyable(item) || IsPasteable(item));
            CopyPasteCommand.Subscribe(this, nameof(IsCopyPasteAllowed));
            ModelAction.SubscribeProperty(nameof(ModelAction.CurrentItem), NotifyChange);

            PasteListCommand = new((item) => CopyPaste(IsPasteableList(CopiedItem, item), item), IsPasteableList);

            CopyMonitor = new()
            {
                Interval = TimeSpan.FromMilliseconds(App.Configuration.IntervalCheckUiClose)
            };
            CopyMonitor.Tick += CheckCopy;
            CopyMonitor.Start();
        }

        public virtual void StopMonitor()
        {
            CopyMonitor.Stop();
        }

        protected virtual void CheckCopy(object? sender, EventArgs e)
        {
            if (App.CancellationToken.IsCancellationRequested)
                StopMonitor();
            else if (LastCopyState != CopiedItem)
            {
                LastCopyState = CopiedItem;
                NotifyChange();
            }
        }

        protected virtual void NotifyChange()
        {
            OnPropertyChanged(nameof(IsCopyAllowed));
            OnPropertyChanged(nameof(IsPasteAllowed));
            OnPropertyChanged(nameof(IsCopyPasteAllowed));
            OnPropertyChanged(nameof(BrushCopyPaste));
            OnPropertyChanged(nameof(ThicknessCopyPaste));
            OnPropertyChanged(nameof(CopiedItem));
            ClipboardChanged?.Invoke(CopiedItem);
        }

        protected virtual Brush GetBrush()
        {
            if (CopiedItem == null)
                return SystemColors.WindowFrameBrush;
            else if (IsPasteAllowed)
                return BrushPaste;
            else
                return SystemColors.HighlightBrush;
        }

        protected virtual Thickness GetThickness()
        {
            if (CopiedItem == null)
                return ThicknessDefault;
            else
                return ThicknessPaste;
        }

        public virtual bool IsCopyable()
        {
            return IsCopyable(CurrentItem);
        }

        public static bool IsCopyable(TreeItemData currentItem)
        {
            return CopiedItem == null && (currentItem.IsModifiable() || currentItem.IsCommandType());
        }

        public virtual bool IsPasteable()
        {
            return IsPasteable(CurrentItem);
        }

        public static bool IsPasteable(TreeItemData currentItem)
        {
            return CopiedItem != null && IsPasteable(CopiedItem, currentItem) != PasteType.NotCompatible;
        }

        public static PasteType IsPasteable(TreeItemData copiedItem, TreeItemData currentItem)
        {
            if (copiedItem == null || currentItem == null)
                return PasteType.NotCompatible;

            //Paste Element
            if (copiedItem.IsElement() && (currentItem.IsElement() || currentItem.IsElementAdd()))
                return PasteType.Element;
            //Paste Manipulator
            else if (copiedItem.IsManipulator() && (currentItem.IsElement() || currentItem.IsManipulator()))
                return PasteType.Manipulator;
            //Paste Condition
            else if (copiedItem.IsCondition() && (currentItem.IsManipulator() || currentItem.IsCommand() || currentItem.IsCondition()))
                return PasteType.Condition;
            //Paste Command List
            else if (copiedItem.IsCommandType() && currentItem.IsCommandType())
                return PasteType.CommandList;
            //Paste Command
            else if (copiedItem.IsCommand() && (currentItem.IsCommandType() || currentItem.IsCommand()))
                return PasteType.Command;
            else
                return PasteType.NotCompatible;
        }

        public static bool IsPasteableList(TreeItemData currentItem)
        {
            return CopiedItem != null && IsPasteableList(CopiedItem, currentItem) != PasteType.NotCompatible;
        }

        public static PasteType IsPasteableList(TreeItemData copiedItem, TreeItemData currentItem)
        {
            if (copiedItem == null || currentItem == null)
                return PasteType.NotCompatible;

            //Paste Manipulator List
            if (copiedItem.IsElement() && currentItem.IsElement())
                return PasteType.ManipulatorList;
            //Paste Condition List (From Manipulator)
            else if (copiedItem.IsManipulator() && (currentItem.IsManipulator() || currentItem.IsCommand()))
                return PasteType.ConditionList;
            //Paste Condition List (From Command)
            else if (copiedItem.IsCommand() && (currentItem.IsManipulator() || currentItem.IsCommand()))
                return PasteType.ConditionList;
            else
                return PasteType.NotCompatible;
        }

        public virtual void CopyPaste(PasteType type, TreeItemData currentItem)
        {
            currentItem ??= CurrentItem;

            if (IsCopyable())
                CopiedItem = currentItem;
            else if (CopiedItem != null)
                ExecutePaste(type, currentItem);

            NotifyChange();
        }

        public virtual void ExecutePaste(PasteType type, TreeItemData currentItem)
        {
            if (type == PasteType.NotCompatible)
                return;

            //Paste Element
            if (type == PasteType.Element)
            {
                PasteSingleItem<ViewModelElement, ModelDisplayElement>(
                    null,
                    currentItem,
                    ModelAction.Elements,
                    (viewModel) => viewModel.Source.Copy(),
                    ModelAction.AddElement,
                    (copy) => new ViewModelElement(copy, ModelAction),
                    (viewModel, id) => TreeItemData.CreateElement(CopiedItem, id, viewModel)
                );
            }
            //Paste Manipulator
            else if (type == PasteType.Manipulator)
            {
                PasteSingleItem<ViewModelManipulator, ModelManipulator>(
                    ElementTree[currentItem.ElementID],
                    currentItem,
                    ModelAction.Elements,
                    (viewModel) => viewModel.Source.Copy(),
                    ModelAction.AddManipulator,
                    (copy) => new ViewModelManipulator(copy, ModelAction),
                    (viewModel, id) => TreeItemData.CreateManipulator(currentItem, id, viewModel)
                );
            }
            //Paste Manipulator Condition
            else if (type == PasteType.Condition && currentItem.IsElementTree())
            {
                PasteSingleItem<ViewModelCondition, ConditionHandler>(
                    ElementTree[currentItem.ElementID]?.Children[currentItem.ManipulatorID],
                    currentItem,
                    ModelAction.Elements,
                    (viewModel) => viewModel.Source.Copy(),
                    ModelAction.AddManipulatorCondition,
                    (copy) => new ViewModelCondition(copy, ModelAction),
                    (viewModel, id) => TreeItemData.CreateManipulatorCondition(currentItem, id, viewModel)
                );
            }
            //Paste Command Condition
            else if (type == PasteType.Condition && currentItem.IsCommandTree())
            {
                PasteSingleItem<ViewModelCondition, ConditionHandler>(
                    CommandTree[(int)currentItem.DeckCommandType]?.Children[currentItem.CommandID],
                    currentItem,
                    ModelAction.Commands,
                    (viewModel) => viewModel.Source.Copy(),
                    ModelAction.AddCommandCondition,
                    (copy) => new ViewModelCondition(copy, ModelAction),
                    (viewModel, id) => TreeItemData.CreateCommandCondition(currentItem, id, viewModel)
                );
            }
            //Paste Command
            else if (type == PasteType.Command)
            {
                PasteSingleItem<ViewModelCommand, ModelCommand>(
                    CommandTree[(int)currentItem.DeckCommandType],
                    currentItem,
                    ModelAction.Commands,
                    (viewModel) => viewModel.Source.Copy(),
                    ModelAction.AddCommand,
                    (copy) => new ViewModelCommand(copy, ModelAction),
                    (viewModel, id) => TreeItemData.CreateCommand(currentItem, id, viewModel)
                );
            }
            //Paste Command List
            else if (type == PasteType.CommandList)
            {
                PasteMultiple<ViewModelCommand, ModelCommand>(
                    CommandTree[(int)currentItem.DeckCommandType],
                    currentItem,
                    ModelAction.Commands,
                    ActionCommands[currentItem.DeckCommandType],
                    (model) => model.Copy(),
                    ModelAction.AddCommand,
                    (copy) => new ViewModelCommand(copy, ModelAction),
                    (viewModel, id) => TreeItemData.CreateCommand(currentItem, id, viewModel)
                );
                currentItem.IsExpanded = true;
            }
            //Paste Manipulator List
            else if (type == PasteType.ManipulatorList)
            {
                PasteMultiple<ViewModelManipulator, ModelManipulator>(
                    ElementTree[currentItem.ElementID],
                    currentItem,
                    ModelAction.Elements,
                    DisplayElements[currentItem.ElementID]?.Manipulators,
                    (model) => model.Copy(),
                    ModelAction.AddManipulator,
                    (copy) => new ViewModelManipulator(copy, ModelAction),
                    (viewModel, id) => TreeItemData.CreateManipulator(currentItem, id, viewModel)
                );
            }
            //Paste Condition List (ElementTree => ElementTree)
            else if (type == PasteType.ConditionList && CopiedItem.IsElementTree() && currentItem.IsElementTree())
            {
                PasteMultiple<ViewModelCondition, ConditionHandler>(
                    ElementTree[currentItem.ElementID]?.Children[currentItem.ManipulatorID],
                    currentItem,
                    ModelAction.Elements,
                    DisplayElements[currentItem.ElementID].Manipulators[currentItem.ManipulatorID]?.Conditions,
                    (model) => model.Copy(),
                    ModelAction.AddManipulatorCondition,
                    (copy) => new ViewModelCondition(copy, ModelAction),
                    (viewModel, id) => TreeItemData.CreateManipulatorCondition(currentItem, id, viewModel)
                );
            }
            //Paste Condition List (ElementTree => CommandTree)
            else if (type == PasteType.ConditionList && CopiedItem.IsElementTree() && currentItem.IsCommandTree())
            {
                PasteMultiple<ViewModelCondition, ConditionHandler>(
                    CommandTree[(int)currentItem.DeckCommandType]?.Children[currentItem.CommandID],
                    currentItem,
                    ModelAction.Commands,
                    ActionCommands[currentItem.DeckCommandType][currentItem.CommandID]?.Conditions,
                    (model) => model.Copy(),
                    ModelAction.AddCommandCondition,
                    (copy) => new ViewModelCondition(copy, ModelAction),
                    (viewModel, id) => TreeItemData.CreateCommandCondition(currentItem, id, viewModel)
                );
            }
            //Paste Condition List (CommandTree => ElementTree)
            else if (type == PasteType.ConditionList && CopiedItem.IsCommandTree() && currentItem.IsElementTree())
            {
                PasteMultiple<ViewModelCondition, ConditionHandler>(
                    ElementTree[currentItem.ElementID]?.Children[currentItem.ManipulatorID],
                    currentItem,
                    ModelAction.Elements,
                    DisplayElements[currentItem.ElementID].Manipulators[currentItem.ManipulatorID]?.Conditions,
                    (model) => model.Copy(),
                    ModelAction.AddManipulatorCondition,
                    (copy) => new ViewModelCondition(copy, ModelAction),
                    (viewModel, id) => TreeItemData.CreateManipulatorCondition(currentItem, id, viewModel)
                );
            }
            //Paste Condition List (CommandTree => CommandTree)
            else if (type == PasteType.ConditionList && CopiedItem.IsCommandTree() && currentItem.IsCommandTree())
            {
                PasteMultiple<ViewModelCondition, ConditionHandler>(
                    CommandTree[(int)currentItem.DeckCommandType]?.Children[currentItem.CommandID],
                    currentItem,
                    ModelAction.Commands,
                    ActionCommands[currentItem.DeckCommandType][currentItem.CommandID]?.Conditions,
                    (model) => model.Copy(),
                    ModelAction.AddCommandCondition,
                    (copy) => new ViewModelCondition(copy, ModelAction),
                    (viewModel, id) => TreeItemData.CreateCommandCondition(currentItem, id, viewModel)
                );
            }
        }

        protected virtual void PasteMultiple<Mview, Msetting>(TreeItemData targetTree, TreeItemData currentItem,
                                                              IViewCollection collection, IDictionary<int, Msetting> target,
                                                              Func<Msetting, Msetting> getCopy, Func<Msetting, TreeItemData, int> addFunc,
                                                              Func<Msetting, Mview> createModel, Func<Mview, int, TreeItemData> createItem)
                                                              where Mview : class where Msetting : class
        {
            if (target == null)
                return;

            if (targetTree?.Children != null)
                targetTree.Children.Clear();
            target.Clear();

            TreeItemData selectItem = null;
            var list = CopiedItem.Children.Select((item) => item.GetModel<IViewModelBaseExtension>().SourceObject as Msetting);
            foreach (var pair in list)
            {
                var copy = getCopy(pair);
                var id = addFunc(copy, currentItem);
                if (id < 0)
                    return;

                Mview newModel = createModel(copy);
                TreeItemData newItem = createItem(newModel, id);
                selectItem ??= newItem;
                if (targetTree?.Children != null)
                    targetTree.Children.Add(newItem);
                else
                    collection.TreeItems.Add(newItem);
            }
            ModelAction.UpdateAction();

            CopiedItem = null;
            if (selectItem?.IsSelected != null)
                selectItem.IsSelected = true;
            if (targetTree?.IsExpanded != null)
                targetTree.IsExpanded = true;
            collection.RefreshFocus = true;
            collection.NotifyCollectionChanged();
        }

        protected virtual void PasteSingleItem<Mview, Msetting>(TreeItemData target, TreeItemData currentItem, IViewCollection collection,
                                                                Func<Mview, Msetting> getCopy, Func<Msetting, TreeItemData, int> addFunc,
                                                                Func<Msetting, Mview> createModel, Func<Mview, int, TreeItemData> createItem)
                                                                where Mview : class where Msetting : class
        {
            if (!CopiedItem.HasModel<Mview>(out var model))
                return;

            Msetting copy = getCopy(model);
            int id = addFunc(copy, currentItem);
            if (id < 0)
                return;
            ModelAction.UpdateAction();

            Mview newModel = createModel(copy);
            TreeItemData newItem = createItem(newModel, id);
            newItem.IsSelected = true;
            if (target?.Children != null)
            {
                target.Children.Add(newItem);
                target.IsExpanded = true;
            }
            else
                collection.TreeItems.Add(newItem);            

            CopiedItem = null;
            collection.RefreshFocus = true;
            collection.NotifyCollectionChanged();
            ModelAction.CurrentItem = newItem;
        }
    }
}
