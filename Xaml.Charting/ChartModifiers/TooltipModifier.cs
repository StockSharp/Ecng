// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// TooltipModifier.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows;
using System.Windows.Controls;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.ChartModifiers
{
    /// <summary>
    /// The <see cref="TooltipModifier"/> provides a mouse-over tooltip to a chart, outputting a single <see cref="SeriesInfo"/> object to bind to which updates as the mouse moves over data-points.
    /// Add to a <see cref="UltrachartSurface"/> and set IsEnabled to true to enable this behaviour
    /// </summary>
    public class TooltipModifier : TooltipModifierBase
    {
        /// <summary>
        /// Defined IncludeSeries Attached Property
        /// </summary>
        public static readonly DependencyProperty IncludeSeriesProperty = DependencyProperty.RegisterAttached("IncludeSeries", typeof(bool), typeof(TooltipModifier), new PropertyMetadata(true));

        /// <summary>
        /// Gets the include Series or not
        /// </summary>
        public static bool GetIncludeSeries(DependencyObject obj)
        {
            return (bool)obj.GetValue(IncludeSeriesProperty);
        }

        /// <summary>
        /// Sets the include Series or not
        /// </summary>
        public static void SetIncludeSeries(DependencyObject obj, bool value)
        {
            obj.SetValue(IncludeSeriesProperty, value);
        }

        /// <summary>
        /// Defines the TooltipLabelDataContext DependencyProperty
        /// </summary>
        public static readonly DependencyProperty TooltipLabelDataContextSelectorProperty = DependencyProperty.Register("TooltipLabelDataContextSelector", typeof(Func<SeriesInfo, object>), typeof(TooltipModifier), new PropertyMetadata(null));

        private const double TooltipXyOffset = 6;

        private TemplatableControl _tooltipLabelCache;

        /// <summary>
        /// Gets or sets the function which is called internally to get a DataContext for a particular data point
        /// </summary>
        public Func<SeriesInfo, object> TooltipLabelDataContextSelector
        {
            get { return (Func<SeriesInfo, object>)GetValue(TooltipLabelDataContextSelectorProperty); }
            set { SetValue(TooltipLabelDataContextSelectorProperty, value); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TooltipModifier"/> class
        /// </summary>
        /// <remarks></remarks>
        public TooltipModifier()
        {
            DefaultStyleKey = typeof (TooltipModifier);
        }

        /// <summary>
        /// When overriden in a derived class, this method should clear all markers and tooltips from the <see cref="UltrachartSurface.ModifierSurface" />
        /// </summary>
        protected override void ClearAll()
        {
            ClearTooltipOverlay();
        }

        private void ClearTooltipOverlay()
        {
            if (_tooltipLabelCache == null || ModifierSurface == null)
                return;

            if (ModifierSurface.Children.Contains(_tooltipLabelCache))
            {
                ModifierSurface.Children.Remove(_tooltipLabelCache);
            }

            CurrentPoint = new Point(double.NaN, double.NaN);
        }

        /// <summary>
        /// When overriden in a derived class, called to handle the Master <see cref="ChartModifierBase" /> MouseMove event
        /// </summary>
        /// <param name="mousePoint">The current Mouse-point</param>
        protected override void HandleMasterMouseEvent(Point mousePoint)
        {
            UltrachartDebugLogger.Instance.WriteLine("TooltipModifier Master MouseMove: {0}, {1}", mousePoint.X,
                                                   mousePoint.Y);

            Point hitTestPoint;
            var dataContext = GetTooltipDataContext(mousePoint, out hitTestPoint);

            if (dataContext != null && _tooltipLabelCache != null)
            {
                _tooltipLabelCache.DataContext = dataContext;
                UpdateTooltipOverlay(mousePoint);
            }
            else
            {
                ClearTooltipOverlay();
            }
        }

        /// <summary>
        /// When overriden in a derived class, called to handle the Slave <see cref="ChartModifierBase" /> MouseMove event
        /// </summary>
        /// <param name="mousePoint">The current Mouse-point</param>
        protected override void HandleSlaveMouseEvent(Point mousePoint) {}

        private object GetTooltipDataContext(Point currentPoint, out Point hitTestPoint)
        {
            hitTestPoint = default(Point);
            object dataContext = null;

            foreach (var seriesInfo in GetSeriesInfoAt(currentPoint))
            {
                // TODO 2: (Yuriy): It would be nice if the selected point was coloured differently or highlighted. How to do this? 
                //          - we can't use PaletteProvider, because this may be overridden by users. 
                //          - We could potentially use something similar to RolloverMarkerTemplate, however, this is not going to work for Column charts or Bubble charts
                //          - what would be cool is if the series itself drew the column, line point or bubble a different colour on mouse-over, we must discuss tomorrow
                if (seriesInfo.IsHit && seriesInfo.RenderableSeries.GetIncludeSeries(Modifier.Tooltip))
                {
                    hitTestPoint = seriesInfo.XyCoordinate;

                    dataContext = TooltipLabelDataContextSelector != null
                                      ? TooltipLabelDataContextSelector(seriesInfo)
                                      : seriesInfo;
                    break;
                }
            }

            return dataContext;
        }

        private void UpdateTooltipOverlay(Point mousePoint)
        {
            if (_tooltipLabelCache == null)
                return;

            PlaceOverlay(_tooltipLabelCache, mousePoint);

            if (!ModifierSurface.Children.Contains(_tooltipLabelCache))
                ModifierSurface.Children.Add(_tooltipLabelCache);
        }

        private void PlaceOverlay(FrameworkElement overlay, Point mousePoint)
        {
            var modifierRect = new Rect(0, 0, ModifierSurface.ActualWidth, ModifierSurface.ActualHeight);

            var x = mousePoint.X + TooltipXyOffset;
            var y = mousePoint.Y + TooltipXyOffset;

            var overlayRect = new Rect(x, y, overlay.ActualWidth, overlay.ActualHeight);

            if (modifierRect.Right < overlayRect.Right)
            {
                x = mousePoint.X - overlayRect.Width - TooltipXyOffset;
            }

            if (modifierRect.Bottom < overlayRect.Bottom)
            {
                var diff = overlayRect.Bottom - modifierRect.Bottom;
                y = y - diff;
                // This check needed to prevent cutting tooltip at the top
                y = y < 0 ? 0 : y;
            }

            Canvas.SetLeft(overlay, x);
            Canvas.SetTop(overlay, y);
        }

        /// <summary>
        /// When overriden in a derived class, applies new template to tooltip label, <seealso cref="TooltipModifierBase.TooltipLabelTemplateSelector" /> and <seealso cref="TooltipModifierBase.TooltipLabelTemplate" />
        /// </summary>
        protected override void OnTooltipLabelTemplateChanged()
        {
            _tooltipLabelCache = CreateFromTemplate(TooltipLabelTemplate, TooltipLabelTemplateSelector, this);
        }

        /// <summary>
        /// When overriden in a derived class, applies new template to axis labels, <seealso cref="TooltipModifierBase.AxisLabelTemplateSelector" /> and <seealso cref="TooltipModifierBase.AxisLabelTemplate" />
        /// </summary>
        protected override void OnAxisLabelTemplateChanged()
        {
            //TODO
        }
    }
}