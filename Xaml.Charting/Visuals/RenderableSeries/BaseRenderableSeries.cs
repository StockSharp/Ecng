// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// BaseRenderableSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml;
using System.Xml.Schema;
using Ecng.Xaml.Charting.ChartModifiers;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Common.Helpers;
using Ecng.Xaml.Charting.Licensing;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Numerics;
using Ecng.Xaml.Charting.Numerics.CoordinateCalculators;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals.Axes;
using Ecng.Xaml.Charting.Visuals.PointMarkers;
using Ecng.Xaml.Licensing.Core;


namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    /// <summary>
    /// Defines the Base class for all RenderableSeries within Ultrachart. 
    /// </summary>
    /// <remarks>
    /// A RenderableSeries has a <see cref="IDataSeries"/> data-source, 
    /// may have a <see cref="BasePointMarker"/> point-marker, and draws onto a specific <see cref="RenderSurfaceBase"/> using the <see cref="IRenderContext2D"/>. 
    /// A given <see cref="UltrachartSurface"/> may have 0..N <see cref="BaseRenderableSeries"/>, each of which may map to, or share a <see cref="IDataSeries"/>
    /// </remarks>
    /// <seealso cref="IDataSeries"/>
    /// <seealso cref="BasePointMarker"/>
    /// <seealso cref="IRenderContext2D"/>
    /// <seealso cref="FastLineRenderableSeries"/>
    /// <seealso cref="FastMountainRenderableSeries"/>
    /// <seealso cref="FastColumnRenderableSeries"/>
    /// <seealso cref="FastOhlcRenderableSeries"/>
    /// <seealso cref="XyScatterRenderableSeries"/>
    /// <seealso cref="FastCandlestickRenderableSeries"/>
    /// <seealso cref="FastBandRenderableSeries"/>
    /// <seealso cref="FastErrorBarsRenderableSeries"/>
    /// <seealso cref="FastBoxPlotRenderableSeries"/>
    /// <seealso cref="FastBubbleRenderableSeries"/>
    /// <seealso cref="FastHeatMapRenderableSeries"/>
    /// <seealso cref="StackedColumnRenderableSeries"/>
    /// <seealso cref="StackedMountainRenderableSeries"/>
    [ContentProperty("PointMarker")]
    public abstract class BaseRenderableSeries : ContentControl, IRenderableSeries
    {        
        /// <summary>
        /// Defines the StrokeThickness DependencyProperty 
        /// </summary>
        public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register("StrokeThickness", typeof(int), typeof(BaseRenderableSeries), new PropertyMetadata(1, OnStrokeThicknessChanged));

        /// <summary>
        /// Defines the IsSelected DependencyProperty
        /// </summary>
        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register("IsSelected", typeof(bool), typeof(BaseRenderableSeries), new PropertyMetadata(OnIsSelectedChanged));

        /// <summary>
        /// Defines the DataSeriesIndex DependencyProperty
        /// </summary>
        [Obsolete("We're sorry! The DataSeriesIndex property has been made obsolete. You can now bind RenderableSeries directly to DataSeries via the BaseRenderableSeries.DataSeries DependencyProperty", true)]
        public static readonly DependencyProperty DataSeriesIndexProperty = DependencyProperty.Register("DataSeriesIndex", typeof(int), typeof(BaseRenderableSeries), new PropertyMetadata(-1, OnInvalidateParentSurface));

        /// <summary>
        /// Defines the DataSeries DependencyProperty
        /// </summary>
        public static readonly DependencyProperty DataSeriesProperty = DependencyProperty.Register("DataSeries", typeof(IDataSeries), typeof(BaseRenderableSeries), new PropertyMetadata(null, (s, e) => ((BaseRenderableSeries)s).OnDataSeriesDependencyPropertyChanged((IDataSeries)e.OldValue, (IDataSeries)e.NewValue)));

        /// <summary>
        /// Defines the IsVisible DependencyProperty
        /// </summary>
        public static readonly
#if !SILVERLIGHT
 new
#endif
 DependencyProperty IsVisibleProperty = DependencyProperty.Register("IsVisible", typeof(bool), typeof(BaseRenderableSeries), new PropertyMetadata(true, OnIsVisibleChanged));

        /// <summary>
        /// Defines the SeriesColor DependencyProperty
        /// </summary>
        public static readonly DependencyProperty SeriesColorProperty = DependencyProperty.Register("SeriesColor", typeof(Color), typeof(BaseRenderableSeries), new PropertyMetadata(Colors.White, OnSeriesColorPropertyChanged));

        /// <summary>
        /// Defines the SelectedSelectedSeriesStyle DependencyProperty
        /// </summary>
        public static readonly DependencyProperty SelectedSeriesStyleProperty = DependencyProperty.Register("SelectedSeriesStyle", typeof(Style), typeof(BaseRenderableSeries), new PropertyMetadata(OnSelectedSeriesStyleChanged));

        /// <summary>
        /// Defines the ResamplingMode DependencyProperty
        /// </summary>
        public static readonly DependencyProperty ResamplingModeProperty = DependencyProperty.Register("ResamplingMode", typeof(ResamplingMode), typeof(BaseRenderableSeries), new PropertyMetadata(ResamplingMode.Auto, OnResamplingPropertyChanged));

        /// <summary>
        /// Defines the AntiAliasing DependencyProperty
        /// </summary>
        public static readonly DependencyProperty AntiAliasingProperty = DependencyProperty.Register("AntiAliasing", typeof(bool), typeof(BaseRenderableSeries), new PropertyMetadata(true, OnInvalidateParentSurface));

        /// <summary>
        /// Defines the PointMarkerTemplate DependencyProperty
        /// </summary>
        public static readonly DependencyProperty PointMarkerTemplateProperty = DependencyProperty.Register("PointMarkerTemplate", typeof(ControlTemplate), typeof(BaseRenderableSeries), new PropertyMetadata(null, OnPointMarkerTemplatePropertyChanged));

        /// <summary>
        /// Defines the PointMarker DependencyProperty
        /// </summary>
        public static readonly DependencyProperty PointMarkerProperty = DependencyProperty.Register("PointMarker", typeof(IPointMarker), typeof(BaseRenderableSeries), new PropertyMetadata(null, OnPointMarkerPropertyChanged));

        /// <summary>
        /// Defines the RolloverMarkerTemplate DependencyProperty
        /// </summary>
        public static readonly DependencyProperty RolloverMarkerTemplateProperty = DependencyProperty.Register("RolloverMarkerTemplate", typeof(ControlTemplate), typeof(BaseRenderableSeries), new PropertyMetadata(null, OnRolloverMarkerPropertyChanged));

        /// <summary>
        /// Defines the LegendMarkerTemplate DependencyProperty
        /// </summary>
        public static readonly DependencyProperty LegendMarkerTemplateProperty =
            DependencyProperty.Register("LegendMarkerTemplate", typeof(DataTemplate), typeof(BaseRenderableSeries), new PropertyMetadata(null));

        /// <summary>
        /// Defines the AxisAlignment DependencyProperty
        /// </summary>
        public static readonly DependencyProperty YAxisIdProperty = DependencyProperty.Register("YAxisId", typeof(string), typeof(BaseRenderableSeries), new PropertyMetadata(AxisBase.DefaultAxisId, OnInvalidateParentSurface));

        /// <summary>
        /// Defines the AxisAlignment DependencyProperty
        /// </summary>
        public static readonly DependencyProperty XAxisIdProperty = DependencyProperty.Register("XAxisId", typeof(string), typeof(BaseRenderableSeries), new PropertyMetadata(AxisBase.DefaultAxisId, OnInvalidateParentSurface));

        /// <summary>
        /// Defines the PaletteProvider DependencyProperty
        /// </summary>
        public static readonly DependencyProperty PaletteProviderProperty = DependencyProperty.Register("PaletteProvider", typeof(IPaletteProvider), typeof(BaseRenderableSeries), new PropertyMetadata(null, OnInvalidateParentSurface));

        /// <summary>
        /// Defines the ZeroLineY DependencyProperty
        /// </summary>
        public static readonly DependencyProperty ZeroLineYProperty = DependencyProperty.Register("ZeroLineY", typeof(double), typeof(BaseRenderableSeries), new PropertyMetadata(0d, OnInvalidateParentSurface));

        /// <summary>
        /// Defines the DrawNaNAs DependencyProperty
        /// </summary>
        public static readonly DependencyProperty DrawNaNAsProperty = DependencyProperty.Register("DrawNaNAs", typeof(LineDrawMode), typeof(BaseRenderableSeries), new PropertyMetadata(LineDrawMode.Gaps, OnInvalidateParentSurface));

        /// <summary>
        /// Event raised whenever IsSelected property changed
        /// </summary>
        public event EventHandler SelectionChanged;
        
        /// <summary>
        /// Event raised whenever IsVisible property changed
        /// </summary>
        public new event EventHandler IsVisibleChanged;
        
        private FrameworkElement _rolloverMarkerCache;

        private IPointMarker _pointMarkerFromTemplate;

        private Size _lastViewportSize = Size.Empty;
        private IDataSeries _dataSeries;

        private bool _useXCoordOnlyForHitTest;
        private IAxis _xAxis;
        private IAxis _yAxis;

        /// <summary>
        /// Finalizes an instance of the <see cref="BaseRenderableSeries"/> class.
        /// </summary>
        //~BaseRenderableSeries()
        //{
        //    DetachDataSeries(_dataSeries);
        //}

        /// <summary>
        /// A default radius used in the <see cref="HitTest(Point,bool)"/> method for interpolation, in RoloverModifier 
        /// and instead of <see cref="PointMarker"/> size when it is not set
        /// </summary>
        public const double DefaultHitTestRadius = 7.07;

        /// <summary>
        /// Default datapoint width used when there is only one point in point series
        /// </summary>
        private const int DefaultDatapointWidth = 70;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseRenderableSeries"/> class.
        /// </summary>
        /// <remarks></remarks>
        protected BaseRenderableSeries()
        {
            DefaultStyleKey = typeof(BaseRenderableSeries);            
            Initialize();
        }

        internal bool IsLicenseValid { get; set; }

        protected internal virtual bool IsPartOfExtendedFeatures => false;

        /// <summary>
        /// Gets the <see cref="ServiceContainer"/> which provides access to services throughout Ultrachart. 
        /// ServiceContainers are created one per <see cref="UltrachartSurface"/> instance, 
        /// and shared between peripheral components such as <see cref="AxisBase"/>, <see cref="BaseRenderableSeries"/>, <see cref="Ecng.Xaml.Charting.ChartModifiers.ChartModifierBase"/> instances.
        /// For a full list of available services, see the remarks on <see cref="ServiceContainer"/>
        /// </summary>
        public IServiceContainer Services { get; set; }

        /// <summary>
        /// Gets or sets the value which determines the zero line in Y direction.
        /// Used to set the bottom of a column, or the zero line in a mountain
        /// </summary>
        public double ZeroLineY
        {
            get { return (double)GetValue(ZeroLineYProperty); }
            set { SetValue(ZeroLineYProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether the series is visible when drawn
        /// </summary>
        public
#if !SILVERLIGHT
 new
#endif
 bool IsVisible
        {
            get { return (bool)GetValue(IsVisibleProperty); }
            set { SetValue(IsVisibleProperty, value); }
        }

        /// <summary>
        /// Gets or sets the StrokeThickness of the line. 
        /// </summary>
        /// <remarks>
        /// Note that increasing stroke thickness from 1 will have a detrimental effect on performance
        /// </remarks>
        public int StrokeThickness
        {
            get { return (int)GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }

        /// <summary>
        /// Gets or sets the resampling resolution. The default is 2, which results in Nyquist resampling. Lower values are not permitted. Higher values result in potentially more visually accurate rendering, but at the expense of performance
        /// </summary>
        /// <value>The resampling resolution.</value>
        /// <remarks></remarks>
        [Obsolete("The ResamplingResolution property is no longer used. Please remove from your code", true)]
        public int ResamplingResolution { get; set; }

        /// <summary>
        /// Gets or Sets an optional <see cref="IPaletteProvider" /> instance, which may be used to override specific data-point colors during rendering.
        /// For more details, see the <see cref="IPaletteProvider" /> documentation
        /// </summary>
        public IPaletteProvider PaletteProvider
        {
            get { return (IPaletteProvider)GetValue(PaletteProviderProperty); }
            set { SetValue(PaletteProviderProperty, value); }
        }

        /// <summary>
        /// Gets a cached Framework Element which is used as a Rollover Marker.
        /// This is generated from a ControlTemplate in xaml via the <see cref="BaseRenderableSeries.RolloverMarkerTemplateProperty"/> DependencyProperty
        /// </summary>
        /// <remarks></remarks>
        public FrameworkElement RolloverMarker
        {
            get { return _rolloverMarkerCache; }
        }

        /// <summary>
        /// Gets or sets the PointMarker ControlTemplate, which defines the point-marker Visual to be rendered on each datapoint of the series
        /// </summary>
        /// <remarks>The ControlTemplate is used to template the visuals only for a blank control, creating a new instance per <see cref="BaseRenderableSeries"/>. 
        /// the resulting FrameworkElement is cached to bitmap and drawn on each redraw of the series, so any triggers, mouse interactions on the ControlTemplate will be lost</remarks>
        public ControlTemplate PointMarkerTemplate
        {
            get { return (ControlTemplate)GetValue(PointMarkerTemplateProperty); }
            set { SetValue(PointMarkerTemplateProperty, value); }
        }

        /// <summary>
        /// Gets or sets a <see cref="BasePointMarker"/> instance directly on the <see cref="BaseRenderableSeries"/>. When a <see cref="BasePointMarker"/>
        /// is present, then the markers will be drawn at each data-point in the series
        /// </summary>
        public IPointMarker PointMarker
        {
            get { return (BasePointMarker)GetValue(PointMarkerProperty); }
            set { SetValue(PointMarkerProperty, value); }
        }

        /// <summary>
        /// Gets or sets the RolloverMarker ControlTemplate, which defines the Visual to be rendered on the series when the <see cref="Ecng.Xaml.Charting.ChartModifiers.RolloverModifier"/> is enabled and the user moves the mouse.
        /// </summary>
        /// <remarks>The ControlTemplate is used to template the visuals only for a blank control, creating a new instance per <see cref="BaseRenderableSeries"/></remarks>
        public ControlTemplate RolloverMarkerTemplate
        {
            get { return (ControlTemplate)GetValue(RolloverMarkerTemplateProperty); }
            set { SetValue(RolloverMarkerTemplateProperty, value); }
        }

        /// <summary>
        /// Gets or sets the DataTemplate, which defines the Visual to be rendered on the <see cref="UltrachartLegend"/> as a series marker
        /// </summary>
        public DataTemplate LegendMarkerTemplate
        {
            get { return (DataTemplate)GetValue(LegendMarkerTemplateProperty); }
            set { SetValue(LegendMarkerTemplateProperty, value); }
        }

        /// <summary>
        /// Gets or sets the ID of the Y-Axis which this renderable series is measured against
        /// </summary>
        public string YAxisId
        {
            get { return (string)GetValue(YAxisIdProperty); }
            set { SetValue(YAxisIdProperty, value); }
        }

        /// <summary>
        /// Gets or sets the ID of the X-Axis which this renderable series is measured against
        /// </summary>
        public string XAxisId
        {
            get { return (string)GetValue(XAxisIdProperty); }
            set { SetValue(XAxisIdProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether this series uses AntiAliasing when drawn
        /// </summary>
        /// <value><c>true</c> if anti aliasing is enabled; otherwise, <c>false</c>.</value>
        /// <remarks></remarks>
        public bool AntiAliasing
        {
            get { return (bool)GetValue(AntiAliasingProperty); }
            set { SetValue(AntiAliasingProperty, value); }
        }

        /// <summary>
        /// Gets or sets the <see cref="Ecng.Xaml.Charting.Numerics.ResamplingMode"/> used when drawing this series
        /// </summary>
        /// <value>The resampling mode.</value>
        /// <remarks></remarks>
        public ResamplingMode ResamplingMode
        {
            get { return (ResamplingMode)GetValue(ResamplingModeProperty); }
            set { SetValue(ResamplingModeProperty, value); }
        }

        public virtual object PointSeriesArg => null;

        /// <summary>
        /// Gets or sets the SeriesColor.
        /// </summary>
        /// <value>The color of the series.</value>
        /// <remarks></remarks>
        public Color SeriesColor
        {
            get { return (Color)GetValue(SeriesColorProperty); }
            set { SetValue(SeriesColorProperty, value); }
        }

        /// <summary>
        /// Gets or sets a style for selected series.
        /// </summary>
        /// <value>The style of the selected series.</value>
        /// <remarks></remarks>
        public Style SelectedSeriesStyle
        {
            get { return (Style)GetValue(SelectedSeriesStyleProperty); }
            set { SetValue(SelectedSeriesStyleProperty, value); }
        }

        /// <summary>
        /// Gets or sets the IsSelectedProperty.
        /// </summary>
        /// <value>The color of the selected series.</value>
        /// <remarks></remarks>
        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        /// <summary>
        /// Gets or sets the DataSeries associated with this series
        /// </summary>
        /// <value>The data series.</value>
        /// <remarks></remarks>
        public IDataSeries DataSeries
        {
            get { return (IDataSeries)GetValue(DataSeriesProperty); }
            set { this.ThreadSafeSetValue(DataSeriesProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating how this renderable series will treat double.NaN. See <see cref="LineDrawMode"/> for available options
        /// </summary>
        public LineDrawMode DrawNaNAs
        {
            get { return (LineDrawMode)GetValue(DrawNaNAsProperty); }
            set { SetValue(DrawNaNAsProperty, value); }
        }

        /// <summary>
        /// Gets or sets the RenderPassData instance used for this render pass
        /// </summary>
        /// <value>The render data.</value>
        /// <remarks></remarks>
        public IRenderPassData CurrentRenderPassData { get; set; }

        /// <summary>
        /// Gets or sets the XAxis that this <see cref="IRenderableSeries"/> is associated with
        /// </summary>
        /// <value>The X axis.</value>
        /// <remarks></remarks>
        public IAxis XAxis
        {
            get { return _xAxis; }
            set { _xAxis = SetAndNotifyAxes(_xAxis, value); }
        }

        /// <summary>
        /// Gets or sets the YAxis that this <see cref="IRenderableSeries"/> is associated with
        /// </summary>
        /// <value>The Y axis.</value>
        /// <remarks></remarks>
        public IAxis YAxis
        {
            get { return _yAxis; }
            set { _yAxis = SetAndNotifyAxes(_yAxis, value); }
        }

        private static IAxis SetAndNotifyAxes(IAxis currentAxis, IAxis newAxis)
        {
            if (currentAxis != newAxis)
            {
                var previousAxis = currentAxis;
                currentAxis = newAxis;

                AxisBase.NotifyDataRangeChanged(previousAxis);
                AxisBase.NotifyDataRangeChanged(currentAxis);
            }

            return currentAxis;
        }
        
        /// <summary>
        /// If true, the data is displayed as XY, e.g. like a Scatter plot, not a line (time) series
        /// </summary>
        public virtual bool DisplaysDataAsXy { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="BaseRenderableSeries"/> is valid for drawing.
        /// </summary>
        /// <remarks></remarks>
        internal virtual bool IsValidForDrawing
        {
            get
            {
                var isValid = GetIsValidForDrawing();

                return isValid
#if LICENSEDDEPLOY
                       && IsLicenseValid
#endif
                    ;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="BaseRenderableSeries"/> is valid for drawing.
        /// </summary>
        protected virtual bool GetIsValidForDrawing()
        {
            var isValid = (DataSeries != null &&
                           DataSeries.HasValues &&
                           IsVisible &&
                           CurrentRenderPassData != null &&
                           CurrentRenderPassData.PointSeries != null);

            return isValid;
        }

        /// <summary>
        /// Returns the PointMarker used by this series
        /// </summary>
        /// <returns></returns>
        protected IPointMarker GetPointMarker()
        {
            return PointMarker ?? _pointMarkerFromTemplate;
        }

        /// <summary>
        /// Raises the <see cref="InvalidateUltrachartMessage"/> which causes the parent <see cref="UltrachartSurface"/> to invalidate
        /// </summary>
        /// <remarks></remarks>
        public virtual void OnInvalidateParentSurface()
        {
            if (Services != null)
            {
                Services.GetService<IUltrachartSurface>().InvalidateElement();
            }
        }

        /// <summary>
        /// Called when the <see cref="BaseRenderableSeries.SeriesColor"/> dependency property changes. Allows derived types to do caching 
        /// </summary>
        protected virtual void OnSeriesColorChanged()
        {
        }

        /// <summary>
        /// Called when the <see cref="BaseRenderableSeries.DataSeries"/> dependency property changes.
        /// </summary>
        protected virtual void OnDataSeriesDependencyPropertyChanged()
        {
        }

        /// <summary>
        /// Used internally by the renderer. Asserts that the input data-type is of the correct format for the current <see cref="BaseRenderableSeries" />
        /// </summary>
        /// <typeparam name="TSeriesPoint">The type of the series point.</typeparam>
        /// <param name="dataSeriesType">Type of the data series.</param>
        /// <exception cref="System.InvalidOperationException"></exception>
        protected void AssertDataPointType<TSeriesPoint>(string dataSeriesType)
            where TSeriesPoint : ISeriesPoint<double>
        {
            // Sanity check on pointseries count
            if (CurrentRenderPassData.PointSeries == null || CurrentRenderPassData.PointSeries.Count == 0)
                return;

            // Check data-point type
            var ptTest = CurrentRenderPassData.PointSeries[0] as GenericPoint2D<TSeriesPoint>;
            if (ptTest == null)
            {
                throw new InvalidOperationException(
                    String.Format("{0} is expecting data passed as {1}. Please use dataseries type {2}",
                        this.GetType(),
                        typeof(TSeriesPoint),
                        dataSeriesType));
            }
        }

        /// <summary>
        /// Called when the instance is drawn
        /// </summary>
        /// <param name="renderContext">The <see cref="IRenderContext2D" /> used for drawing</param>
        /// <param name="renderPassData">The current render pass data</param>
        void IDrawable.OnDraw(IRenderContext2D renderContext, IRenderPassData renderPassData)
        {
            CurrentRenderPassData = renderPassData;

            if (IsValidForDrawing)
            {
                if (_lastViewportSize != renderContext.ViewportSize)
                {
                    OnParentSurfaceViewportSizeChanged();
                    _lastViewportSize = renderContext.ViewportSize;
                }

                // Don't draw invalid viewport sizes
                if (renderContext.ViewportSize.IsEmpty || renderContext.ViewportSize == new Size(1, 1))
                {
                    UltrachartDebugLogger.Instance.WriteLine("Aborting {0}.Draw() as ViewportSize is (1,1)", GetType().Name);
                    return;
                }

                var stopwatch = new Stopwatch();
                stopwatch.Start();

                InternalDraw(renderContext, renderPassData);

                stopwatch.Stop();
                UltrachartDebugLogger.Instance.WriteLine("{0} DrawTime: {1}ms", GetType().Name,
                    stopwatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Draws the series using the <see cref="IRenderContext2D"/> and the <see cref="IRenderPassData"/> passed in
        /// </summary>
        /// <param name="renderContext">The render context. This is a graphics object which has methods to draw lines, quads and polygons to the screen</param>
        /// <param name="renderPassData">The render pass data. Contains a resampled <see cref="IPointSeries"/>, the <see cref="IndexRange"/> of points on the screen
        /// and the current YAxis and XAxis <see cref="ICoordinateCalculator{T}"/> to convert data-points to screen points</param>
        protected abstract void InternalDraw(IRenderContext2D renderContext, IRenderPassData renderPassData);

        /// <summary>
        /// Gets the width of data-points, used to compute column and OHLC bar widths
        /// </summary>
        /// <param name="xCoordinateCalculator">The current x coordinate calculator.</param>
        /// <param name="pointSeries">The current <see cref="IPointSeries" /> being rendered.</param>
        /// <param name="widthFraction">The width fraction from 0.0 to 1.0, where 0.0 is infinitey small, 0.5 takes up half the available width and 1.0 means a data-point is the full width between points</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">widthFraction should be between 0.0 and 1.0 inclusive;widthFraction</exception>
        public int GetDatapointWidth(ICoordinateCalculator<double> xCoordinateCalculator, IPointSeries pointSeries, double widthFraction)
        {
            return GetDatapointWidth(xCoordinateCalculator, pointSeries, pointSeries.Count, widthFraction);
        }

        /// <summary>
        /// Gets the width of data-points, used to compute column and OHLC bar widths
        /// </summary>
        /// <param name="xCoordinateCalculator">The current x coordinate calculator.</param>
        /// <param name="pointSeries">The current <see cref="IPointSeries" /> being rendered.</param>
        /// <param name="barsAmount">Amount of bars within viewport</param>
        /// <param name="widthFraction">The width fraction from 0.0 to 1.0, where 0.0 is infinitey small, 0.5 takes up half the available width and 1.0 means a data-point is the full width between points</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">widthFraction should be between 0.0 and 1.0 inclusive;widthFraction</exception>
        public int GetDatapointWidth(ICoordinateCalculator<double> xCoordinateCalculator, IPointSeries pointSeries, double barsAmount, double widthFraction)
        {
            if (widthFraction < 0.0 || widthFraction > 1.0)
            {
                throw new ArgumentException("WidthFraction should be between 0.0 and 1.0 inclusive", "widthFraction");
            }

            double dataPointWidth = xCoordinateCalculator.IsHorizontalAxisCalculator ? _lastViewportSize.Width : _lastViewportSize.Height;
            if (barsAmount > 1)
            {
                var max = xCoordinateCalculator.GetCoordinate(pointSeries[pointSeries.Count - 1].X);
                var min = xCoordinateCalculator.GetCoordinate(pointSeries[0].X);
                var dist = Math.Abs(max - min);
                dataPointWidth = dist / (barsAmount - 1);
            }

            return (int)(dataPointWidth * widthFraction);
        }

        /// <summary>
        /// Returns a value that determines the position of Y zero line on a chart.
        /// Significant for the series types that render negative data points differently, 
        /// such as the <see cref="FastColumnRenderableSeries"/>, <see cref="FastMountainRenderableSeries"/>, <see cref="FastImpulseRenderableSeries"/>.
        /// </summary>
        /// <returns>The value in pixels indicating the position of zero line</returns>
        protected virtual double GetYZeroCoord()
        {
            var yZeroValue = (double)GetValue(ZeroLineYProperty);

            // End Y-point or X-point(depends on chart orientation) is either the height (e.g. the bottom of the chart pane), 
            // or the zero line (e.g. if the chart has negative numbers)
            var zeroCoord = CurrentRenderPassData.IsVerticalChart
                                  ? Math.Min(_lastViewportSize.Width + 1, CurrentRenderPassData.YCoordinateCalculator.GetCoordinate(yZeroValue))
                                  : Math.Min(_lastViewportSize.Height + 1, CurrentRenderPassData.YCoordinateCalculator.GetCoordinate(yZeroValue));

            return zeroCoord;
        }

        /// <summary>
        /// Performs a hit-test at the specific mouse point (X,Y coordinate on the parent <see cref="UltrachartSurface" />) with the default HitTestRadius,
        /// returning a <see cref="HitTestInfo" /> struct with the results
        /// </summary>
        /// <param name="rawPoint">The mouse point on the parent <see cref="UltrachartSurface" /></param>
        /// <param name="interpolate">If true, use interpolation to perform a hit-test between data-points, or on the area if a <seealso cref="Ecng.Xaml.Charting.Visuals.RenderableSeries.FastMountainRenderableSeries"/>, <seealso cref="Ecng.Xaml.Charting.Visuals.RenderableSeries.FastColumnRenderableSeries"/> or <seealso cref="Ecng.Xaml.Charting.Visuals.RenderableSeries.FastCandlestickRenderableSeries"/></param>
        /// <returns>
        /// A <see cref="HitTestInfo" /> struct with the results
        /// </returns>
        public HitTestInfo HitTest(Point rawPoint, bool interpolate = false)
        {
            return HitTest(rawPoint, DefaultHitTestRadius, interpolate);
        }

        /// <summary>
        /// Performs a hit-test at the specific mouse point with zero hit-test radius. 
        /// Method considers only X value and returns a <see cref="HitTestInfo" /> struct based on the data point with the closest X value
        /// </summary>
        /// <param name="rawPoint">The mouse point on the parent <see cref="UltrachartSurface" /></param>
        /// <param name="interpolate">If true, use interpolation to perform a hit-test between data-points, or on the area if a <seealso cref="Ecng.Xaml.Charting.Visuals.RenderableSeries.FastMountainRenderableSeries"/>, <seealso cref="Ecng.Xaml.Charting.Visuals.RenderableSeries.FastColumnRenderableSeries"/> or <seealso cref="Ecng.Xaml.Charting.Visuals.RenderableSeries.FastCandlestickRenderableSeries"/></param>
        /// <returns>
        /// A <see cref="HitTestInfo" /> struct with the results
        /// </returns>
        /// <remarks>
        /// Used by <see cref="RolloverModifier"/> and <see cref="VerticalSliceModifier"/>
        /// </remarks>
        public HitTestInfo VerticalSliceHitTest(Point rawPoint, bool interpolate = false)
        {
            _useXCoordOnlyForHitTest = true;

            var hitResult = HitTest(rawPoint, 0, interpolate);

            _useXCoordOnlyForHitTest = false;

            return hitResult;
        }

        /// <summary>
        /// Performs a hit-test at the specific mouse point (X,Y coordinate on the parent <see cref="UltrachartSurface" />) using passed <paramref name="hitTestRadius"/>,
        /// returning a <see cref="HitTestInfo" /> struct with the results
        /// </summary>
        /// <param name="rawPoint">The mouse point on the parent <see cref="UltrachartSurface" /></param>
        /// <param name="hitTestRadius">The radius in pixels to determine whether a mouse is over a data-point</param>
        /// <param name="interpolate">If true, use interpolation to perform a hit-test between data-points, or on the area if a <seealso cref="Ecng.Xaml.Charting.Visuals.RenderableSeries.FastMountainRenderableSeries"/>, <seealso cref="Ecng.Xaml.Charting.Visuals.RenderableSeries.FastColumnRenderableSeries"/> or <seealso cref="Ecng.Xaml.Charting.Visuals.RenderableSeries.FastCandlestickRenderableSeries"/></param>
        /// <returns>
        /// A <see cref="HitTestInfo" /> struct with the results
        /// </returns>
        public virtual HitTestInfo HitTest(Point rawPoint, double hitTestRadius, bool interpolate = false)
        {
            var nearestHitResult = HitTestInfo.Empty;

            //Used internally by either NearestHitResult and InterpolatePoint,
            //so need to check for a Null and ensure that the DataSeries contains a data
            if (CurrentRenderPassData != null && DataSeries != null && DataSeries.HasValues)
            {
                var hitRadius = hitTestRadius + StrokeThickness/2d;

                var transformationStrategy = CurrentRenderPassData.TransformationStrategy;
                
                rawPoint = transformationStrategy.Transform(rawPoint);

                nearestHitResult = HitTestInternal(rawPoint, hitRadius, interpolate);

                nearestHitResult.HitTestPoint = transformationStrategy.ReverseTransform(nearestHitResult.HitTestPoint);
                nearestHitResult.Y1HitTestPoint = transformationStrategy.ReverseTransform(nearestHitResult.Y1HitTestPoint);
            }

            return nearestHitResult;
        }

        protected virtual HitTestInfo HitTestInternal(Point rawPoint, double hitTestRadius, bool interpolate)
        {
            var searchMode = interpolate ? SearchMode.RoundDown : SearchMode.Nearest;

            var nearestHitResult = NearestHitResult(rawPoint, hitTestRadius, searchMode, !_useXCoordOnlyForHitTest);

            if (interpolate)
            {
                nearestHitResult = InterpolatePoint(rawPoint, nearestHitResult, hitTestRadius);
            }

            return nearestHitResult;
        }

        protected double GetHitTestRadiusConsideringPointMarkerSize(double hitTestRadius)
        {
            var pm = GetPointMarker();

            // For some reason sometimes PointMarker.Width is NAN
            var isValid = pm != null && !pm.Height.IsNaN() && !pm.Width.IsNaN();

            var dataPointRadius = isValid
                ? Math.Max(pm.Width, pm.Height) / 2 + hitTestRadius
                : hitTestRadius;

            return dataPointRadius;
        }

        /// <summary>
        /// Called by <see cref="BaseRenderableSeries.HitTest(Point, bool)" /> to get the nearest (non-interpolated) <see cref="HitTestInfo" /> to the mouse point
        /// </summary>
        /// <param name="mouseRawPoint">The mouse point</param>
        /// <param name="hitTestRadiusInPixels">The radius (in pixels) to use when determining if the <paramref name="mouseRawPoint" /> is over a data-point</param>
        /// <param name="searchMode">The search mode.</param>
        /// <param name="considerYCoordinateForDistanceCalculation">if set to <c>true</c> then perform a true euclidean distance to find the nearest hit result.</param>
        /// <returns>
        /// The <see cref="HitTestInfo" /> result
        /// </returns>
        /// <exception cref="System.ArgumentException">hitTestRadiusInPixels is NAN</exception>
        /// <exception cref="System.NotImplementedException"></exception>
        protected virtual HitTestInfo NearestHitResult(Point mouseRawPoint, double hitTestRadiusInPixels, SearchMode searchMode, bool considerYCoordinateForDistanceCalculation)
        {
            if (Double.IsNaN(hitTestRadiusInPixels)) throw new ArgumentException("hitTestRadiusInPixels is NAN");

            var mouseDataPoint = GetHitDataValue(mouseRawPoint);
            var transformedMouseRawPoint = TransformPoint(mouseRawPoint, CurrentRenderPassData.IsVerticalChart);
            var xUnitsPerPixel = Math.Abs(CurrentRenderPassData.XCoordinateCalculator.GetDataValue(transformedMouseRawPoint.X + 1) - CurrentRenderPassData.XCoordinateCalculator.GetDataValue(transformedMouseRawPoint.X));
            
            // in case of datetime it is number of ticks per pixel
            var yUnitsPerPixel = Math.Abs(CurrentRenderPassData.YCoordinateCalculator.GetDataValue(transformedMouseRawPoint.Y + 1) - CurrentRenderPassData.YCoordinateCalculator.GetDataValue(transformedMouseRawPoint.Y));
            var xyScaleRatio = considerYCoordinateForDistanceCalculation ? xUnitsPerPixel / yUnitsPerPixel : 0;

            int closestPointIndex;
            var dataSeries = DataSeries;

            if (dataSeries.Count < 2)
            {
                searchMode = SearchMode.Nearest;
            }

            // TODO InterpolationMode.SnapToNearestPointByX
            switch (searchMode)
            {
                case SearchMode.Nearest:
                    // This is a hardcoded patch for RolloverModifier
                    if (hitTestRadiusInPixels.CompareTo(0) == 0 && !DataSeries.IsSorted)
                    {
                        hitTestRadiusInPixels = DefaultHitTestRadius;
                    }

                    // Is used when 'UseInterpolation' is False
                    closestPointIndex = dataSeries.FindClosestPoint(
                        mouseDataPoint.Item1, mouseDataPoint.Item2, xyScaleRatio,
                        xUnitsPerPixel * hitTestRadiusInPixels);

                    break;

                case SearchMode.RoundDown:
                    // Is used when 'UseInterpolation' is True
                    closestPointIndex = dataSeries.FindClosestLine(
                        mouseDataPoint.Item1, mouseDataPoint.Item2, xyScaleRatio,
                        xUnitsPerPixel * hitTestRadiusInPixels, DrawNaNAs);

                    break;
                default:
                    throw new NotImplementedException();
            }

            var result = closestPointIndex != -1 && ((IComparable)dataSeries.YValues[closestPointIndex]).IsDefined()
                ? GetHitTestInfo(closestPointIndex, mouseRawPoint, hitTestRadiusInPixels, mouseDataPoint.Item1)
                : HitTestInfo.Empty;

            return result;
        }

        protected Tuple<IComparable, IComparable> GetHitDataValue(Point rawPoint)
        {
            rawPoint = TransformPoint(rawPoint, CurrentRenderPassData.IsVerticalChart);

            var xCoordinateCalculator = CurrentRenderPassData.XCoordinateCalculator;
            var yCoordinateCalculator = CurrentRenderPassData.YCoordinateCalculator;

            // Get X nearest to rawPoint
            double xHitDouble = xCoordinateCalculator.GetDataValue(rawPoint.X);
            IComparable hitXValue = ComparableUtil.FromDouble(xHitDouble, DataSeries.XValues[0].GetType());

            var catCoordCalc = xCoordinateCalculator as ICategoryCoordinateCalculator;
            // Need to interpolate between indexes
            if (catCoordCalc != null)
            {
                var firstIndex = (int) xHitDouble;
                hitXValue = catCoordCalc.TransformIndexToData(firstIndex);

                var firstCoord = catCoordCalc.GetCoordinate(firstIndex);
                var isLeft = firstCoord <= rawPoint.X;

                var secondIndex = isLeft
                    ? Math.Min(firstIndex + 1, DataSeries.XValues.Count - 1)
                    : Math.Max(firstIndex - 1, 0);

                if (firstIndex != secondIndex)
                {
                    var secondValue = catCoordCalc.TransformIndexToData(secondIndex).ToDouble();
                    var secondCoord = catCoordCalc.GetCoordinate(secondIndex);

                    var fraction =
                        Math.Abs((rawPoint.X - Math.Min(firstCoord, secondCoord))/(secondCoord - firstCoord));
                    var interpolatedValue = Math.Min(secondValue, hitXValue.ToDouble()) +
                                            Math.Abs(secondValue - hitXValue.ToDouble())*fraction;

                    hitXValue = new DateTime((long) interpolatedValue);
                }
            }

            var yHitDouble = yCoordinateCalculator.GetDataValue(rawPoint.Y);
            var hitYValue = ComparableUtil.FromDouble(yHitDouble, DataSeries.YValues[0].GetType());

            return new Tuple<IComparable, IComparable>(hitXValue, hitYValue);
        }
        
        /// <param name="hitTestRadius">is used to calculate HitTestInfo.IsHit</param>
        protected HitTestInfo GetHitTestInfo(int nearestDataPointIndex, Point rawPoint, double hitTestRadius, IComparable hitXValue)
        {
            var hitTestInfo = DataSeries.ToHitTestInfo(nearestDataPointIndex);

            lock (DataSeries.SyncRoot)
            {
                hitTestInfo.IsWithinDataBounds = !DataSeries.IsSorted ||
                                                 DataSeries.HasValues &&
                                                 hitXValue.CompareTo(DataSeries.XValues[0]) >= 0 &&
                                                 hitXValue.CompareTo(DataSeries.XValues[DataSeries.XValues.Count - 1]) <=
                                                 0;
            }

            var catCoordCalc = CurrentRenderPassData.XCoordinateCalculator as ICategoryCoordinateCalculator;

            // Compute the X and Y coordinate of the nearest data-point after hit testing
            double xCoord = catCoordCalc != null
                                ? catCoordCalc.GetCoordinate(nearestDataPointIndex)
                                : CurrentRenderPassData.XCoordinateCalculator.GetCoordinate(hitTestInfo.XValue.ToDouble());

            double yCoord = CurrentRenderPassData.YCoordinateCalculator.GetCoordinate(hitTestInfo.YValue.ToDouble());

            var nearestPoint = new Point(xCoord, yCoord);
            rawPoint = TransformPoint(rawPoint, CurrentRenderPassData.IsVerticalChart);

            hitTestInfo.HitTestPoint = hitTestInfo.Y1HitTestPoint = TransformPoint(nearestPoint, CurrentRenderPassData.IsVerticalChart);

            hitTestInfo.IsVerticalHit = hitTestInfo.IsWithinDataBounds |= Math.Abs(xCoord - rawPoint.X) < hitTestRadius;

            var distance = XAxis != null && XAxis.IsPolarAxis
                ? PointUtil.PolarDistance(nearestPoint, rawPoint)
                : PointUtil.Distance(nearestPoint, rawPoint);
            hitTestInfo.IsHit = distance < hitTestRadius;

            return hitTestInfo;
        }

        /// <summary>
        /// Interpolation function called by <see cref="BaseRenderableSeries.HitTest(Point, bool)"/> when the inpolate flag is true
        /// </summary>
        /// <param name="rawPoint">Mouse point</param>
        /// <param name="nearestHitResult">Non-interpolated <see cref="HitTestInfo"/></param>
        /// <param name="hitTestRadius">The value, which indicates distance used to consider whether the series is hit or not</param>
        /// <returns>Intepolated <see cref="HitTestInfo"/></returns>
        protected virtual HitTestInfo InterpolatePoint(Point rawPoint, HitTestInfo nearestHitResult, double hitTestRadius)
        {
            if (!nearestHitResult.IsEmpty())
            {
                var prevDataPointIndex = nearestHitResult.DataSeriesIndex;
                var nextDataPointIndex = nearestHitResult.DataSeriesIndex + 1;
                
                // Ensure the index isn't out of the bounds of the DataSeries.XValues
                if (nextDataPointIndex >= 0 && nextDataPointIndex < DataSeries.Count)
                {
                    var yValues = GetPrevAndNextYValues(prevDataPointIndex, i => ((IComparable)DataSeries.YValues[i]).ToDouble());
                    nearestHitResult = InterpolatePoint(rawPoint, nearestHitResult, hitTestRadius, yValues);
                }
            }

            return nearestHitResult;
        }

        protected HitTestInfo InterpolatePoint(Point rawPoint, HitTestInfo nearestHitResult, double hitTestRadius, Tuple<double, double> yValues, Tuple<double, double> y1Values = null)
        {
            var hitValues = GetHitDataValue(rawPoint);
            var hitChartPoint = new Point(hitValues.Item1.ToDouble(), hitValues.Item2.ToDouble());

            var nextDataPointIndex = nearestHitResult.DataSeriesIndex + 1;

            // Ensure the index isn't out of the bounds of the DataSeries.XValues
            var nearestDataPoint = new Point(nearestHitResult.XValue.ToDouble(), yValues.Item1);
            var prevDatapoint = nearestDataPoint;

            var nextDataPointX = ((IComparable) DataSeries.XValues[nextDataPointIndex]).ToDouble();
            var nextDataPoint = new Point(nextDataPointX, yValues.Item2);

            Point nearestDataPointY1 = default(Point);
            Point nextDataPointY1 = default(Point);
            
            if (y1Values != null)
            {
                nearestDataPointY1 = new Point(nearestDataPoint.X, y1Values.Item1);
                nextDataPointY1 = new Point(nextDataPoint.X, y1Values.Item2);
            }
            
            var coordsDistance = CurrentRenderPassData.IsVerticalChart
                ? Math.Abs(rawPoint.Y - nearestHitResult.HitTestPoint.Y)
                : Math.Abs(rawPoint.X - nearestHitResult.HitTestPoint.X);
            const double minInterpolationDist = 2;

            if (coordsDistance >= minInterpolationDist)
            {
                nearestDataPoint = InterpolateAtPoint(hitChartPoint, nearestDataPoint, nextDataPoint);
                if (y1Values != null)
                {
                    nearestDataPointY1 = InterpolateAtPoint(hitChartPoint, nearestDataPointY1, nextDataPointY1);
                }
                nearestHitResult.HitTestPoint = GetCoordinateForDataPoint(nearestDataPoint.X, nearestDataPoint.Y);
            }

            if (!nearestHitResult.IsHit)
            {
                nearestHitResult.IsHit = IsHitTest(rawPoint, nearestHitResult, hitTestRadius, prevDatapoint, nextDataPoint);
            }

            nearestHitResult.XValue = ComparableUtil.FromDouble(nearestDataPoint.X, DataSeries.XValues[0].GetType());
            nearestHitResult.YValue = ComparableUtil.FromDouble(nearestDataPoint.Y, DataSeries.YValues[0].GetType());
            if (y1Values != null)
            {
                nearestHitResult.Y1Value = ComparableUtil.FromDouble(nearestDataPointY1.Y, DataSeries.YValues[0].GetType());
            }

            return nearestHitResult;
        }

        protected Tuple<double, double> GetPrevAndNextYValues(int dataPointIndex, Func<int, double> getYValue)
        {
            var prevDataPointY = getYValue(dataPointIndex);

            dataPointIndex++;
            var nextDataPointY = getYValue(dataPointIndex);
            if (DrawNaNAs == LineDrawMode.ClosedLines)
            {
                // skip NaN data points
                while (Double.IsNaN(nextDataPointY) && dataPointIndex < DataSeries.Count - 1)
                {
                    dataPointIndex++;
                    nextDataPointY = getYValue(dataPointIndex);
                }
            }
            var prevAndNextYValues = new Tuple<double, double>(prevDataPointY, nextDataPointY);

            return prevAndNextYValues;
        }

        private Point InterpolateAtPoint(Point rawPoint, Point pt1, Point pt2)
        {
            var xCoord1 = pt1.X;
            var yCoord1 = pt1.Y;

            var xCoord2 = pt2.X;
            var yCoord2 = pt2.Y;

            // Now perform interpolation
            NumberUtil.SortedSwap(ref xCoord1, ref xCoord2, ref yCoord1, ref yCoord2);

            // Use this fraction to perform linear interpolation on coordinates
            double fraction = (rawPoint.X - xCoord1) / (xCoord2 - xCoord1);

            // limit fraction for case when difference of X coordinates between mouse point and data point is greater than 
            // difference of X coordinates between adjacent data points
            if (fraction > 1) fraction = 1;
            else if (fraction < 0) fraction = 0;

            // Interpolate coord using Pythagoras
            xCoord1 = xCoord1 + (xCoord2 - xCoord1) * fraction;

            // Don't interpolate if digital line
            if (!this.HasDigitalLine())
            {
                yCoord1 = yCoord1 + (yCoord2 - yCoord1) * fraction;
            }

            return new Point(xCoord1, yCoord1);
        }

        protected Point GetCoordinateForDataPoint(double xDataValue, double yDataValue)
        {
            var xCoordinateCalculator = CurrentRenderPassData.XCoordinateCalculator;
            var yCoordinateCalculator = CurrentRenderPassData.YCoordinateCalculator;

            var catCoordCalc = xCoordinateCalculator as ICategoryCoordinateCalculator;
            if (catCoordCalc != null)
            {
                xDataValue = catCoordCalc.TransformDataToIndex(xDataValue);
            }

            var xCoord = xCoordinateCalculator.GetCoordinate(xDataValue);
            var yCoord = yCoordinateCalculator.GetCoordinate(yDataValue);

            var result = TransformPoint(new Point(xCoord, yCoord), CurrentRenderPassData.IsVerticalChart);

            return result;
        }

        /// <summary>
        /// When overridden in derived classes, performs hit test on series using interpolated values
        /// </summary>
        /// <param name="rawPoint"></param>
        /// <param name="nearestHitResult"></param>
        /// <param name="hitTestRadius"></param>
        /// <param name="previousDataPoint"> </param>
        /// <param name="nextDataPoint"></param>
        /// <returns></returns>
        protected virtual bool IsHitTest(Point rawPoint, HitTestInfo nearestHitResult, double hitTestRadius, Point previousDataPoint, Point nextDataPoint)
        {
            var prevPoint = GetCoordinateForDataPoint(previousDataPoint.X, previousDataPoint.Y);
            var nextPoint = GetCoordinateForDataPoint(nextDataPoint.X, nextDataPoint.Y);

            var isHit = false;
            if (this.HasDigitalLine())
            {
                Point middlePoint;

                var prevDataPointIndex = nearestHitResult.DataSeriesIndex - 1;
                if (prevDataPointIndex >= 0 && prevDataPointIndex < DataSeries.Count)
                {
                    var prevDataPoint = new Point(((IComparable) DataSeries.XValues[prevDataPointIndex]).ToDouble(), previousDataPoint.Y);
                    var prevPointCoord = GetCoordinateForDataPoint(prevDataPoint.X, prevDataPoint.Y);

                    middlePoint = CurrentRenderPassData.IsVerticalChart
                                      ? new Point(prevPointCoord.X, prevPoint.Y)
                                      : new Point(prevPoint.X, prevPointCoord.Y);
                    isHit = PointUtil.DistanceFromLine(rawPoint, middlePoint, prevPoint) < hitTestRadius;
                }

                middlePoint = CurrentRenderPassData.IsVerticalChart
                                  ? new Point(prevPoint.X, nextPoint.Y)
                                  : new Point(nextPoint.X, prevPoint.Y);

                isHit |= PointUtil.DistanceFromLine(rawPoint, prevPoint, middlePoint) < hitTestRadius ||
                    PointUtil.DistanceFromLine(rawPoint, middlePoint, nextPoint) < hitTestRadius;
            }
            else
            {
                isHit = PointUtil.DistanceFromLine(rawPoint, prevPoint, nextPoint) < hitTestRadius;
            }

            return isHit;
        }

        protected HitTestInfo HitTestSeriesWithBody(Point rawPoint, HitTestInfo nearestHitPoint, double hitTestRadius)
        {
            // Check if the click was actually on a series body, not just near the dataPoint
            if (DataSeries != null && CurrentRenderPassData != null)
            {
                var isVerticalChart = CurrentRenderPassData.IsVerticalChart;

                var hitLowerBoundCoord = CurrentRenderPassData.YCoordinateCalculator.GetCoordinate(GetSeriesBodyLowerDataBound(nearestHitPoint));
                var hitUpperBoundCoord = CurrentRenderPassData.YCoordinateCalculator.GetCoordinate(GetSeriesBodyUpperDataBound(nearestHitPoint));

                if (hitUpperBoundCoord < hitLowerBoundCoord)
                {
                    NumberUtil.Swap(ref hitUpperBoundCoord, ref hitLowerBoundCoord);
                }

                var hitArea = StrokeThickness * 0.5 + hitTestRadius;

                hitLowerBoundCoord -= hitArea;
                hitUpperBoundCoord += hitArea;

                var halfHitBody = GetSeriesBodyWidth(nearestHitPoint) * 0.5 + hitArea;
                var bodyCenterCoord = isVerticalChart ? nearestHitPoint.HitTestPoint.Y : nearestHitPoint.HitTestPoint.X;

                var corner1 = new Point(bodyCenterCoord - halfHitBody, hitUpperBoundCoord);
                var corner2 = new Point(bodyCenterCoord + halfHitBody, hitLowerBoundCoord);

                var hitTestRect = new Rect(TransformPoint(corner1, isVerticalChart), TransformPoint(corner2, isVerticalChart));

                // Check if the point lies inside series body 
                nearestHitPoint.IsHit = IsBodyHit(rawPoint, hitTestRect, nearestHitPoint);
            }

            return nearestHitPoint;
        }

        /// <summary>
        /// When overridden in derived classes, returns the width of a single bar, which is
        /// defined by the passed coordinate
        /// </summary>
        protected virtual double GetSeriesBodyWidth(HitTestInfo nearestHitPoint) { return 0; }

        /// <summary>
        /// When overridden in derived classes, returns the lower value of a compound data point, which is
        /// defined by the passed coordinate
        /// </summary>
        protected virtual double GetSeriesBodyLowerDataBound(HitTestInfo nearestHitPoint) { return 0; }

        /// <summary>
        /// When overridden in derived classes, returns the upper value of a compound data point, which is
        /// defined by the passed coordinate
        /// </summary>
        protected virtual double GetSeriesBodyUpperDataBound(HitTestInfo nearestHitPoint) { return 0; }

        /// <summary>
        /// When overridden in derived classes, performs the hit-test check on the bounding rect of a single series bar
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="boundaries"></param>
        /// <param name="nearestHitPoint"></param>
        /// <returns></returns>
        protected virtual bool IsBodyHit(Point pt, Rect boundaries, HitTestInfo nearestHitPoint)
        {
            return boundaries.Contains(pt);
        }

        /// <summary>
        /// Returns True if the coordinate is between the lower and upper bounds supplied
        /// </summary>
        /// <param name="coord"></param>
        /// <param name="lowerBound"></param>
        /// <param name="upperBound"></param>
        /// <returns></returns>
        protected static bool CheckIsInBounds(double coord, double lowerBound, double upperBound)
        {
            if (lowerBound > upperBound) NumberUtil.Swap(ref lowerBound, ref upperBound);
            return coord >= lowerBound && coord <= upperBound;
        }
        
        internal SeriesInfo GetSeriesInfo(Point point)
        {
            HitTestInfo hitTestInfo = HitTest(point);

            return GetSeriesInfo(hitTestInfo);
        }

        /// <summary>
        /// Converts the result of a Hit-Test operation (<see cref="HitTestInfo"/>) to a <see cref="SeriesInfo"/> class, which may be used as a
        /// ViewModel when outputting series values as bindings. <see cref="SeriesInfo"/> is used by the <see cref="Ecng.Xaml.Charting.ChartModifiers.RolloverModifier"/>, <see cref="Ecng.Xaml.Charting.ChartModifiers.CursorModifier"/>
        /// and <see cref="UltrachartLegend"/> classes
        /// </summary>
        /// <param name="hitTestInfo"></param>
        /// <returns></returns>
        public virtual SeriesInfo GetSeriesInfo(HitTestInfo hitTestInfo)
        {
            return RenderableSeriesExtension.GetSeriesInfo(this, hitTestInfo);
        }

        /// <summary>
        /// Returns the data range of the assosiated <see cref="IDataSeries"/> on X direction
        /// </summary>
        public virtual IRange GetXRange()
        {
            return DataSeries.XRange;
        }

        /// <summary>
        /// Returns the data range of the assosiated <see cref="IDataSeries"/> on Y direction
        /// <param name="xRange">The X-Axis Range currently in view</param>
        /// </summary>
        public IRange GetYRange(IRange xRange)
        {
            return GetYRange(xRange, false);
        }

        /// <summary>
        /// Returns the data range of the assosiated <see cref="IDataSeries"/> on Y direction
        /// <param name="xRange">The X-Axis Range currently in view</param>
        /// <param name="getPositiveRange">Indicates whether to return positive YRange only</param>
        /// </summary>
        public virtual IRange GetYRange(IRange xRange, bool getPositiveRange)
        {
            return DataSeries.GetWindowedYRange(xRange, getPositiveRange);
        }

        public virtual IndexRange GetExtendedXRange(IndexRange range) {
            return range;
        }

        // TODO: (Nazar) Dodgy code ... adds a layer of coupling from RenderableSeries to modifier 
        public bool GetIncludeSeries(Modifier modifier)
        {
            bool includeSeries = true;

            switch (modifier)
            {
                case Modifier.Rollover:
                    includeSeries = RolloverModifier.GetIncludeSeries(this);
                    break;
                case Modifier.Cursor:
                    includeSeries = CursorModifier.GetIncludeSeries(this);
                    break;
                case Modifier.Tooltip:
                    includeSeries = TooltipModifier.GetIncludeSeries(this);
                    break;
                case Modifier.VerticalSlice:
                    includeSeries = VerticalSliceModifier.GetIncludeSeries(this);
                    break;
            }

            return includeSeries;
        }

        /// <summary>
        /// Called when resampling mode changes
        /// </summary>
        protected virtual void OnResamplingModeChanged()
        {
            OnInvalidateParentSurface();
        }

        /// <summary>
        /// Called when the parent surface viewport size changes, immediately before a draw pass
        /// </summary>
        protected virtual void OnParentSurfaceViewportSizeChanged()
        {
        }

        private void OnSelectionChanged(EventArgs args)
        {
            var handler = SelectionChanged;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        private void OnVisibilityChanged(EventArgs args)
        {
            var handler = IsVisibleChanged;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Used Internally: Gets the rotation angle of the chart, which is 0 degrees or 90 degrees depending on whether the parent <see cref="UltrachartSurface"/>
        /// has swapped X and Y Axes or not. 
        /// </summary>
        /// <param name="renderPassData">The current <see cref="IRenderPassData"/></param>
        /// <returns></returns>
        protected double GetChartRotationAngle(IRenderPassData renderPassData)
        {
            var isVerticalChart = renderPassData.IsVerticalChart;
            var isYCoordsFlipped = renderPassData.YCoordinateCalculator.HasFlippedCoordinates;

            // If vertical chart, assume it's rotated on 90
            double angle = isVerticalChart ? Math.PI / 2 : 0;

            // If Y coords are flipped, increase the angle per 180
            angle += isYCoordsFlipped ? Math.PI : 0;

            return angle;
        }

        /// <summary>
        /// Transposes a point (swaps X, Y) if the <paramref name="isVerticalChart"/> flag is true
        /// </summary>
        /// <param name="x">The X-Value</param>
        /// <param name="y">The Y-Value</param>
        /// <param name="isVerticalChart">A flag indicating the orientation of the chart</param>
        /// <returns>The transposed point</returns>
        protected Point TransformPoint(float x, float y, bool isVerticalChart)
        {
            return DrawingHelper.TransformPoint(x, y, isVerticalChart);
        }

        /// <summary>
        /// Used internally: Transposes a <see cref="Point"/> depending on whether the current <see cref="UltrachartSurface"/> is being drawn vertically or not
        /// </summary>
        /// <param name="point"></param>
        /// <param name="isVerticalChart"></param>
        /// <returns></returns>
        protected Point TransformPoint(Point point, bool isVerticalChart)
        {
            return DrawingHelper.TransformPoint(point, isVerticalChart);
        }


        /// <summary>
        /// When called, invalidates the parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <param name="d">The DependencyObject that raised the event.</param>
        /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        protected static void OnInvalidateParentSurface(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var series = d as BaseRenderableSeries;
            if (series != null)
            {
                series.OnInvalidateParentSurface();
            }
        }

        /// <summary>
        /// Called when the <see cref="BaseRenderableSeries.DataSeries"/> property changes - i.e. a new <see cref="IDataSeries"/> has been set
        /// </summary>
        /// <param name="oldDataSeries">The old <see cref="IDataSeries"/></param>
        /// <param name="newDataSeries">The new <see cref="IDataSeries"/></param>
        protected virtual void OnDataSeriesDependencyPropertyChanged(IDataSeries oldDataSeries, IDataSeries newDataSeries)
        {
            var parentSurface = GetParentSurface();
            if (parentSurface != null)
            {
                parentSurface.DetachDataSeries(oldDataSeries);
                parentSurface.AttachDataSeries(newDataSeries);
            }

            _dataSeries = newDataSeries;

            if (IsVisible)
            {
                OnDataSeriesDependencyPropertyChanged();
            }
            OnInvalidateParentSurface();
        }

        /// <summary>
        /// Gets the parent <see cref="IUltrachartSurface"/> for this <see cref="BaseRenderableSeries"/>
        /// </summary>
        /// <returns></returns>
        protected internal IUltrachartSurface GetParentSurface()
        {
           return Services != null ? Services.GetService<IUltrachartSurface>() : null;
        }

        /// <summary>
        /// Creates a RolloverMarker from the RolloverMarkerTemplate property
        /// </summary>
        protected virtual void CreateRolloverMarker()
        {
            _rolloverMarkerCache = RenderableSeries.PointMarker.CreateFromTemplate(RolloverMarkerTemplate, this);
        }

        /// <summary>
        /// Returns an XmlSchema that describes the XML representation of the object that is produced by the WriteXml method and consumed by the ReadXml method
        /// </summary>
        /// <remarks>
        /// This method is reserved by <see cref="System.Xml.Serialization.IXmlSerializable"/> and should not be used
        /// </remarks>
        /// <returns></returns>
        public XmlSchema GetSchema()
        {
            return null;
        }

        /// <summary>
        /// Generates <see cref="BaseRenderableSeries"/> from its XML representation.
        /// </summary>
        /// <param name="reader"></param>
        public virtual void ReadXml(XmlReader reader)
        {
            if (reader.MoveToContent() == XmlNodeType.Element)
            {
                RenderableSeriesSerializationHelper.Instance.DeserializeProperties(this, reader);
            }
        }

        /// <summary>
        /// Converts <see cref="BaseRenderableSeries"/> into its XML representation.
        /// </summary>
        /// <param name="writer"></param>
        public virtual void WriteXml(XmlWriter writer)
        {
            RenderableSeriesSerializationHelper.Instance.SerializeProperties(this, writer);
        }

        private static void OnStrokeThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Guard.Assert((int)e.NewValue, "StrokeThickness").IsGreaterThanOrEqualTo(0);
            OnInvalidateParentSurface(d, e);
        }

        private static void OnSeriesColorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var series = d as BaseRenderableSeries;
            if (series != null)
            {
                series.OnSeriesColorChanged();
                series.OnInvalidateParentSurface();
            }
        }

        private static void OnSelectedSeriesStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var series = d as BaseRenderableSeries;
            if (series != null && series.IsSelected && series.SelectedSeriesStyle != null)
            {
                ApplyStyle(series);
            }
        }

        private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var series = d as BaseRenderableSeries;
            var isSelected = (bool)e.NewValue;
            if (series != null && (bool)e.OldValue != isSelected)
            {
                ApplyStyle(series);

                //Raise SelectionChanged event
                series.OnSelectionChanged(EventArgs.Empty);
            }
        }

        private static void OnIsVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var series = d as BaseRenderableSeries;
            var isVisible = (bool) e.NewValue;
            if (series != null && (bool) e.OldValue != isVisible)
            {
                series.OnVisibilityChanged(EventArgs.Empty);
                OnInvalidateParentSurface(d, e);
            }
        }

        private static void ApplyStyle(BaseRenderableSeries series)
        {
            series.SetStyle(series.IsSelected ? series.SelectedSeriesStyle : (Style)series.GetValue(RenderableSeriesExtension.SeriesStyleProperty));
            series.OnSeriesColorChanged();

            series.OnInvalidateParentSurface();
        }

        private static void OnResamplingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var series = d as BaseRenderableSeries;
            if (series != null)
            {
                series.OnResamplingModeChanged();
            }
        }

        private static void OnRolloverMarkerPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var series = d as BaseRenderableSeries;
            if (series != null)
            {
                series.CreateRolloverMarker();

                series.OnInvalidateParentSurface();
            }
        }

        private static void OnPointMarkerTemplatePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var series = (BaseRenderableSeries)d;

            series._pointMarkerFromTemplate = series.CreatePointMarker((ControlTemplate) e.NewValue, series);

            series.OnInvalidateParentSurface();
        }

        private IPointMarker CreatePointMarker(ControlTemplate template, object dataContext)
        {
            IPointMarker result = null;

            if (template != null)
            {
                var pointMarker = RenderableSeries.PointMarker.CreateFromTemplate(template, dataContext);

                result = pointMarker.FindVisualChild<BasePointMarker>() ??
                       new SpritePointMarker {PointMarkerTemplate = template, DataContext = dataContext};
            }

            return result;
        }

        // BaseRenderableSeries.cs
        private static void OnPointMarkerPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var series = (BaseRenderableSeries)d;

            var pm = e.NewValue as BasePointMarker;

            if (pm != null)
            {
                pm.DataContext = series;

                // Fixes SC-2511 Realtime Cursors crash -- WindowsXP (user reported)
                var oldParent = pm.Parent as BaseRenderableSeries;
                if (oldParent != null)
                {
                    oldParent.Content = null;
                }

                series.Content = pm;
            }

            series.OnInvalidateParentSurface();
        }

        [Obfuscation(Feature = "encryptmethod", Exclude = false)]
        private void Initialize()
        {
#if LICENSEDDEPLOY
            new LicenseManager().Validate(this, new UltrachartLicenseProviderFactory());
#endif
        }

        
        internal double HitTestRadius
        {
            get { return DefaultHitTestRadius; }
        }

    }
}