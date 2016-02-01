// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// DataSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Numerics;
using Ecng.Xaml.Charting.Numerics.GenericMath;
using Ecng.Xaml.Charting.Numerics.PointResamplers;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.Axes;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.Model.DataSeries
{
    /// <summary>
    /// A generic abstract base class which defines a generic DataSeries which contains 1..N columns, 
    /// such as X,Y in the case of plain X,Y data, or X, Open, High, Low, Close in the case of OHLC data.
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
    /// <seealso cref="BaseRenderableSeries"/>
    /// <seealso cref="Ecng.Xaml.Charting.Visuals.RenderableSeries.IRenderableSeries.DataSeries"/>
    /// <remarks>DataSeries are assigned to the RenderableSeries via the <see cref="Ecng.Xaml.Charting.Visuals.RenderableSeries.IRenderableSeries"/> property. Any time a DataSeries is appended to, the
    /// parent <see cref="BoxPlotDataSeries{TX,TY}"/> will be redrawn</remarks>
    public abstract class DataSeries<TX, TY> : BindableObject, IDataSeries<TX, TY> 
        where TX : IComparable
        where TY : IComparable
    {
        /// <summary>
        /// Event raised whenever points are added to, removed or one or more DataSeries properties changes
        /// </summary>
        /// <remarks></remarks>
        public event EventHandler<DataSeriesChangedEventArgs> DataSeriesChanged;        

        /// <summary>
        /// The XColumn for this DataSeries
        /// </summary>
        protected ISeriesColumn<TX> _xColumn = new SeriesColumn<TX>();
        /// <summary>
        /// The primary YColumn for this DataSeries
        /// </summary>
        protected ISeriesColumn<TY> _yColumn = new SeriesColumn<TY>();

        /// <summary>
        /// The computed Y-Minimum for this DataSeries
        /// </summary>
        [Obsolete("XRange, YRange, XMin, YMin are all obsolete for performance reasons, Sorry!", true)]
        protected TY _yMin;
        /// <summary>
        /// The computed minimum positive Y value for this DataSeries
        /// </summary>
        [Obsolete("XRange, YRange, XMin, YMin are all obsolete for performance reasons, Sorry!", true)]
        protected TY _yMinPositive;
        /// <summary>
        /// The computed Y-Maximum for this DataSeries
        /// </summary>
        [Obsolete("XRange, YRange, XMin, YMin are all obsolete for performance reasons, Sorry!", true)]
        protected TY _yMax;
        /// <summary>
        /// The computed X-Minimum for this DataSeries
        /// </summary>
        [Obsolete("XRange, YRange, XMin, YMin are all obsolete for performance reasons, Sorry!", true)]
        protected TX _xMin;
        /// <summary>
        /// The computed minimum positive X value for this DataSeries
        /// </summary>
        [Obsolete("XRange, YRange, XMin, YMin are all obsolete for performance reasons, Sorry!", true)]
        protected TX _xMinPositive;
        /// <summary>
        /// The computed X-Maximum for this DataSeries
        /// </summary>
        [Obsolete("XRange, YRange, XMin, YMin are all obsolete for performance reasons, Sorry!", true)]
        protected TX _xMax;

        public static readonly IMath<TY> YMath = GenericMathFactory.New<TY>();
        public static readonly IMath<TX> XMath = GenericMathFactory.New<TX>();

        private int? _fifoCapacity;
        private string _seriesName;

        private readonly object _syncRoot = new object();

        /// <summary>
        /// Synchronization object (per instance) 
        /// </summary>
        public object SyncRoot { get { return _syncRoot; } }

        /// <summary>
        /// Gets or Sets the DataDistrutionCalculator instance for this DataSeries. Used when resampling data to determine the correct algorithm 
        /// </summary>
        /// <remarks>By default, Ultrachart provides a DataDistributionCalculator which calculates if data is sorted appending, or evenly spaced as you append data. 
        /// However, this process takes approximately 30% of the time to append data (or more if appending in blocks). If you know in advance what the distribution of your data will be, 
        /// you can set the DataDistributionCalculator = new UserDefinedDistributionCalculator and set the flags yourself </remarks>
        public IDataDistributionCalculator<TX> DataDistributionCalculator { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataSeries&lt;TX, TY&gt;"/> class.
        /// </summary>
        /// <remarks></remarks>
        protected DataSeries()
        {
            Clear();
        }

        private Ecng.Xaml.Charting.Common.WeakReference<IUltrachartSurface> _parentSurface;

        /// <summary>
        /// Gets or sets the parent <see cref="IUltrachartSurface" /> which this <see cref="IDataSeries" /> instance is attached to
        /// </summary>
        public IUltrachartSurface ParentSurface
        {
            get { return _parentSurface != null ? _parentSurface.Target : null; }
            set
            {
                _parentSurface = null;
                if (value != null)
                {
                    _parentSurface = new Ecng.Xaml.Charting.Common.WeakReference<IUltrachartSurface>(value);
                }
            }
        }

        /// <summary>
        /// Gets the Type of X-data points in this DataSeries. Used to check compatibility with Axis types
        /// </summary>
        public Type XType { get { return typeof (TX); } }

        /// <summary>
        /// Gets the Type of Y-data points in this DataSeries. Used to check compatibility with Axis types
        /// </summary>
        public Type YType { get { return typeof (TY); } }

        /// <summary>
        /// Gets the computed XRange, which is an <see cref="IRange"/> wrapping XMin and XMax properties
        /// </summary>
        /// <remarks>Note: The performance implications of calling this is the DataSeries will perform a full recalculation on each get. It is recommended to get and cache if this property is needed more than once</remarks>
        public virtual IRange XRange
        {
            get
            {
                lock (SyncRoot)
                {
                    TX min = XMath.MaxValue;
                    TX max = XMath.MinValue;

                    if (DataDistributionCalculator.DataIsSortedAscending)
                    {
                        IList<TX> xValues = _xColumn;

                        if (xValues.Count > 0)
                        {
                            min = xValues[0];
                            max = xValues[xValues.Count - 1];
                        }
                    }
                    else
                    {
                        ArrayOperations.MinMax(_xColumn, out min, out max);
                    }

                    var xRange = RangeFactory.NewRange(min, max);
                    return xRange;
                }
            }
        }

        /// <summary>
        /// Gets the computed YRange, which is an <see cref="IRange"/> wrapping YMin and YMax properties
        /// </summary>
        /// <remarks>Note: The performance implications of calling this is the DataSeries will perform a full recalculation on each get. It is recommended to get and cache if this property is needed more than once</remarks>
        public virtual IRange YRange
        {
            get
            {
                lock (SyncRoot)
                {
                    TY min, max;
                    ArrayOperations.MinMax(_yColumn, out min, out max);
                    return RangeFactory.NewRange(min, max);
                }
            }
        }

        /// <summary>
        /// Gets the computed Minimum value in Y for this series
        /// </summary>
        public IComparable YMin { get { return YRange.Min; } }

        /// <summary>
        /// Gets the latest Y-Value of the DataSeries
        /// </summary>
        public IComparable LatestYValue { get { return YValues.Count == 0 ? null : (IComparable)YValues[YValues.Count - 1]; } }

        /// <summary>
        /// Gets the computed minimum positive value in Y for this series
        /// </summary>
        [Obsolete("XRange, YRange, XMin, YMin are all obsolete for performance reasons, Sorry!", true)]
        public IComparable YMinPositive { get { return _yMinPositive; } }

        /// <summary>
        /// Gets the computed Maximum value in Y for this series
        /// </summary>
        public IComparable YMax { get { return YRange.Max; } }

        /// <summary>
        /// Gets the computed Minimum value in X for this series
        /// </summary>
        public IComparable XMin { get { return XRange.Min; } }

        /// <summary>
        /// Gets the computed minimum positive value in X for this series
        /// </summary>
        [Obsolete("XRange, YRange, XMin, YMin are all obsolete for performance reasons, Sorry!", true)]
        public IComparable XMinPositive { get { return _xMinPositive; } }

        /// <summary>
        /// Gets the computed Maximum value in X for this series
        /// </summary>
        public IComparable XMax { get { return XRange.Max; } }

        /// <summary>
        /// Gets the number of points in this data series
        /// </summary>
        /// <remarks></remarks>
        public int Count { get { return ((ISeriesColumn)_yColumn).Count; } }

        /// <summary>
        /// New to v3.3: when AcceptsUnsortedData is false, the DataSeries with throw an InvalidOperationException if unsorted data is appended. Unintentional unsorted data can result in much slower performance. 
        /// To disable this check, set AcceptsUnsortedData = true. 
        /// </summary>        
        public bool AcceptsUnsortedData { get; set; }

        /// <summary>
        /// Gets or sets the name of this series
        /// </summary>
        /// <value>The name of the series.</value>
        /// <remarks></remarks>
        public string SeriesName
        {
            get { return _seriesName; }
            set
            {
                _seriesName = value;
                OnPropertyChanged("SeriesName");
                OnDataSeriesChanged(DataSeriesUpdate.DataChanged);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is attached.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is attached; otherwise, <c>false</c>.
        /// </value>
        [Obsolete("IsAttached is obsolete because there is no DataSeries now")]
        public bool IsAttached { get { return false; } }

        /// <summary>
        /// Gets the <see cref="DataSeriesType"/> for this DataSeries
        /// </summary>
        public abstract DataSeriesType DataSeriesType { get; }

        /// <summary>
        /// Gets the item at the index from the specified <see cref="DataSeriesColumn"/>.
        /// </summary>
        /// <remarks></remarks>
        public TY this[DataSeriesColumn column, int index]
        {
            get
            {
                switch(column)
                {
                    case DataSeriesColumn.Close:
                    case DataSeriesColumn.Y: return ((IList<TY>)_yColumn)[index];
                    default: throw new InvalidOperationException(string.Format("The enumeration value {0} has not been catered", column));
                }
            }
        }

        /// <summary>
        /// Gets whether the Data Series has values (is not empty)
        /// </summary>
        /// <remarks></remarks>
        public abstract bool HasValues { get; }

        /// <summary>
        /// Gets the X Values of this series
        /// </summary>
        public IList<TX> XValues     { get { return _xColumn; } }

        /// <summary>
        /// Gets the Y Values of this series
        /// </summary>
        public IList<TY> YValues { get { return _yColumn; } }

        /// <summary>
        /// Gets the X Values of this dataseries
        /// </summary>
        IList IDataSeries.XValues     { get { return _xColumn; } }

        /// <summary>
        /// Gets the Y Values of this dataseries
        /// </summary>
        IList IDataSeries.YValues { get { return _yColumn; } }

        /// <summary>
        /// Gets whether the dataseries behaves as a FIFO
        /// If True, when the FifoCapacity is reached, old points will be
        /// discarded in favour of new points
        /// </summary>
        /// <remarks></remarks>
        public bool IsFifo { get { return _fifoCapacity.HasValue; } }

        /// <summary>
        /// Gets whether this DataSeries contains Sorted data in the X-direction. 
        /// Note: Sorted data will result in far faster indexing operations. If at all possible, try to keep your data sorted in the X-direction
        /// </summary>
        public bool IsSorted
        {
            get { return DataDistributionCalculator.DataIsSortedAscending; }
        }
      
        /// <summary>
        /// Gets or sets the size of the FIFO buffer.
        /// If null, then the dataseries is unlimited.
        /// If a value is set, when the point count reaches this value, older points will be discarded
        /// </summary>
        /// <value>The fifo capacity.</value>
        /// <remarks></remarks>
        public int? FifoCapacity
        {
            get { return _fifoCapacity; }
            set
            {
                _fifoCapacity = value;
                Clear();
            }
        }

        /// <summary>
        /// Appends an X, Y point to the series
        /// </summary>
        /// <exception cref="InvalidOperationException">Exception will be thrown if the count of y differ</exception>
        /// <param name="x">The X Value</param>
        /// <param name="yValues">The Y Values (depends on series type)</param>
        public abstract void Append(TX x, params TY[] yValues);

        /// <summary>
        /// Appends a list of X, Y points to the series
        /// </summary>
        /// <exception cref="InvalidOperationException">Exception will be thrown if the count of x and each y differ</exception>
        /// <param name="x">The list of X points</param>
        /// <param name="yValues">Lists of Y points (depends on series type)</param>
        public abstract void Append(IEnumerable<TX> x, params IEnumerable<TY>[] yValues);

        /// <summary>
        /// Removes the associated Y-Values for the specified X-Value
        /// </summary>
        /// <param name="x">The X DataValue to remove. Removes all associated Y-Values</param>
        public void Remove(TX x)
        {
            int index = ((IList)XValues).FindIndex(IsSorted, x, SearchMode.Exact);
            if (index == -1)
                return;

            RemoveAt(index);
        }

        /// <summary>
        /// Removes the X,Y values at the specified index
        /// </summary>
        /// <param name="index">The index to remove at</param>
        public abstract void RemoveAt(int index);

        /// <summary>
        /// Removes a range of points starting from the specified index
        /// </summary>
        /// <param name="startIndex">Starting index of the range of elements to remove</param>
        /// <param name="count">The number of elements to remove</param>
        public abstract void RemoveRange(int startIndex, int count);

        /// <summary>
        /// Clears the series, resetting internal lists to zero size
        /// </summary>
        public void Clear()
        {
            lock (SyncRoot)
            {
                ClearColumns();

                OnDataSeriesChanged(DataSeriesUpdate.DataChanged | DataSeriesUpdate.DataSeriesCleared);
                DataDistributionCalculator = DataDistributionCalculatorFactory.Create<TX>(IsFifo);
                DataDistributionCalculator.Clear();
            }            
        }

        /// <summary>
        /// Creates a deep copy of a DataSeries
        /// </summary>
        /// <returns></returns>
        public abstract IDataSeries<TX, TY> Clone();

        /// <summary>
        /// Gets the integer indices of the XValues array that are currently in the VisibleRange passed in.
        /// </summary>
        /// <param name="range">The VisibleRange to get the indices range</param>
        /// <returns>
        /// The indices to the X-Data that are currently in range
        /// </returns>
        /// <example>If the input X-data is 0...99 in steps of 1, the VisibleRange is 10, 30 then the Indices Range will be 10, 30</example>
        public IndexRange GetIndicesRange(IRange range)
        {
            return GetIndicesRange(range, SearchMode.RoundDown, SearchMode.RoundUp);
        }

        private IndexRange GetIndicesRange(IRange range, SearchMode downSearchMode, SearchMode upSearchMode)
        {
            // Changed from (0, 0) to (0, -1), since (0,0) will give a range count of 1. 
            // TODO: Check if there's a problem with this when the DataSeries is empty
            var result = new IndexRange(0, -1);

            if (((ICollection)_xColumn).Count > 0)
            {
                var indexRange = range.Clone() as IndexRange;

                var indicesRange = indexRange ??
                                          SearchDataIndexesOn(range, downSearchMode, upSearchMode);

                result = ToIndicesRange(indicesRange);
            }

            return result;
        }

        private IndexRange SearchDataIndexesOn(IRange range, SearchMode downSearchMode, SearchMode upSearchMode)
        {
            int count = ((ICollection)_xColumn).Count;

            var indicesRange = new IndexRange(-1, -1);

            if (!IsSorted)
            {
                // For unsorted datasets, we need to draw everything
                indicesRange.Min = 0;
                indicesRange.Max = count - 1;
            }
            else
            {
                // For sorted data, we need to draw only the points in the viewport
                var xType = typeof (TX);

                var rangeMin = range.Min;
                var rangeMax = range.Max;

                // Fixes http://ulcsoftware.myjetbrains.com/youtrack/issue/SC-1183.
                // The issue was caused by the default numbers rounding during a type convertion
                if (NumberUtil.IsIntegerType(xType))
                {
                    rangeMin = Math.Floor(rangeMin.ToDouble());
                    rangeMax = Math.Ceiling(rangeMax.ToDouble());
                }

                var xMin = (TX)Convert.ChangeType(rangeMin, xType, CultureInfo.InvariantCulture);
                var xMax = (TX)Convert.ChangeType(rangeMax, xType, CultureInfo.InvariantCulture);

                var dataMin = ((IList<TX>) _xColumn)[0];
                var dataMax = ((IList<TX>) _xColumn)[count - 1];

                if (dataMax.CompareTo(xMin) >= 0 && dataMin.CompareTo(xMax) <= 0)
                {
                    indicesRange.Min = ((IList)_xColumn).FindIndex(true, xMin, downSearchMode);
                    indicesRange.Max = ((IList)_xColumn).FindIndex(true, xMax, upSearchMode);
                }
            }

            return indicesRange;
        }

        private IndexRange ToIndicesRange(IndexRange indexRange)
        {
            int count = ((ICollection)_xColumn).Count;

            if (indexRange.IsDefined)
            {
                indexRange.Min = Math.Max(indexRange.Min, 0);
                indexRange.Max = Math.Min(indexRange.Max, count - 1);
            }

            if (indexRange.Min.CompareTo(indexRange.Max) > 0)
                indexRange.Min = 0;

            UltrachartDebugLogger.Instance.WriteLine("GetIndicesRange: Min={0}, Max={1}", indexRange.Min, indexRange.Max);

            return indexRange;
        }

        /// <summary>
        /// Converts the default <see cref="IDataSeries.YValues" /> to an <see cref="IPointSeries" /> which is used to render XY series
        /// </summary>
        /// <param name="resamplingMode">The desired <see cref="ResamplingMode" /></param>
        /// <param name="pointRange">The integer Indices range in the parent data-set</param>
        /// <param name="viewportWidth">The current width of the viewport</param>
        /// <param name="isCategoryAxis">If true, uses the indices to form the resampled X-values, else uses the X-Values themselves</param>
        /// <param name="dataIsDisplayedAs2D">If true, then data is considered as a scatter series without relationship between the points, e.g. not a line series </param>
        /// <param name="visibleXRange"></param>
        /// <param name="factory">The PointResamplerFactory which returns <see cref="IPointResampler" /> instances</param>
        /// <returns>
        /// A <see cref="IPointSeries" /> which is used to render XY series
        /// </returns>
        public abstract IPointSeries ToPointSeries(ResamplingMode resamplingMode, IndexRange pointRange, int viewportWidth, bool isCategoryAxis, bool? dataIsDisplayedAs2D, IRange visibleXRange, IPointResamplerFactory factory, object pointSeriesArg = null);

        /// <summary>
        /// OBSOLETE. Use ToPointSeries instead, and cast to correct type of point series
        /// </summary>
        /// <param name="column"></param>
        /// <param name="resamplingMode"></param>
        /// <param name="pointRange"></param>
        /// <param name="viewportWidth"></param>
        /// <param name="isCategoryAxis"></param>
        /// <returns></returns>
        [Obsolete("ToPointSeries overload has been deprecated, use ToPointSeries instead, and cast to correct type of point series", true)]
        public IPointSeries ToPointSeries(IList column, ResamplingMode resamplingMode, IndexRange pointRange, int viewportWidth, bool isCategoryAxis)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the YRange of the data (min, max of the series) in the input visible range point range, where the input range is the <see cref="IAxisParams.VisibleRange" />
        /// </summary>
        /// <param name="xRange">The X-Axis Range currently in view</param>
        /// <returns>
        /// The YRange of the data in this window
        /// </returns>
        public IRange GetWindowedYRange(IRange xRange)
        {
           return GetWindowedYRange(xRange, false);
        }

        /// <summary>
        /// Gets the YRange of the data (min, max of the series) in the input visible range point range, where the input range is the <see cref="IAxisParams.VisibleRange"/>
        /// </summary>
        /// <param name="xRange">The X-Axis Range currently in view</param>
        /// <param name="getPositiveRange">Indicates whether to return positive YRange only</param>
        /// <returns>The YRange of the data in this window</returns>
        /// <exception cref="System.ArgumentNullException">xRange</exception>
        /// <exception cref="System.InvalidOperationException">Invalid Range Type. Please ensure you are using DateTimeAxis where the X-Data is DateTime, and NumericAxis where the X-Data is Double, Float, Int, Long</exception>
        public IRange GetWindowedYRange(IRange xRange, bool getPositiveRange)
        {
            if (xRange == null)
            {
                throw new ArgumentNullException("xRange");
            }

            var indicesRange = GetIndicesRange(xRange, SearchMode.Nearest, SearchMode.Nearest);

            var yRange = indicesRange.IsDefined
                ? GetWindowedYRange(indicesRange, getPositiveRange)
                : NewRange(YMath.MinValue, YMath.MaxValue);

            return yRange;
        }

        /// <summary>
        /// Gets the YRange of the data (min, max of the series) in the input IndexRange, where indices are point-indices on the DataSeries columns
        /// </summary>
        /// <param name="xIndexRange">The X-Axis Indices currently in view</param>
        /// <returns>
        /// The YRange of the data in this window
        /// </returns>
        public IRange GetWindowedYRange(IndexRange xIndexRange)
        {
            return GetWindowedYRange(xIndexRange, false);
        }

        /// <summary>
        /// Gets the YRange of the data (min, max of the series) in the input IndexRange, where indices are point-indices on the DataSeries columns
        /// </summary>
        /// <param name="xIndexRange">The X-Axis Indices currently in view</param>
        /// <param name="getPositiveRange">Indicates whether to return positive YRange only</param>
        /// <returns>
        /// The YRange of the data in this window
        /// </returns>
        public IRange GetWindowedYRange(IndexRange xIndexRange, bool getPositiveRange)
        {
            lock (SyncRoot)
            {
                var yMax = YMath.MinValue;
                var yMin = YMath.MaxValue;

                int iMin = Math.Max(xIndexRange.Min, 0);
                int iMax = Math.Min(xIndexRange.Max, Count - 1);

                for (int i = iMin; i <= iMax; i++)
                {
                    var min = GetYMinAt(i, yMin);
                    yMin = getPositiveRange ? GetPositiveMin(yMin, min) : min;

                    yMax = GetYMaxAt(i, yMax);
                }

                return NewRange(yMin, yMax);
            }
        }

        /// <summary>
        /// Finds the index to the DataSeries at the specified X-Value
        /// </summary>
        /// <param name="x">The X-value to search for</param>
        /// <param name="searchMode">The <see cref="SearchMode" /> options to use. Default is exact, where -1 is returned if the index is not found</param>
        /// <returns>
        /// The index of the found value
        /// </returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        public int FindIndex(IComparable x, SearchMode searchMode = SearchMode.Exact)
        {
            Guard.NotNull(x, "x");
            if (x.GetType() != typeof(TX))
            {
                if (x.GetType() == typeof (double))
                {
                    // convert from double to TX
                    var xAsDouble = (double) x;
                    var zeroX = XMath.ZeroValue;
                    var oneX = zeroX;
                    oneX = XMath.Inc(ref oneX);
                    x = XMath.Mult(oneX, xAsDouble);
                }
                else throw new InvalidOperationException(string.Format("The X-value type {0} does not match the DataSeries X-Type", x.GetType()));
            }

            return XValues.FindIndex(IsSorted, x, searchMode);
        }

        /// <summary>
        /// When overridden in a derived class, returns a <see cref="HitTestInfo"/> struct containing data about the data-point at the specified index
        /// </summary>
        /// <param name="index">The index to the DataSeries</param>
        /// <returns>The HitTestInfo</returns>
        public virtual HitTestInfo ToHitTestInfo(int index)
        {
            lock (SyncRoot)
            {
                if (index < 0 || index >= Count)
                {
                    return HitTestInfo.Empty;
                }

                var xValue = XValues[index];
                var yValue = YValues[index];

                var hitTestInfo = new HitTestInfo
                                      {
                                          DataSeriesName = SeriesName,
                                          DataSeriesType = DataSeriesType,
                                          XValue = xValue,
                                          YValue = yValue,
                                          DataSeriesIndex = index
                                      };

                return hitTestInfo;
            }
        }

        /// <summary>
        /// May be called to trigger a redraw on the parent <see cref="UltrachartSurface" />. This method is extremely useful
        /// when <see cref="IDataSeries" /> are in a ViewModel and bound via MVVM to <see cref="IRenderableSeries" />.
        /// Please see the <paramref name="rangeMode" /> parameter for invalidation options
        /// </summary>
        /// <param name="rangeMode">Provides <see cref="RangeMode" /> invalidation options for the parent surface</param>
        public void InvalidateParentSurface(RangeMode rangeMode)
        {
            if (ParentSurface != null)
            {
                switch (rangeMode)
                {
                    case RangeMode.None: ParentSurface.InvalidateElement(); break;
                    case RangeMode.ZoomToFit: ParentSurface.ZoomExtents(); break;
                    case RangeMode.ZoomToFitY: ParentSurface.ZoomExtentsY(); break;
                }
            }
        }

        /// <summary>
        /// Gets the minimum positive value of (positiveMin, min)
        /// </summary>
        /// <param name="positiveMin"></param>
        /// <param name="min"></param>
        protected T GetPositiveMin<T>(T positiveMin, T min) where T:IComparable
        {
            //Doesn't make sense for dates or TimeSpans
            if (typeof(T) == typeof(DateTime))
            {
                return positiveMin;
            }

            if (typeof(T) == typeof(TimeSpan))
            {
                return positiveMin;
            }

            // Note: I had to move these getters out of the ternery operator to fix 'Operation can destabilise the runtime' exception in Silverlight
            IComparable xZero = XMath.ZeroValue;
            IComparable yZero = YMath.ZeroValue;

            var zero = typeof (T) == typeof (TX) ? xZero : yZero;
            return min.CompareTo(zero) > 0 ? min : positiveMin;
        }

        /// <summary>
        /// When overridden in a derived class, clears all columns in the Data Series
        /// </summary>
        protected abstract void ClearColumns();

        /// <summary>
        /// When overriden in a derived class, gets the Min(existingYMin, currentMin), where currentMin is the minimum at the specified index
        /// </summary>
        /// <param name="index">The index to the underlying dataset</param>
        /// <param name="existingYMin">The existing minimum</param>
        /// <returns>
        /// The new YMin, which is the Min(existingYMin, currentMin)
        /// </returns>
        public abstract TY GetYMinAt(int index, TY existingYMin);

        /// <summary>
        /// When overriden in a derived class, gets the Max(existingYMax, currentMax), where currentMax is the maximum at the specified index
        /// </summary>
        /// <param name="index">The index to the underlying dataset</param>
        /// <param name="existingYMax">The existing maximum</param>
        /// <returns>
        /// The new YMax, which is the Min(existingYMax, currentMax)
        /// </returns>
        public abstract TY GetYMaxAt(int index, TY existingYMax);

        /// <summary>
        /// When overriden in a derived class, gets the Min(existingXMin, currentMin), where currentMin is the minimum at the specified index
        /// </summary>
        /// <param name="index">The index to the underlying dataset</param>
        /// <param name="existingXMin">The existing minimum</param>
        /// <returns>The new XMin, which is the Min(existingXMin, currentMin)</returns>
        public TX GetXMinAt(int index, TX existingXMin)
        {
            TX min = XValues[index];
            return min.IsDefined() ? XMath.Min(existingXMin, min) : existingXMin;
        }

        internal TY ComputeMin(TY currentMin, TY inputMin, ISeriesColumn<TY> seriesColumn) 
        {
            if (IsFifo)
            {
                return seriesColumn.GetMinimum();
            }

            return YMath.Min(currentMin, inputMin);
        }

        internal TY ComputeMax(TY currentMax, TY inputMax, ISeriesColumn<TY> seriesColumn)
        {
            if (IsFifo)
            {                
                return seriesColumn.GetMaximum();
            }

            return YMath.Max(currentMax, inputMax);
        }

        internal TX ComputeMin(TX currentMin, TX inputMin, ISeriesColumn<TX> seriesColumn)
        {
            if (IsFifo)
            {
                return seriesColumn.GetMinimum();
            }

            return XMath.Min(currentMin, inputMin);
        }

        internal TX ComputeMax(TX currentMax, TX inputMax, ISeriesColumn<TX> seriesColumn)
        {
            if (IsFifo)
            {
                return seriesColumn.GetMaximum();
            }

            return XMath.Max(currentMax, inputMax);
        }

        internal void OnDataSeriesChanged(DataSeriesUpdate dataSeriesUpdate)
        {
            this.OnPropertyChanged("LatestYValue");

            var handler = DataSeriesChanged;
            if (handler != null)
            {
                handler(this, new DataSeriesChangedEventArgs(dataSeriesUpdate));
            }
        }

        private DoubleRange NewRange(IComparable min, IComparable max)
        {
            // Note, to acheive cast of generic typeparam to DateTime or decimal, we must cast through object
            // see http://stackoverflow.com/questions/3558834/cast-t-parameter-in-generic-method-to-datetime
            if (typeof(TY) == typeof(DateTime))
                return new DoubleRange(((DateTime)((object)min)).Ticks, ((DateTime)((object)max)).Ticks);

            if (!min.IsDefined() || !max.IsDefined())
                return new DoubleRange(double.MinValue, double.MaxValue);

            double dMin = min.ToDouble();
            double dMax = max.ToDouble();

            return new DoubleRange(dMin, dMax);
        }

        /// <summary>
        /// Asserts correct number of parameters supplied to Append(TX, params TY) and throws if necessary
        /// </summary>
        /// <param name="paramsCount">The expected params count</param>
        protected void ThrowWhenAppendInvalid(int paramsCount)
        {
            throw new InvalidOperationException(String.Format("Append(TX x, params TY[] yValues) in type {0} must receive only {1} list(s) of y values.", GetType().Name, paramsCount));
        }

        /// <summary>
        /// Gets a value indicating whether updates for the target are currently suspended
        /// </summary>
        public bool IsSuspended
        {
            get { return UpdateSuspender.GetIsSuspended(this); }
        }

        /// <summary>
        /// Suspends drawing updates on the target until the returned object is disposed, when a final draw call will be issued
        /// </summary>
        /// <returns>
        /// The disposable Update Suspender
        /// </returns>
        public IUpdateSuspender SuspendUpdates()
        {
            var ps = ParentSurface;
            if (ps != null)
            {
                // Synchronization object on the parent surface
                Monitor.Enter(ps.SyncRoot);
                return new UpdateSuspender(this, ps.SyncRoot);
            }

            return new UpdateSuspender(this);
        }

        /// <summary>
        /// Resumes updates on the target, intended to be called by IUpdateSuspender
        /// </summary>
        /// <param name="suspender"></param>
        public void ResumeUpdates(IUpdateSuspender suspender)
        {
            if (suspender.ResumeTargetOnDispose)
            {
                OnDataSeriesChanged(DataSeriesUpdate.DataChanged | DataSeriesUpdate.DataSeriesCleared);
            }

            if (suspender.Tag != null)
            {
                // Synchronization object on the parent surface
                Monitor.Exit(suspender.Tag);
            }
        }

        /// <summary>
        /// Called by IUpdateSuspender each time a target suspender is disposed. When the final
        /// target suspender has been disposed, ResumeUpdates is called
        /// </summary>
        public void DecrementSuspend()
        {            
        }

        /// <summary>
        /// Finds the closest point to a point with given X and Y value. Search region is a vertical area with center in X and [maxXDistance] X units to left and right
        /// </summary>
        /// <param name="xValue">The X-value of point [X data units, not pixels]</param>
        /// <param name="yValue">The Y-value of point [Y data units, not pixels]</param>
        /// <param name="xyScaleRatio">xUnitsPerPixel/yUnitsPerPixel</param>
        /// <param name="hitTestRadius">Specifies search region in chart coordinates(ticks for DateTime or TimeSpan)</param>
        /// <returns>
        /// The index of the found value, -1 if not found (when count is zero)
        /// </returns>
        public virtual int FindClosestPoint(IComparable xValue, IComparable yValue, double xyScaleRatio,
            double hitTestRadius)
        {
            var closestPointIndex = -1;

            if (xValue == null || yValue == null)
            {
                return closestPointIndex;
            }

            lock (_syncRoot)
            {
                var xValuesArrayLength = XValues.Count;

                int startIndex = 0;
                int count = xValuesArrayLength - 1;

                var xValuesArray = XValues.ToUncheckedList();
                var yValuesArray = YValues.ToUncheckedList();

                // TODO: Cast to unsigned type will cause an exception if xValue is negative (possible during hit-test)
                var x = (TX) xValue;
                var xAsDouble = XMath.ToDouble(x);

                if (IsSorted)
                {
                    if (xyScaleRatio.CompareTo(0) == 0)
                    {
                        closestPointIndex = xValuesArray.FindIndexInSortedData(xValuesArrayLength, x,
                            SearchMode.Nearest, XMath);
                    }
                    else
                    {
                        var xScale = XMath.ZeroValue;

                        // for DateTimes it will be DateTime.MinValue.AddTicks(maxXDistance) 
                        xScale = XMath.Inc(ref xScale);

                        // workaround for http://ulcsoftware.myjetbrains.com/youtrack/issue/SC-1903
                        // TODO: what if TX is TimeSpan?
                        if (typeof (TX) == typeof (DateTime) && hitTestRadius >= DateTime.MaxValue.Ticks)
                        {
                            hitTestRadius = DateTime.MaxValue.Ticks - 1;
                        }

                        xScale = XMath.Mult(xScale, hitTestRadius);

                        // use binary search to reduce search region
                        var leftBound = XMath.Subtract(x, xScale);
                        var rightBound = XMath.Add(x, xScale);

                        var leftIndexInclusive = xValuesArray.FindIndexInSortedData(xValuesArrayLength, leftBound,
                            SearchMode.RoundDown, XMath);
                        var rightIndexInclusive = xValuesArray.FindIndexInSortedData(xValuesArrayLength, rightBound,
                            SearchMode.RoundUp, XMath);

                        if (leftIndexInclusive >= 0)
                        {
                            startIndex = leftIndexInclusive;
                        }

                        if (rightIndexInclusive >= 0)
                        {
                            count = rightIndexInclusive;
                        }

                        count -= startIndex;
                    }
                }

                if (closestPointIndex == -1)
                {
                    double minDistance = Double.MaxValue;
                    double yDiff, distance;

                    int endIndex = startIndex + count + 1;
                    for (int index = startIndex; index < endIndex; index++)
                    {
                        var currXAsDouble = XMath.ToDouble(xValuesArray[index]);

                        // TODO use http://www.geeksforgeeks.org/closest-pair-of-points/ later
                        distance = Math.Abs(currXAsDouble - xAsDouble);

                        if (xyScaleRatio.CompareTo(0) != 0)
                        {
                            yDiff = YMath.ToDouble(YMath.Subtract(yValuesArray[index], (TY) yValue));

                            distance += Math.Abs(yDiff)*xyScaleRatio;
                        }

                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            closestPointIndex = index;
                        }
                    }
                }

                return closestPointIndex;
            }
        }

        /// <summary>
        /// Finds the closest line to a point with given X and Y value. Search region is a vertical area with center in X and [maxXDistance] X units to left and right
        /// </summary>
        /// <param name="x">The X-value of point [X data units, not pixels]</param>
        /// <param name="y">The Y-value of point [Y data units, not pixels]</param>
        /// <param name="xyScaleRatio">xUnitsPerPixel/yUnitsPerPixel</param>
        /// <param name="xRadius">specifies search region in X units (ticks for DateTime or TimeSpan)</param>
        /// <param name="drawNanAs">specifies how to handle NAN elements</param>
        /// <returns>
        /// The index of first point in line, -1 if not found (when count is zero)
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public virtual int FindClosestLine(IComparable x, IComparable y, double xyScaleRatio, double xRadius, LineDrawMode drawNanAs)
        {
            var count = Count;
            var closestPointIndex = -1;

            if (count < 2 || x == null || y == null)
            {
                return closestPointIndex;
            }

            lock (_syncRoot)
            {
                var xValuesArray = XValues.ToUncheckedList();
                var yValuesArray = YValues.ToUncheckedList();

                int start = 0;
                var xValuesArrayLength = XValues.Count;

                var xValue = (TX) x;
                var yValue = (TY) y;

                if (IsSorted)
                {
                    // Use binary search to reduce search region
                    TX maxXDistanceAsTx = XMath.ZeroValue;
                    maxXDistanceAsTx = XMath.Inc(ref maxXDistanceAsTx);
                    maxXDistanceAsTx = XMath.Mult(maxXDistanceAsTx, xRadius);

                    // var middleIndex = xValuesArray.FindIndexInSortedData(xValuesArrayLength, x1, SearchMode.Nearest, _xMath);
                    var leftIndex = xValuesArray.FindIndexInSortedData(xValuesArrayLength,
                        XMath.Subtract(xValue, maxXDistanceAsTx),
                        SearchMode.RoundDown, XMath);

                    var rightIndex = xValuesArray.FindIndexInSortedData(xValuesArrayLength,
                        XMath.Add(xValue, maxXDistanceAsTx),
                        SearchMode.RoundUp, XMath);

                    //TODO can be optimized here: use leftIndex as a start point for the binary search
                    if (drawNanAs == LineDrawMode.ClosedLines)
                    {
                        // Extend search region to 'real' (not NaN) data points
                        while (leftIndex > 0 && YMath.IsNaN(yValuesArray[leftIndex]))
                            leftIndex--;

                        while (rightIndex < Count - 1 && YMath.IsNaN(yValuesArray[rightIndex]))
                            rightIndex++;

                        // Include the right bound
                        if (rightIndex < Count - 1) rightIndex++; 
                    }

                    closestPointIndex = start = leftIndex;
                    count = rightIndex - leftIndex + 1;
                }

                var xAsDouble = XMath.ToDouble(xValue);
                var yAsDouble = YMath.ToDouble(yValue);

                // Point in data space with Y adjusted by xyScaleRatio=xUnitsPerPixel/yUnitsPerPixel
                var point = new Point(xAsDouble, yAsDouble*xyScaleRatio);
                    
                double minDistance = Double.MaxValue;

                var prevX = XMath.ToDouble(xValuesArray[start]);
                var prevY = YMath.ToDouble(yValuesArray[start])*xyScaleRatio;

                var leftBound = xAsDouble - xRadius;
                var rightBound = xAsDouble + xRadius;

                int prevIndex = start;
                int endIndex = start + count;
                for (int index = start + 1; index < endIndex; index++)
                {
                    var currYValue = yValuesArray[index];
                    if (YMath.IsNaN(currYValue))
                    {
                        if (drawNanAs == LineDrawMode.Gaps)
                        {
                            prevY = double.NaN;
                        }

                        continue;
                    }

                    // Point in data space with Y adjusted by xyScaleRatio=xUnitsPerPixel/yUnitsPerPixel
                    var currX = XMath.ToDouble(xValuesArray[index]);
                    var currY = YMath.ToDouble(currYValue)*xyScaleRatio;

                    // Is suitable only if one of the ends lies inside the range or the range lies between the ends
                    var isInRange =
                        !(prevX < leftBound && currX < leftBound || prevX > rightBound && currX > rightBound);

                    if (isInRange)
                    {
                        var distance = PointUtil.DistanceFromLine(point, new Point(prevX, prevY),
                            new Point(currX, currY));

                        if (distance < minDistance)
                        {
                            // Index of the first point of the line
                            closestPointIndex = prevIndex;
                            minDistance = distance;
                        }
                    }

                    prevX = currX;
                    prevY = currY;

                    prevIndex = index;
                }

                return closestPointIndex;
            }
        }

        public virtual void OnBeginRenderPass() { }
    }    
}