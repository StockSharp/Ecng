// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// XyDataSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Globalization;
using System.Linq;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Numerics;
using Ecng.Xaml.Charting.Numerics.GenericMath;
using Ecng.Xaml.Charting.Numerics.PointResamplers;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.Model.DataSeries
{
    /// <summary>
    /// A DataSeries to store Xy data-points, containing X and Y values which must be sorted in the X-direction. 
    /// May be used as a DataSource for <seealso cref="FastLineRenderableSeries"/> as well as standard XY renderable series types
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
    public class XyDataSeries<TX, TY> : DataSeries<TX, TY>, IXyDataSeries<TX, TY>
        where TX : IComparable
        where TY : IComparable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="XyDataSeries{TX,TY}" /> class.
        /// </summary>
        public XyDataSeries()
        {
        }

        /// <summary>
        /// Gets the <see cref="DataSeriesType"/> for this DataSeries
        /// </summary>
        public override DataSeriesType DataSeriesType
        {
            get { return DataSeriesType.Xy; }
        }

        /// <summary>
        /// Gets whether the Data Series has values (is not empty)
        /// </summary>
        /// <remarks></remarks>
        public override bool HasValues
        {
            get { return _xColumn.HasValues && _yColumn.HasValues; }
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
        public override IPointSeries ToPointSeries(
            ResamplingMode resamplingMode, 
            IndexRange pointRange, 
            int viewportWidth, 
            bool isCategoryAxis, 
            bool? dataIsDisplayedAs2D, 
            IRange visibleXRange, 
            IPointResamplerFactory factory)
        {
            lock (SyncRoot)
            {
                IPointResampler pointResampler = factory.GetPointResampler<TX, TY>();
                var r = pointResampler.Execute(resamplingMode, pointRange, viewportWidth, IsFifo,
                     isCategoryAxis, _xColumn, _yColumn, DataDistributionCalculator.DataIsSortedAscending, DataDistributionCalculator.DataIsEvenlySpaced, dataIsDisplayedAs2D, visibleXRange);
                return r;
            }
        }

        /// <summary>
        /// When overridden in a derived class, clears all columns in the Data Series
        /// </summary>
        protected override void ClearColumns()
        {
            bool isFifo = FifoCapacity.HasValue;
            if (isFifo)
            {
                int size = FifoCapacity.Value;

                _xColumn = new FifoSeriesColumn<TX>(size);
                _yColumn = new FifoSeriesColumn<TY>(size);
            }
            else
            {
                _xColumn = new SeriesColumn<TX>();
                _yColumn = new SeriesColumn<TY>();
            }

            ((ICollection<TX>)_xColumn).Clear();
            ((ICollection<TY>)_yColumn).Clear();
        }

        /// <summary>
        /// Removes the X,Y values at the specified index
        /// </summary>
        /// <param name="index">The index to remove at</param>
        public override void RemoveAt(int index)
        {
            lock (SyncRoot)
            {
                var y = YValues[index];
                var x = XValues[index];

                XValues.RemoveAt(index);
                YValues.RemoveAt(index);

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
                _xColumn.RemoveRange(startIndex, count);
                _yColumn.RemoveRange(startIndex, count);
               
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
                IXyDataSeries<TX, TY> dataSeries = new XyDataSeries<TX, TY>();
                dataSeries.FifoCapacity = FifoCapacity;
                dataSeries.AcceptsUnsortedData = AcceptsUnsortedData;

                dataSeries.Append(XValues, YValues);
                return dataSeries;
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
            const int expectedYValuesCount = 1;

            if (yValues.Length != expectedYValuesCount)
            {
                ThrowWhenAppendInvalid(expectedYValuesCount);
            }

            Append(x, yValues[0]);
        }

        /// <summary>
        /// Appends a list of X, Y points to the series
        /// </summary>
        /// <exception cref="InvalidOperationException">Exception will be thrown if the count of x and each y differ</exception>
        /// <param name="x">The list of X points</param>
        /// <param name="yValues">Lists of Y points (depends on series type)</param>
        public override void Append(IEnumerable<TX> x, params IEnumerable<TY>[] yValues)
        {
            const int expectedYValuesCount = 1;

            if (yValues.Length != expectedYValuesCount)
            {
                ThrowWhenAppendInvalid(expectedYValuesCount);
            }

            Append(x, yValues[0]);
        }

        /// <summary>
        /// Appends an X, Y point to the series
        /// </summary>
        /// <param name="x">The X Value</param>
        /// <param name="y">The Y Value</param>
        public virtual void Append(TX x, TY y)
        {
            lock (SyncRoot)
            {
                _xColumn.Add(x);
                _yColumn.Add(y);

                OnDataSeriesChanged(DataSeriesUpdate.DataChanged);
                DataDistributionCalculator.OnAppendXValue(_xColumn, x, AcceptsUnsortedData);
            }
        }

        /// <summary>
        /// Appends a list of X, Y points to the series
        /// </summary>
        /// <param name="x">The list of X points</param>
        /// <param name="y">The list of Y points</param>
        public virtual void Append(IEnumerable<TX> x, IEnumerable<TY> y)
        {
            if (x.IsEmpty())
                return;

            lock (SyncRoot)
            {
                var xCountBeforeAppending = ((ISeriesColumn) _xColumn).Count;
                _xColumn.AddRange(x);
                _yColumn.AddRange(y);

                OnDataSeriesChanged(DataSeriesUpdate.DataChanged);
                DataDistributionCalculator.OnAppendXValues(_xColumn, xCountBeforeAppending, x, AcceptsUnsortedData);
            }
        }

        /// <summary>
        /// Updates an X,Y point specified by the X-Value passed in. 
        /// </summary>
        /// <param name="x">The X Value to key on when updating</param>
        /// <param name="y">The new Y value</param>
        /// <exception cref="InvalidOperationException">Thrown if the x value is not in the DataSeries</exception>
        public virtual void Update(TX x, TY y)
        {
            lock (SyncRoot)
            {
                int index = ((IList)_xColumn).FindIndex(IsSorted, x, SearchMode.Exact);
                if (index == -1)
                {
                    return;
                }

                ((IList<TY>)_yColumn)[index] = y;

                OnDataSeriesChanged(DataSeriesUpdate.DataChanged);
            }
        }

        /// <summary>
        /// Inserts an X,Y point at the specified index
        /// </summary>
        /// <param name="index">The index to insert at</param>
        /// <param name="x">The X value</param>
        /// <param name="y">The Y value</param>
        public virtual void Insert(int index, TX x, TY y)
        {
            lock (SyncRoot)
            {
                XValues.Insert(index, x);
                YValues.Insert(index, y);

                OnDataSeriesChanged(DataSeriesUpdate.DataChanged);
                DataDistributionCalculator.OnInsertXValue(_xColumn, index, x, AcceptsUnsortedData);
            }
        }

        /// <summary>
        /// Inserts a list of X, Y points at the specified index
        /// </summary>
        /// <exception cref="InvalidOperationException">Exception will be thrown if the count of x and y differ</exception>
        /// <param name="startIndex">The index to insert at</param>
        /// <param name="x">The list of X points</param>
        /// <param name="y">The list of Y points</param>
        public virtual void InsertRange(int startIndex, IEnumerable<TX> x, IEnumerable<TY> y)
        {
            lock (SyncRoot)
            {
                var xCountBeforeInserting = ((ISeriesColumn)_xColumn).Count;
                _xColumn.InsertRange(startIndex, x);
                var xCountAfterInserting = ((ISeriesColumn)_xColumn).Count;

                _yColumn.InsertRange(startIndex, y);

                OnDataSeriesChanged(DataSeriesUpdate.DataChanged);                
                DataDistributionCalculator.OnInsertXValues(_xColumn, startIndex, xCountAfterInserting - xCountBeforeInserting, x, AcceptsUnsortedData);
            }
        }

        /// <summary>
        /// Used internally by AutoRanging algorithm. 
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
        /// Used internally by AutoRanging algorithm. 
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
    }


}