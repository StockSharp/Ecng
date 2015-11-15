// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// AxisProxy.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using System.Xml.Schema;
using Ecng.Xaml.Charting.ChartModifiers;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Numerics;
using Ecng.Xaml.Charting.Numerics.CoordinateCalculators;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Themes;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.Annotations;
using Ecng.Xaml.Charting.Visuals.Axes;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting
{
    internal class RenderSeriesProxy : IRenderableSeries
    {
        private readonly IRenderableSeries _renderableSeries;

        public RenderSeriesProxy(IRenderableSeries renderableSeries)
        {
            _renderableSeries = renderableSeries;
            IsVisible = renderableSeries.IsVisible;
            ResamplingMode = renderableSeries.ResamplingMode;
            DataSeries = renderableSeries.DataSeries;
            XAxisId = renderableSeries.XAxisId;
            YAxisId = renderableSeries.YAxisId;
            DisplaysDataAsXy = renderableSeries.DisplaysDataAsXy;
        }

        public double Width { get; set; }
        public double Height { get; set; }
        public void OnDraw(IRenderContext2D renderContext, IRenderPassData renderPassData)
        {          
            _renderableSeries.OnDraw(renderContext, renderPassData);
        }

        public event EventHandler SelectionChanged;
        public event EventHandler IsVisibleChanged;
        public IServiceContainer Services { get; set; }
        public bool IsVisible { get; set; }
        public bool AntiAliasing { get; set; }
        public ResamplingMode ResamplingMode { get; set; }
        public IDataSeries DataSeries { get; set; }
        public IDataSeries DataSeriesForCore { get; set; }
        public IAxis XAxis { get; set; }
        public IAxis YAxis { get; set; }
        public Color SeriesColor { get; set; }
        public Style SelectedSeriesStyle { get; set; }
        public Style Style { get; set; }
        public object DataContext { get; set; }
        public bool IsSelected { get; set; }
        public FrameworkElement RolloverMarker { get; private set; }
        public string YAxisId { get; set; }
        public string XAxisId { get; set; }
        public IRenderPassData CurrentRenderPassData { get; set; }
        public IPaletteProvider PaletteProvider { get; set; }
        public int StrokeThickness { get; set; }
        public HitTestInfo HitTest(Point rawPoint, bool interpolate, double? dataPointRadius)
        {
            throw new NotImplementedException();
        }

        public bool DisplaysDataAsXy { get; private set; }

        public HitTestInfo HitTest(Point rawPoint, bool interpolate = false)
        {
            throw new NotImplementedException();
        }

        public HitTestInfo HitTest(Point rawPoint, double hitTestRadius, bool interpolate = false)
        {
            throw new NotImplementedException();
        }

        public HitTestInfo VerticalSliceHitTest(Point rawPoint, bool interpolate = false)
        {
            throw new NotImplementedException();
        }

        public SeriesInfo GetSeriesInfo(HitTestInfo hitTestInfo)
        {
            throw new NotImplementedException();
        }

        public IRange GetXRange()
        {
            throw new NotImplementedException();
        }

        public IRange GetYRange(IRange xRange)
        {
            throw new NotImplementedException();
        }

        public IRange GetYRange(IRange xRange, bool getPositiveRange)
        {
            throw new NotImplementedException();
        }

        public virtual IndexRange GetExtendedXRange(IndexRange range)
        {
            throw new NotImplementedException();
        }

        public bool GetIncludeSeries(Modifier modifier)
        {
            throw new NotImplementedException();
        }

        public XmlSchema GetSchema()
        {
            throw new NotImplementedException();
        }

        public void ReadXml(XmlReader reader)
        {
            throw new NotImplementedException();
        }

        public void WriteXml(XmlWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    // NOTE: Currently not used, was needed for prototype of parallel resampling
    // 

    /// <summary>
    /// Wraps an Axis, exposing certain properties for use in multi-threaded rendering routines 
    /// 
    /// (e.g. DependencyProperties cannot be accessed on background threads)
    /// </summary>
    internal class AxisProxy : IAxis
    {
        private IAxis _axis;

        public AxisProxy(IAxis axis)
        {
            VisibleRange = axis.VisibleRange;
            IsCategoryAxis = axis.IsCategoryAxis;
            Id = axis.Id;
            _axis = axis;
        }

        public double ActualWidth { get; private set; }
        public double ActualHeight { get; private set; }
        public Point TranslatePoint(Point point, IHitTestable relativeTo)
        {
            throw new NotImplementedException();
        }

        public bool IsPointWithinBounds(Point point)
        {
            throw new NotImplementedException();
        }

        public Rect GetBoundsRelativeTo(IHitTestable relativeTo)
        {
            throw new NotImplementedException();
        }

        public bool IsSuspended { get; private set; }
        public IUpdateSuspender SuspendUpdates()
        {
            throw new NotImplementedException();
        }

        public void ResumeUpdates(IUpdateSuspender suspender)
        {
            throw new NotImplementedException();
        }

        public void DecrementSuspend()
        {
            throw new NotImplementedException();
        }

        public void InvalidateElement()
        {
            throw new NotImplementedException();
        }

        public event EventHandler<VisibleRangeChangedEventArgs> VisibleRangeChanged;
        public event EventHandler<EventArgs> DataRangeChanged;

        public string Id { get; set; }
        public bool AutoTicks { get; set; }
        public ITickProvider TickProvider { get; set; }
        public IRange AnimatedVisibleRange { get; set; }
        public IRange VisibleRange { get; set; }
        public IRange DataRange { get; private set; }
        public double Width { get; private set; }
        double IDrawable.Height
        {
            get { return Height; }
            set { Height = value; }
        }

        public void OnDraw(IRenderContext2D renderContext, IRenderPassData renderPassData)
        {
            throw new NotImplementedException();
        }

        double IDrawable.Width
        {
            get { return Width; }
            set { Width = value; }
        }

        public double Height { get; private set; }
        public IRange<double> GrowBy { get; set; }
        public IComparable MinorDelta { get; set; }
        public IComparable MajorDelta { get; set; }
        public IServiceContainer Services { get; set; }
        public IUltrachartSurface ParentSurface { get; set; }
        public Orientation Orientation { get; set; }
        public Brush MajorLineStroke { get; set; }
        public Brush MinorLineStroke { get; set; }
        public Style MajorTickLineStyle { get; set; }
        public Style MinorTickLineStyle { get; set; }
        public Style MajorGridLineStyle { get; set; }
        public Style MinorGridLineStyle { get; set; }
        public AutoRange AutoRange { get; set; }
        public bool AutoRangeOnce { get; set; }
        public string TextFormatting { get; set; }
        public string CursorTextFormatting { get; set; }
        public ILabelProvider LabelProvider { get; set; }
        public bool IsXAxis { get; set; }
        public bool IsHorizontalAxis { get; private set; }
        public bool IsStaticAxis { get; set; }
        public bool FlipCoordinates { get; set; }
        public bool HasValidVisibleRange { get; private set; }
        public bool HasDefaultVisibleRange { get; private set; }
        public string AxisTitle { get; set; }
        public Brush TickTextBrush { get; set; }
        public bool AutoAlignVisibleRange { get; set; }
        public bool DrawMinorTicks { get; set; }
        public bool DrawMajorTicks { get; set; }
        public bool DrawMajorGridLines { get; set; }
        public bool DrawMinorGridLines { get; set; }
        public HorizontalAlignment HorizontalAlignment { get; set; }
        public VerticalAlignment VerticalAlignment { get; set; }
        public AxisMode AxisMode { get; set; }
        public AxisAlignment AxisAlignment { get; set; }
        public bool IsCategoryAxis { get; private set; }
        public bool IsLogarithmicAxis { get; private set; }
        public bool IsPolarAxis { get; private set; }
        public bool IsCenterAxis { get; set; }
        public bool IsPrimaryAxis { get; set; }
        public IAnnotationCanvas ModifierAxisCanvas { get; private set; }
        public Visibility Visibility { get; set; }
        public bool IsAxisFlipped { get; private set; }
        public IRange VisibleRangeLimit { get; set; }
        public RangeClipMode VisibleRangeLimitMode { get; set; }
        public IComparable MinimalZoomConstrain { get; set; }
        public bool IsLabelCullingEnabled { get; set; }

        public ICoordinateCalculator<double> GetCurrentCoordinateCalculator()
        {
            throw new NotImplementedException();
        }

        public IAxisInteractivityHelper GetCurrentInteractivityHelper()
        {
            throw new NotImplementedException();
        }

        public bool CaptureMouse()
        {
            throw new NotImplementedException();
        }

        public void ReleaseMouseCapture()
        {
            throw new NotImplementedException();
        }

        public void SetMouseCursor(Cursor cursor)
        {
            throw new NotImplementedException();
        }

        public AxisInfo HitTest(Point atPoint)
        {
            throw new NotImplementedException();
        }

        public IntegerRange GetPointRange()
        {
            throw new NotImplementedException();
        }

        public IRange CalculateYRange(RenderPassInfo renderPassInfo)
        {
            throw new NotImplementedException();
        }

        public IRange GetMaximumRange()
        {
            throw new NotImplementedException();
        }

        public IRange GetWindowedYRange(IDictionary<string, IRange> xRanges)
        {
            throw new NotImplementedException();
        }

        public void ScrollXRange(int deltaX)
        {
            throw new NotImplementedException();
        }

        public double GetCoordinate(IComparable value)
        {
            throw new NotImplementedException();
        }

        public double GetAxisOffset()
        {
            throw new NotImplementedException();
        }

        public IComparable GetDataValue(double pixelCoordinate)
        {
            throw new NotImplementedException();
        }

        public Size OnArrangeAxis()
        {
            throw new NotImplementedException();
        }

        public void OnBeginRenderPass(RenderPassInfo renderPassInfo = default(RenderPassInfo), IPointSeries firstPointSeries = null)
        {
            throw new NotImplementedException();
        }

        public void Scroll(double pixelsToScroll, ClipMode clipMode)
        {
            throw new NotImplementedException();
        }

        public void Scroll(double pixelsToScroll, ClipMode clipMode, TimeSpan duration)
        {
            throw new NotImplementedException();
        }

        public void ScrollByDataPoints(int pointAmount)
        {
            throw new NotImplementedException();
        }

        public void ScrollByDataPoints(int pointAmount, TimeSpan duration)
        {
            throw new NotImplementedException();
        }

        public void Zoom(double fromCoord, double toCoord)
        {
            throw new NotImplementedException();
        }

        public void Zoom(double fromCoord, double toCoord, TimeSpan duration)
        {
            throw new NotImplementedException();
        }

        public void ZoomBy(double minFraction, double maxFraction)
        {
            throw new NotImplementedException();
        }

        public void ZoomBy(double minFraction, double maxFraction, TimeSpan duration)
        {
            throw new NotImplementedException();
        }

        public void ScrollTo(IRange startVisibleRange, double pixelsToScroll)
        {
            throw new NotImplementedException();
        }

        public void ScrollToWithLimit(IRange startVisibleRange, double pixelsToScroll, IRange rangeLimit)
        {
            throw new NotImplementedException();
        }

        public void AssertDataType(Type dataType)
        {
            _axis.AssertDataType(dataType);
        }

        public string FormatText(IComparable value, string format)
        {
            throw new NotImplementedException();
        }

        public string FormatText(IComparable value)
        {
            throw new NotImplementedException();
        }

        public string FormatCursorText(IComparable value)
        {
            throw new NotImplementedException();
        }

        public bool IsValidRange(IRange range)
        {
            throw new NotImplementedException();
        }

        public IAxis Clone()
        {
            throw new NotImplementedException();
        }

        public void AnimateVisibleRangeTo(IRange range, TimeSpan duration)
        {
            throw new NotImplementedException();
        }

        public void ValidateAxis()
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public IRange GetUndefinedRange()
        {
            throw new NotImplementedException();
        }

        public IRange GetDefaultNonZeroRange()
        {
            throw new NotImplementedException();
        }

        public double CurrentDatapointPixelSize {get {return double.NaN;}}

        public XmlSchema GetSchema()
        {
            throw new NotImplementedException();
        }
    
        public double ZoomScale { get; set; }
        
        public double ZoomScaleLog { get; set; }
    }
}