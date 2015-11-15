// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IAxis.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Ecng.Xaml.Charting.ChartModifiers;
using Ecng.Xaml.Charting.Common.Databinding;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Numerics;
using Ecng.Xaml.Charting.Numerics.CoordinateCalculators;
using Ecng.Xaml.Charting.Themes;
using Ecng.Xaml.Charting.Visuals.Annotations;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.Visuals.Axes
{
    /// <summary>
    /// Defines the base interface to an Axis used throughout Ultrachart
    /// </summary>
    public interface IAxis : IAxisParams, IHitTestable, ISuspendable, IInvalidatableElement, IDrawable
    {
        /// <summary>
        /// Raised when the VisibleRange is changed
        /// </summary>
        event EventHandler<VisibleRangeChangedEventArgs> VisibleRangeChanged;

        /// <summary>
        /// Raised when data range is changed
        /// </summary>
        event EventHandler<EventArgs> DataRangeChanged;
        

        /// <summary>
        /// Gets or sets the string Id of this axis. Used to associated <see cref="IRenderableSeries"/> and <see cref="YAxisDragModifier"/>
        /// </summary>
        string Id { get; set; }

        /// <summary>
        /// Gets or sets value, that indicates whether calculate ticks automatically
        /// </summary>
        bool AutoTicks { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="ITickProvider"/> instance on current axis,
        /// which is used to compute the data-values of Axis Gridlines, Ticks and Labels.
        /// </summary>
        ITickProvider TickProvider { get; set; }

        /// <summary>
        /// Gets or sets the animated VisibleRange of the Axis. When this property is set, the axis animates the VisibleRange to the new value
        /// </summary>
        /// <value>The visible range.</value>
        /// <remarks></remarks>
        [TypeConverter(typeof(StringToDoubleRangeTypeConverter))]
        IRange AnimatedVisibleRange { get; set; }

        /// <summary>
        /// Gets the DataRange (full extents of the data) of the Axis
        /// </summary>
        IRange DataRange { get; }

        /// <summary>
        /// Gets the <see cref="ServiceContainer"/> which provides access to services throughout Ultrachart. 
        /// ServiceContainers are created one per <see cref="UltrachartSurface"/> instance, 
        /// and shared between peripheral components such as <see cref="AxisBase"/>, <see cref="BaseRenderableSeries"/>, <see cref="ChartModifierBase"/> instances.
        /// For a full list of available services, see the remarks on <see cref="ServiceContainer"/>
        /// </summary>
        IServiceContainer Services { get; set; }

        /// <summary>
        /// Gets or sets the ParentSurface that this Axis is associated with
        /// </summary>
        IUltrachartSurface ParentSurface { get; set; }        

        /// <summary>
        /// Gets or sets the Axis Orientation, e.g. Horizontal (XAxis) or Vertical (YAxis)
        /// </summary>
        Orientation Orientation { get; set; }

        /// <summary>
        /// Gets or sets the Major Line Stroke for this axis
        /// </summary>
        [Obsolete("MajorLineStroke is obsolete, please use MajorTickLineStyle instead", true)]
        Brush MajorLineStroke { get; set; }

        /// <summary>
        /// Gets or sets the Minoe Line Stroke for this axis
        /// </summary>
        [Obsolete("MinorLineStroke is obsolete, please use MajorTickLineStyle instead", true)]
        Brush MinorLineStroke { get; set; }

        /// <summary>
        /// Gets or sets the Major Tick Line Style (TargetType <see cref="Line"/>), applied to all major ticks on this axis
        /// </summary>
        /// <remarks>
        /// The depth of the tick is defined by the <see cref="Line.Y2"/> and <see cref="Line.X2"/> properties. For instance, setting
        /// Y2 and X2 to 6 will result in Major ticks being 6 pixels in size, whether on the X or Y axis
        /// </remarks>
        Style MajorTickLineStyle { get; set; }

        /// <summary>
        /// Gets or sets the Minor Tick Line Style (TargetType <see cref="Line"/>), applied to all major ticks on this axis
        /// </summary>
        /// <remarks>
        /// The depth of the tick is defined by the <see cref="Line.Y2"/> and <see cref="Line.X2"/> properties. For instance, setting
        /// Y2 and X2 to 3 will result in Minor ticks being 6 pixels in size, whether on the X or Y axis
        /// </remarks>
        Style MinorTickLineStyle { get; set; }

        /// <summary>
        /// Gets or sets the Major Grid Line Style (TargetType <see cref="Line"/>), applied to all major gridlines drawn by this axis
        /// </summary>        
        Style MajorGridLineStyle { get; set; }

        /// <summary>
        /// Gets or sets the Minor Grid Line Style (TargetType <see cref="Line"/>), applied to all minor gridlines drawn by this axis
        /// </summary>        
        Style MinorGridLineStyle { get; set; }

        /// <summary>
        /// Gets or sets whether this current axis <see cref="AutoRange"/>. Default is AutoRange.Once
        /// </summary>
        /// <value>If AutoRange.Always, the axis should scale to fit the data, else AutoRange.Once, the axis will try to fit the data once. 
        /// If AutoRange.Never, then the axis will never autorange.</value>
        /// <remarks>GrowBy is applied when the axis scales to fit</remarks>
        AutoRange AutoRange { get; set; }

        /// <summary>
        /// Gets or sets the Text Formatting String for Axis Tick Labels on this axis
        /// </summary>
        string TextFormatting { get; set; }

        /// <summary>
        /// Gets or sets the Text Formatting String for Labels on this cursor
        /// </summary>
        string CursorTextFormatting { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="ILabelProvider"/> instance, which may be used to programmatically override the formatting of text and cursor labels. 
        /// For examples, see the <see cref="NumericLabelProvider"/> and <see cref="TradeChartAxisLabelProvider"/>
        /// </summary>
        ILabelProvider LabelProvider { get; set; }

        /// <summary>
        /// Gets whether this axis is an X-Axis or not
        /// </summary>
        bool IsXAxis { get; set; }

        /// <summary>
        /// Gets whether this axis is horizontal or not
        /// </summary>
        bool IsHorizontalAxis { get; }

        /// <summary>
        /// Gets or sets whether current Axis is a static axis
        /// </summary>
        bool IsStaticAxis { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating whether to flip the tick and pixel coordinate generation for this axis,
        /// causing the axis ticks to decrement and chart to be flipped in the axis direction
        /// </summary>
        /// <value>
        /// If <c>true</c> reverses the ticks and coordinates for the axis.
        /// </value>
        bool FlipCoordinates { get; set; }

        /// <summary>
        /// Gets whether the VisibleRange is valid, e.g. is not null, is not NaN and the difference between Max and Min is not zero
        /// </summary>
        bool HasValidVisibleRange { get; }

        /// <summary>
        /// Gets whether the VisibleRange has default value
        /// </summary>
        bool HasDefaultVisibleRange { get; }

        /// <summary>
        /// Gets or sets the Axis Title
        /// </summary>
        string AxisTitle { get; set; }

        /// <summary>
        /// Gets or sets the tick text brush applied to text labels
        /// </summary>
        /// <value>The tick text brush</value>
        /// <remarks></remarks>
        Brush TickTextBrush { get; set; }

        /// <summary>
        /// Gets or sets whether to auto-align the visible range to the data when it is set. Note that this property only applies to the X-Axis. 
        /// The default value is True. Whenever the <see cref="IAxisParams.VisibleRange"/> is set on the X-Axis, the Min and Max values will be aligned to data values in the <see cref="IDataSeries.XValues"/>
        /// </summary>
        bool AutoAlignVisibleRange { get; set; }

        /// <summary>
        /// If True, draws Minor Tick Lines, else skips this step
        /// </summary>
        bool DrawMinorTicks { get; set; }

        /// <summary>
        /// If True, draws Major Tick Lines, else skips this step
        /// </summary>
        bool DrawMajorTicks { get; set; }

        /// <summary>
        /// If True, draws Major Grid Lines, else skips this step
        /// </summary>
        bool DrawMajorGridLines { get; set; }

        /// <summary>
        /// If True, draws Minor Grid Lines, else skips this step
        /// </summary>
        bool DrawMinorGridLines { get; set; }

        /// <summary>
        /// Gets or sets the horizontal alignment characteristics that are applied to a <see cref="T:System.Windows.FrameworkElement"/> when it is composed in a layout parent, such as a panel or items control.
        /// </summary>
        /// <returns>
        /// A horizontal alignment setting, as a value of the enumeration. The default is <see cref="F:System.Windows.HorizontalAlignment.Stretch"/>.
        /// </returns>
        HorizontalAlignment HorizontalAlignment { get; set; }

        /// <summary>
        /// Gets or sets the vertical alignment characteristics that are applied to a <see cref="T:System.Windows.FrameworkElement"/> when it is composed in a parent object such as a panel or items control.
        /// </summary>
        /// <returns>
        /// A vertical alignment setting. The default is <see cref="F:System.Windows.VerticalAlignment.Stretch"/>.
        /// </returns>
        VerticalAlignment VerticalAlignment { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="AxisMode"/>, e.g. Linear or Logarithmic, that this Axis operates in
        /// </summary>
        [Obsolete("IAxis.AxisMode is obsolete, please use NumericAxis or LogarithmicNumericAxis instead")]
        AxisMode AxisMode { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="AxisAlignment"/> for this Axis. Default is Right.
        /// </summary>
        AxisAlignment AxisAlignment { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is a category axis.
        /// </summary>
        bool IsCategoryAxis { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is a logarithmic axis.
        /// </summary>
        bool IsLogarithmicAxis { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is a polar axis.
        /// </summary>
        bool IsPolarAxis { get; }

        /// <summary>
        /// Gets or sets whether current Axis should placed in the center of chart or not
        /// </summary>
        bool IsCenterAxis { get; set; }

        /// <summary>
        /// Gets or sets whether current Axis is the main one in axis collection. This is the axis which is responsible for drawing grid lines on the <see cref="GridLinesPanel"/> and by default, 
        /// is the first axis in the collection
        /// </summary>
        /// <remarks>Primary axis determines grid coordinates</remarks>
        bool IsPrimaryAxis { get; set; }

        /// <summary>
        /// Gets the modifier axis canvas, which is used by the CursorModifier to overlay cursor labels and by AxisMarkerAnnotations
        /// </summary>        
        IAnnotationCanvas ModifierAxisCanvas { get; }

        /// <summary>
        /// Gets or sets the visibility of the Axis
        /// </summary>
        Visibility Visibility { get; set; }

        /// <summary>
        /// Gets whether the current axis is flipped (e.g. YAxis on the bottom or top, or XAxis on the left or right)
        /// </summary>
        bool IsAxisFlipped { get; }

        /// <summary>
        /// Gets or sets the VisibleRangeLimit of the Axis. This will be used to clip the axis during ZoomExtents and AutoRange operations
        /// </summary>
        /// <value>The visible range.</value>
        /// <remarks></remarks>
        [TypeConverter(typeof (StringToDoubleRangeTypeConverter))]
        IRange VisibleRangeLimit { get; set; }

        /// <summary>
        /// Gets or setts the VisibleRangeLimitMode of the Axis. This property defines which parts of <see cref="VisibleRangeLimit"/> will be used by axis
        /// </summary>
        RangeClipMode VisibleRangeLimitMode { get; set; }

        /// <summary>
        /// Gets or sets the MinimalZoomConstrain of the Axis. This is used to set minimum distance between Min and Max of the VisibleRange 
        /// </summary>
        /// <value>The minimum distance between Min and Max of the VisibleRange</value>
        IComparable MinimalZoomConstrain { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Label Culling is enabled (when labels overlap) on this AxisPanel instance
        /// </summary>
        bool IsLabelCullingEnabled { get; set; }

        /// <summary>
        /// Gets the current <see cref="ICoordinateCalculator{T}"/> for this Axis, based on current Visible Range and axis type
        /// </summary>
        /// <returns></returns>
        ICoordinateCalculator<double> GetCurrentCoordinateCalculator();

        /// <summary>
        /// Gets the current <see cref="IAxisInteractivityHelper"/> for this Axis
        /// </summary>
        /// <returns></returns>
        IAxisInteractivityHelper GetCurrentInteractivityHelper();

        /// <summary>
        /// Captures the mouse for this Axis
        /// </summary>
        /// <returns></returns>
        bool CaptureMouse();

        /// <summary>
        /// Releases the mouse for this Axis
        /// </summary>
        void ReleaseMouseCapture();

        /// <summary>
        /// Sets the cursor for this Axis
        /// </summary>
        /// <param name="cursor">The Cursor instance</param>
        void SetMouseCursor(Cursor cursor);

        /// <summary>
        /// Performs a HitTest on this axis. Given the input mouse point, returns an AxisInfo struct containing the Value and FormattedValue closest to that point
        /// </summary>
        /// <param name="atPoint">The mouse x,y point</param>
        /// <returns>The AxisInfo struct containing the value and formatted value closest to the mouse point</returns>
        AxisInfo HitTest(Point atPoint);

        /// <summary>
        /// Gets the integer indices of the X-Data array that are currently in range. 
        /// </summary>
        /// <example>If the input X-data is 0...100 in steps of 1, the VisibleRange is 10, 30 then the PointRange will be 10, 30</example>
        /// <returns>The indices to the X-Data that are currently in range</returns>
        [Obsolete("IAxis.GetPointRange is obsolete, please call IDataSeries.GetIndicesRange(VisibleRange) instead", true)]
        IntegerRange GetPointRange();

        /// <summary>
        /// Gets the aligned VisibleRange of the axis, with optional ZoomToFit flag. 
        /// If ZoomToFit is true, it will return the DataRange plus any GrowBy applied to the axis
        /// </summary>
        /// <returns>The VisibleRange of the axis</returns>
        IRange CalculateYRange(RenderPassInfo renderPassInfo);

        /// <summary>
        ///  Called by the UltrachartSurface internally. Returns the max range only for that axis (by the data-series on it), based on <paramref name="xRanges"/>
        /// "windowed" = "displayed in current viewport"
        /// uses GrowBy()
        /// </summary>
        /// <param name="xRanges">Calculates the max range based on corresponding x ranges</param>
        /// <returns></returns>
        IRange GetWindowedYRange(IDictionary<string, IRange> xRanges);

        /// <summary>
        /// Given the Data Value, returns the x or y pixel coordinate at that value on the Axis
        /// </summary>
        /// <example>
        /// Given an axis with a VisibleRange of 1..10 and height of 100, a value of 7 passed in to GetCoordinate would return 70 pixels
        /// </example>
        /// <param name="value">The DataValue as input</param>
        /// <returns>The pixel coordinate on this Axis corresponding to the input DataValue</returns>
        double GetCoordinate(IComparable value);

        /// <summary>
        /// Given the x or y pixel coordinate, returns the data value at that coordinate
        /// </summary>
        /// <param name="pixelCoordinate">The x or y pixel coordinate as input</param>
        /// <returns>The data value on this Axis corresponding to the input x or y pixel coordinate</returns>
        IComparable GetDataValue(double pixelCoordinate);

        /// <summary>
        /// Returns the offset of the Axis
        /// </summary>
        /// <returns></returns>
        double GetAxisOffset();

        /// <summary>
        /// Called at the start of a render pass, passing in the root <see cref="IPointSeries"/> which will define the categories
        /// </summary>
        /// <param name="firstPointSeries">the root <see cref="IPointSeries"/> which will define the categories</param>
        void OnBeginRenderPass(RenderPassInfo renderPassInfo = default(RenderPassInfo), IPointSeries firstPointSeries = null);

        /// <summary>
        /// Scrolls current <see cref="IAxisParams.VisibleRange"/> by the specified number of pixels
        /// </summary>
        /// <param name="pixelsToScroll">Scroll N pixels from the current visible range</param>
        /// <param name="clipMode">Defines how scrolling behaves when you reach the edge of the Axis extents.
        /// e.g. ClipMode.ClipAtExtents prevents panning outside of the Axis, ClipMode.None allows panning outside</param>
        void Scroll(double pixelsToScroll, ClipMode clipMode);

        /// <summary>
        /// Scrolls current <see cref="IAxisParams.VisibleRange"/> by the specified number of pixels with the specified animation duration
        /// </summary>
        /// <param name="pixelsToScroll">Scroll N pixels from the current visible range</param>
        /// <param name="clipMode">Defines how scrolling behaves when you reach the edge of the Axis extents.
        /// e.g. ClipMode.ClipAtExtents prevents panning outside of the Axis, ClipMode.None allows panning outside</param>
        /// <param name="duration">The duration of animation when zooming to extents</param>
        void Scroll(double pixelsToScroll, ClipMode clipMode, TimeSpan duration);

        /// <summary>
        /// Translates current <see cref="IAxisParams.VisibleRange"/> by the specified number of datapoints
        /// </summary>
        /// <param name="pointAmount">Amount of data points that the start visible range is scrolled by</param>
        /// <remarks>For XAxis only,  is suitable for <see cref="CategoryDateTimeAxis"/>, <see cref="DateTimeAxis"/> and <see cref="NumericAxis"/>
        /// where data is regularly spaced</remarks>
        void ScrollByDataPoints(int pointAmount);

        /// <summary>
        /// Translates current <see cref="IAxisParams.VisibleRange"/> by the specified number of datapoints with the specified animation duration
        /// </summary>
        /// <param name="pointAmount">Amount of points that the start visible range is scrolled by</param>
        /// <remarks>For XAxis only,  is suitable for <see cref="CategoryDateTimeAxis"/>, <see cref="DateTimeAxis"/> and <see cref="NumericAxis"/>
        /// where data is regularly spaced</remarks>
        /// <param name="duration">The duration of animation when zooming to extents</param>
        void ScrollByDataPoints(int pointAmount, TimeSpan duration);

        /// <summary>
        /// Performs zoom on current <see cref="IAxis"/>, using <paramref name="fromCoord"/> as a coordinate of new range start and
        /// <paramref name="toCoord"/> as a coordinate of new range end
        /// </summary>
        /// <param name="fromCoord">The coordinate of new range start in pixels</param>
        /// <param name="toCoord">The coordinate of new range end in pixels</param>
        void Zoom(double fromCoord, double toCoord);

        /// <summary>
        /// Performs zoom on current <see cref="IAxis"/>, using <paramref name="fromCoord"/> as a coordinate of new range start and
        /// <paramref name="toCoord"/> as a coordinate of new range end
        /// </summary>
        /// <param name="fromCoord">The coordinate of new range start in pixels</param>
        /// <param name="toCoord">The coordinate of new range end in pixels</param>
        /// <param name="duration">The duration of animation when zooming to extents</param>
        void Zoom(double fromCoord, double toCoord, TimeSpan duration);

        /// <summary>
        /// Performs zoom on current <see cref="IAxis"/>, using <paramref name="minFraction"/> as a multiplier of range start and
        /// <paramref name="maxFraction"/> as a multiplier of range end
        /// </summary>
        /// <param name="minFraction">The multiplier of range start</param>
        /// <param name="maxFraction">The multiplier of range end</param>
        void ZoomBy(double minFraction, double maxFraction);

        /// <summary>
        /// Performs zoom on current <see cref="IAxis"/>, using <paramref name="minFraction"/> as a multiplier of range start and
        /// <paramref name="maxFraction"/> as a multiplier of range end
        /// </summary>
        /// <param name="minFraction">The multiplier of range start</param>
        /// <param name="maxFraction">The multiplier of range end</param>
        /// <param name="duration">The duration of animation when zooming to extents</param>
        void ZoomBy(double minFraction, double maxFraction, TimeSpan duration);

        /// <summary>
        /// From the starting <see cref="IRange"/>, scrolls to a new range by the specified number of pixels
        /// </summary>
        /// <param name="startVisibleRange">The start visible range</param>
        /// <param name="pixelsToScroll">Scroll N pixels from the start visible range</param>
        [Obsolete("IAxis.ScrollTo is obsolete, please call IAxis.Scroll(pixelsToScroll) instead")]
        void ScrollTo(IRange startVisibleRange, double pixelsToScroll);

        /// <summary>
        /// From the starting <see cref="IRange"/>, scrolls to a new range by the specified number of pixels, with the specified range limit
        /// </summary>
        /// <param name="startVisibleRange">The start visible range</param>
        /// <param name="pixelsToScroll">Scroll N pixels from the start visible range</param>
        /// <param name="rangeLimit">The range limit.</param>
        void ScrollToWithLimit(IRange startVisibleRange, double pixelsToScroll, IRange rangeLimit);

        /// <summary>
        /// Asserts the type passed in is supported by the current axis implementation
        /// </summary>
        /// <param name="dataType"></param>
        void AssertDataType(Type dataType);

        /// <summary>
        /// String formats the text, using the <see cref="AxisBase.TextFormatting"/> property as a formatting string
        /// </summary>
        /// <param name="value">The data value to format</param>
        /// /// <param name="format">A composite format string</param>
        /// <returns>The string formatted data value</returns>
        [Obsolete("The FormatText method which takes a format string is obsolete. Please use the method overload with one argument instead.", true)]
        string FormatText(IComparable value, string format);

        /// <summary>
        /// String formats the text, using the <see cref="AxisBase.TextFormatting"/> property as a formatting string
        /// </summary>
        /// <param name="value">The data value to format</param>
        /// <returns>The string formatted data value</returns>
        string FormatText(IComparable value);
        
        /// <summary>
        /// String formats text for the cursor, using the <see cref="AxisBase.CursorTextFormatting"/> property as a formatting string
        /// </summary>
        /// <param name="value">The data value to format</param>
        /// <returns>The string formatted data value</returns>
        string FormatCursorText(IComparable value);

        /// <summary>
        /// Checks whether <paramref name="range"/> is valid visible range for this axis
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        bool IsValidRange(IRange range);

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns></returns>
        IAxis Clone();

        /// <summary>
        /// Animates the VisibleRange of the current axis to the end-range, with the specified duration
        /// </summary>
        /// <param name="range">The range to animate to</param>
        /// <param name="duration">The duration to animate</param>
        void AnimateVisibleRangeTo(IRange range, TimeSpan duration);

        /// <summary>
        /// Called by the UltrachartSurface internally to validate current axis during render pass
        /// </summary>
        /// <remarks>Throws if <see cref="AxisBase.AutoTicks"/> is False
        /// and <see cref="AxisBase.MajorDelta"/>, <see cref="AxisBase.MinorDelta"/> aren't set</remarks>
        /// <exception cref="InvalidOperationException"/>
        void ValidateAxis();

        /// <summary>
        /// Clears the axis of tick-marks and labels 
        /// </summary>
        void Clear();

        /// <summary>
        /// Returns an undefined <see cref="IRange"/>, called internally by Ultrachart to reset the VisibleRange of an axis to an undefined state
        /// </summary>
        /// <returns></returns>
        IRange GetUndefinedRange();

        /// <summary>
        /// Returns an default non zero <see cref="IRange"/>, called internally by Ultrachart to reset the VisibleRange of an axis to an undefined state
        /// </summary>
        /// <returns></returns>
        IRange GetDefaultNonZeroRange();

        /// <summary>
        /// Gets the current data-point size in pixels
        /// </summary>
        double CurrentDatapointPixelSize { get; }
    }
}