using PilotsDeck.UI.ActionDesignerUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PilotsDeck.UI.ActionDesignerUI.TreeViews
{
    public partial class ActionTreeView : UserControl
    {
        public virtual ViewModelAction ModelAction { get; }
        public virtual TreeItemData CurrentItem => ModelAction.CurrentItem;
        public virtual IViewCollection ViewModelCollection { get; }
        public virtual List<TreeItemData> ItemsSource => ViewModelCollection.TreeItems;

        public ActionTreeView(ViewModelAction viewModel, IViewCollection itemCollection)
        {
            InitializeComponent();
            ModelAction = viewModel;
            ViewModelCollection = itemCollection;
            ModelAction.Clipboard.ClipboardChanged += Clipboard_StateChanged;

            TreeViewControl.ItemsSource = ViewModelCollection;
            TreeViewControl.SelectedItemChanged += SelectedItemChanged;
            TreeViewControl.LayoutUpdated += TreeView_LayoutUpdated;
        }

        public virtual void AddToPanel(DockPanel panel)
        {
            DockPanel.SetDock(this, Dock.Top);
            panel.Children.Add(this);
        }

        protected virtual void SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (TreeViewControl.SelectedValue is TreeItemData itemData)
            {
                ModelAction.CurrentItem = itemData;
                e.Handled = true;
            }
        }

        public virtual void ClearSelection()
        {
            if (TreeViewControl.SelectedItem == null)
                return;

            ViewModelCollection.WalkTree((i) => i.IsSelected = false);
        }

        protected virtual void Clipboard_StateChanged(TreeItemData copiedItem)
        {
            ViewModelCollection.WalkTree((item) => item.UpdatePasteState(ModelAction));
        }

        protected virtual void TreeView_LayoutUpdated(object? sender, EventArgs e)
        {
            if (ViewModelCollection.RefreshFocus)
            {
                BringCurrentIntoView();
                ViewModelCollection.RefreshFocus = false;
            }
        }

        public virtual void BringCurrentIntoView()
        {
            var tvi = GetTreeViewItem(CurrentItem);
            tvi?.BringIntoView();
        }

        public virtual TreeViewItem GetTreeViewItem(TreeItemData itemData = null)
        {
            return SearchVisualItem(TreeViewControl, itemData ?? TreeViewControl.SelectedValue as TreeItemData);
        }

        protected virtual TreeViewItem SearchVisualItem(Visual visualItem, TreeItemData currentItem)
        {
            int count = VisualTreeHelper.GetChildrenCount(visualItem);
            Visual childVisual;
            TreeViewItem result = null;

            for (int i = 0; i < count; i++)
            {
                childVisual = (Visual)VisualTreeHelper.GetChild(visualItem, i);
                if (childVisual is not TreeViewItem treeItem)
                    result = SearchVisualItem(childVisual, currentItem);
                else if (treeItem.Header is TreeItemData itemData && itemData.IsItemSelected(currentItem))
                    result = treeItem;
                else
                    result = SearchVisualItem(treeItem, currentItem);

                if (result != null)
                    break;
            }

            return result;
        }
    }
}
