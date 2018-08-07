// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// UltrachartGroup.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
// For full terms and conditions of the license, see http://www.ultrachart.com/ultrachart-eula/
// 
// This source code is protected by international copyright law. Unauthorized
// reproduction, reverse-engineering, or distribution of all or any portion of
// this source code is strictly prohibited.
// 
// This source code contains confidential and proprietary trade secrets of
// ulc software Services Ltd., and should at no time be copied, transferred, sold,
// distributed or made available without express written permission.
// *************************************************************************************
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Licensing;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.Axes;
using Ecng.Xaml.Licensing.Core;
using LicenseManager = Ecng.Xaml.Licensing.Core.LicenseManager;

namespace Ecng.Xaml.Charting
{
    /// <summary>
    /// An ItemsControl which allows grouping of multiple <see cref="UltrachartSurface"/> instances to create a multi-paned chart. Used specifically by StockCharts but also applicable to other chart types
    /// </summary>
    [TemplatePart(Name = "PART_MainGrid")]
    [TemplatePart(Name = "PART_TabbedContent")]
    [TemplatePart(Name = "PART_StackedContent")]
    [TemplatePart(Name = "PART_MainPane")]
    [TemplatePart(Name = "PART_UltrachartGroupModifierCanvas")]
    [UltrachartLicenseProvider(typeof(UltraTradeChartLicenseProvider))]
    public class UltrachartGroup : ItemsControl, INotifyPropertyChanged
    {
        /// <summary>
        /// Defines the VerticalChartGroup DependencyProperty
        /// </summary>
        public static readonly DependencyProperty VerticalChartGroupProperty = DependencyProperty.RegisterAttached("VerticalChartGroup", typeof(string), typeof(UltrachartGroup), new PropertyMetadata(null, OnVerticalChartGroupChanged));

        /// <summary>
        /// Defines the IsTabbed DependencyProperty
        /// </summary>
        public static readonly DependencyProperty IsTabbedProperty = DependencyProperty.Register("IsTabbed", typeof(bool), typeof(UltrachartGroup), new PropertyMetadata(false, OnIsTabbedChanged));

        internal static Dictionary<ChartGroup, string> VerticalChartGroups = new Dictionary<ChartGroup, string>();

#if SILVERLIGHT
        /// <summary>Identifies the ItemContainerStyle dependency property. </summary>
        /// <returns>The identifier for the ItemContainerStyle dependency property.</returns>
        public static readonly DependencyProperty ItemContainerStyleProperty = DependencyProperty.Register("ItemContainerStyle", typeof(Style), typeof(ItemsControl), new PropertyMetadata(null, (d, e) => ((UltrachartGroup)d).OnUltrachartGroupItemContainerStyleChanged((Style)e.OldValue, (Style)e.NewValue)));

        /// <summary>
        /// Gets or sets the ItemContainerStyle, which is applied to the containers around individal <see cref="UltrachartSurface"/> instances 
        /// </summary>
        public Style ItemContainerStyle
        {
            get { return (Style)GetValue(ItemContainerStyleProperty); }
            set { SetValue(ItemContainerStyleProperty, value); }
        }

        private void OnUltrachartGroupItemContainerStyleChanged(Style oldStyle, Style newStyle)
        {
            if (Items.Count <= 0)
                return;

            _items.ForEachDo(itemPane=>
                                 {
                                     if(!itemPane.IsMainPane)
                                     {
                                         itemPane.PaneElement.Style = newStyle;
                                     }
                                 });
        }

#endif
        private ContentPresenter _mainPane;
        private TabControl _tabbedViewPanel;
        private StackPanel _stackedViewPanel;
        private Canvas _modifierCanvas;

        private double _resizeTotalDragDiff;
        private double _resizeInitialHeight;
        private double _resizeTotalHeight;
        private double _resizeInitialMouseYCoord;
        private double _resizeMinHeight;

        private ItemPane _resizePane;
        private ItemPane _resizeUpperPane;

        private Action<ItemPane> _addPane;

        private readonly List<ItemPane> _items = new List<ItemPane>();

        /// <summary>
        /// Initializes a new instance of the <see cref="UltrachartGroup" /> class.
        /// </summary>
        public UltrachartGroup()
        {
            DefaultStyleKey = typeof(UltrachartGroup);

            _addPane = AddStackedPane;
        }

        /// <summary>
        /// Gets or sets value, indicates whether panes are tabbed or not
        /// </summary>
        /// <remarks></remarks>
        public bool IsTabbed
        {
            get { return (bool)GetValue(IsTabbedProperty); }
            set { SetValue(IsTabbedProperty, value); }
        }

        /// <summary>
        /// Gets a value, indicates, whether container has tabbed panes
        /// </summary>
        public bool HasTabbedItems
        {
            get { return _tabbedViewPanel != null && _tabbedViewPanel.Items.Count > 0; }
        }

        /// <summary>
        /// Sets the vertical chart group dependency Property
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="syncWidthGroup">The named group</param>
        public static void SetVerticalChartGroup(DependencyObject element, string syncWidthGroup)
        {
            element.SetValue(VerticalChartGroupProperty, syncWidthGroup);
        }

        /// <summary>
        /// Gets the vertical chart group depedency property
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns></returns>
        public static string GetVerticalChartGroup(DependencyObject element)
        {
            return (string)element.GetValue(VerticalChartGroupProperty);
        }

        /// <summary>
        /// Determines if the specified item is (or is eligible to be) its own container.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <returns>
        /// true if the item is (or is eligible to be) its own container; otherwise, false.
        /// </returns>
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return base.IsItemItsOwnContainerOverride(item);
        }

        /// <summary>
        /// Creates or identifies the element that is used to display the given item.
        /// </summary>
        /// <returns>
        /// The element that is used to display the given item.
        /// </returns>
        protected override DependencyObject GetContainerForItemOverride()
        {
            return base.GetContainerForItemOverride();
        }

#if !SILVERLIGHT
        /// <summary>
        /// Called when the <see cref="P:System.Windows.Controls.ItemsControl.ItemsSource" /> property changes.
        /// </summary>
        /// <param name="oldValue">Old value of the <see cref="P:System.Windows.Controls.ItemsControl.ItemsSource" /> property.</param>
        /// <param name="newValue">New value of the <see cref="P:System.Windows.Controls.ItemsControl.ItemsSource" /> property.</param>
        protected override void OnItemsSourceChanged(System.Collections.IEnumerable oldValue, System.Collections.IEnumerable newValue)
        {
            base.OnItemsSourceChanged(oldValue, newValue);

            OnItemsCollectionChanged(oldValue, newValue);
        }
#endif

        private void OnItemsCollectionChanged(System.Collections.IEnumerable oldValue, System.Collections.IEnumerable newValue)
        {
            if (_mainPane == null) return;

            if (oldValue != null)
            {
                RemovePanes(oldValue.Cast<IChildPane>());
            }

            if (newValue != null)
            {
                AddPanes(newValue.Cast<IChildPane>());
            }
        }

        /// <summary>
        /// Invoked when the <see cref="P:System.Windows.Controls.ItemsControl.Items" /> property changes.
        /// </summary>
        /// <param name="e">Information about the change.</param>
        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);
            
            if (_mainPane == null) return;

            if ((e.Action == NotifyCollectionChangedAction.Add && e.NewItems == null) ||
                (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems == null))
            {
                return;
            }
            
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddPanes(e.NewItems.Cast<IChildPane>());
                    break;
                case NotifyCollectionChangedAction.Remove:
                    RemovePanes(e.OldItems.Cast<IChildPane>());
                    break;
                case NotifyCollectionChangedAction.Reset:
                    ClearAll();
                    break;
            }
        }

        /// <summary>
        /// When overridden in a derived class, is invoked whenever application code or internal processes call <see cref="M:System.Windows.FrameworkElement.ApplyTemplate" />.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            new LicenseManager().Validate(this, new UltrachartLicenseProviderFactory());

            ClearAll();

            _mainPane = GetTemplateChild("PART_MainPane") as ContentPresenter;
            _tabbedViewPanel = GetTemplateChild("PART_TabbedContent") as TabControl;
            _stackedViewPanel = GetTemplateChild("PART_StackedContent") as StackPanel;
            _modifierCanvas = GetTemplateChild("PART_UltrachartGroupModifierCanvas") as Canvas;

            if (ItemsSource != null)
            {
                OnItemsCollectionChanged(null, ItemsSource);
            }
        }

        private void ClearAll()
        {
            while (!_items.IsNullOrEmpty())
            {
                RemovePane(_items[0]);
            }
        }

        private void RemovePanes(IEnumerable<IChildPane> items)
        {
            items.ForEachDo(item => RemovePane(item));
        }

        private ItemPane RemovePane(IChildPane item)
        {
            var removed = _items.FirstOrDefault(pane => pane.PaneViewModel.Equals(item));

            RemovePane(removed);

            return removed;
        }

        private void RemovePane(ItemPane itemPane)
        {
            if (itemPane != null)
            {
                if (!itemPane.IsMainPane)
                {
                    Unsubscribe(itemPane.PaneElement as UltrachartGroupPane);

                    _items.Remove(itemPane);

                    if (itemPane.IsTabbed)
                    {
                        RemoveTabbedPane(itemPane);
                    }
                    else
                    {
                        RemoveStackedPane(itemPane);
                    }
                }
                else
                {
                    RemoveMainPane(itemPane);
                }
            }
        }

        private void RemoveStackedPane(ItemPane item)
        {
            _stackedViewPanel.Children.Remove(item.PaneElement);
        }

        private void RemoveTabbedPane(ItemPane itemPane)
        {
            var tabItem =
                _tabbedViewPanel.Items
                .OfType<TabItem>()
                .FirstOrDefault(item => item.Content.Equals(itemPane.PaneElement));

            if (tabItem != null)
            {
                tabItem.Content = null;
                _tabbedViewPanel.Items.Remove(tabItem);

                OnPropertyChanged("HasTabbedItems");
            }
        }

        private void RemoveMainPane(ItemPane mainPane)
        {
            if (!mainPane.IsMainPane)
            {
                throw new ArgumentException("Attempt to remove MainPane was failed. Passed invalid ItemPane argument.");
            }

            _mainPane.Content = null;
            _items.Remove(mainPane);
            mainPane.IsMainPane = false;
        }

        private void AddPanes(IEnumerable<IChildPane> items)
        {
            if (_mainPane.Content == null && items.Any())
            {
                AddMainPane(items.First());
                items.Skip(1).ForEachDo(AddPane);
            }
            else
            {
                items.ForEachDo(AddPane);
            }
        }

        private void AddMainPane(IChildPane paneViewModel)
        {
            var templatedItem = GetItemFromTemplate();

            var itemPane = new ItemPane { PaneViewModel = paneViewModel, PaneElement = templatedItem, IsMainPane = true };
            templatedItem.DataContext = itemPane.PaneViewModel;

            _mainPane.Content = templatedItem;

            _items.Add(itemPane);
        }

        private void AddPane(IChildPane paneViewModel)
        {
            var templatedItem = GetItemFromTemplate();

            var itemPane = new ItemPane { PaneViewModel = paneViewModel, PaneElement = templatedItem, };
            templatedItem.DataContext = paneViewModel;
            itemPane.ChangeOrientationCommand = new ActionCommand(() => MovePane(itemPane));
            itemPane.ClosePaneCommand = paneViewModel.ClosePaneCommand;

            var itemContainer = new UltrachartGroupPane { Content = templatedItem, DataContext = itemPane, Style = ItemContainerStyle };
            itemPane.PaneElement = itemContainer;
            Subscribe(itemContainer);

            _addPane(itemPane);

            _items.Add(itemPane);
        }

        private void AddStackedPane(ItemPane container)
        {
            container.IsTabbed = false;

            _stackedViewPanel.Children.Add(container.PaneElement);
        }

        private void AddTabbedPane(ItemPane container)
        {
            SynchronizeTabbedHeight(container.PaneElement as UltrachartGroupPane);

            container.IsTabbed = true;

            var tabItem = new TabItem { Header = container.PaneViewModel.Title, DataContext = container, Content = container.PaneElement };
            _tabbedViewPanel.Items.Add(tabItem);

            _tabbedViewPanel.SelectedItem = tabItem;

            OnPropertyChanged("HasTabbedItems");
        }

        public void MovePane(IChildPane item)
        {
            var container = RemovePane(item);

            if (container != null)
            {
                if (container.IsTabbed)
                {
                    AddStackedPane(container);
                }
                else
                {
                    AddTabbedPane(container);
                }
            }
        }

        private void MovePane(ItemPane container)
        {
            if (container.IsTabbed)
            {
                RemoveTabbedPane(container);
                AddStackedPane(container);
            }
            else
            {
                RemoveStackedPane(container);
                AddTabbedPane(container);
            }
        }

        private void Subscribe(UltrachartGroupPane itemContainer)
        {
            itemContainer.Resizing += OnItemContainerResizing;
            itemContainer.Resized += OnItemContainerResized;
        }

        private void Unsubscribe(UltrachartGroupPane itemContainer)
        {
            itemContainer.Resizing -= OnItemContainerResizing;
            itemContainer.Resized -= OnItemContainerResized;
        }

        private void OnItemContainerResizing(object sender, DragDeltaEventArgs e)
        {
            ItemPane paneByElement(UIElement el) => _items.First(item => ReferenceEquals(item.PaneElement, el));

            if (_resizePane == null)
            {
                _resizePane = paneByElement((UIElement) sender);
                if(_resizePane == null)
                    return;

                var idx = _stackedViewPanel.Children.IndexOf(_resizePane.PaneElement);
                if (idx < 0)
                {
                    Guard.IsTrue(_resizePane.IsTabbed, "unexpected pane type");

                    var cnt = _stackedViewPanel.Children.Count;
                    _resizeUpperPane = cnt > 0 ? paneByElement(_stackedViewPanel.Children[cnt - 1]) : _items.First(i => i.IsMainPane);
                }
                else if (idx == 0)
                    _resizeUpperPane = _items.First(i => i.IsMainPane);
                else
                    _resizeUpperPane = paneByElement(_stackedViewPanel.Children[idx - 1]);

                _resizeTotalDragDiff = 0;
                _resizeInitialHeight = _resizePane.PaneElement.ActualHeight;
                _resizeTotalHeight = _resizeInitialHeight + _resizeUpperPane.PaneElement.ActualHeight;
                _resizeInitialMouseYCoord = Mouse.GetPosition(_resizeUpperPane.PaneElement.GetWindow()).Y;
                _resizeMinHeight = ((UltrachartGroupPane)_resizePane.PaneElement).MeasureMinHeight();
            }

            var y = Mouse.GetPosition(_resizeUpperPane.PaneElement.GetWindow()).Y;

            _resizeTotalDragDiff = y - _resizeInitialMouseYCoord;

            var newHeight = Math.Min(_resizeTotalHeight - _resizeMinHeight, Math.Max(_resizeMinHeight, _resizeInitialHeight - _resizeTotalDragDiff));

            _resizePane.PaneElement.Height = newHeight;

            if(!_resizeUpperPane.IsMainPane)
                _resizeUpperPane.PaneElement.Height = _resizeTotalHeight - newHeight;

            if (_resizePane.IsTabbed)
                _items.Where(item => item.IsTabbed).ForEachDo(item => item.PaneElement.Height = newHeight);
        }

        private void OnItemContainerResized(object sender, DragCompletedEventArgs e)
        {
            _resizePane = _resizeUpperPane = null;
            _resizeTotalDragDiff = _resizeInitialHeight = 0;
        }

        private double CoerceDesiredHeight(ItemPane pane, double desiredHeight)
        {
            var paneHeight = pane.PaneElement.ActualHeight;
            var allowedChange = desiredHeight - paneHeight;

            allowedChange = Math.Min(allowedChange, _mainPane.ActualHeight);

            return paneHeight + allowedChange;
        }

        private void SynchronizeTabbedHeight(UltrachartGroupPane itemContainer)
        {
            var tabbedItem = _items.FirstOrDefault(item => item.IsTabbed && item.PaneElement != itemContainer);
            if (tabbedItem != null)
            {
                itemContainer.Height = tabbedItem.PaneElement.Height;
            }
        }

        private void OnItemContainerSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var itemContainer = sender as UltrachartGroupPane;

            //Tabbed panes height synchronization
            if (itemContainer != null && e.NewSize.Height.CompareTo(e.PreviousSize.Height) != 0)
            {
                var pane = _items.FirstOrDefault(item => item.PaneElement == itemContainer && item.IsTabbed);

                if (pane != null)
                {
                    _items.Where(item => item.IsTabbed).ForEachDo(item => item.PaneElement.Height = e.NewSize.Height);
                }
            }
        }

        private static void OnVerticalChartGroupChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var surface = d as UltrachartSurface;
            if (surface == null)
            {
                throw new InvalidOperationException(
                    "VerticalChartGroupProperty can only be applied to types UltrachartSurface derived types");
            }

            string newGroupName = e.NewValue as string;
            string oldGroupName = e.OldValue as string;

            if (String.IsNullOrEmpty(newGroupName))
            {
                // Removing the axis from grouping
                surface.Loaded -= OnSurfaceLoaded;
                surface.Unloaded -= OnSurfaceUnloaded;
                DetachUltrachartSurfaceFromGroup(surface);
            }
            else
            {
                // Switching to a new group
                if (newGroupName != oldGroupName)
                {
                    if (!String.IsNullOrEmpty(oldGroupName))
                    {
                        // Remove the old group mapping
                        DetachUltrachartSurfaceFromGroup(surface);
                    }

                    // Unsubscribe before subscribe guarantees only one subscriber
                    surface.Loaded -= OnSurfaceLoaded;
                    surface.Unloaded -= OnSurfaceUnloaded;
                    surface.Loaded += OnSurfaceLoaded;
                    surface.Unloaded += OnSurfaceUnloaded;

                    // If already loaded, apply the ChartGroup. Subsequent Load/Unload calls will toggle it until the chart
                    // is removed from the VerticalChartGroup
                    if (surface.IsLoaded)
                    {
                        AttachUltrachartSurfaceToGroup(surface, newGroupName);
                    }
                }
            }
        }

        private static void OnSurfaceLoaded(object sender, RoutedEventArgs e)
        {
            var surface = sender as UltrachartSurface;
            string newGroupName = GetVerticalChartGroup(surface);
            AttachUltrachartSurfaceToGroup(surface, newGroupName);
        }

        private static void OnSurfaceUnloaded(object sender, RoutedEventArgs e)
        {
            var surface = sender as UltrachartSurface;
            DetachUltrachartSurfaceFromGroup(surface);
        }

        private static void AttachUltrachartSurfaceToGroup(IUltrachartSurface surface, string newGroupName)
        {
            var chartGroup = new ChartGroup(surface);
            if (VerticalChartGroups.ContainsKey(chartGroup) == false)
            {
                VerticalChartGroups.Add(chartGroup, newGroupName);
                SynchronizeAxisSizes(surface);
                surface.Rendered += OnUltrachartRendered;
            }
        }

        private static void DetachUltrachartSurfaceFromGroup(UltrachartSurface surface)
        {
            foreach (var pair in VerticalChartGroups)
            {
                if (ReferenceEquals(pair.Key.UltrachartSurface, surface))
                {
                    pair.Key.RestoreState();
                }
            }
            VerticalChartGroups.Remove(new ChartGroup(surface));
            surface.Rendered -= OnUltrachartRendered;
        }

        private static void OnUltrachartRendered(object sender, EventArgs e)
        {
            SynchronizeAxisSizes((IUltrachartSurface)sender);
        }

        private static void SynchronizeAxisSizes(IUltrachartSurface ultraChartSurface)
        {
            string verticalGroup;
            if (!VerticalChartGroups.TryGetValue(new ChartGroup(ultraChartSurface), out verticalGroup))
                return;

            var synchronizedCharts = VerticalChartGroups.Where(pair => pair.Value == verticalGroup)
                .Select(p => p.Key)
                .ToArray();

            var leftAreaMaxCalculatedWidth = CalculateMaxAxisAreaWidth(synchronizedCharts, AxisAlignment.Left);
            var rightAreaMaxCalculatedWidth = CalculateMaxAxisAreaWidth(synchronizedCharts, AxisAlignment.Right);

            synchronizedCharts.Select(x => x.UltrachartSurface).OfType<UltrachartSurface>().ForEachDo(x =>
            {
                if (x.AxisAreaLeft != null)
                {
                    x.AxisAreaLeft.Margin = new Thickness(leftAreaMaxCalculatedWidth - x.AxisAreaLeft.ActualWidth, 0, 0, 0);
                }

                if (x.AxisAreaRight != null)
                {
                    x.AxisAreaRight.Margin = new Thickness(0, 0, rightAreaMaxCalculatedWidth - x.AxisAreaRight.ActualWidth, 0);
                }
            });
        }

        private static double CalculateMaxAxisAreaWidth(IEnumerable<ChartGroup> synchronizedCharts, AxisAlignment axisAlignment)
        {
            var calculatedWidth =
                synchronizedCharts.Select(
                // for each axis from particular side (axisAlignment)
                    x => x.UltrachartSurface.YAxes.OfType<AxisBase>().Where(axis => axis.AxisAlignment == axisAlignment))
                // sum all widths
                                  .Select(collection => collection.Aggregate(0d, (sum, axis) => sum + axis.ActualWidth))
                // get maximal width through all the charts
                                  .Max();

            return calculatedWidth;
        }

        private FrameworkElement GetItemFromTemplate()
        {
            return ItemTemplate.LoadContent() as FrameworkElement;
        }

        private static void OnIsTabbedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var group = d as UltrachartGroup;
            var isTabbed = (bool)e.NewValue;

            if (isTabbed)
            {
                group._addPane = group.AddTabbedPane;

                group._items
                    .Where(container => !container.IsTabbed && !container.IsMainPane)
                    .ForEachDo(group.MovePane);
            }
            else
            {
                group._addPane = group.AddStackedPane;

                group._items
                    .Where(container => container.IsTabbed && !container.IsMainPane)
                    .ForEachDo(group.MovePane);
            }
        }

        /// <summary>
        /// Occurs when a property changes. Part of the <see cref="INotifyPropertyChanged"/> implementation
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        
        internal List<ItemPane> Panes
        {
            get { return _items; }
        }

    }

    /// <summary>
    /// A HelperClass used to perform the functionality of <see cref="UltrachartGroup.VerticalChartGroupProperty"/> but when the chart is rotated (e.g. YAxis <see cref="AxisAlignment"/> = <see cref="AxisAlignment.Top"/>
    /// </summary>
    public class HorizontalGroupHelper 
    {
        /// <summary>
        /// Defines the HorizontalChartGroup DependencyProperty
        /// </summary>
        public static readonly DependencyProperty HorizontalChartGroupProperty = DependencyProperty.RegisterAttached("HorizontalChartGroup", typeof(string), typeof(HorizontalGroupHelper), new PropertyMetadata(null, OnHorizontalChartGroupChanged));        

        internal static Dictionary<ChartGroup, string> HorizontalChartGroups = new Dictionary<ChartGroup, string>();

        /// <summary>
        /// Sets the Horizontal chart group dependency Property
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="syncWidthGroup">The named group</param>
        public static void SetHorizontalChartGroup(DependencyObject element, string syncWidthGroup)
        {
            element.SetValue(HorizontalChartGroupProperty, syncWidthGroup);
        }

        /// <summary>
        /// Gets the Horizontal chart group depedency property
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns></returns>
        public static string GetHorizontalChartGroup(DependencyObject element)
        {
            return (string)element.GetValue(HorizontalChartGroupProperty);
        }

        private static void OnHorizontalChartGroupChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var surface = d as UltrachartSurface;
            if (surface == null)
            {
                throw new InvalidOperationException(
                    "HorizontalChartGroupProperty can only be applied to types UltrachartSurface derived types");
            }

            string newGroupName = e.NewValue as string;
            string oldGroupName = e.OldValue as string;

            if (String.IsNullOrEmpty(newGroupName))
            {
                // Removing the axis from grouping
                surface.Loaded -= OnSurfaceLoaded;
                surface.Unloaded -= OnSurfaceUnloaded;
                DetachUltrachartSurfaceFromGroup(surface);
            }
            else
            {
                // Switching to a new group
                if (newGroupName != oldGroupName)
                {
                    if (!String.IsNullOrEmpty(oldGroupName))
                    {
                        // Remove the old group mapping
                        DetachUltrachartSurfaceFromGroup(surface);
                    }

                    // Unsubscribe before subscribe guarantees only one subscriber
                    surface.Loaded -= OnSurfaceLoaded;
                    surface.Unloaded -= OnSurfaceUnloaded;
                    surface.Loaded += OnSurfaceLoaded;
                    surface.Unloaded += OnSurfaceUnloaded;

                    // If already loaded, apply the ChartGroup. Subsequent Load/Unload calls will toggle it until the chart
                    // is removed from the HorizontalChartGroup
                    if (surface.IsLoaded)
                    {
                        AttachUltrachartSurfaceToGroup(surface, newGroupName);
                    }
                }
            }
        }

        private static void OnSurfaceLoaded(object sender, RoutedEventArgs e)
        {
            var surface = sender as UltrachartSurface;
            string newGroupName = GetHorizontalChartGroup(surface);
            AttachUltrachartSurfaceToGroup(surface, newGroupName);
        }

        private static void OnSurfaceUnloaded(object sender, RoutedEventArgs e)
        {
            var surface = sender as UltrachartSurface;
            DetachUltrachartSurfaceFromGroup(surface);
        }

        private static void AttachUltrachartSurfaceToGroup(IUltrachartSurface surface, string newGroupName)
        {
            var chartGroup = new ChartGroup(surface);
            if (HorizontalChartGroups.ContainsKey(chartGroup) == false)
            {
                HorizontalChartGroups.Add(chartGroup, newGroupName);
                SynchronizeAxisSizes(surface);
                surface.Rendered += OnUltrachartRendered;
            }
        }

        private static void DetachUltrachartSurfaceFromGroup(UltrachartSurface surface)
        {
            foreach (var pair in HorizontalChartGroups)
            {
                if (ReferenceEquals(pair.Key.UltrachartSurface, surface))
                {
                    pair.Key.RestoreState();
                }
            }
            HorizontalChartGroups.Remove(new ChartGroup(surface));
            surface.Rendered -= OnUltrachartRendered;
        }

        private static void OnUltrachartRendered(object sender, EventArgs e)
        {
            SynchronizeAxisSizes((IUltrachartSurface)sender);
        }

        private static void SynchronizeAxisSizes(IUltrachartSurface ultraChartSurface)
        {
            string HorizontalGroup;
            if (!HorizontalChartGroups.TryGetValue(new ChartGroup(ultraChartSurface), out HorizontalGroup))
                return;

            var synchronizedCharts = HorizontalChartGroups.Where(pair => pair.Value == HorizontalGroup)
                .Select(p => p.Key)
                .ToArray();

            var bottomAreaMaxHeight = CalculateMaxAxisAreaWidth(synchronizedCharts, AxisAlignment.Bottom);
            var topAreaMaxHeight = CalculateMaxAxisAreaWidth(synchronizedCharts, AxisAlignment.Top);

            synchronizedCharts.Select(x => x.UltrachartSurface).OfType<UltrachartSurface>().ForEachDo(x =>
            {
                if (x.AxisAreaBottom != null)
                {
                    x.AxisAreaBottom.Margin = new Thickness(0, 0, 0, bottomAreaMaxHeight - x.AxisAreaBottom.ActualHeight);
                }

                if (x.AxisAreaTop != null)
                {
                    x.AxisAreaTop.Margin = new Thickness(0, topAreaMaxHeight - x.AxisAreaTop.ActualHeight, 0, 0);
                }
            });
        }

        private static double CalculateMaxAxisAreaWidth(IEnumerable<ChartGroup> synchronizedCharts, AxisAlignment axisAlignment)
        {
            var calculatedAxisHeight =
                synchronizedCharts.Select(
                // for each axis from particular side (axisAlignment)
                    x => x.UltrachartSurface.YAxes.OfType<AxisBase>().Where(axis => axis.AxisAlignment == axisAlignment))
                // sum all widths
                                  .Select(collection => collection.Aggregate(0d, (sum, axis) => sum + axis.ActualHeight))
                // get maximal width through all the charts
                                  .Max();

            return calculatedAxisHeight;
        }
    }
}