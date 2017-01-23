using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace TAS.Client.Common.Controls
{
    public class TreeViewEx : TreeView
    {
        public TreeViewEx()
        {
            this.SelectedItemChanged += new RoutedPropertyChangedEventHandler<object>(TreeViewEx_SelectedItemChanged);
        }

        void TreeViewEx_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            this.SelectedItem = e.NewValue;
        }

        #region SelectedItem

        /// <summary>
        /// Gets or Sets the SelectedItem possible Value of the TreeViewItem object.
        /// </summary>
        public new object SelectedItem
        {
            get { return this.GetValue(TreeViewEx.SelectedItemProperty); }
            set { this.SetValue(TreeViewEx.SelectedItemProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public new static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(object), typeof(TreeViewEx),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SelectedItemProperty_Changed));

        static void SelectedItemProperty_Changed(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            TreeViewEx targetObject = dependencyObject as TreeViewEx;
            if (targetObject != null)
            {
                TreeViewItem tvi = targetObject.FindItemNode(targetObject.SelectedItem) as TreeViewItem;
                if (tvi != null)
                    tvi.IsSelected = true;
            }
        }
        #endregion SelectedItem   

        public TreeViewItem FindItemNode(object item)
        {
            TreeViewItem node = null;
            foreach (object data in this.Items)
            {
                node = this.ItemContainerGenerator.ContainerFromItem(data) as TreeViewItem;
                if (node != null)
                {
                    if (data == item)
                        break;
                    node = _findItemNodeInChildren(node, item);
                    if (node != null)
                        break;
                }
            }
            return node;
        }

        protected TreeViewItem _findItemNodeInChildren(TreeViewItem parent, object item)
        {
            TreeViewItem node = null;
            if (parent == null)
                return null;
            bool isExpanded = parent.IsExpanded;
            if (!isExpanded) //Can't find child container unless the parent node is Expanded once
            {
                parent.IsExpanded = true;
                parent.UpdateLayout();
            }
            foreach (object data in parent.Items)
            {
                node = parent.ItemContainerGenerator.ContainerFromItem(data) as TreeViewItem;
                if (data == item && node != null)
                    break;
                node = _findItemNodeInChildren(node, item);
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
