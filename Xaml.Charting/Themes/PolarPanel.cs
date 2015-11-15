// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// PolarPanel.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting.Themes
{
    /// <summary>
    /// The PolarPanel provides the panel which arranges items from center of panel to outer bounds
    /// </summary>
    public class PolarPanel : Panel
    {
        /// <summary>
        /// Defines the StretchToSize DependencyProperty
        /// </summary>
        public static readonly DependencyProperty StretchToSizeProperty = DependencyProperty.Register("StretchToSize", typeof(bool), typeof(PolarPanel), new PropertyMetadata(default(bool), InvalidateMeasure));

        /// <summary>
        /// Defines the IsReversedOrder DependencyProperty
        /// </summary>
        public static readonly DependencyProperty IsReversedOrderProperty = DependencyProperty.Register("IsReversedOrder", typeof (bool), typeof (PolarPanel), new PropertyMetadata(default(bool), InvalidateMeasure));

        /// <summary>
        /// Defines the MinimalItemSize DependencyProperty
        /// </summary>
        public static readonly DependencyProperty MinimimalItemSizeProperty = DependencyProperty.Register("MinimimalItemSize", typeof (double), typeof (PolarPanel), new PropertyMetadata(default(double)));

        /// <summary>
        /// Defines the ShouldCopyThicknessToParent AttachedProperty
        /// </summary>
        public static readonly DependencyProperty ShouldCopyThicknessToParentProperty = DependencyProperty.RegisterAttached("ShouldCopyThicknessToParent", typeof (bool), typeof (PolarPanel), new PropertyMetadata(default(bool)));

        /// <summary>
        /// Defines the Thicknes AttachedProperty
        /// </summary>
        public static readonly DependencyProperty ThicknessProperty = DependencyProperty.RegisterAttached("Thickness", typeof(double), typeof(PolarPanel), new PropertyMetadata(default(double), OnThicknessPropertyChanged));

        private const double MinPolarAreaSize = 50;

        private Size _desiredSize = new Size();

        private UIElement _itemsParent;

        private bool _isLoaded = false;

        /// <summary>
        /// Initializes a new instance of <see cref="PolarPanel"/> class
        /// </summary>
        public PolarPanel()
        {
            Loaded += (sender, args) =>
            {
                UpdateItemsParent();
                _isLoaded = true;

#if SILVERLIGHT
                var elements = GetElements().ToArray();
                var panelThickness = elements.Select(GetThickness).Sum();

                TryUpdateItemsParentThickness(panelThickness);
#endif
            };

            Unloaded += (sender, args) =>
            {
                _itemsParent = null;
            };
        }

        private void UpdateItemsParent()
        {
            if (IsItemsHost)
            {
                _itemsParent = this.FindVisualParent<ItemsControl>();
            }
            else
            {
                _itemsParent = this;
            }
        }

        /// <summary>
        /// Gets or sets whether panel should stretch children to its size or not
        /// </summary>
        public bool StretchToSize
        {
            get { return (bool)GetValue(StretchToSizeProperty); }
            set { SetValue(StretchToSizeProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether panel should arrange child elements in reverse order
        /// </summary>
        public bool IsReversedOrder
        {
            get { return (bool)GetValue(IsReversedOrderProperty); }
            set { SetValue(IsReversedOrderProperty, value); }
        }

        /// <summary>
        /// Gets or sets minimal child size of panel
        /// </summary>
        public double MinimimalItemSize
        {
            get { return (double) GetValue(MinimimalItemSizeProperty); }
            set { SetValue(MinimimalItemSizeProperty, value); }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var isPolarChart = !StretchToSize;

            _desiredSize = base.MeasureOverride(availableSize);
            var elements = GetElements().ToArray();

            var panelThickness = elements.Select(GetThickness).Sum();

            var areaSize = GetCenterSize(availableSize.Width, availableSize.Height, panelThickness);

            foreach (var element in elements)
            {
                var elementThickness = GetThickness(element);

                areaSize.Width += 2 * elementThickness;
                areaSize.Height += 2 * elementThickness;

                // TODO: Temporary workaround for http://ulcsoftware.myjetbrains.com/youtrack/issue/SC-2840
                if (isPolarChart)
                {
                    element.Measure(_desiredSize);
                }
            }

            if (areaSize.Width.IsRealNumber() && areaSize.Height.IsRealNumber())
            {
                _desiredSize.Width = Math.Max(areaSize.Width, _desiredSize.Width);
                _desiredSize.Height = Math.Max(areaSize.Height, _desiredSize.Height);
            }

            // TODO: Temporary workaround http://ulcsoftware.myjetbrains.com/youtrack/issue/SC-2840
            if (!isPolarChart)
            {
                // Restrict each child to panel's size
                foreach (var element in elements)
                {
                    element.Measure(_desiredSize);
                }
            }

            return _desiredSize;
        }

        private IEnumerable<UIElement> GetElements()
        {
            var elements = Children.OfType<UIElement>().Where(x => x.IsVisible());
            return IsReversedOrder ? elements.Reverse() : elements;
        }

        private Size GetCenterSize(double width, double height, double panelThickness)
        {
            if (!StretchToSize)
            {
                width = height = Math.Min(width, height);
            }

            width = width - panelThickness * 2;
            height = height - panelThickness * 2;

            // Constrain in the case of Polar chart
            if (!StretchToSize)
            {
                width = Math.Max(width, MinPolarAreaSize);
                height = Math.Max(height, MinPolarAreaSize);
            }

            return new Size(width, height);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var elements = GetElements().ToArray();
            var panelThickness = elements.Select(GetThickness).Sum();

            var arrangedSize = GetCenterSize(finalSize.Width, finalSize.Height, panelThickness);

            var center = new Point(finalSize.Width / 2, finalSize.Height / 2);

            foreach (var element in elements)
            {
                var elementThickness = GetThickness(element);

                arrangedSize.Width += 2 * elementThickness;
                arrangedSize.Height += 2 * elementThickness;

                var location = new Point(center.X - arrangedSize.Width / 2, center.Y - arrangedSize.Height / 2);
                var rect = new Rect(location, arrangedSize);

                element.Arrange(rect);
            }

            TryUpdateItemsParentThickness(panelThickness);

            return finalSize;
        }

        private void TryUpdateItemsParentThickness(double panelThickness)
        {
            // #SC-2637 Broken export to bitmap of polar chart
            // need to update items parent manually if panel isn't loaded ( we render chart in memory )
            if(!_isLoaded)
                UpdateItemsParent();

            if (_itemsParent != null)
                SetThickness(_itemsParent, panelThickness);
        }

        /// <summary>
        /// Sets the value of the Thickness attached property for a specified UIElement.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="value"></param>
        public static void SetThickness(UIElement element, double value)
        {
            element.SetValue(ThicknessProperty, value);
        }

        /// <summary>
        /// Gets the value of the Thickness attached property for a specified UIElement.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="value"></param>
        public static double GetThickness(UIElement element)
        {
            return (double)element.GetValue(ThicknessProperty);
        }

        /// <summary>
        /// Sets the value of the ShouldCopyThicknessToParent attached property for a specified UIElement.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="value"></param>
        public static void SetShouldCopyThicknessToParent(UIElement element, bool value)
        {
            element.SetValue(ShouldCopyThicknessToParentProperty, value);
        }

        /// <summary>
        /// Gets the value of the ShouldCopyThicknessToParent attached property for a specified UIElement.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="value"></param>
        public static bool GetShouldCopyThicknessToParent(UIElement element)
        {
            return (bool)element.GetValue(ShouldCopyThicknessToParentProperty);
        }

        private static void InvalidateMeasure(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var polarPanel = d as PolarPanel;
            if (polarPanel != null)
                polarPanel.InvalidateMeasure();

        }
        
        private static void OnThicknessPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = d as UIElement;
            if (element != null)
            {
                var polarPanel = VisualTreeHelper.GetParent(element) as PolarPanel;
                if (polarPanel != null)
                    polarPanel.InvalidateMeasure();

                TrySetThicknessOnParent(element, (double)e.NewValue);
            }
        }

        private static void TrySetThicknessOnParent(UIElement element, double value)
        {
            var shouldSetParentThickness = GetShouldCopyThicknessToParent(element);
            if (shouldSetParentThickness)
            {
                var parent = VisualTreeHelper.GetParent(element) as UIElement;
                if (parent != null)
                    parent.SetValue(ThicknessProperty, value);
            }
        }
    }
}
