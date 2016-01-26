// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IDataSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections;
using System.Collections.Generic;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Numerics;
using Ecng.Xaml.Charting.Numerics.PointResamplers;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.Axes;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.Model.DataSeries
{
    /// <summary>
    /// Defines the base interface to a DataSeries which provides a data-source for <see cref="BaseRenderableSeries"/>
    /// </summary>
    /// <seealso cref="DataSeries{TX,TY}"/>
    /// <seealso cref="IDataSeries"/>
    /// <seealso cref="IDataSeries{TX,TY}"/>
    /// <seealso cref="IXyDataSeries{TX,TY}"/>
    /// <seealso cref="IXyDataSeries"/>
    /// <seealso cref="XyDataSeries{TX,TY}"/>
    /// <seealso cref="IXyyDataSeries{TX,TY}"/>
    /// <seealso cref="IXyyDataSeries"/>
    /// <seealso cref="XyyDataSeries{TX,TY}"/>
    /// <seealso cref="IOhlcDataSeries{TX,TY}"/>
    /// <seealso cref="IOhlcDataSeries"/>
    /// <seealso cref="OhlcDataSeries{TX,TY}"/>
    /// <seealso cref="IHlcDataSeries{TX,TY}"/>
    /// <seealso cref="IHlcDataSeries"/>
    /// <seealso cref="HlcDataSeries{TX,TY}"/>
    /// <seealso cref="IXyzDataSeries{TX,TY,TZ}"/>
    /// <seealso cref="IXyzDataSeries"/>
    /// <seealso cref="XyzDataSeries{TX,TY,TZ}"/>
    /// <seealso cref="BoxPlotDataSeries{TX,TY}"/>
    /// <seealso cref="BaseRenderableSeries"/>
    /// <remarks>DataSeries are assigned to the RenderableSeries via the <see cref="IRenderableSeries.DataSeries"/> property. Any time a DataSeries is appended to, the
    /// parent <see cref="UltrachartSurface"/> will be redrawn</remarks>
    public interface IDataSeries : ISuspendable
    {
        /// <summary>
        /// Event raised whenever points are added to, removed or one or more DataSeries properties changes
        /// </summary>
        event EventHandler<DataSeriesChangedEventArgs> DataSeriesChanged;

        /// <summary>
        /// Gets the Type of X-data points in this DataSeries. Used to check compatibility with Axis types
        /// </summary>
        Type XType { get; }

        /// <summary>
        /// Gets the Type of Y-data points in this DataSeries. Used to check compatibility with Axis types
        /// </summary>
        Type YType { get; }

        /// <summary>
        /// Gets or sets the parent <see cref="IUltrachartSurface"/> which this <see cref="IDataSeries"/> instance is attached to
        /// </summary>
        IUltrachartSurface ParentSurface { get; set; }

        /// <summary>
        /// Gets whether the current DataSeries is attached to a DataSet
        /// </summary>
        [Obsolete("IsAttached is obsolete because there is no DataSeriesSet now")]
        bool IsAttached { get; }

        /// <summary>
        /// Gets the total extents of the <see cref="IDataSeries"/> in the X-Direction
        /// </summary>
        /// <remarks>Note: The performance implications of calling this is the DataSeries will perform a full recalculation on each get. It is recommended to get and cache if this property is needed more than once</remarks>
        IRange XRange { get; }

        /// <summary>
        /// Gets the total extents of the <see cref="IDataSeries"/> in the Y-Direction
        /// </summary>
        /// <remarks>Note: The performance implications of calling this is the DataSeries will perform a full recalculation on each get. It is recommended to get and cache if this property is needed more than once</remarks>
        IRange YRange { get; }

        /// <summary>
        /// Gets the <see cref="DataSeriesType"/> for this DataSeries
        /// </summary>
        DataSeriesType DataSeriesType { get; }

        /// <summary>
        /// Gets the XValues of this dataseries
        /// </summary>
        IList XValues { get;}

        /// <summary>
        /// Gets the YValues of this dataseries        
        /// </summary>
        IList YValues { get; }

        /// <summary>
        /// Gets the latest Y-Value of the DataSeries
        /// </summary>
        IComparable LatestYValue { get; }

        /// <summary>
        /// Gets or sets the name of this series
        /// </summary>
        string SeriesName { get; set; }

        /// <summary>
        /// Gets the computed Minimum value in Y for this series
        /// </summary>
        IComparable YMin { get; }

        /// <summary>
        /// Gets the computed minimum positive value in Y for this series
        /// </summary>
        [Obsolete("XRange, YRange, XMin, YMin are all obsolete for performance reasons, Sorry!", true)]
        IComparable YMinPositive { get; }

        /// <summary>
        /// Gets the computed Maximum value in Y for this series
        /// </summary>
        IComparable YMax { get; }

        /// <summary>
        /// Gets the computed Minimum value in X for this series
        /// </summary>
        IComparable XMin { get; }

        /// <summary>
        /// Gets the computed minimum positive value in X for this series
        /// </summary>
        [Obsolete("XRange, YRange, XMin, YMin are all obsolete for performance reasons, Sorry!", true)]
        IComparable XMinPositive { get; }

        /// <summary>
        /// Gets the computed Maximum value in X for this series
        /// </summary>
        IComparable XMax { get; }

        /// <summary>
        /// Gets whether the dataseries behaves as a FIFO. 
        /// If True, when the FifoCapacity is reached, old points will be
        /// discarded in favour of new points, resulting in a scrolling chart
        /// </summary>
        bool IsFifo { get; }

        /// <summary>
        /// Gets or sets the size of the FIFO buffer. 
        /// If null, then the dataseries is unlimited. 
        /// If a value is set, when the point count reaches this value, older points will be discarded
        /// </summary>
        int? FifoCapacity { get; set; }

        /// <summary>
        /// Gets whether the DataSeries has values (is not empty)
        /// </summary>
        bool HasValues { get; }

        /// <summary>
        /// Gets the number of points in this dataseries
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets whether this DataSeries contains Sorted data in the X-direction. 
        /// Note: Sorted data will result in far faster indexing operations. If at all possible, try to keep your data sorted in the X-direction
        /// </summary>
        bool IsSorted { get; }

        /// <summary>
        /// Gets a synchronization object used to lock this data-series. Also locked on append, update, remove or clear
        /// </summary>
        object SyncRoot { get; }

        /// <summary>
        /// New to v3.3: when AcceptsUnsortedData is false, the DataSeries with throw an InvalidOperationException if unsorted data is appended. Unintentional unsorted data can result in much slower performance. 
        /// To disable this check, set AcceptsUnsortedData = true. 
        /// </summary>        
        bool AcceptsUnsortedData { get; set; }

        /// <summary>
        /// Clears the series, resetting internal lists to zero size
        /// </summary>
        void Clear();

        /// <summary>
        /// Gets the integer indices of the XValues array that are currently in the VisibleRange passed in,
        /// and an indefinite range otherwise
        /// </summary>
        /// <example>If the input X-data is 0...99 in steps of 1, the VisibleRange is 10, 30 then the Indices Range will be 10, 30</example>
        /// <param name="visibleRange">The VisibleRange to get the indices range</param>
        /// <returns>The indices to the X-Data that are currently in range</returns>
        IndexRange GetIndicesRange(IRange visibleRange);

        /// <summary>
        /// Converts the default <see cref="IDataSeries.YValues"/> to an <see cref="IPointSeries"/> which is used to render XY series
        /// </summary>
        /// <param name="resamplingMode">The desired <see cref="ResamplingMode"/></param>
        /// <param name="pointRange">The integer Indices range in the parent data-set</param>
        /// <param name="viewportWidth">The current width of the viewport</param>
        /// <param name="isCategoryAxis">If true, uses the indices to form the resampled X-values, else uses the X-Values themselves</param>
        /// <param name="visibleXRange">The VisibleRange of the XAxis at the time resampling occurs</param>
        /// <param name="factory">The PointResamplerFactory which returns <see cref="IPointResampler"/> instances</param>
        /// <param name="dataIsDisplayedAs2D">If true, then data is presented as a scatter series without relationship between the points, e.g. not a line series </param>
        /// <returns>A <see cref="IPointSeries"/> which is used to render XY series</returns>
        IPointSeries ToPointSeries(ResamplingMode resamplingMode, IndexRange pointRange, int viewportWidth, bool isCategoryAxis, bool? dataIsDisplayedAs2D, IRange visibleXRange, IPointResamplerFactory factory);

        /// <summary>
        /// OBSOLETE: ToPointSeries overload has been deprecated, use ToPointSeries instead, and cast to correct type of point series
        /// </summary>
        /// <param name="column"></param>
        /// <param name="resamplingMode"></param>
        /// <param name="pointRange"></param>
        /// <param name="viewportWidth"></param>
        /// <param name="isCategoryAxis"></param>
        /// <returns></returns>
        [Obsolete("ToPointSeries overload has been deprecated, use ToPointSeries instead, and cast to correct type of point series", true)]
        IPointSeries ToPointSeries(IList column, ResamplingMode resamplingMode, IndexRange pointRange, int viewportWidth, bool isCategoryAxis);

        /// <summary>
        /// Gets the YRange of the data (min, max of the series) in the input visible range point range, where the input range is the <see cref="IAxisParams.VisibleRange"/>
        /// </summary>
        /// <param name="xRange">The X-Axis Range currently in view</param>
        /// <returns>The YRange of the data in this window</returns>
        IRange GetWindowedYRange(IRange xRange);

        /// <summary>
        /// Gets the YRange of the data (min, max of the series) in the input IndexRange, where indices are point-indices on the DataSeries columns
        /// </summary>
        /// <param name="xIndexRange">The X-Axis Indices currently in view</param>
        /// <returns>The YRange of the data in this window</returns>
        IRange GetWindowedYRange(IndexRange xIndexRange);

        /// <summary>
        /// Gets the YRange of the data (min, max of the series) in the input IndexRange, where indices are point-indices on the DataSeries columns
        /// </summary>
        /// <param name="xIndexRange">The X-Axis Indices currently in view</param>
        /// <param name="getPositiveRange">If true, returns an <seealso cref="IRange"/> which only has positive values, e.g, when viewing a Logarithmic chart this value might be set</param>
        /// <returns>The YRange of the data in this window</returns>
        IRange GetWindowedYRange(IndexRange xIndexRange, bool getPositiveRange);

        /// <summary>
        /// Gets the YRange of the data (min, max of the series) in the input visible range point range, where the input range is the <see cref="IAxisParams.VisibleRange"/>
        /// </summary>
        /// <param name="xRange">The X-Axis Range currently in view</param>
        /// <param name="getPositiveRange">If true, returns an <seealso cref="IRange"/> which only has positive values, e.g, when viewing a Logarithmic chart this value might be set</param>
        /// <returns>The YRange of the data in this window</returns>
        IRange GetWindowedYRange(IRange xRange, bool getPositiveRange);

        /// <summary>
        /// Finds the index to the DataSeries at the specified X-Value
        /// </summary>
        /// <param name="x">The X-value to search for</param>
        /// <param name="searchMode">The <see cref="SearchMode"/> options to use. Default is exact, where -1 is returned if the index is not found</param>
        /// <returns>The index of the found value</returns>
        int FindIndex(IComparable x, SearchMode searchMode = SearchMode.Exact);

        /// <summary>
        /// When overridden in a derived class, returns a <see cref="HitTestInfo"/> struct containing data about the data-point at the specified index
        /// </summary>
        /// <param name="index">The index to the DataSeries</param>
        /// <returns>The HitTestInfo</returns>
        HitTestInfo ToHitTestInfo(int index);

        /// <summary>
        /// May be called to trigger a redraw on the parent <see cref="UltrachartSurface"/>. This method is extremely useful 
        /// when <see cref="IDataSeries"/> are in a ViewModel and bound via MVVM to <see cref="IRenderableSeries"/>. 
        /// 
        /// Please see the <paramref name="rangeMode"/> parameter for invalidation options
        /// </summary>
        /// <param name="rangeMode">Provides <see cref="RangeMode"/> invalidation options for the parent surface</param>
        void InvalidateParentSurface(RangeMode rangeMode);

        /// <summary>
		/// Finds the closest point to a point with given X and Y value. Search region is a vertical area with center in X and [maxXDistance] X units to left and right
        /// </summary>
        /// <param name="x">The X-value of point [X data units, not pixels]</param>
		/// <param name="y">The Y-value of point [Y data units, not pixels]</param>
		/// <param name="xyScaleRatio">xUnitsPerPixel/yUnitsPerPixel</param>
		/// <param name="maxXDistance">specifies search region in X units (ticks for DateTime or TimeSpan)</param>
        /// <returns>The index of the found value, -1 if not found (when count is zero)</returns>
        int FindClosestPoint(IComparable x, IComparable y, double xyScaleRatio, double maxXDistance);

		/// <summary>
		/// Finds the closest line to a point with given X and Y value. Search region is a vertical area with center in X and [maxXDistance] X units to left and right
		/// </summary>
		/// <param name="x">The X-value of point [X data units, not pixels]</param>
		/// <param name="y">The Y-value of point [Y data units, not pixels]</param>
		/// <param name="xyScaleRatio">xUnitsPerPixel/yUnitsPerPixel</param>
		/// <param name="xRadius">specifies search region in X units (ticks for DateTime or TimeSpan)</param>
		/// <param name="drawNanAs">specifies how to handle NAN elements</param>
		/// <returns>The index of first point in line, -1 if not found (when count is zero)</returns>
		int FindClosestLine(IComparable x, IComparable y, double xyScaleRatio, double xRadius, LineDrawMode drawNanAs);

        void OnBeginRenderPass();
    }

    /// <summary>
    /// Defines the Generic interface to a DataSeries which provides a data-source to a <see cref="BaseRenderableSeries"/>
    /// </summary>
    /// <typeparam name="TX">The type of the X-Data.</typeparam>
    /// <typeparam name="TY">The type of the Y-Data.</typeparam>
    /// <seealso cref="DataSeries{TX,TY}"/>
    /// <seealso cref="IDataSeries"/>
    /// <seealso cref="IDataSeries{TX,TY}"/>
    /// <seealso cref="IXyDataSeries{TX,TY}"/>
    /// <seealso cref="IXyDataSeries"/>
    /// <seealso cref="XyDataSeries{TX,TY}"/>
    /// <seealso cref="IXyyDataSeries{TX,TY}"/>
    /// <seealso cref="IXyyDataSeries"/>
    /// <seealso cref="XyyDataSeries{TX,TY}"/>
    /// <seealso cref="IOhlcDataSeries{TX,TY}"/>
    /// <seealso cref="IOhlcDataSeries"/>
    /// <seealso cref="OhlcDataSeries{TX,TY}"/>
    /// <seealso cref="IHlcDataSeries{TX,TY}"/>
    /// <seealso cref="IHlcDataSeries"/>
    /// <seealso cref="HlcDataSeries{TX,TY}"/>
    /// <seealso cref="IXyzDataSeries{TX,TY,TZ}"/>
    /// <seealso cref="IXyzDataSeries"/>
    /// <seealso cref="XyzDataSeries{TX,TY,TZ}"/>
    /// <seealso cref="BoxPlotDataSeries{TX,TY}"/>
    /// <seealso cref="BaseRenderableSeries"/>
    /// <remarks>DataSeries are assigned to the RenderableSeries via the <see cref="IRenderableSeries.DataSeries"/> property. Any time a DataSeries is appended to, the
    /// parent <see cref="UltrachartSurface"/> will be redrawn</remarks>
    public interface IDataSeries<TX, TY> : IDataSeries
        where TX : IComparable
        where TY : IComparable
    {
        /// <summary>
        /// Gets the X Values of this series
        /// </summary>
        new IList<TX> XValues { get; }

        /// <summary>
        /// Gets the Y Values of this series
        /// </summary>
        new IList<TY> YValues { get; }

        /// <summary>
        /// Appends an X, Y point to the series
        /// </summary>
        /// <exception cref="InvalidOperationException">Exception will be thrown if the count of y differ</exception>
        /// <param name="x">The X Value</param>
        /// <param name="yValues">The Y Values (depends on series type)</param>
        void Append(TX x, params TY[] yValues);

        /// <summary>
        /// Appends a list of X, Y points to the series
        /// </summary>
        /// <exception cref="InvalidOperationException">Exception will be thrown if the count of x and each y differ</exception>
        /// <param name="x">The list of X points</param>
        /// <param name="yValues">Lists of Y points (depends on series type)</param>
        void Append(IEnumerable<TX> x, params IEnumerable<TY>[] yValues);

        /// <summary>
        /// Removes a point with the specified X Value
        /// </summary>
        /// <param name="x"></param>
        void Remove(TX x);

        /// <summary>
        /// Removes a point at the specified index
        /// </summary>
        /// <param name="index"></param>
        void RemoveAt(int index);

        /// <summary>
        /// Removes a range of points starting from the specified index
        /// </summary>
        /// <param name="startIndex">Starting index of the range of elements to remove</param>
        /// <param name="count">The number of elements to remove</param>
        void RemoveRange(int startIndex, int count);        

        /// <summary>
        /// Creates a deep copy of a DataSeries
        /// </summary>
        /// <returns></returns>
        IDataSeries<TX, TY> Clone();

        /// <summary>
        /// Used internally by AutoRanging algorithm. 
        /// When overriden in a derived class, gets the Min(existingYMin, currentMin), where currentMin is the minimum at the specified index
        /// </summary>
        /// <param name="index">The index to the underlying dataset</param>
        /// <param name="existingYMin">The existing minimum</param>
        /// <returns>The new YMin, which is the Min(existingYMin, currentMin)</returns>
        TY GetYMinAt(int index, TY existingYMin);

        /// <summary>
        /// Used internally by AutoRanging algorithm. 
        /// When overriden in a derived class, gets the Max(existingYMax, currentMax), where currentMax is the maximum at the specified index
        /// </summary>
        /// <param name="index">The index to the underlying dataset</param>
        /// <param name="existingYMax">The existing maximum</param>
        /// <returns>The new YMax, which is the Min(existingYMax, currentMax)</returns>
        TY GetYMaxAt(int index, TY existingYMax);
    }
}