using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;
using System.Collections;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace TAS.Client.Common
{
	public class TreeViewExtensions : DependencyObject
	{
		public static bool GetEnableMultiSelect(DependencyObject obj)
		{
			return (bool)obj.GetValue(EnableMultiSelectProperty);
		}

		public static void SetEnableMultiSelect(DependencyObject obj, bool value)
		{
			obj.SetValue(EnableMultiSelectProperty, value);
		}

		// Using a DependencyProperty as the backing store for EnableMultiSelect.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty EnableMultiSelectProperty =
			DependencyProperty.RegisterAttached("EnableMultiSelect", typeof(bool), typeof(TreeViewExtensions), new FrameworkPropertyMetadata(false)
			{
				PropertyChangedCallback = EnableMultiSelectChanged,
				BindsTwoWayByDefault = true
			});

        public static bool GetCursorWaitOnGeneratingItems(DependencyObject obj)
        {
            return (bool)obj.GetValue(CursorWaitOnGeneratingItemsProperty);
        }

        public static void SetCursorWaitOnGeneratingItems(DependencyObject obj, bool value)
        {
            obj.SetValue(CursorWaitOnGeneratingItemsProperty, value);
        }


        public static readonly DependencyProperty CursorWaitOnGeneratingItemsProperty =
            DependencyProperty.RegisterAttached("CursorWaitOnGeneratingItems", typeof(bool), typeof(TreeViewExtensions), new FrameworkPropertyMetadata(false)
            {
                PropertyChangedCallback = EnableCursorWaitOnGeneratingItemsChanged,
                BindsTwoWayByDefault = true
            });


		public static IList GetSelectedItems(DependencyObject obj)
		{
            return obj == null ? null : (IList)obj.GetValue(SelectedItemsProperty);
		}

		public static void SetSelectedItems(DependencyObject obj, IList value)
		{
			obj.SetValue(SelectedItemsProperty, value);
		}

		// Using a DependencyProperty as the backing store for SelectedItems.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty SelectedItemsProperty =
			DependencyProperty.RegisterAttached("SelectedItems", typeof(IList), typeof(TreeViewExtensions), new PropertyMetadata(null));



		static TreeViewItem GetAnchorItem(DependencyObject obj)
		{
			return (TreeViewItem)obj.GetValue(AnchorItemProperty);
		}

		static void SetAnchorItem(DependencyObject obj, TreeViewItem value)
		{
			obj.SetValue(AnchorItemProperty, value);
		}

		// Using a DependencyProperty as the backing store for AnchorItem.  This enables animation, styling, binding, etc...
		static readonly DependencyProperty AnchorItemProperty =
			DependencyProperty.RegisterAttached("AnchorItem", typeof(TreeViewItem), typeof(TreeViewExtensions), new PropertyMetadata(null));


		static void EnableMultiSelectChanged(DependencyObject s, DependencyPropertyChangedEventArgs args)
		{
			TreeView tree = (TreeView)s;
			var wasEnable = (bool)args.OldValue;
			var isEnabled = (bool)args.NewValue;
			if(wasEnable)
			{
				tree.RemoveHandler(TreeViewItem.MouseDownEvent, new MouseButtonEventHandler(ItemClicked));
				tree.RemoveHandler(TreeView.KeyDownEvent, new KeyEventHandler(KeyDown));
                tree.RemoveHandler(TreeViewItem.CollapsedEvent, new RoutedEventHandler(ItemColapsed));
            }
			if(isEnabled)
			{
				tree.AddHandler(TreeViewItem.MouseDownEvent, new MouseButtonEventHandler(ItemClicked), true);
				tree.AddHandler(TreeView.KeyDownEvent, new KeyEventHandler(KeyDown));
                tree.AddHandler(TreeViewItem.CollapsedEvent, new RoutedEventHandler(ItemColapsed), true);
			}
		}

        static void EnableCursorWaitOnGeneratingItemsChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
        {
            TreeView tree = (TreeView)source;
            var wasEnabled = (bool)args.OldValue;
            var isEnabled = (bool)args.NewValue;
            if (wasEnabled)
                tree.RemoveHandler(TreeViewItem.ExpandedEvent, new RoutedEventHandler(ItemExpanded));
            if (isEnabled)
                tree.AddHandler(TreeViewItem.ExpandedEvent, new RoutedEventHandler(ItemExpanded));
        }

        static void ItemExpanded(object sender, RoutedEventArgs e)
        {
            // we will only go through with this if our children haven't been populated
            TreeViewItem sourceItem = e.OriginalSource as TreeViewItem;
            if ((sourceItem != null)
                && (sourceItem.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated))
                UiServices.SetBusyState();
            e.Handled = true;
        }

		static TreeView GetTree(TreeViewItem item)
		{
            Func<DependencyObject, DependencyObject> getParent = (o) => o == null ? null : VisualTreeHelper.GetParent(o);
			FrameworkElement currentItem = item;
            DependencyObject parent;
			while(!((parent = getParent(currentItem)) is TreeView || parent == null))
				currentItem = (FrameworkElement)parent;
			return (TreeView)getParent(currentItem);
		}



		static void RealSelectedChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			TreeViewItem item = sender as TreeViewItem;
            if (item == null)
                return;
            var selectedItems = GetSelectedItems(GetTree(item));
			if(selectedItems != null)
			{
				var isSelected = GetIsSelected(item);
				if(isSelected)
					try
					{
						selectedItems.Add(item.Header);
					}
					catch(ArgumentException)
					{
					}
				else
					selectedItems.Remove(item.Header);
			}
		}

		static void KeyDown(object sender, KeyEventArgs e)
		{
			TreeView tree = (TreeView)sender;
			if(e.Key == Key.Enter)
			{
                ItemSelected((TreeView)sender, FindTreeViewItem(e.OriginalSource), MouseButton.Left);
                e.Handled = true;
			}
		}

        static void ItemColapsed(object sender, RoutedEventArgs e)
        {
            var tree = sender as TreeView;
            if (tree == null)
                return;
            var selectedItems = GetSelectedItems(tree);
            if (e.OriginalSource is TreeViewItem)
                foreach (TreeViewItem item in GetSubItems(e.OriginalSource as TreeViewItem))
                {
                    if (selectedItems != null && selectedItems.Contains(item.Header))
                        selectedItems.Remove(item.Header);
                    var isSelectedProperty = item.Header.GetType().GetProperty("IsMultiSelected");
                    if (isSelectedProperty != null)
                        isSelectedProperty.SetValue(item.Header, false, null);
                }
        }
        
        static IEnumerable<TreeViewItem> GetSubItems(TreeViewItem item)
        {
            if (item != null)
            {
                for (int i = 0; i < item.Items.Count; i++)
                {
                    TreeViewItem si = item.ItemContainerGenerator.ContainerFromIndex(i) as TreeViewItem;
                    if (si != null)
                    {
                        yield return si;
                        foreach (TreeViewItem ssi in GetSubItems(si))
                            yield return ssi;
                    }
                }
            }
        }

        static void ItemClicked(object sender, MouseButtonEventArgs e)
        {
            ItemSelected((TreeView)sender, FindTreeViewItem(e.OriginalSource), e.ChangedButton);
        }

        static void ItemSelected(TreeView tree, TreeViewItem item, MouseButton mouseButton)
		{
			if(item == null)
				return;

			if(mouseButton != MouseButton.Left)
			{
				if((mouseButton == MouseButton.Right) && ((Keyboard.Modifiers & (ModifierKeys.Shift | ModifierKeys.Control)) == ModifierKeys.None))
				{
					if(GetIsSelected(item))
					{
						UpdateAnchorAndActionItem(tree, item);
						return;
					}
					MakeSingleSelection(tree, item);
				}
				return;
			}
			if(mouseButton != MouseButton.Left)
			{
				if((mouseButton == MouseButton.Right) && ((Keyboard.Modifiers & (ModifierKeys.Shift | ModifierKeys.Control)) == ModifierKeys.None))
				{
					if(GetIsSelected(item))
					{
						UpdateAnchorAndActionItem(tree, item);
						return;
					}
					MakeSingleSelection(tree, item);
				}
				return;
			}
			if((Keyboard.Modifiers & (ModifierKeys.Shift | ModifierKeys.Control)) != (ModifierKeys.Shift | ModifierKeys.Control))
			{
				if((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
				{
					MakeToggleSelection(tree, item);
					return;
				}
				if((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
				{
					MakeAnchorSelection(tree, item, true);
					return;
				}
				MakeSingleSelection(tree, item);
				return;
			}
			//MakeAnchorSelection(item, false);


			//SetIsSelected(tree.SelectedItem
		}

		private static TreeViewItem FindTreeViewItem(object obj)
		{
            DependencyObject dpObj = obj as Visual;
			if(dpObj == null)
				return null;
			if(dpObj is TreeViewItem)
				return (TreeViewItem)dpObj;
			return FindTreeViewItem(VisualTreeHelper.GetParent(dpObj));
		}



		private static IEnumerable<TreeViewItem> GetExpandedTreeViewItems(ItemsControl tree)
		{
			for(int i = 0; i < tree.Items.Count; i++)
			{
				var item = (TreeViewItem)tree.ItemContainerGenerator.ContainerFromIndex(i);
				if(item == null)
					continue;
				yield return item;
				if(item.IsExpanded)
					foreach(var subItem in GetExpandedTreeViewItems(item))
						yield return subItem;
			}
		}

		private static void MakeAnchorSelection(TreeView tree, TreeViewItem actionItem, bool clearCurrent)
		{
			if(GetAnchorItem(tree) == null)
			{
				var selectedItems = GetSelectedTreeViewItems(tree);
				if(selectedItems.Count > 0)
				{
					SetAnchorItem(tree, selectedItems[selectedItems.Count - 1]);
				}
				else
				{
					SetAnchorItem(tree, GetExpandedTreeViewItems(tree).Skip(3).FirstOrDefault());
				}
				if(GetAnchorItem(tree) == null)
				{
					return;
				}
			}

			var anchor = GetAnchorItem(tree);

			var items = GetExpandedTreeViewItems(tree);
			bool betweenBoundary = false;
			foreach(var item in items)
			{
				bool isBoundary = item == anchor || item == actionItem;
				if(isBoundary)
				{
					betweenBoundary = !betweenBoundary;
				}
				if(betweenBoundary || isBoundary)
					SetIsSelected(item, true);
				else
					if(clearCurrent)
						SetIsSelected(item, false);
					else
						break;

			}
		}

		private static List<TreeViewItem> GetSelectedTreeViewItems(TreeView tree)
		{
			return GetExpandedTreeViewItems(tree).Where(i => GetIsSelected(i)).ToList();
		}

		private static void MakeSingleSelection(TreeView tree, TreeViewItem item)
		{
			foreach(TreeViewItem selectedItem in GetExpandedTreeViewItems(tree))
			{
				if(selectedItem == null)
					continue;
				if(selectedItem != item)
					SetIsSelected(selectedItem, false);
				else
				{
					SetIsSelected(selectedItem, true);
				}
			}
			UpdateAnchorAndActionItem(tree, item);
		}

		private static void MakeToggleSelection(TreeView tree, TreeViewItem item)
		{
			SetIsSelected(item, !GetIsSelected(item));
			UpdateAnchorAndActionItem(tree, item);
		}

		private static void UpdateAnchorAndActionItem(TreeView tree, TreeViewItem item)
		{
			SetAnchorItem(tree, item);
		}
        

		public static bool GetIsSelected(DependencyObject obj)
		{
			return (bool)obj.GetValue(IsSelectedProperty);
		}

		public static void SetIsSelected(DependencyObject obj, bool value)
		{
			obj.SetValue(IsSelectedProperty, value);
		}

		// Using a DependencyProperty as the backing store for IsSelected.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty IsSelectedProperty =
			DependencyProperty.RegisterAttached("IsSelected", typeof(bool), typeof(TreeViewExtensions), new PropertyMetadata(false)
			{
				PropertyChangedCallback = RealSelectedChanged
			});


	}
}
