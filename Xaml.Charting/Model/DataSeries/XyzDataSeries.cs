// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// XyzDataSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
    /// Provides a generic <seealso cref="IDataSeries"/> to hold X,Y,Z values. Used as a data-source for the <seealso cref="FastBubbleRenderableSeries"/>, 
    /// if this DataSeries is assigned to any other X-Y type, then the X-Y values will be rendered (Z ignored). 
    /// </summary>
    /// <typeparam name="TX">The type of the X-data</typeparam>
    /// <typeparam name="TY">The type of the Y-data</typeparam>
    /// <typeparam name="TZ">The type of the Z-data</typeparam>
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
    /// <seealso cref="FastBubbleRenderableSeries"/>
    /// <remarks>DataSeries are assigned to the RenderableSeries via the <see cref="IRenderableSeries.DataSeries"/> property. Any time a DataSeries is appended to, the
    /// parent <see cref="UltrachartSurface"/> will be redrawn</remarks>
    public class XyzDataSeries<TX, TY, TZ> : DataSeries<TX, TY>, IXyzDataSeries<TX, TY, TZ>
        where TX : IComparable
        where TY : IComparable
        where TZ : IComparable
    {
        public static readonly IMath<TZ> ZMath = GenericMathFactory.New<TZ>();

        readonly DataSeriesAppendBuffer<ValTuple<TX, TY, TZ>> _appendBuffer;

        /// <summary>
        /// Initializes a new instance of the <see cref="XyzDataSeries{TX,TY,TZ}" /> class.
        /// </summary>
        public XyzDataSeries()
        {
            _appendBuffer = new DataSeriesAppendBuffer<ValTuple<TX, TY, TZ>>(FlushAppendBuffer);
        }

        protected ISeriesColumn<TZ> _zColumn = new SeriesColumn<TZ>();
        private TZ _zMin;
        private TZ _zMax;

        /// <summary>
        /// Gets the <see cref="DataSeriesType"/> for this DataSeries
        /// </summary>
        public override DataSeriesType DataSeriesType
        {
            get { return DataSeriesType.Xyz; }
        }

        /// <summary>
        /// Gets the Z Values as a list of <see cref="IComparable"/>
        /// </summary>
        IList IXyzDataSeries.ZValues
        {
            get { return _zColumn; }
        }

        /// <summary>
        /// Gets the Z values
        /// </summary>
        public IList<TZ> ZValues
        {
            get { return _zColumn; }
        }

        /// <summary>
        /// Gets whether the Data Series has values (is not empty)
        /// </summary>
        /// <remarks></remarks>
        public override bool HasValues
        {
            get { return _xColumn.HasValues && _yColumn.HasValues && _zColumn.HasValues; }
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

                XValues.RemoveAt(index);
                YValues.RemoveAt(index);
                ZValues.RemoveAt(index);

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
            lock (SyncRoot)
            {
                _appendBuffer.Flush();

                _xColumn.RemoveRange(startIndex, count);
                _yColumn.RemoveRange(startIndex, count);
                _zColumn.RemoveRange(startIndex, count);

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

                var dataSeries = new XyzDataSeries<TX, TY, TZ>();
                dataSeries.FifoCapacity = FifoCapacity;
                dataSeries.AcceptsUnsortedData = AcceptsUnsortedData;

                dataSeries.Append(XValues, YValues, ZValues);
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
            var min = YValues[index];
            return YMath.IsNaN(min) ? existingYMin : YMath.Min(existingYMin, min);
        }

        /// <summary>
        /// When overriden in a derived class, gets the Max(existingYMax, currentMax), where currentMax is the maximum at the specified index
        /// </summary>
        /// <param name="index">The index to the underlying dataset</param>
        /// <param name="existingYMax">The existing maximum</param>
        /// <returns>The new YMax, which is the Min(existingYMax, currentMax)</returns>
        public override TY GetYMaxAt(int index, TY existingYMax)
        {
            var max = YValues[index];
            return YMath.IsNaN(max) ? existingYMax : YMath.Max(existingYMax, max);
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
                var yPoints = factory.GetPointResampler<TX, TY>().Execute(resamplingMode, pointRange, viewportWidth, IsFifo, isCategoryAxis, _xColumn, _yColumn, DataDistributionCalculator.DataIsSortedAscending, DataDistributionCalculator.DataIsEvenlySpaced, dataIsDisplayedAs2D, visibleXRange);
                var zPoints = factory.GetPointResampler<TX, TZ>().Execute(resamplingMode, pointRange, viewportWidth, IsFifo, isCategoryAxis, _xColumn, _zColumn, DataDistributionCalculator.DataIsSortedAscending, DataDistributionCalculator.DataIsEvenlySpaced, dataIsDisplayedAs2D, visibleXRange);

                return new XyzPointSeries(yPoints, zPoints);
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
                    var zValue = ZValues[index];
                    hitTestInfo.ZValue = zValue;
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
                    _zColumn = new FifoSeriesColumn<TZ>(size);
                }
                else
                {
                    _xColumn = new SeriesColumn<TX>();
                    _yColumn = new SeriesColumn<TY>();
                    _zColumn = new SeriesColumn<TZ>();
                }

                ((ICollection<TX>)_xColumn).Clear();
                ((ICollection<TY>)_yColumn).Clear();
                ((ICollection<TZ>)_zColumn).Clear();

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
            throw new InvalidOperationException(String.Format("Append(TX x, params TY[] yValues) in type {0} must receive X, Y and Z values. Please use the Append(x,y,z) overload", GetType().Name));
        }

        /// <summary>
        /// Appends a list of X, Y points to the series
        /// </summary>
        /// <exception cref="InvalidOperationException">Exception will be thrown if the count of x and each y differ</exception>
        /// <param name="x">The list of X points</param>
        /// <param name="yValues">Lists of Y points (depends on series type)</param>
        public override void Append(IEnumerable<TX> x, params IEnumerable<TY>[] yValues)
        {
            throw new InvalidOperationException(String.Format("Append(TX x, params TY[] yValues) in type {0} must receive X, Y and Z values. Please use the Append(x,y,z) overload", GetType().Name));
        }


        /// <summary>
        /// Appends a single X, Y0, Y1 point to the series, automatically triggering a redraw
        /// </summary>
        /// <param name="x">The X-value</param>
        /// <param name="y">The Y-value</param>
        /// <param name="z">The Z-value</param>
        public void Append(TX x, TY y, TZ z)
        {
            // ReSharper disable once InconsistentlySynchronizedField
            _appendBuffer.Append(ValTuple.Create(x, y, z));

            OnDataSeriesChanged(DataSeriesUpdate.DataChanged);
        }

        /// <summary>
        /// Appends a collection of X, Y0 and Y1 points to the series, automatically triggering a redraw
        /// </summary>
        /// <param name="x">The X-values</param>
        /// <param name="y">The Y-values</param>
        /// <param name="z">The Z-values</param>
        public void Append(IEnumerable<TX> x, IEnumerable<TY> y, IEnumerable<TZ> z)
        {
            if (x.IsEmpty())
                return;

            var xe = x.GetEnumerator();
            var ye = y.GetEnumerator();
            var ze = z.GetEnumerator();

            lock(_appendBuffer.SyncRoot)
                while(xe.MoveNext() && ye.MoveNext() && ze.MoveNext())
                    _appendBuffer.Append(ValTuple.Create(xe.Current, ye.Current, ze.Current));
                
            OnDataSeriesChanged(DataSeriesUpdate.DataChanged);
        }

        /// <summary>
        /// Updates (overwrites) the Y0, Y1 values at the specified X-value. Automatically triggers a redraw
        /// </summary>
        /// <param name="x">The X-value</param>
        /// <param name="y">The Y-value</param>
        /// <param name="z">The Z-value</param>
        public void Update(TX x, TY y, TZ z)
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
                ((IList<TZ>)_zColumn)[index] = z;

                OnDataSeriesChanged(DataSeriesUpdate.DataChanged);
            }
        }

        /// <summary>
        /// Inserts an X, Y0, Y1 point at the specified index. Automatically triggers a redraw
        /// </summary>
        /// <param name="index">The index to insert at</param>
        /// <param name="x">The X-value</param>
        /// <param name="y">The Y-value</param>
        /// <param name="z">The Z-value</param>
        public void Insert(int index, TX x, TY y, TZ z)
        {
            lock (SyncRoot)
            {
                _appendBuffer.Flush();

                XValues.Insert(index, x);
                YValues.Insert(index, y);
                ZValues.Insert(index, z);

                OnDataSeriesChanged(DataSeriesUpdate.DataChanged);
                DataDistributionCalculator.OnInsertXValue(_xColumn, index, x, AcceptsUnsortedData);
            }
        }

        /// <summary>
        /// Inserts a collection of X, Y0 and Y1 points at the specified index, automatically triggering a redraw
        /// </summary>
        /// <param name="startIndex">The index to insert at</param>
        /// <param name="x">The X-values</param>
        /// <param name="y">The Y-values</param>
        /// <param name="z">The Z-values</param>
        public void InsertRange(int startIndex, IEnumerable<TX> x, IEnumerable<TY> y, IEnumerable<TZ> z)
        {
            lock (SyncRoot)
            {
                _appendBuffer.Flush();

                var xCountBeforeInserting = ((ISeriesColumn) _xColumn).Count;
                _xColumn.InsertRange(startIndex, x);
                var xCountAfterInserting = ((ISeriesColumn) _xColumn).Count;
                _yColumn.InsertRange(startIndex, y);
                _zColumn.InsertRange(startIndex, z);

                OnDataSeriesChanged(DataSeriesUpdate.DataChanged);
                DataDistributionCalculator.OnInsertXValues(_xColumn, startIndex,
                    xCountAfterInserting - xCountBeforeInserting, x, AcceptsUnsortedData);
            }
        }

        void FlushAppendBuffer(IList<ValTuple<TX, TY, TZ>> bufferedValues) {
            lock (SyncRoot) {
                var newX = bufferedValues.Select(b => b.Item1);
                var xCountBeforeAppending = ((ISeriesColumn) _xColumn).Count;

                _xColumn.AddRange(newX);
                _yColumn.AddRange(bufferedValues.Select(b => b.Item2));
                _zColumn.AddRange(bufferedValues.Select(b => b.Item3));
                DataDistributionCalculator.OnAppendXValues(_xColumn, xCountBeforeAppending, newX, AcceptsUnsortedData);
            }
        }
    }	
}
