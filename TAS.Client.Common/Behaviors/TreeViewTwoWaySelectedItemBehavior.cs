using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace TAS.Client.Common.Behaviors
{
    public class TreeViewTwoWaySelectedItemBehavior : Behavior<TreeView>
    {
        #region SelectedItem Property

        public object SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(object), typeof(TreeViewTwoWaySelectedItemBehavior), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));

        private static void OnSelectedItemChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(sender is TreeViewTwoWaySelectedItemBehavior behavior))
                return;
            var tvi = behavior.FindItemNode(e.NewValue);
            if (tvi != null)
                tvi.IsSelected = true;
        }

        #endregion

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.SelectedItemChanged += OnTreeViewSelectedItemChanged;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            if (AssociatedObject != null)
            {
                AssociatedObject.SelectedItemChanged -= OnTreeViewSelectedItemChanged;
            }
        }

        private void OnTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            SelectedItem = e.NewValue;
        }


        private TreeViewItem FindItemNode(object item)
        {
            TreeViewItem node = null;
            foreach (var data in AssociatedObject.Items)
            {
                node = AssociatedObject.ItemContainerGenerator.ContainerFromItem(data) as TreeViewItem;
                if (node == null) continue;
                if (data == item)
                    break;
                node = FindItemNodeInChildren(node, item);
                if (node != null)
                    break;
            }
            return node;
        }

        private static TreeViewItem FindItemNodeInChildren(TreeViewItem parent, object item)
        {
            TreeViewItem node = null;
            if (parent == null)
                return null;
            var isExpanded = parent.IsExpanded;
            if (!isExpanded) //Can't find child container unless the parent node is Expanded once
            {
                parent.IsExpanded = true;
                parent.UpdateLayout();
            }
            foreach (var data in parent.Items)
            {
                node = parent.ItemContainerGenerator.ContainerFromItem(data) as TreeViewItem;
                if (data == item && node != null)
                    break;
                node = FindItemNodeInChildren(node, item);
                if (node != null)
                    break;
            }
            if (node == null && parent.IsExpanded != isExpanded)
                parent.IsExpanded = isExpanded;
            if (node != null)
                parent.IsExpanded = true;
            return node;
        }
    }
}
