// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// OhlcDataSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Linq;
using Ecng.Common;
using Ecng.Xaml.Charting.Common;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Numerics;
using Ecng.Xaml.Charting.Numerics.GenericMath;
using Ecng.Xaml.Charting.Numerics.PointResamplers;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.Model.DataSeries
{
    /// <summary>
    /// A DataSeries to store OHLC data-points, containing X and Y-Open, Y-High, Y-Low, Y-Close values. 
    /// May be used as a DataSource for <seealso cref="FastCandlestickRenderableSeries"/> and <seealso cref="FastOhlcRenderableSeries"/> as well as standard XY renderable series types
    /// </summary>
    public class OhlcDataSeries<TX, TY> : DataSeries<TX, TY>, IOhlcDataSeries<TX, TY>
        where TX : IComparable
        where TY : IComparable
    {
        private ISeriesColumn<TY> _openColumn = new SeriesColumn<TY>();
        private ISeriesColumn<TY> _highColumn = new SeriesColumn<TY>();
        private ISeriesColumn<TY> _lowColumn = new SeriesColumn<TY>();
        private ISeriesColumn<TY> _closeColumn;

        readonly DataSeriesAppendBuffer<ValTuple<TX, TY, TY, TY, TY>> _appendBuffer;

        /// <summary>
        /// Initializes a new instance of the <see cref="OhlcDataSeries{TX,TY}" /> class.
        /// </summary>
        public OhlcDataSeries()
        {
            _closeColumn = _yColumn;
            _appendBuffer = new DataSeriesAppendBuffer<ValTuple<TX, TY, TY, TY, TY>>(FlushAppendBuffer);
        }

        /// <summary>
        /// Gets the computed YRange, which is an <see cref="IRange"/> wrapping YMin and YMax properties
        /// </summary>
        /// <remarks>Note: The performance implications of calling this is the DataSeries will perform a full recalculation on each get. It is recommended to get and cache if this property is needed more than once</remarks>
        public override IRange YRange
        {
            get
            {
                TY max = ArrayOperations.Maximum<TY>(_highColumn);
                TY min = ArrayOperations.Minimum<TY>(_lowColumn);

                return RangeFactory.NewRange(min, max);
            }
        }

        /// <summary>
        /// Gets whether the Data Series has values (is not empty)
        /// </summary>
        /// <remarks></remarks>
        public override bool HasValues => 
                                _xColumn.HasValues && 
                                _openColumn.HasValues && 
                                _highColumn.HasValues &&
                                _lowColumn.HasValues &&
                                _closeColumn.HasValues;

        /// <summary>
        /// Gets the <see cref="DataSeriesType"/> for this DataSeries
        /// </summary>
        public override DataSeriesType DataSeriesType
        {
            get { return DataSeriesType.Ohlc; }
        }

        /// <summary>
        /// When overridden in a derived class, returns a <see cref="HitTestInfo"/> struct containing data about the data-point at the specified index
        /// </summary>
        /// <param name="index">The index to the DataSeries</param>
        /// <returns>The HitTestInfo</returns>
        public override HitTestInfo ToHitTestInfo(int index)
        {
            lock (SyncRoot)
            {
                var hitTestInfo = base.ToHitTestInfo(index);

                if (!hitTestInfo.IsEmpty())
                {
                    hitTestInfo.OpenValue = OpenValues[index];
                    hitTestInfo.HighValue = HighValues[index];
                    hitTestInfo.LowValue = LowValues[index];
                    hitTestInfo.CloseValue = CloseValues[index];
                }

                return hitTestInfo;
            }
        }

        /// <summary>
        /// When overridden in a derived class, clears all columns in the Data Series
        /// </summary>
        protected override void ClearColumns()
        {
            lock(SyncRoot) {
                bool isFifo = FifoCapacity.HasValue;
                if (isFifo)
                {
                    int size = FifoCapacity.Value;

                    _xColumn = new FifoSeriesColumn<TX>(size);
                    _openColumn = new FifoSeriesColumn<TY>(size);
                    _highColumn = new FifoSeriesColumn<TY>(size);
                    _lowColumn = new FifoSeriesColumn<TY>(size);
                    _yColumn = new FifoSeriesColumn<TY>(size);
                    _closeColumn = _yColumn;
                }
                else
                {
                    _xColumn = new SeriesColumn<TX>();
                    _openColumn = new SeriesColumn<TY>();
                    _highColumn = new SeriesColumn<TY>();
                    _lowColumn = new SeriesColumn<TY>();
                    _yColumn = new SeriesColumn<TY>();
                    _closeColumn = _yColumn;
                }

                ((ICollection<TX>)_xColumn).Clear();
                ((ICollection<TY>)_openColumn).Clear();
                ((ICollection<TY>)_highColumn).Clear();
                ((ICollection<TY>)_lowColumn).Clear();
                ((ICollection<TY>)_closeColumn).Clear();

                _appendBuffer.Do(b => b.Clear());
            }
        }

        /// <summary>
        /// Removes the X,Y values at the specified index
        /// </summary>
        /// <param name="index">The index to remove at</param>
        public override void RemoveAt(int index)
        {
            lock (SyncRoot) {
                _appendBuffer.Flush();

                //var y = YValues[index];
                //var x = XValues[index];

                XValues.RemoveAt(index);
                YValues.RemoveAt(index);

                // If removed y-value was YMax or YMin, recalculate YMax/YMin
                //bool needsYRecalc = y.CompareTo(YMax) == 0 || y.CompareTo(YMin) == 0;

                //var high = HighValues[index];
                //var low = LowValues[index];

                // If removed high/low-value was YMax or YMin, recalculate YMax/YMin
                //needsYRecalc |= high.CompareTo(YMax) == 0 || low.CompareTo(YMin) == 0;

                OpenValues.RemoveAt(index);
                HighValues.RemoveAt(index);
                LowValues.RemoveAt(index);

                //_xMin = XValues.Count > 0 ? XValues[0] : ComparableUtil.MaxValue<TX>();
                //_xMax = XValues.Count > 0 ? XValues[XValues.Count - 1] : ComparableUtil.MinValue<TX>();
                //
                //if (needsYRecalc)
                //{
                //    RecalculateYMinMaxFull();
                //}
                //
                //TryRecalculatePositiveMinimumAt(x, low);

                OnDataSeriesChanged(DataSeriesUpdate.DataChanged);
                DataDistributionCalculator.UpdateDataDistributionFlagsWhenRemovedXValues();
            }      
        }

        /// <summary>
        /// Removes a range of points starting from the specified index
        /// </summary>
        /// <param name="startIndex">Starting index of the range of elements to remove</param>
        /// <param name="count">The number of elements to remove</param>
        public override void RemoveRange(int startIndex, int count)
        {
            lock(SyncRoot) {
                _appendBuffer.Flush();

                _xColumn.RemoveRange(startIndex,count);
                _yColumn.RemoveRange(startIndex,count);

                _openColumn.RemoveRange(startIndex,count);
                _highColumn.RemoveRange(startIndex,count);
                _lowColumn.RemoveRange(startIndex,count);

                //_xMin = XValues.Count > 0 ? XValues[0] : ComparableUtil.MaxValue<TX>();
                //_xMax = XValues.Count > 0 ? XValues[XValues.Count - 1] : ComparableUtil.MinValue<TX>();
                //RecalculateXPositiveMinimum();
                //
                //RecalculateYMinMaxFull();

                OnDataSeriesChanged(DataSeriesUpdate.DataChanged);
                DataDistributionCalculator.UpdateDataDistributionFlagsWhenRemovedXValues();
            }
        }

        /// <summary>
        /// Creates a deep copy of a DataSeries
        /// </summary>
        /// <returns></returns>
        public override IDataSeries<TX, TY> Clone()
        {
            lock (SyncRoot)
            {
                _appendBuffer.Flush();

                var dataSeries = new OhlcDataSeries<TX, TY>();
                dataSeries.FifoCapacity = FifoCapacity;
                dataSeries.AcceptsUnsortedData = AcceptsUnsortedData;

                dataSeries.Append(XValues, OpenValues, HighValues, LowValues, CloseValues);
                return dataSeries;
            }
        }

        /// <summary>
        /// Converts the default <see cref="IDataSeries.YValues"/> to an <see cref="IPointSeries"/> which is used to render XY series
        /// </summary>
        /// <param name="resamplingMode">The desired <see cref="ResamplingMode"/></param>
        /// <param name="pointRange">The integer Indices range in the parent data-set</param>
        /// <param name="viewportWidth">The current width of the viewport</param>
        /// <param name="isCategoryAxis">If true, uses the indices to form the resampled X-values, else uses the X-Values themselves</param>
        /// <param name="dataIsDisplayedAs2D">If true, then data is presented as a scatter series without relationship between the points, e.g. not a line series </param>
        /// <param name="visibleXRange">The XAxis VisibleRange at the time of resampling</param>
        /// <param name="factory">The <see cref="IPointResamplerFactory"/> Instance</param>
        /// <returns>
        /// A <see cref="IPointSeries"/> which is used to render XY series
        /// </returns>
        public override IPointSeries ToPointSeries(ResamplingMode resamplingMode, IndexRange pointRange, int viewportWidth, bool isCategoryAxis, bool? dataIsDisplayedAs2D, IRange visibleXRange, IPointResamplerFactory factory)
        {
            lock (SyncRoot)
            {
                // Either Mid or None. Cannot have mix MinMax with Max/Min as they result in different numbers of points
                var ocResample = resamplingMode == ResamplingMode.None ? ResamplingMode.None : ResamplingMode.Mid;
                var hResample = resamplingMode == ResamplingMode.None ? ResamplingMode.None : ResamplingMode.Max;
                var lResample = resamplingMode == ResamplingMode.None ? ResamplingMode.None : ResamplingMode.Min;

                var r = factory.GetPointResampler<TX, TY>();
                var openPoints = r.Execute(ocResample, pointRange, viewportWidth, IsFifo, isCategoryAxis, _xColumn, _openColumn, DataDistributionCalculator.DataIsSortedAscending, DataDistributionCalculator.DataIsEvenlySpaced, dataIsDisplayedAs2D, visibleXRange);
                var highPoints = r.Execute(hResample, pointRange, viewportWidth, IsFifo, isCategoryAxis, _xColumn, _highColumn, DataDistributionCalculator.DataIsSortedAscending, DataDistributionCalculator.DataIsEvenlySpaced, dataIsDisplayedAs2D, visibleXRange);
                var lowPoints = r.Execute(lResample, pointRange, viewportWidth, IsFifo, isCategoryAxis, _xColumn, _lowColumn, DataDistributionCalculator.DataIsSortedAscending, DataDistributionCalculator.DataIsEvenlySpaced, dataIsDisplayedAs2D, visibleXRange);
                var closePoints = r.Execute(ocResample, pointRange, viewportWidth, IsFifo, isCategoryAxis, _xColumn, _closeColumn, DataDistributionCalculator.DataIsSortedAscending, DataDistributionCalculator.DataIsEvenlySpaced, dataIsDisplayedAs2D, visibleXRange);

                return new OhlcPointSeries(openPoints, highPoints, lowPoints, closePoints);
            }
        }

        public override void OnBeginRenderPass() {
            base.OnBeginRenderPass();
            // ReSharper disable once InconsistentlySynchronizedField
            _appendBuffer.Flush();
        }

        /// <summary>
        /// Gets the Open Values of this DataSeries
        /// </summary>
        IList IOhlcDataSeries.OpenValues
        {
            get { return _openColumn; }
        }

        /// <summary>
        /// Gets the High Values of this DataSeries as a List of <see cref="IComparable"/>
        /// </summary>
        IList IOhlcDataSeries.HighValues
        {
            get { return _highColumn; }
        }

        /// <summary>
        /// Gets the Low Values of this DataSeries as a List of <see cref="IComparable"/>
        /// </summary>
        IList IOhlcDataSeries.LowValues
        {
            get { return _lowColumn; }
        }

        /// <summary>
        /// Gets the Close Values of this DataSeries as a List of <see cref="IComparable"/>
        /// </summary>
        /// <remarks>Close equates to Y Values in either OHLC or simple XY dataseries</remarks>
        IList IOhlcDataSeries.CloseValues
        {
            get { return _closeColumn; }
        }

        /// <summary>
        /// Gets the Open Values of this DataSeries, if the data is OHLC
        /// </summary>
        public IList<TY> OpenValues
        {
            get { return _openColumn; }
        }

        /// <summary>
        /// Gets the High Values of this DataSeries, if the data is OHLC
        /// </summary>
        public IList<TY> HighValues
        {
            get { return _highColumn; }
        }

        /// <summary>
        /// Gets the Low Values of this DataSeries, if the data is OHLC
        /// </summary>
        public IList<TY> LowValues
        {
            get { return _lowColumn; }
        }

        /// <summary>
        /// Gets the Close Values of this DataSeries, if the data is OHLC
        /// </summary>
        /// <remarks>Close equates to Y Values in either OHLC or simple XY dataseries</remarks>
        public IList<TY> CloseValues
        {
            get { return _closeColumn; }
        }

        /// <summary>
        /// Appends an X, Y point to the series
        /// </summary>
        /// <exception cref="InvalidOperationException">Exception will be thrown if the count of y differ</exception>
        /// <param name="x">The X Value</param>
        /// <param name="yValues">The Y Values (depends on series type)</param>
        public override void Append(TX x, params TY[] yValues)
        {
            const int expectedYValuesCount = 4;

            if (yValues.Length != expectedYValuesCount)
            {
                ThrowWhenAppendInvalid(expectedYValuesCount);
            }

            Append(x, yValues[0], yValues[1], yValues[2], yValues[3]);
        }

        /// <summary>
        /// Appends a list of X, Y points to the series
        /// </summary>
        /// <exception cref="InvalidOperationException">Exception will be thrown if the count of x and each y differ</exception>
        /// <param name="x">The list of X points</param>
        /// <param name="yValues">Lists of Y points (depends on series type)</param>
        public override void Append(IEnumerable<TX> x, params IEnumerable<TY>[] yValues)
        {
            const int expectedYValuesCount = 4;

            if (yValues.Length != expectedYValuesCount)
            {
                ThrowWhenAppendInvalid(expectedYValuesCount);
            }

            Append(x, yValues[0], yValues[1], yValues[2], yValues[3]);
        }

        /// <summary>
        /// Appends an Open, High, Low, Close point to the series
        /// </summary>
        /// <param name="x">The X value</param>
        /// <param name="open">The Open value</param>
        /// <param name="high">The High value</param>
        /// <param name="low">The Low value</param>
        /// <param name="close">The Close value</param>
        public void Append(TX x, TY open, TY high, TY low, TY close) {
            // ReSharper disable once InconsistentlySynchronizedField
            _appendBuffer.Append(ValTuple.Create(x, open, high, low, close));

            OnDataSeriesChanged(DataSeriesUpdate.DataChanged);
        }

        /// <summary>
        /// Appends a list of Open, High, Low, Close points to the series
        /// </summary>
        /// <param name="x">The list of X values</param>
        /// <param name="open">The list of Open values</param>
        /// <param name="high">The list of High values</param>
        /// <param name="low">The list of Low values</param>
        /// <param name="close">The list of Close values</param>
        public void Append(IEnumerable<TX> x, IEnumerable<TY> open, IEnumerable<TY> high, IEnumerable<TY> low, IEnumerable<TY> close)
        {
            if (x.IsEmpty())
                return;

            var xe = x.GetEnumerator();
            var oe = open.GetEnumerator();
            var he = high.GetEnumerator();
            var le = low.GetEnumerator();
            var ce = close.GetEnumerator();

            lock(_appendBuffer.SyncRoot)
                while(xe.MoveNext() && oe.MoveNext() && he.MoveNext() && le.MoveNext() && ce.MoveNext())
                    _appendBuffer.Append(ValTuple.Create(xe.Current, oe.Current, he.Current, le.Current, ce.Current));
                
            OnDataSeriesChanged(DataSeriesUpdate.DataChanged);
        }

        /// <summary>
        /// Updates an Open, High, Low, Close point specified by the X-Value passed in. 
        /// </summary>
        /// <param name="x">The X Value to key on when updating</param>
        /// <param name="open">The Open value</param>
        /// <param name="high">The High value</param>
        /// <param name="low">The Low value</param>
        /// <param name="close">The Close value</param>
        /// <exception cref="InvalidOperationException">Thrown if the x value is not in the DataSeries</exception>
        public void Update(TX x, TY open, TY high, TY low, TY close)
        {
            lock (SyncRoot)
            {
                _appendBuffer.Flush();

                int index = ((IList)_xColumn).FindIndex(IsSorted, x, SearchMode.Exact);
                if (index == -1)
                {
                    return;
                }

                ((IList<TY>)_openColumn)[index] = open;
                ((IList<TY>)_highColumn)[index] = high;
                ((IList<TY>)_lowColumn)[index] = low;
                ((IList<TY>)_closeColumn)[index] = close;

                //_yMax = ComputeMax(_yMax, high, _closeColumn);
                //_yMin = ComputeMin(_yMin, low, _closeColumn);
                //
                //var min = ComputeMin(_yMinPositive, low, _closeColumn);
                //_yMinPositive = GetPositiveMin(_yMinPositive, min);
                //
                //_xMin = ((IList<TX>)_xColumn)[0];
                //_xMax = x;
                //
                //var xMin = ComputeMin(_xMinPositive, x, _xColumn);
                //_xMinPositive = GetPositiveMin(_xMinPositive, xMin);

                OnDataSeriesChanged(DataSeriesUpdate.DataChanged);
            }
        }

        /// <summary>
        /// Inserts an Open, High, Low, Close point at the specified index
        /// </summary>
        /// <param name="index">The index to insert at</param>
        /// <param name="x">The X value</param>
        /// <param name="open">The Open value</param>
        /// <param name="high">The High value</param>
        /// <param name="low">The low value</param>
        /// <param name="close">The close value</param>
        public void Insert(int index, TX x, TY open, TY high, TY low, TY close)
        {
            lock (SyncRoot)
            {
                _appendBuffer.Flush();

                XValues.Insert(index, x);
                OpenValues.Insert(index, open);
                HighValues.Insert(index, high);
                LowValues.Insert(index, low);
                CloseValues.Insert(index, close);

                //_yMax = ComputeMax(_yMax, high, _closeColumn);
                //_yMin = ComputeMin(_yMin, low, _closeColumn);
                //
                //var min = ComputeMin(_yMinPositive, low, _closeColumn);
                //_yMinPositive = GetPositiveMin(_yMinPositive, min);
                //
                //_xMin = ((IList<TX>)_xColumn)[0];
                //_xMax = ((IList<TX>)_xColumn)[XValues.Count - 1];
                //
                //var xMin = ComputeMin(_xMinPositive, x, _xColumn);
                //_xMinPositive = GetPositiveMin(_xMinPositive, xMin);

                OnDataSeriesChanged(DataSeriesUpdate.DataChanged);
                DataDistributionCalculator.OnInsertXValue(_xColumn, index, x, AcceptsUnsortedData);
            }
        }

        /// <summary>
        /// Inserts a list of Open, High, Low, Close points at the specified index
        /// </summary>
        /// <param name="startIndex">The index to insert at</param>
        /// <param name="x">The list of X values</param>
        /// <param name="open">The list of Open values</param>
        /// <param name="high">The list of High values</param>
        /// <param name="low">The list of Low values</param>
        /// <param name="close">The list of Close values</param>
        public void InsertRange(int startIndex, IEnumerable<TX> x, IEnumerable<TY> open, IEnumerable<TY> high, IEnumerable<TY> low, IEnumerable<TY> close)
        {
            //TY highMax = ArrayOperations.Maximum(high);
            //TY lowMin = ArrayOperations.Minimum(low);
            //
            //TY lowPositiveMin = _yMinPositive;
            //if (typeof(TY) != typeof(DateTime))
            //{
            //    var zero = (TY)Convert.ChangeType(0d, typeof(TY), CultureInfo.InvariantCulture);
            //    lowPositiveMin = ArrayOperations.MinGreaterThan(low, zero);
            //}

            lock (SyncRoot)
            {
                _appendBuffer.Flush();

                var xCountBeforeInserting = ((ISeriesColumn)_xColumn).Count;
                _xColumn.InsertRange(startIndex, x);
                var xCountAfterInserting = ((ISeriesColumn)_xColumn).Count;
                _openColumn.InsertRange(startIndex, open);
                _highColumn.InsertRange(startIndex, high);
                _lowColumn.InsertRange(startIndex, low);
                _closeColumn.InsertRange(startIndex, close);

                //_yMax = _yMath.Max(_yMax, highMax);
                //_yMin = _yMath.Min(_yMin, lowMin);
                //
                //var min = _yMath.Min(_yMinPositive, lowPositiveMin);
                //_yMinPositive = GetPositiveMin(_yMinPositive, min);
                //
                //_xMin = ((IList<TX>)_xColumn)[0];
                //_xMax = ((IList<TX>)_xColumn)[XValues.Count - 1]; 
                //
                //var xMin = ComputeMin(_xMinPositive, ArrayOperations.MinGreaterThan(x, default(TX)), _xColumn);
                //_xMinPositive = GetPositiveMin(_xMinPositive, xMin);

                OnDataSeriesChanged(DataSeriesUpdate.DataChanged);
                DataDistributionCalculator.OnInsertXValues(_xColumn, startIndex, xCountAfterInserting - xCountBeforeInserting, x, AcceptsUnsortedData);
            }
        }

        /// <summary>
        /// When overriden in a derived class, gets the Max(existingYMax, currentMax), where currentMax is the maximum at the specified index
        /// </summary>
        /// <param name="index">The index to the underlying dataset</param>
        /// <param name="existingYMax">The existing maximum</param>
        /// <returns>The new YMax, which is the Min(existingYMax, currentMax)</returns>
        public override TY GetYMaxAt(int index, TY existingYMax)
        {
            TY high = HighValues[index];
            return YMath.IsNaN(high) == false ? YMath.Max(existingYMax, high): existingYMax;
        }

        /// <summary>
        /// When overriden in a derived class, gets the Min(existingYMin, currentMin), where currentMin is the minimum at the specified index
        /// </summary>
        /// <param name="index">The index to the underlying dataset</param>
        /// <param name="existingYMin">The existing minimum</param>
        /// <returns>The new YMin, which is the Min(existingYMin, currentMin)</returns>
        public override TY GetYMinAt(int index, TY existingYMin)
        {
            TY low = LowValues[index];
            return YMath.IsNaN(low) == false ? YMath.Min(existingYMin, low) : existingYMin;
        }

        void FlushAppendBuffer(IList<ValTuple<TX, TY, TY, TY, TY>> bufferedValues) {
            lock (SyncRoot) {
                var newX = bufferedValues.Select(b => b.Item1);
                var xCountBeforeAppending = ((ISeriesColumn) _xColumn).Count;

                _xColumn.AddRange(newX);
                _openColumn.AddRange(bufferedValues.Select(b => b.Item2));
                _highColumn.AddRange(bufferedValues.Select(b => b.Item3));
                _lowColumn.AddRange(bufferedValues.Select(b => b.Item4));
                _closeColumn.AddRange(bufferedValues.Select(b => b.Item5));
                DataDistributionCalculator.OnAppendXValues(_xColumn, xCountBeforeAppending, newX, AcceptsUnsortedData);
            }
        }
    }
}
