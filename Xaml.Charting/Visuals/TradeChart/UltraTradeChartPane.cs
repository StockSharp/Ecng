// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// UltraTradeChartPane.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using Ecng.Xaml.Charting.Common.Extensions;

namespace Ecng.Xaml.Charting
{
    /// <summary>
    /// An ItemPane for the <see cref="UltrachartGroup"/> control. Wraps your custom UIElement (provided by <see cref="UltrachartGroup"/> ItemTemplate property)
    /// </summary>
    [TemplatePart(Name = "PART_ContentHost", Type = typeof(ContentPresenter)),
    TemplatePart(Name = "PART_Header", Type = typeof(Grid)),
    TemplatePart(Name = "PART_TopSplitter", Type = typeof(Thumb))]
    public class UltrachartGroupPane : ContentControl
    {
        /// <summary>
        /// The header template property
        /// </summary>
        public static DependencyProperty HeaderTemplateProperty = DependencyProperty.Register("HeaderTemplate", typeof(DataTemplate), typeof(UltrachartGroupPane), new PropertyMetadata(null, OnHeaderTemplateChanged));

        /// <summary>
        /// Fired when the outline of a pane is dragged
        /// </summary>
        public event EventHandler<DragDeltaEventArgs> Resizing;

        /// <summary>
        /// Fired after a dragging of the outline of a pane is done
        /// </summary>
        public event EventHandler<DragCompletedEventArgs> Resized;

        private ContentPresenter _mainPane;
        private Thumb _topSplitter;
        private Grid _headerPanel;

        private const double DefaultPaneHeight = 50.0;

        private double _topSplitterVerticalChange;
        private double _topSplitterHorizontalChange;

        /// <summary>
        /// Initializes a new instance of the <see cref="UltrachartGroupPane"/> class.
        /// </summary>
        public UltrachartGroupPane()
        {
            DefaultStyleKey = typeof (UltrachartGroupPane);

            this.SetCurrentValue(MinHeightProperty, DefaultPaneHeight);
            this.SetCurrentValue(HeightProperty, DefaultPaneHeight);
        }

        /// <summary>
        /// Gets or sets the header template.
        /// </summary>
        /// <value>
        /// The header template.
        /// </value>
        public DataTemplate HeaderTemplate
        {
            get { return (DataTemplate)GetValue(HeaderTemplateProperty); }
            set { SetValue(HeaderTemplateProperty, value); }
        }

        /// <summary>
        /// When overridden in a derived class, is invoked whenever application code or internal processes call <see cref="M:System.Windows.FrameworkElement.ApplyTemplate" />.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _mainPane = (ContentPresenter)GetTemplateChild("PART_ContentHost");
            _headerPanel = (Grid)GetTemplateChild("PART_Header");
            _topSplitter = (Thumb)GetTemplateChild("PART_TopSplitter");

            if (_topSplitter != null)
            {
                // Implement Thumb PreviewStyle by setting Height only on DragCompleted
                _topSplitter.DragDelta += OnSplitterDragDelta;
                _topSplitter.DragCompleted += OnSplitterDragCompleted;
#if SILVERLIGHT
                _topSplitter.DragCompleted += (s, e) => 
                {
                    _topSplitterHorizontalChange = 0;
                    _topSplitterVerticalChange = 0;
                };
#endif
            }

            TryApplyHeaderTemplate();
        }

        internal double MeasureMinHeight()
        {
            var availableSize = new Size(double.PositiveInfinity, Double.PositiveInfinity);

            _headerPanel.Measure(availableSize);
            _topSplitter.Measure(availableSize);

            return Math.Max(MinHeight, _headerPanel.DesiredSize.Height + _topSplitter.DesiredSize.Height);
        }

        private void OnSplitterDragCompleted(object sender, DragCompletedEventArgs e)
        {
            OnResized(e);
        }

        private void OnSplitterDragDelta(object sender, DragDeltaEventArgs e)
        {
#if SILVERLIGHT
            _topSplitterHorizontalChange += e.HorizontalChange;
            _topSplitterVerticalChange += e.VerticalChange;
#else
            _topSplitterHorizontalChange = e.HorizontalChange;
            _topSplitterVerticalChange = e.VerticalChange;
#endif
            var eventArgs = new DragDeltaEventArgs(_topSplitterHorizontalChange, _topSplitterVerticalChange);

            OnResizing(eventArgs);
        }

        private void OnResizing(DragDeltaEventArgs args)
        {
            var handler = Resizing;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        private void OnResized(DragCompletedEventArgs args)
        {
            var handler = Resized;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        private void TryApplyHeaderTemplate()
        {
            if (HeaderTemplate != null && _headerPanel != null)
            {
                var obj = HeaderTemplate.LoadContent() as FrameworkElement;

                if (obj != null)
                {
                    _headerPanel.Children.Add(obj);
                }
            }
        }

        private static void OnHeaderTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var groupPane = d as UltrachartGroupPane;

            groupPane.TryApplyHeaderTemplate();
        }
    }
}
