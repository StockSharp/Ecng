// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// RolloverModifier.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Shapes;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.ChartModifiers
{
    /// <summary>
    /// The <see cref="RolloverModifier"/> provides a mouse-over hit-test to a chart, plus a collection of <see cref="SeriesInfo"/> objects to bind to which updates as the mouse moves.
    /// Add to a <see cref="UltrachartSurface"/> and set IsEnabled to true to enable this behaviour
    /// </summary>
    public class RolloverModifier : VerticalSliceModifierBase
    {
        /// <summary>
        /// Defined IncludeSeries Attached Property
        /// </summary>
        public static readonly DependencyProperty IncludeSeriesProperty = DependencyProperty.RegisterAttached("IncludeSeries", typeof(bool), typeof(RolloverModifier), new PropertyMetadata(true));

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
        /// Defines the DrawVerticalLine DependencyProperty
        /// </summary>
        public static readonly DependencyProperty DrawVerticalLineProperty = DependencyProperty.Register("DrawVerticalLine", typeof(bool), typeof(RolloverModifier), new PropertyMetadata(true));
        
        private bool _isLineDrawn;
        private Line _line;
        private IPlaceRolloverLineStrategy _placeRolloverLineStrategy;

        /// <summary>
        /// Initializes a new instance of the <see cref="RolloverModifier"/> class.
        /// </summary>
        /// <remarks></remarks>
        public RolloverModifier()
        {
            DefaultStyleKey = typeof(RolloverModifier);
            this.SetCurrentValue(SeriesDataProperty, new ChartDataObject());
        }

        /// <summary>
        /// Gets or sets whether a Vertical Line should be drawn at the rollover location
        /// </summary>
        public bool DrawVerticalLine
        {
            get { return (bool)GetValue(DrawVerticalLineProperty); }
            set { SetValue(DrawVerticalLineProperty, value); }
        }

        protected override void FillWithIncludedSeries(IEnumerable<SeriesInfo> infos, ObservableCollection<SeriesInfo> seriesInfos)
        {
            infos.ForEachDo(info =>
            {
                var includeSeries = info.RenderableSeries.GetIncludeSeries(Modifier.Rollover);
                if (includeSeries)
                {
                    seriesInfos.Add(info);
                }
            });
        }

        /// <summary>
        /// Get rollover marker from <see cref="SeriesInfo"/> to place on chart 
        /// </summary>
        /// <param name="seriesInfo"></param>
        /// <returns></returns>
        protected override FrameworkElement GetRolloverMarkerFrom(SeriesInfo seriesInfo)
        {
            var bandSeriesInfo = seriesInfo as BandSeriesInfo;
            var bandSeries = seriesInfo.RenderableSeries as FastBandRenderableSeries;

            // Choose different rollover marker for each line if FastBandRenderableSeries
            var rolloverMarker = bandSeriesInfo != null && bandSeries != null && bandSeriesInfo.IsFirstSeries
                                     ? bandSeries.RolloverMarker1
                                     : seriesInfo.RenderableSeries.RolloverMarker;

            return rolloverMarker;
        }

        /// <summary>
        /// Called when the parent surface SelectedSeries collection changes
        /// </summary>
        /// <param name="oldSeries"></param>
        /// <param name="newSeries"></param>
        protected override void OnSelectedSeriesChanged(IEnumerable<IRenderableSeries> oldSeries, IEnumerable<IRenderableSeries> newSeries)
        {
            base.OnSelectedSeriesChanged(oldSeries, newSeries);

            RemoveLine();
        }

        /// <summary>
        /// When overriden in a derived class, this method should clear all markers and tooltips from the <see cref="UltrachartSurface.ModifierSurface" />
        /// </summary>
        protected override void ClearAll()
        {
            base.ClearAll();

            RemoveLine();
        }

        /// <summary>
        /// When overriden in a derived class, called to handle the Master <see cref="ChartModifierBase" /> MouseMove event
        /// </summary>
        /// <param name="mousePoint">The current Mouse-point</param>
        protected override void HandleMasterMouseEvent(Point mousePoint)
        {
            _isLineDrawn = false;
            RemoveLine();

            base.HandleMasterMouseEvent(mousePoint);
        }

        private void RemoveLine()
        {
            if (ModifierSurface != null && _line != null)
            {
                ModifierSurface.Children.Remove(_line);
            }
        }

        /// <summary>
        /// If the current modifier IsEnabled, and the Point is valid for the modifier, updates axis and chart overlays
        /// </summary>
        /// <param name="atPoint">The current mouse point</param>
        protected override void TryUpdateOverlays(Point atPoint)
        {
            base.TryUpdateOverlays(atPoint);

            if (!IsEnabledAt(atPoint)) return;

            if (ShowAxisLabels && !_isLineDrawn)
            {
                UpdateAxesOverlay(atPoint);
            }

            TryDrawVerticalLine(atPoint);
        }

        private void TryDrawVerticalLine(Point showLineAt)
        {
            if (DrawVerticalLine && !_isLineDrawn)
            {
                var isVerticalChart = XAxis != null && !XAxis.IsHorizontalAxis;

                _isLineDrawn = ShowVerticalLine(showLineAt, isVerticalChart);
            }
        }

        private bool ShowVerticalLine(Point hitPoint, bool isVerticalChart)
        {
            _placeRolloverLineStrategy = GetPlaceRolloverLineStrategy();
            _line = _placeRolloverLineStrategy.ShowVerticalLine(hitPoint, isVerticalChart);

            var isLineDrawn = _line != null;

            if (isLineDrawn)
            {
                _line.Style = LineOverlayStyle;
                _line.IsHitTestVisible = false;

                ModifierSurface.Children.Add(_line);
            }

            return isLineDrawn;
        }

        private IPlaceRolloverLineStrategy GetPlaceRolloverLineStrategy()
        {
            IPlaceRolloverLineStrategy strategy;
            if (XAxis != null && XAxis.IsPolarAxis)
            {
                strategy = _placeRolloverLineStrategy as PolarPlaceRolloverLineStrategy ?? new PolarPlaceRolloverLineStrategy(this);
            }
            else
            {
                strategy = _placeRolloverLineStrategy as CartesianPlaceRolloverLineStrategy ?? new CartesianPlaceRolloverLineStrategy(this);
            }

            return strategy;
        }
    }
}