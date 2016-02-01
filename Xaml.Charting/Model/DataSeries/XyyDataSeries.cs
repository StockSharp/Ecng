// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// XyyDataSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
    /// A DataSeries to store Xyy data-points, containing X and Y0, Y1 values
    /// May be used as a DataSource for <seealso cref="FastBandRenderableSeries"/> as well as standard XY renderable series types
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
    public class XyyDataSeries<TX, TY> : DataSeries<TX, TY>, IXyyDataSeries<TX, TY>
        where TX : IComparable
        where TY : IComparable
    {
        private ISeriesColumn<TY> _y1Column = new SeriesColumn<TY>();

        readonly DataSeriesAppendBuffer<ValTuple<TX, TY, TY>> _appendBuffer;

        /// <summary>
        /// Initializes a new instance of the <see cref="XyyDataSeries{TX,TY}" /> class.
        /// </summary>
        public XyyDataSeries()
        {
            _appendBuffer = new DataSeriesAppendBuffer<ValTuple<TX, TY, TY>>(FlushAppendBuffer);
        }

        /// <summary>
        /// Gets the computed YRange, which is an <see cref="IRange"/> wrapping YMin and YMax properties
        /// </summary>
        /// <remarks>Note: The performance implications of calling this is the DataSeries will perform a full recalculation on each get. It is recommended to get and cache if this property is needed more than once</remarks>
        public override IRange YRange
        {
            get
            {
                TY min = default(TY), max = default(TY), min1 = default(TY), max1 = default(TY);
                ArrayOperations.MinMax<TY>(_yColumn, out min, out max);
                ArrayOperations.MinMax<TY>(_y1Column, out min1, out max1);

                min = YMath.Min(min, min1);
                max = YMath.Max(max, max1);

                return RangeFactory.NewRange(min, max);
            }
        }

        /// <summary>
        /// Gets the <see cref="DataSeriesType"/> for this DataSeries
        /// </summary>
        public override DataSeriesType DataSeriesType
        {
            get { return DataSeriesType.Xyy; }
        }

        /// <summary>
        /// Gets the Y1 Values as a list of <see cref="IComparable"/>
        /// </summary>
        IList IXyyDataSeries.Y1Values
        {
            get { return _y1Column; }
        }

        /// <summary>
        /// Gets the Y1 values
        /// </summary>
        public IList<TY> Y1Values
        {
            get { return _y1Column; }
        }

        /// <summary>
        /// Gets whether the Data Series has values (is not empty)
        /// </summary>
        /// <remarks></remarks>
        public override bool HasValues
        {
            get { return _xColumn.HasValues && _yColumn.HasValues && _y1Column.HasValues; }
        }

        /// <summary>
        /// Removes the X,Y values at the specified index
        /// </summary>
        /// <param name="index">The index to remove at</param>
        public override void RemoveAt(int index)
        {
            lock(SyncRoot)
            {
                _appendBuffer.Flush();

                var y0 = YValues[index];
                var y1 = Y1Values[index];
                var x = XValues[index];

                XValues.RemoveAt(index);
                YValues.RemoveAt(index);
                Y1Values.RemoveAt(index);

                //bool needsYRecalc = y0.CompareTo(YMax) == 0 || y0.CompareTo(YMin) == 0 ||
                //                    y1.CompareTo(YMax) == 0 || y1.CompareTo(YMin) == 0;
                //
                //_xMin = XValues.Count > 0 ? XValues[0] : ComparableUtil.MaxValue<TX>();
                //_xMax = XValues.Count > 0 ? XValues[XValues.Count - 1] : ComparableUtil.MinValue<TX>();
                //
                //if (needsYRecalc)
                //{
                //    RecalculateYMinMaxFull();
                //}
                //
                //TryRecalculatePositiveMinimumAt(x, _yMath.Min(y0, y1));

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
                _yColumn.RemoveRange(startIndex, count);
                _y1Column.RemoveRange(startIndex, count);

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

                var dataSeries = new XyyDataSeries<TX, TY>();
                dataSeries.FifoCapacity = FifoCapacity;
                dataSeries.AcceptsUnsortedData = AcceptsUnsortedData;

                dataSeries.Append(XValues, YValues, Y1Values);
                return dataSeries;
            }
        }

        /// <summary>
        /// When overriden in a derived class, gets the Min(existingYMin, currentMin), where currentMin is the minimum at the specified index
        /// </summary>
        /// <param name="index">The index to the underlying dataset</param>
        /// <param name="existingYMin">The existing minimum</param>
        /// <returns>The new YMin, which is the Min(existingYMin, currentMin)</returns>
        public override TY GetYMinAt(int index, TY existingYMin)
        {
            var y1 = Y1Values[index];
            var y = YValues[index];

            return YMath.IsNaN(y) || YMath.IsNaN(y1)
                       ? existingYMin
                       : YMath.Min(YMath.Min(y, y1), existingYMin);
        }

        /// <summary>
        /// When overriden in a derived class, gets the Max(existingYMax, currentMax), where currentMax is the maximum at the specified index
        /// </summary>
        /// <param name="index">The index to the underlying dataset</param>
        /// <param name="existingYMax">The existing maximum</param>
        /// <returns>The new YMax, which is the Min(existingYMax, currentMax)</returns>
        public override TY GetYMaxAt(int index, TY existingYMax)
        {
            var y1 = Y1Values[index];
            var y = YValues[index];

            return YMath.IsNaN(y) || YMath.IsNaN(y1)
                       ? existingYMax
                       : YMath.Max(YMath.Max(y, y1), existingYMax);
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
        public override IPointSeries ToPointSeries(ResamplingMode resamplingMode, IndexRange pointRange, int viewportWidth, bool isCategoryAxis, bool? dataIsDisplayedAs2D, IRange visibleXRange, IPointResamplerFactory factory, object pointSeriesArg = null)
        {
            lock (SyncRoot)
            {
                var yPoints = factory.GetPointResampler<TX, TY>().Execute(resamplingMode, pointRange, viewportWidth, IsFifo, isCategoryAxis, _xColumn, _yColumn, DataDistributionCalculator.DataIsSortedAscending, DataDistributionCalculator.DataIsEvenlySpaced, dataIsDisplayedAs2D, visibleXRange);
                var y1Points = factory.GetPointResampler<TX, TY>().Execute(resamplingMode, pointRange, viewportWidth, IsFifo, isCategoryAxis, _xColumn, _y1Column, DataDistributionCalculator.DataIsSortedAscending, DataDistributionCalculator.DataIsEvenlySpaced, dataIsDisplayedAs2D, visibleXRange);

                return new XyyPointSeries(yPoints, y1Points);
            }
        }

        public override void OnBeginRenderPass() {
            base.OnBeginRenderPass();
            // ReSharper disable once InconsistentlySynchronizedField
            _appendBuffer.Flush();
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
                    var y1Value = Y1Values[index];
                    hitTestInfo.Y1Value = y1Value;
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
                    _yColumn = new FifoSeriesColumn<TY>(size);
                    _y1Column = new FifoSeriesColumn<TY>(size);

                }
                else
                {
                    _xColumn = new SeriesColumn<TX>();
                    _yColumn = new SeriesColumn<TY>();
                    _y1Column = new SeriesColumn<TY>();
                }

                ((ICollection<TX>)_xColumn).Clear();
                ((ICollection<TY>)_yColumn).Clear();
                ((ICollection<TY>)_y1Column).Clear();

                _appendBuffer.Do(b => b.Clear());
            }
        }

        /// <summary>
        /// Appends an X, Y point to the series
        /// </summary>
        /// <exception cref="InvalidOperationException">Exception will be thrown if the count of y differ</exception>
        /// <param name="x">The X Value</param>
        /// <param name="yValues">The Y Values (depends on series type)</param>
        public override void Append(TX x, params TY[] yValues)
        {
            const int expectedYValuesCount = 2;

            if (yValues.Length != expectedYValuesCount)
            {
                ThrowWhenAppendInvalid(expectedYValuesCount);
            }

            Append(x, yValues[0], yValues[1]);
        }

        /// <summary>
        /// Appends a list of X, Y points to the series
        /// </summary>
        /// <exception cref="InvalidOperationException">Exception will be thrown if the count of x and each y differ</exception>
        /// <param name="x">The list of X points</param>
        /// <param name="yValues">Lists of Y points (depends on series type)</param>
        public override void Append(IEnumerable<TX> x, params IEnumerable<TY>[] yValues)
        {
            const int expectedYValuesCount = 2;

            if (yValues.Length != expectedYValuesCount)
            {
                ThrowWhenAppendInvalid(expectedYValuesCount);
            }

            Append(x, yValues[0], yValues[1]);
        }


        /// <summary>
        /// Appends a single X, Y0, Y1 point to the series, automatically triggering a redraw
        /// </summary>
        /// <param name="x">The X-value</param>
        /// <param name="y0">The Y0-value</param>
        /// <param name="y1">The Y1-value</param>
        public void Append(TX x, TY y0, TY y1)
        {
            // ReSharper disable once InconsistentlySynchronizedField
            _appendBuffer.Append(ValTuple.Create(x, y0, y1));

            OnDataSeriesChanged(DataSeriesUpdate.DataChanged);
        }

        /// <summary>
        /// Appends a collection of X, Y0 and Y1 points to the series, automatically triggering a redraw
        /// </summary>
        /// <param name="x">The X-values</param>
        /// <param name="y0">The Y0-values</param>
        /// <param name="y1">The Y1-values</param>
        public void Append(IEnumerable<TX> x, IEnumerable<TY> y0, IEnumerable<TY> y1)
        {
            if (x.IsEmpty())
                return;

            var xe = x.GetEnumerator();
            var y0e = y0.GetEnumerator();
            var y1e = y1.GetEnumerator();

            lock(_appendBuffer.SyncRoot)
                while(xe.MoveNext() && y0e.MoveNext() && y1e.MoveNext())
                    _appendBuffer.Append(ValTuple.Create(xe.Current, y0e.Current, y1e.Current));
                
            OnDataSeriesChanged(DataSeriesUpdate.DataChanged);
        }

        /// <summary>
        /// Updates (overwrites) the Y0, Y1 values at the specified X-value. Automatically triggers a redraw
        /// </summary>
        /// <param name="x">The X-value</param>
        /// <param name="y0">The Y0-value</param>
        /// <param name="y1">The Y1-value</param>
        public void Update(TX x, TY y0, TY y1)
        {
            lock (SyncRoot)
            {
                _appendBuffer.Flush();

                int index = ((IList)_xColumn).FindIndex(IsSorted, x, SearchMode.Exact);
                if (index == -1)
                {
                    return;
                }

                ((IList<TY>)_yColumn)[index] = y0;
                ((IList<TY>)_y1Column)[index] = y1;

                //_yMax = ComputeMax(_yMax, ComputeMax(y0, y1, _yColumn), _yColumn);
                //_yMin = ComputeMin(_yMin, ComputeMin(y0, y1, _y1Column), _y1Column);
                //
                //var min = ComputeMin(_yMinPositive, ComputeMin(y0, y1, _y1Column), _y1Column);
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
        /// Inserts an X, Y0, Y1 point at the specified index. Automatically triggers a redraw
        /// </summary>
        /// <param name="index">The index to insert at</param>
        /// <param name="x">The X-value</param>
        /// <param name="y0">The Y0-value</param>
        /// <param name="y1">The Y1-value</param>
        public void Insert(int index, TX x, TY y0, TY y1)
        {
            lock (SyncRoot)
            {
                _appendBuffer.Flush();

                XValues.Insert(index, x);
                YValues.Insert(index, y0);
                Y1Values.Insert(index, y1);

                //_yMax = ComputeMax(_yMax, ComputeMax(y0, y1, _yColumn), _yColumn);
                //_yMin = ComputeMin(_yMin, ComputeMin(y0, y1, _y1Column), _y1Column);
                //
                //var min = ComputeMin(_yMinPositive, ComputeMin(y0, y1, _y1Column), _y1Column);
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
        /// Inserts a collection of X, Y0 and Y1 points at the specified index, automatically triggering a redraw
        /// </summary>
        /// <param name="startIndex">The index to insert at</param>
        /// <param name="x">The X-values</param>
        /// <param name="y0">The Y0-values</param>
        /// <param name="y1">The Y1-values</param>
        public void InsertRange(int startIndex, IEnumerable<TX> x, IEnumerable<TY> y0, IEnumerable<TY> y1)
        {
            TY inputY0Min = ArrayOperations.Minimum(y0);
            TY inputY0Max = ArrayOperations.Maximum(y0);

            TY inputY1Min = ArrayOperations.Minimum(y1);
            TY inputY1Max = ArrayOperations.Maximum(y1);

            lock (SyncRoot)
            {
                _appendBuffer.Flush();

                var xCountBeforeInserting = ((ISeriesColumn)_xColumn).Count;
                _xColumn.InsertRange(startIndex, x);
                var xCountAfterInserting = ((ISeriesColumn)_xColumn).Count;
                
                _yColumn.InsertRange(startIndex, y0);
                _y1Column.InsertRange(startIndex, y1);

                //_yMax = _yMath.Max(_yMax, _yMath.Max(inputY0Max, inputY1Max));
                //_yMin = _yMath.Min(_yMin, _yMath.Min(inputY0Min, inputY1Min));
                //
                //TY inputYPositiveMin = _yMinPositive;
                //if (typeof(TY) != typeof(DateTime))
                //{
                //    var zero = (TY)Convert.ChangeType(0d, typeof(TY), CultureInfo.InvariantCulture);
                //    inputYPositiveMin = _yMath.Min(ArrayOperations.MinGreaterThan(y0, zero), ArrayOperations.MinGreaterThan(y1, zero));
                //}
                //
                //var min = _yMath.Min(_yMinPositive, inputYPositiveMin);
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

        void FlushAppendBuffer(IList<ValTuple<TX, TY, TY>> bufferedValues) {
            lock (SyncRoot) {
                var newX = bufferedValues.Select(b => b.Item1);
                var xCountBeforeAppending = ((ISeriesColumn) _xColumn).Count;

                _xColumn.AddRange(newX);
                _yColumn.AddRange(bufferedValues.Select(b => b.Item2));
                _y1Column.AddRange(bufferedValues.Select(b => b.Item3));
                DataDistributionCalculator.OnAppendXValues(_xColumn, xCountBeforeAppending, newX, AcceptsUnsortedData);
            }
        }
    }
}