// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IRenderableSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Media;
using System.Xml.Serialization;
using Ecng.Xaml.Charting.ChartModifiers;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Numerics;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Visuals.Axes;
using Ecng.Xaml.Charting.Visuals.PointMarkers;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    /// <summary>
    /// Defines common properties to 2D and 3D RenderableSeries througout the Ultrachart SDK
    /// </summary>
    public interface IRenderableSeriesBase
    {
        /// <summary>
        /// Gets the <see cref="ServiceContainer"/> which provides access to services throughout Ultrachart. 
        /// ServiceContainers are created one per <see cref="UltrachartSurface"/> instance, 
        /// and shared between peripheral components such as <see cref="AxisBase"/>, <see cref="BaseRenderableSeries"/>, <see cref="ChartModifierBase"/> instances.
        /// For a full list of available services, see the remarks on <see cref="ServiceContainer"/>
        /// </summary>
        IServiceContainer Services { get; set; }
    }

    /// <summary>
    /// Defines the interface to all RenderableSeries within Ultrachart. A RenderableSeries is a Line series, or Scatter series or Candlestick series and provides the visual element in pair of <see cref="BaseRenderableSeries"/> and <see cref="IDataSeries"/>
    /// </summary>
    /// <seealso cref="BaseRenderableSeries"/>
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
    public interface IRenderableSeries : IRenderableSeriesBase, IDrawable, IXmlSerializable
    {
        /// <summary>
        /// Event raised whenever IsSelected property changed
        /// </summary>
        event EventHandler SelectionChanged;

        /// <summary>
        /// Event raised whenever IsVisible property changed
        /// </summary>
        event EventHandler IsVisibleChanged;

        /// <summary>
        /// Gets or sets whether the series is visible when drawn
        /// </summary>
        bool IsVisible { get; set; }

        /// <summary>
        /// Gets or sets whether this series uses AntiAliasing when drawn
        /// </summary>
        bool AntiAliasing { get; set; }

        /// <summary>
        /// Gets or sets the SeriesColor. 
        /// </summary>
        /// <remarks>This is used by each series type in different ways. For instance:
        /// <list type="bullet">
        ///     <item><see cref="FastLineRenderableSeries"/> and <see cref="FastImpulseRenderableSeries"/> uses the SeriesColor to draw the line series</item>
        ///     <item><see cref="FastMountainRenderableSeries"/> uses the SeriesColor to draw the line over the filled area</item>
        ///     <item><see cref="FastBandRenderableSeries"/> uses the SeriesColor to draw the first line of the pair</item>
        ///     <item><see cref="FastColumnRenderableSeries"/> uses this property to draw the outline of the columns</item>
        ///     <item><see cref="FastCandlestickRenderableSeries"/>, <see cref="FastOhlcRenderableSeries"/> and <see cref="XyScatterRenderableSeries"/> all ignore this property</item>
        /// </list>
        /// </remarks>
        Color SeriesColor { get; set; }

        /// <summary>
        /// Gets or sets value, indicates whether this <see cref="IRenderableSeries"/> is selected
        /// </summary>
        bool IsSelected { get; set; }

        /// <summary>
        /// Gets or sets the StrokeThickness in pixels for this series 
        /// </summary>
        int StrokeThickness { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Ecng.Xaml.Charting.Numerics.ResamplingMode"/> used when drawing this series
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        ///     <item>
        ///         None: Do not use resampling when redrawing a series
        ///     </item>
        ///         MinMax: Resample by taking the min-max of oversampled data. This results in the most visually accurate resampling, with the most performant rendering
        ///     <item>
        ///         Mid: Resample by taking the median point of oversampled data
        ///     </item>
        ///     <item>
        ///         Max: Resample by taking the maximum point of oversampled data
        ///     </item>
        ///     <item>
        ///         Min: Resample by taking the minimum point of oversampled data
        ///     </item>
        /// </list>
        /// </remarks>
        ResamplingMode ResamplingMode { get; set; }

        /// <summary>
        /// ToPointSeries argument.
        /// </summary>        
        object PointSeriesArg { get; }

        /// <summary>
        /// Gets or sets the DataSeries associated with this series
        /// </summary>
        IDataSeries DataSeries { get; set; }

        /// <summary>
        /// Gets or sets the XAxis that this <see cref="IRenderableSeries"/> is associated with
        /// </summary>
        IAxis XAxis { get; set; }

        /// <summary>
        /// Gets or sets the YAxis that this <see cref="IRenderableSeries"/> is associated with
        /// </summary>
        IAxis YAxis { get; set; }

        /// <summary>
        /// Gets or sets style for selected series
        /// </summary>
        Style SelectedSeriesStyle { get; set; }

        /// <summary>
        /// Gets or sets the style to apply to the <see cref="IRenderableSeries"/>
        /// </summary>        
        Style Style { get; set; }

        /// <summary>
        /// Gets or sets the DataContext to apply to the <see cref="IRenderableSeries"/>
        /// </summary>        
        object DataContext { get; set; }

        /// <summary>
        /// Gets a cached Framework Element which is used as a Rollover Marker. 
        /// This is generated from a ControlTemplate in xaml via the <see cref="BaseRenderableSeries.RolloverMarkerTemplateProperty"/> DependencyProperty
        /// </summary>
        FrameworkElement RolloverMarker { get; }

        /// <summary>
        /// Gets or sets the ID of the Y-Axis which this renderable series is measured against
        /// </summary>
        string YAxisId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the X-Axis which this renderable series is measured against
        /// </summary>
        string XAxisId { get; set; }

        /// <summary>
        /// Gets or sets the RenderPassData instance used for this render pass
        /// </summary>
        /// <value>The render data.</value>
        /// <remarks></remarks>
        IRenderPassData CurrentRenderPassData { get; set; }

        /// <summary>
        /// Gets or Sets an optional <see cref="IPaletteProvider"/> instance, which may be used to override specific data-point colors during rendering. 
        /// For more details, see the <see cref="IPaletteProvider"/> documentation
        /// </summary>
        IPaletteProvider PaletteProvider { get; set; }

        /// <summary>
        /// If true, the data is displayed as XY, e.g. like a Scatter plot, not a line (time) series
        /// </summary>
        bool DisplaysDataAsXy { get; }

        /// <summary>
        /// Performs a hit-test at the specific mouse point (X,Y coordinate on the parent <see cref="UltrachartSurface" />),
        /// returning a <see cref="HitTestInfo" /> struct with the results
        /// </summary>
        /// <param name="rawPoint">The mouse point on the parent <see cref="UltrachartSurface" /></param>
        /// <param name="interpolate">If true, use interpolation to perform a hit-test between data-points, or on the area if a <see cref="FastMountainRenderableSeries"/>, <see cref="FastColumnRenderableSeries"/> or <see cref="FastCandlestickRenderableSeries"/></param>
        /// <returns>
        /// A <see cref="HitTestInfo" /> struct with the results
        /// </returns>
        HitTestInfo HitTest(Point rawPoint, bool interpolate = false);

        /// <summary>
        /// Performs a hit-test at the specific mouse point (X,Y coordinate on the parent <see cref="UltrachartSurface" />),
        /// returning a <see cref="HitTestInfo" /> struct with the results
        /// </summary>
        /// <param name="rawPoint">The mouse point on the parent <see cref="UltrachartSurface" /></param>
        /// <param name="hitTestRadius">The radius in pixels to determine whether a mouse is over a data-point</param>
        /// <param name="interpolate">If true, use interpolation to perform a hit-test between data-points, or on the area if a <see cref="FastMountainRenderableSeries"/>, <see cref="FastColumnRenderableSeries"/> or <see cref="FastCandlestickRenderableSeries"/></param>
        /// <returns>
        /// A <see cref="HitTestInfo" /> struct with the results
        /// </returns>
        HitTestInfo HitTest(Point rawPoint, double hitTestRadius, bool interpolate = false);

        /// <summary>
        /// Performs a hit-test at the specific mouse point with zero hit-test radius. 
        /// Method considers only X values and returns a <see cref="HitTestInfo" /> struct with the closest X value
        /// </summary>
        /// <param name="rawPoint">The mouse point on the parent <see cref="UltrachartSurface" /></param>
        /// <param name="interpolate">If true, use interpolation to perform a hit-test between data-points, or on the area if a <seealso cref="Ecng.Xaml.Charting.Visuals.RenderableSeries.FastMountainRenderableSeries"/>, <seealso cref="Ecng.Xaml.Charting.Visuals.RenderableSeries.FastColumnRenderableSeries"/> or <seealso cref="Ecng.Xaml.Charting.Visuals.RenderableSeries.FastCandlestickRenderableSeries"/></param>
        /// <returns>
        /// A <see cref="HitTestInfo" /> struct with the results
        /// </returns>
        /// <remarks>
        /// Used by <see cref="RolloverModifier"/> and <see cref="VerticalSliceModifier"/>
        /// </remarks>
        HitTestInfo VerticalSliceHitTest(Point rawPoint, bool interpolate = false);

        /// <summary>
        /// Converts a <see cref="HitTestInfo"/> hit-test result into a <see cref="SeriesInfo"/> viewmodel for use in the 
        /// <see cref="UltrachartLegend"/>, <see cref="RolloverModifier"/>, <see cref="CursorModifier"/>.
        /// </summary>
        /// <remarks>All the legend and tooltip
        /// types bind to <see cref="SeriesInfo"/>, so this is a useful API function to convert hit-test results into this useful type.</remarks>
        /// <param name="hitTestInfo">The hit-test result to convert</param>
        /// <returns></returns>
        /// <seealso cref="SeriesInfo"/>
        /// <seealso cref="RolloverModifier"/>
        /// <seealso cref="CursorModifier"/>
        /// <seealso cref="LegendModifier"/>
        /// <seealso cref="UltrachartLegend"/>
        SeriesInfo GetSeriesInfo(HitTestInfo hitTestInfo);

        /// <summary>
        /// Returns the data range of the assosiated <see cref="IDataSeries"/> on X direction
        /// </summary>
        IRange GetXRange();  
        
        /// <summary>
        /// Returns the data range of the assosiated <see cref="IDataSeries"/> on Y direction
        /// <param name="xRange">The X-Axis Range currently in view</param>
        /// </summary>
        IRange GetYRange(IRange xRange);

        /// <summary>
        /// Returns the data range of the assosiated <see cref="IDataSeries"/> on Y direction
        /// <param name="xRange">The X-Axis Range currently in view</param>
        /// <param name="getPositiveRange">Indicates whether to return positive YRange only</param>
        /// </summary>
        IRange GetYRange(IRange xRange, bool getPositiveRange);

        /// <summary>
        /// Returns the data range of the assosiated <see cref="IDataSeries"/> on X direction which is enough to render VisibleRange=xRange correctly.
        /// </summary>
        IndexRange GetExtendedXRange(IndexRange IndexRange);

        bool GetIncludeSeries(Modifier modifier);
    }
}