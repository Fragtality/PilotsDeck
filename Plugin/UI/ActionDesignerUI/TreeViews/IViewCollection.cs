using PilotsDeck.Actions.Advanced;
using PilotsDeck.UI.ActionDesignerUI.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace PilotsDeck.UI.ActionDesignerUI.TreeViews
{
    public enum ActionTreeType
    {
        Elements = 1,
        Commands = 2,
    }

    public interface IViewCollection : IEnumerable, INotifyCollectionChanged
    {
        public ViewModelAction ModelAction { get; }
        public ActionMeta Action => ModelAction.Action;
        public IViewCollection Interface { get; }
        public ActionTreeType TreeType { get; }
        public List<TreeItemData> TreeItems { get; }
        public int Count => TreeItems.Count;
        public bool RefreshFocus { get; set; }

        public void NotifyCollectionChanged(NotifyCollectionChangedEventArgs e = null);
        public bool IsItemInTree(TreeItemData treeItem);
        public List<TreeItemData> BuildTreeItemData();

        public virtual TreeItemData MoveItem(TreeItemData prevItem, string propertyId, int offset)
        {
            var list = SearchList(TreeItems, prevItem);

            var prevId = prevItem.GetId(propertyId);
            var nextId = prevId + offset;

            var nextItem = SearchItem(list, propertyId, nextId);
            if (prevItem == null || nextItem == null)
                return TreeItemData.CreateNone();

            prevItem.UpdateId(propertyId, nextId);
            nextItem.UpdateId(propertyId, prevId);

            Sort(list, propertyId);
            NotifyCollectionChanged();

            return prevItem;
        }

        public virtual TreeItemData RemoveItem(TreeItemData item, string propertyId)
        {
            var list = SearchList(TreeItems, item);

            int oldId = item.GetId(propertyId);
            TreeItemData newItem = TreeItemData.CreateNone();

            if (list == null)
                return newItem;
            list.Remove(item);
            
            if (oldId >= list.Count)
                oldId--;

            for (int i = 0; i < list.Count; i++)
            {
                list[i].UpdateId(propertyId, i);
                if (i == oldId)
                    newItem = list[i];
            }

            if (newItem.IsItem())
                newItem.IsSelected = true;

            Sort(list, propertyId);
            NotifyCollectionChanged();
            return newItem;
        }

        public static TreeItemData SearchItem(List<TreeItemData> source, string propertyId, int id)
        {
            var query = source?.Where(i => i.GetId(propertyId) == id);
            if (query?.Any() == true)
                return query.First();
            else
                return null;
        }

        public virtual List<TreeItemData> SearchList(List<TreeItemData> source, TreeItemData item)
        {
            if (source.Contains(item))
                return source;
            
            foreach (var child in source)
            {
                var result = SearchList(child.Children, item);
                if (result != null)
                    return result;
            }

            return null;
        }

        public static void Sort(List<TreeItemData> itemList, string propertyId)
        {
            itemList?.Sort(delegate (TreeItemData x, TreeItemData y)
            {
                if (x?.GetId(propertyId) > y?.GetId(propertyId))
                    return 1;
                else if (x?.GetId(propertyId) < y?.GetId(propertyId))
                    return -1;
                else
                    return 0;
            });
        }

        public virtual void RefreshNames()
        {
            WalkTree((item) => { item.NotifyNameChanged(); });
        }

        public virtual void WalkTree(Action<TreeItemData> action, List<TreeItemData> itemsSource = null)
        {
            WalkItems(itemsSource ?? TreeItems, action);
        }

        protected virtual void WalkItems(List<TreeItemData> items, Action<TreeItemData> action)
        {
            if (items == null)
                return;

            foreach (var item in items)
            {
                WalkItems(item?.Children, action);
                action?.Invoke(item);
            }
        }
    }
}
