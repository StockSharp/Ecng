// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// HlcDataSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.Model.DataSeries
{
    /// <summary>
    /// Defines the interface to a High-Low-Close DataSeries, which contains columns of X-Values and Y-Values, High-Values and Low-Values
    /// </summary>    
    public interface IHlcDataSeries : IDataSeries
    {
        /// <summary>
        /// Gets the High Values of this DataSeries as a List of <see cref="IComparable"/>
        /// </summary>
        IList HighValues { get; }

        /// <summary>
        /// Gets the Low Values of this DataSeries as a List of <see cref="IComparable"/>
        /// </summary>
        IList LowValues { get; }
    }

    /// <summary>
    /// Defines the interface to a typed High-Low-Close DataSeries, which contains columns of X-Values and Y-Values, High-Values and Low-Values
    /// </summary>    
    /// <typeparam name="TX">The type of the X-data</typeparam>
    /// <typeparam name="TY">The type of the Y-data</typeparam>
    public interface IHlcDataSeries<TX,TY> : IDataSeries<TX,TY>, IHlcDataSeries 
        where TX:IComparable 
        where TY:IComparable
    {
        /// <summary>
        /// Gets the High Values of this DataSeries, if the data is OHLC
        /// </summary>
        new IList<TY> HighValues { get; }

        /// <summary>
        /// Gets the Low Values of this DataSeries, if the data is OHLC
        /// </summary>
        new IList<TY> LowValues { get; }
    }

    /// <summary>
    /// The HlcDataSeries provides a generic data-source in High-Low-Close format for Hlc charts as well as Error bar charts. See also <seealso cref="FastErrorBarsRenderableSeries"/>
    /// which requires this <see cref="DataSeries{TX,TY}"/> type as a Data-source. Any 2D renderable Series type such as <seealso cref="FastLineRenderableSeries"/> will render the X-Close value 
    /// as X-Y.
    /// </summary>
    /// <typeparam name="TX"></typeparam>
    /// <typeparam name="TY"></typeparam>
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
    /// <seealso cref="FastErrorBarsRenderableSeries"/>
    /// <remarks>DataSeries are assigned to the RenderableSeries via the <see cref="IRenderableSeries.DataSeries"/> property. Any time a DataSeries is appended to, the
    /// parent <see cref="UltrachartSurface"/> will be redrawn</remarks>
    public sealed class HlcDataSeries<TX, TY> : DataSeries<TX, TY>, IHlcDataSeries<TX,TY>
        where TX:IComparable
        where TY:IComparable
    {
        private ISeriesColumn<TY> _highColumn = new SeriesColumn<TY>();
        private ISeriesColumn<TY> _lowColumn = new SeriesColumn<TY>();

        readonly DataSeriesAppendBuffer<ValTuple<TX, TY, TY, TY>> _appendBuffer;

        /// <summary>
        /// Initializes a new instance of the <see cref="HlcDataSeries{TX,TY}" /> class.
        /// </summary>
        public HlcDataSeries()
        {
            _appendBuffer = new DataSeriesAppendBuffer<ValTuple<TX, TY, TY, TY>>(FlushAppendBuffer);
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
        /// Gets a value indicating whether this instance has values.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance has values; otherwise, <c>false</c>.
        /// </value>
        public override bool HasValues
        {
            get
            {
                return _xColumn.HasValues
                       && _yColumn.HasValues
                       && _highColumn.HasValues
                       && _lowColumn.HasValues;
            }
        }

        /// <summary>
        /// Gets the <see cref="DataSeriesType"/> for this DataSeries
        /// </summary>
        public override DataSeriesType DataSeriesType
        {
            get { return DataSeriesType.Hlc; }
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
                    hitTestInfo.ErrorHigh = ((IList<TY>)_highColumn)[index];
                    hitTestInfo.ErrorLow = ((IList<TY>)_lowColumn)[index];
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
                    _lowColumn = new FifoSeriesColumn<TY>(size);
                    _highColumn = new FifoSeriesColumn<TY>(size);
                    _yColumn = new FifoSeriesColumn<TY>(size);
                }
                else
                {
                    _xColumn = new SeriesColumn<TX>();
                    _lowColumn = new SeriesColumn<TY>();
                    _highColumn = new SeriesColumn<TY>();
                    _yColumn = new SeriesColumn<TY>();
                }

                ((ICollection<TX>)_xColumn).Clear();
                ((ICollection<TY>)_lowColumn).Clear();
                ((ICollection<TY>)_highColumn).Clear();
                ((ICollection<TY>)_yColumn).Clear();

                _appendBuffer.Do(b => b.Clear());
            }
        }

        /// <summary>
        /// Removes the X,Y values at the specified index
        /// </summary>
        /// <param name="index">The index to remove at</param>
        public override void RemoveAt(int index)
        {
            lock (SyncRoot)
            {
                _appendBuffer.Flush();

                //var y = YValues[index];
                //var x = XValues[index];

                XValues.RemoveAt(index);
                YValues.RemoveAt(index);

                // If removed y-value was YMax or YMin, recalculate YMax/YMin
                //bool needsYRecalc = y.CompareTo(YMax) == 0 || y.CompareTo(YMin) == 0;
                //
                //var high = ((IList<TY>)_highColumn)[index];
                //var low = ((IList<TY>)_lowColumn)[index];

                // If removed high/low-value was YMax or YMin, recalculate YMax/YMin
                //needsYRecalc |= high.CompareTo(YMax) == 0 || low.CompareTo(YMin) == 0;

                ((IList<TY>)_highColumn).RemoveAt(index);
                ((IList<TY>)_lowColumn).RemoveAt(index);

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

                _xColumn.RemoveRange(startIndex, count);
                _yColumn.RemoveRange(startIndex, count);

                _lowColumn.RemoveRange(startIndex, count);
                _highColumn.RemoveRange(startIndex, count);

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

                var dataSeries = new HlcDataSeries<TX, TY>();
                dataSeries.FifoCapacity = FifoCapacity;
                dataSeries.AcceptsUnsortedData = AcceptsUnsortedData;

                dataSeries.Append(XValues, YValues, _highColumn, _lowColumn);
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
                var midResample = resamplingMode == ResamplingMode.None ? ResamplingMode.None : ResamplingMode.Mid;
                var hResample = resamplingMode == ResamplingMode.None ? ResamplingMode.None : ResamplingMode.Max;
                var lResample = resamplingMode == ResamplingMode.None ? ResamplingMode.None : ResamplingMode.Min;

                var r = factory.GetPointResampler<TX, TY>();
                var yPoints = r.Execute(midResample, pointRange, viewportWidth, IsFifo, isCategoryAxis, _xColumn, _yColumn, DataDistributionCalculator.DataIsSortedAscending, DataDistributionCalculator.DataIsEvenlySpaced, dataIsDisplayedAs2D, visibleXRange);
                var highPoints = r.Execute(hResample, pointRange, viewportWidth, IsFifo, isCategoryAxis, _xColumn, _highColumn, DataDistributionCalculator.DataIsSortedAscending, DataDistributionCalculator.DataIsEvenlySpaced, dataIsDisplayedAs2D, visibleXRange);
                var lowPoints = r.Execute(lResample, pointRange, viewportWidth, IsFifo, isCategoryAxis, _xColumn, _lowColumn, DataDistributionCalculator.DataIsSortedAscending, DataDistributionCalculator.DataIsEvenlySpaced, dataIsDisplayedAs2D, visibleXRange);

                return new HlcPointSeries(yPoints, highPoints, lowPoints);
            }
        }

        public override void OnBeginRenderPass() {
            base.OnBeginRenderPass();
            // ReSharper disable once InconsistentlySynchronizedField
            _appendBuffer.Flush();
        }

        /// <summary>
        /// Gets the High Values of this DataSeries as a List of <see cref="IComparable"/>
        /// </summary>
        IList IHlcDataSeries.HighValues
        {
            get { return _highColumn; }
        }

        /// <summary>
        /// Gets the High Values of this DataSeries
        /// </summary>
        public IList<TY> HighValues
        {
            get { return _highColumn; }
        }

        /// <summary>
        /// Gets the Low Values of this DataSeries as a List of <see cref="IComparable"/>
        /// </summary>
        IList IHlcDataSeries.LowValues
        {
            get { return _lowColumn; }
        }

        /// <summary>
        /// Gets the Low Values of this DataSeries
        /// </summary>
        public IList<TY> LowValues
        {
            get { return _lowColumn; }
        }

        /// <summary>
        /// Appends an X, Y point to the series
        /// </summary>
        /// <exception cref="InvalidOperationException">Exception will be thrown if the count of y differ</exception>
        /// <param name="x">The X Value</param>
        /// <param name="yValues">The Y Values (depends on series type)</param>
        public override void Append(TX x, params TY[] yValues)
        {
            const int expectedYValuesCount = 3;

            if (yValues.Length != expectedYValuesCount)
            {
                ThrowWhenAppendInvalid(expectedYValuesCount);
            }

            Append(x, yValues[0], yValues[1], yValues[2]);
        }

        /// <summary>
        /// Appends a list of X, Y points to the series
        /// </summary>
        /// <exception cref="InvalidOperationException">Exception will be thrown if the count of x and each y differ</exception>
        /// <param name="x">The list of X points</param>
        /// <param name="yValues">Lists of Y points (depends on series type)</param>
        public override void Append(IEnumerable<TX> x, params IEnumerable<TY>[] yValues)
        {
            const int expectedYValuesCount = 3;

            if (yValues.Length != expectedYValuesCount)
            {
                ThrowWhenAppendInvalid(expectedYValuesCount);
            }

            Append(x, yValues[0], yValues[1], yValues[2]);
        }

        /// <summary>
        /// Appends an Open, High, Low, Close point to the series
        /// </summary>
        /// <param name="x">The X value</param>
        /// <param name="y">The Y value</param>
        /// <param name="high">The High value</param>
        /// <param name="low">The Low value</param>
        public void Append(TX x, TY y, TY high, TY low)
        {
            // ReSharper disable once InconsistentlySynchronizedField
            _appendBuffer.Append(ValTuple.Create(x, high, low, y));

            OnDataSeriesChanged(DataSeriesUpdate.DataChanged);
        }

        /// <summary>
        /// Appends a list of Open, High, Low, Close points to the series
        /// </summary>
        /// <param name="x">The list of X values</param>
        /// <param name="y">The list of Y values</param>
        /// <param name="high">The list of High values</param>
        /// <param name="low">The list of Low values</param>
        public void Append(IEnumerable<TX> x, IEnumerable<TY> y, IEnumerable<TY> high, IEnumerable<TY> low)
        {
            if (x.IsEmpty())
                return;

            var xe = x.GetEnumerator();
            var he = high.GetEnumerator();
            var le = low.GetEnumerator();
            var ce = y.GetEnumerator();

            lock(_appendBuffer.SyncRoot)
                while(xe.MoveNext() && he.MoveNext() && le.MoveNext() && ce.MoveNext())
                    _appendBuffer.Append(ValTuple.Create(xe.Current, he.Current, le.Current, ce.Current));
                
            OnDataSeriesChanged(DataSeriesUpdate.DataChanged);
        }

        /// <summary>
        /// Updates an Open, High, Low, Close point specified by the X-Value passed in. 
        /// </summary>
        /// <param name="x">The X Value to key on when updating</param>
        /// <param name="y">The Y value</param>
        /// <param name="high">The High value</param>
        /// <param name="low">The Low value</param>
        /// <exception cref="InvalidOperationException">Thrown if the x value is not in the DataSeries</exception>
        public void Update(TX x, TY y, TY high, TY low)
        {
            lock (SyncRoot)
            {
                _appendBuffer.Flush();

                int index = ((IList)_xColumn).FindIndex(IsSorted, x, SearchMode.Exact);
                if (index == -1)
                {
                    return;
                }

                ((IList<TY>)_yColumn)[index] = y;
                ((IList<TY>)_highColumn)[index] = high;
                ((IList<TY>)_lowColumn)[index] = low;

                OnDataSeriesChanged(DataSeriesUpdate.DataChanged);
            }
        }

        /// <summary>
        /// Inserts an Open, High, Low, Close point at the specified index
        /// </summary>
        /// <param name="index">The index to insert at</param>
        /// <param name="x">The X value</param>
        /// <param name="y">The Y value</param>
        /// <param name="high">The High value</param>
        /// <param name="low">The low value</param>
        public void Insert(int index, TX x, TY y, TY high, TY low)
        {
            lock (SyncRoot)
            {
                _appendBuffer.Flush();

                XValues.Insert(index, x);
                _yColumn.Insert(index, y);
                _highColumn.Insert(index, high);
                _lowColumn.Insert(index, low);

                OnDataSeriesChanged(DataSeriesUpdate.DataChanged);
                DataDistributionCalculator.OnInsertXValue(_xColumn, index, x, AcceptsUnsortedData);
            }
        }

        /// <summary>
        /// Inserts a list of Open, High, Low, Close points at the specified index
        /// </summary>
        /// <param name="startIndex">The index to insert at</param>
        /// <param name="x">The list of X values</param>
        /// <param name="y">The list of y values</param>
        /// <param name="high">The list of High values</param>
        /// <param name="low">The list of Low values</param>
        public void InsertRange(int startIndex, IEnumerable<TX> x, IEnumerable<TY> y, IEnumerable<TY> high, IEnumerable<TY> low)
        {
            lock (SyncRoot)
            {
                _appendBuffer.Flush();

                var xCountBeforeInserting = ((ISeriesColumn) _xColumn).Count;
                _xColumn.InsertRange(startIndex, x);
                var xCountAfterInserting = ((ISeriesColumn)_xColumn).Count;
                _yColumn.InsertRange(startIndex, y);
                _highColumn.InsertRange(startIndex, high);
                _lowColumn.InsertRange(startIndex, low);

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
            TY high = ((IList<TY>)_highColumn)[index];
            return YMath.IsNaN(high) == false ? YMath.Max(existingYMax, high) : existingYMax;
        }

        /// <summary>
        /// When overriden in a derived class, gets the Min(existingYMin, currentMin), where currentMin is the minimum at the specified index
        /// </summary>
        /// <param name="index">The index to the underlying dataset</param>
        /// <param name="existingYMin">The existing minimum</param>
        /// <returns>The new YMin, which is the Min(existingYMin, currentMin)</returns>
        public override TY GetYMinAt(int index, TY existingYMin)
        {
            TY low = ((IList<TY>)_lowColumn)[index];
            return YMath.IsNaN(low) == false ? YMath.Min(existingYMin, low) : existingYMin;
        }

        void FlushAppendBuffer(IList<ValTuple<TX, TY, TY, TY>> bufferedValues) {
            lock (SyncRoot) {
                var newX = bufferedValues.Select(b => b.Item1);
                var xCountBeforeAppending = ((ISeriesColumn) _xColumn).Count;

                _xColumn.AddRange(newX);
                _highColumn.AddRange(bufferedValues.Select(b => b.Item2));
                _lowColumn.AddRange(bufferedValues.Select(b => b.Item3));
                _yColumn.AddRange(bufferedValues.Select(b => b.Item4));
                DataDistributionCalculator.OnAppendXValues(_xColumn, xCountBeforeAppending, newX, AcceptsUnsortedData);
            }
        }
    }
}