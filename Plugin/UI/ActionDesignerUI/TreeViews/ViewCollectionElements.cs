using CFIT.AppFramework.UI.ViewModels;
using PilotsDeck.Actions.Advanced;
using PilotsDeck.Actions.Advanced.SettingsModel;
using PilotsDeck.UI.ActionDesignerUI.TreeViews;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace PilotsDeck.UI.ActionDesignerUI.ViewModels.Elements
{
    public partial class ViewCollectionElements(ViewModelAction modelAction)
                            : ViewModelBase<SortedDictionary<int, ModelDisplayElement>>(modelAction.Settings.DisplayElements), IViewCollection
    {
        public virtual ViewModelAction ModelAction { get; } = modelAction;
        public virtual ActionMeta Action => ModelAction.Action;
        public virtual ActionTreeType TreeType => ActionTreeType.Elements;
        public IViewCollection Interface => this;
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
            return item.IsElementTree();
        }

        public virtual List<TreeItemData> BuildTreeItemData()
        {
            TreeItems.Clear();

            foreach (var element in Source)
                TreeItems.Add(TreeItemData.CreateElement(element.Value.ElementType, new ViewModelElement(element.Value, ModelAction), element.Key));

            NotifyCollectionChanged();
            return TreeItems;
        }

        public IEnumerator GetEnumerator()
        {
            return TreeItems.GetEnumerator();
        }
    }
}
