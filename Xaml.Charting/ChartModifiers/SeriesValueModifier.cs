// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// SeriesValueModifier.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Ecng.Xaml.Charting.Common.AttachedProperties;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.Annotations;
using Ecng.Xaml.Charting.Visuals.Axes;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.ChartModifiers
{
    /// <summary>
    /// A custom <see cref="ChartModifierBase"/> which places an <see cref="AxisMarkerAnnotation"/> on the YAxis for each
    /// <see cref="BaseRenderableSeries"/> in the chart, showing the current <see cref="BaseRenderableSeries"/> latest Y-value. 
    /// 
    /// E.g. for each series, place one axis-marker with the latest Y-value of the series
    /// </summary>
    public class SeriesValueModifier : ChartModifierBase
    {
        private ObservableCollection<IRenderableSeries> _renderSeriesCollection;
        private readonly IDictionary<IRenderableSeries, IAnnotation> _annotationsBySeries = new Dictionary<IRenderableSeries, IAnnotation>();

        private PropertyChangeNotifier _renderSeriesNotifier;

        /// <summary>
        /// The axis marker style DependencyProperty. 
        /// </summary>
        public static readonly DependencyProperty AxisMarkerStyleProperty = DependencyProperty.Register("AxisMarkerStyle", typeof(Style), typeof(SeriesValueModifier), new PropertyMetadata(null));

        /// <summary>
        /// Defines the YAxisId DependencyProperty
        /// </summary>
        public static readonly DependencyProperty YAxisIdProperty = DependencyProperty.Register("YAxisId", typeof (string), typeof (SeriesValueModifier), new PropertyMetadata(AxisBase.DefaultAxisId,OnAxisIdDependencyPropertyChanged));

        /// <summary>
        /// The IsSeriesValueModifier DependencyProperty. When Set to True on a RenderableSeries, this series will be included in the SeriesValueModifier processing, else it will be excluded
        /// </summary>
        public static readonly DependencyProperty IsSeriesValueModifierEnabledProperty = DependencyProperty.RegisterAttached("IsSeriesValueModifierEnabled", typeof(bool), typeof(SeriesValueModifier), new PropertyMetadata(true));

        /// <summary>
        /// Sets the IsSeriesValueModifierEnabled property on the element. 
        /// When Set to True on a RenderableSeries, this series will be included in the SeriesValueModifier processing, else it will be excluded
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="value">if set to <c>true</c> this series will be included in the SeriesValueModifier processing, else it will be excluded.</param>
        public static void SetIsSeriesValueModifierEnabled(UIElement element, bool value)
        {
            element.SetValue(IsSeriesValueModifierEnabledProperty, value);
        }

        /// <summary>
        /// Gets the IsSeriesValueModifierEnabled property on the element. 
        /// When Set to True on a RenderableSeries, this series will be included in the SeriesValueModifier processing, else it will be excluded
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>if set to <c>true</c> this series will be included in the SeriesValueModifier processing, else it will be excluded.</returns>
        public static bool GetIsSeriesValueModifierEnabled(UIElement element)
        {
            return (bool)element.GetValue(IsSeriesValueModifierEnabledProperty);
        }

        /// <summary>
        /// Defines the IsRenderableSeriesInViewport attached property. It is used to hide the AxisMarker associated with a renderable series when it goes outside the viewport.
        /// </summary>
        public static readonly DependencyProperty IsRenderableSeriesInViewportProperty =
            DependencyProperty.RegisterAttached("IsRenderableSeriesInViewport", typeof(bool), typeof(SeriesValueModifier), new PropertyMetadata(false));

        /// <summary>
        /// Gets the value of the <see cref="IsRenderableSeriesInViewportProperty"/>
        /// </summary>
        public static bool GetIsRenderableSeriesInViewport(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsRenderableSeriesInViewportProperty);
        }

        /// <summary>
        /// Gets the value of the <see cref="IsRenderableSeriesInViewportProperty"/>
        /// </summary>
        public static void SetIsRenderableSeriesInViewport(DependencyObject obj, bool value)
        {
            obj.SetValue(IsRenderableSeriesInViewportProperty, value);
        }

        /// <summary>
        /// Defines the IsLastPointInViewportProperty attached property.  It is used to change the opacity of AxisMarker 
        /// associated with a renderable series when the last point it goes outside the viewport.
        /// </summary>
        public static readonly DependencyProperty IsLastPointInViewportProperty = DependencyProperty.RegisterAttached("IsLastPointInViewport", 
                                                                                                                       typeof(bool),
                                                                                                                       typeof(SeriesValueModifier),
                                                                                                                       new PropertyMetadata(false));

        /// <summary>
        /// Gets the value of the <see cref="IsLastPointInViewportProperty"/>
        /// </summary>
        public static bool GetIsLastPointInViewport(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsLastPointInViewportProperty);
        }

        /// <summary>
        /// Sets the value of the <see cref="IsLastPointInViewportProperty"/>
        /// </summary>
        public static void SetIsLastPointInViewport(DependencyObject obj, bool value)
        {
            obj.SetValue(IsLastPointInViewportProperty, value);
        }

        /// <summary>
        /// Defines the LastPointColor attached property. Holds the color of the last visible bar in a viewport for a RenderableSeries
        /// </summary>
        public static readonly DependencyProperty SeriesMarkerColorProperty =
            DependencyProperty.RegisterAttached("SeriesMarkerColor", typeof(Color), typeof(SeriesValueModifier), new PropertyMetadata(default(Color)));

        /// <summary>
        /// Gets the value of the <see cref="SeriesMarkerColorProperty"/>
        /// </summary>
        public static Color GetSeriesMarkerColor(DependencyObject obj)
        {
            return (Color)obj.GetValue(SeriesMarkerColorProperty);
        }

        /// <summary>
        /// Sets the value of the <see cref="SeriesMarkerColorProperty"/>
        /// </summary>
        public static void SetSeriesMarkerColor(DependencyObject obj, Color value)
        {
            obj.SetValue(SeriesMarkerColorProperty, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SeriesValueModifier"/> class.
        /// </summary>
        public SeriesValueModifier()
        {
            IsPolarChartSupported = false;
        }

        /// <summary>
        /// Stores <see cref="AxisMarkerAnnotation"/> instances keyed by <see cref="IRenderableSeries"/>
        /// </summary>
        protected IDictionary<IRenderableSeries, IAnnotation> AnnotationsBySeries { get { return _annotationsBySeries; } } 

        /// <summary>
        /// Gets or sets a Style to apply to Axis Markers. 
        /// See remarks for implementation
        /// </summary>
        /// <remarks>
        /// NOTE: If you intend to override the AxisMarkerStyle, assume the DataContext is the RenderableSeries and you should include bindings of AxisMarker.Y1 to the RenderableSeries.DataSeries.LatestValue</remarks>
        public Style AxisMarkerStyle
        {
            get { return (Style)GetValue(AxisMarkerStyleProperty); }
            set { SetValue(AxisMarkerStyleProperty, value); }
        }

        /// <summary>
        /// Defines which YAxis to bind the <see cref="SeriesValueModifier"/> to, matching by string Id
        /// </summary>
        public string YAxisId
        {
            get { return (string) GetValue(YAxisIdProperty); }
            set { SetValue(YAxisIdProperty, value); }
        }

        /// <summary>
        /// Called when the Chart Modifier is attached to the Chart Surface
        /// </summary>
        public override void OnAttached()
        {
            base.OnAttached();

            var scs = ParentSurface as UltrachartSurface;

            if (scs != null)
            {
                _renderSeriesNotifier = new PropertyChangeNotifier(scs, UltrachartSurface.RenderableSeriesProperty);
                _renderSeriesNotifier.ValueChanged += OnRenderableSeriesDrasticallyChanged;
            }

            OnRenderableSeriesDrasticallyChanged();
        }

        private void OnRenderableSeriesDrasticallyChanged()
        {
            ResetAllMarkers();

            if (_renderSeriesCollection != null)
            {
                _renderSeriesCollection.CollectionChanged -= OnRenderableSeriesCollectionChanged;
            }

            _renderSeriesCollection = ParentSurface.RenderableSeries;

            if (_renderSeriesCollection != null)
            {
                _renderSeriesCollection.CollectionChanged += OnRenderableSeriesCollectionChanged;
            }
        }

        private void OnRenderableSeriesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                ResetAllMarkers();
                return;
            }

            if (ParentSurface != null && ParentSurface.Annotations != null)
            {
                e.OldItems.ForEachDo<IRenderableSeries>(DetachMarkerFor);
                e.NewItems.ForEachDo<IRenderableSeries>(AttachMarkerFor);
            }
        }

        private void ResetAllMarkers()
        {
            if (ParentSurface != null && ParentSurface.Annotations != null)
            {
                ClearAllMarkers();

                if (IsEnabled)
                {
                    ParentSurface.RenderableSeries.ForEachDo(AttachMarkerFor);
                }
            }
        }

        private void ClearAllMarkers()
        {
            if (ParentSurface != null && ParentSurface.Annotations != null)
            {
                _annotationsBySeries.ForEachDo(kvp => ParentSurface.Annotations.Remove(kvp.Value));
            }

            _annotationsBySeries.Clear();
        }

        private void AttachMarkerFor(IRenderableSeries renderableSeries)
        {
            if (renderableSeries.YAxisId == YAxisId && !_annotationsBySeries.ContainsKey(renderableSeries))
            {
                var axisMarker = new SeriesValueAxisMarkerAnnotation
                                 {
                                     Style = AxisMarkerStyle,
                                     DataContext = renderableSeries,
                                     Y1 =
                                         renderableSeries.DataSeries != null
                                             ? renderableSeries.DataSeries.LatestYValue
                                             : null,
                                     XAxisId = renderableSeries.XAxisId,
                                     YAxisId = renderableSeries.YAxisId
                                 };

                ParentSurface.Annotations.Add(axisMarker);
                _annotationsBySeries.Add(renderableSeries, axisMarker);
            }
        }

        private void DetachMarkerFor(IRenderableSeries renderableSeries)
        {
            IAnnotation annotation;
            if (_annotationsBySeries.TryGetValue(renderableSeries, out annotation))
            {
                ParentSurface.Annotations.Remove(annotation);
                _annotationsBySeries.Remove(renderableSeries);
            }
        }

        /// <summary>
        /// Called immediately before the Chart Modifier is detached from the Chart Surface
        /// </summary>
        public override void OnDetached()
        {
            ClearAllMarkers();
            if (_renderSeriesCollection != null)
            {
                _renderSeriesCollection.CollectionChanged -= OnRenderableSeriesCollectionChanged;
                _renderSeriesCollection = null;
            }

            if (_renderSeriesNotifier != null)
            {
                _renderSeriesNotifier.ValueChanged -= OnRenderableSeriesDrasticallyChanged;
                _renderSeriesNotifier = null;
            }
        }

        /// <summary>
        /// Called when the IsEnabled property changes on this <see cref="SeriesValueModifier"/> instance
        /// </summary>
        protected override void OnIsEnabledChanged()
        {
            base.OnIsEnabledChanged();

            ResetAllMarkers();
        }

        protected override void OnAnnotationCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            base.OnAnnotationCollectionChanged(sender, args);

            if (args.Action == NotifyCollectionChangedAction.Reset)
            {
                ResetAllMarkers();
            }
        }

        /// <summary>
        /// Called when the parent <see cref="UltrachartSurface" /> is rendered
        /// </summary>
        /// <param name="e">The <see cref="UltrachartRenderedMessage" /> which contains the event arg data</param>
        public override void OnParentSurfaceRendered(UltrachartRenderedMessage e)
        {
            base.OnParentSurfaceRendered(e);

            if(!IsEnabled) return;
           
            if (ParentSurface.RenderableSeries.Count(s=>s.YAxisId == YAxisId) != _annotationsBySeries.Count)
            {
                ResetAllMarkers();
            }

            foreach (var renderableSeries in ParentSurface.RenderableSeries.Where(CanUpdateAxisMarkerFor))
            {
                var axisMarker = (AxisMarkerAnnotation)_annotationsBySeries[renderableSeries];

                var xRange = renderableSeries.XAxis.VisibleRange;
                var visiblePoints = renderableSeries.DataSeries.GetIndicesRange(xRange);

                HitTestInfo hitTest = HitTestInfo.Empty;
                var lastInViewport = renderableSeries.DataSeries.LatestYValue;

                var isInViewport = visiblePoints.IsDefined;
                if (isInViewport)
                {
                    var xValue = (IComparable)renderableSeries.DataSeries.XValues[visiblePoints.Max];
                    if (!xRange.AsDoubleRange().IsValueWithinRange(xValue.ToDouble()))
                    {
                        var coord = ModifierSurface.ActualWidth - 1;
                        var point = XAxis.IsHorizontalAxis ? new Point(coord, 0) : new Point(0, coord);

                        hitTest = renderableSeries.VerticalSliceHitTest(point, true);

                        lastInViewport = hitTest.YValue;
                    }
                }

                var lastYValue = renderableSeries.DataSeries.LatestYValue;
                var isLastVisibleYInViewPort = lastYValue != null &&
                                               lastInViewport != null &&
                                               lastYValue.CompareTo(lastInViewport) == 0;

                var series = renderableSeries as BaseRenderableSeries;
                if (series != null)
                {
                    series.SetValue(IsRenderableSeriesInViewportProperty, isInViewport);
                    series.SetValue(IsLastPointInViewportProperty, isLastVisibleYInViewPort);

                    var markerColor = !hitTest.IsEmpty()
                        ? renderableSeries.GetSeriesColorAtPoint(hitTest)
                        : series.SeriesColor;

                    series.SetValue(SeriesMarkerColorProperty, markerColor);
                }
                
                axisMarker.Y1 = lastInViewport;

                if (renderableSeries.YAxis != null)
                {                    
                    axisMarker.FormattedValue = FormatAxisMarker(renderableSeries, lastInViewport);
                }
            }
        }

        protected virtual string FormatAxisMarker(IRenderableSeries renderableSeries, IComparable latestPointInViewport)
        {
            var yAxis = (AxisBase)renderableSeries.YAxis;
            return yAxis.FormatCursorText(latestPointInViewport);
        }

        private bool CanUpdateAxisMarkerFor(IRenderableSeries renderSeries)
        {
            return renderSeries.DataSeries != null && renderSeries.XAxis != null && renderSeries.YAxisId == YAxisId;
        }

        private static void OnAxisIdDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var modifier = d as SeriesValueModifier;
            if (modifier != null && modifier.ParentSurface != null)
            {
                modifier.ResetAllMarkers();
            }
        }
    }
}