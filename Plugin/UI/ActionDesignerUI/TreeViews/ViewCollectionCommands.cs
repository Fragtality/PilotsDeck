using CFIT.AppFramework.UI.ViewModels;
using PilotsDeck.Actions;
using PilotsDeck.Actions.Advanced;
using PilotsDeck.Actions.Advanced.SettingsModel;
using PilotsDeck.UI.ActionDesignerUI.TreeViews;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace PilotsDeck.UI.ActionDesignerUI.ViewModels.Commands
{
    public partial class ViewCollectionCommands(ViewModelAction modelAction)
                            : ViewModelBase<SortedDictionary<StreamDeckCommand, SortedDictionary<int, ModelCommand>>>(modelAction.Settings.ActionCommands), IViewCollection
    {
        public virtual ViewModelAction ModelAction { get; } = modelAction;
        public virtual ActionMeta Action => ModelAction.Action;
        public IViewCollection Interface => this;
        public virtual ActionTreeType TreeType => ActionTreeType.Commands;
        public virtual List<TreeItemData> TreeItems { get; } = [];
        public virtual int Count => TreeItems.Count;
        public virtual bool RefreshFocus { get; set; } = false;

        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        protected override void InitializeModel()
        {
            BuildTreeItemData();
        }

        public virtual void NotifyCollectionChanged(NotifyCollectionChangedEventArgs e = null)
        {
            e ??= new(NotifyCollectionChangedAction.Reset);
            CollectionChanged?.Invoke(this, e);
        }

        public virtual bool IsItemInTree(TreeItemData item)
        {
            return item.IsCommandTree();
        }

        public virtual List<TreeItemData> BuildTreeItemData()
        {
            TreeItems.Clear();

            foreach (var commandType in Source)
            {
                var item = TreeItemData.CreateCommandType(commandType.Key, commandType.Value, ModelAction);
                if (item.Children.Count > 0)
                    item.IsExpanded = true;
                TreeItems.Add(item);
            }

            NotifyCollectionChanged();
            return TreeItems;
        }

        public IEnumerator GetEnumerator()
        {
            return TreeItems.GetEnumerator();
        }
    }
}
