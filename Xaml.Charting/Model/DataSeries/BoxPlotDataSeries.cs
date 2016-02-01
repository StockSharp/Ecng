// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// BoxPlotDataSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Globalization;
using System.Linq;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Numerics;
using Ecng.Xaml.Charting.Numerics.GenericMath;
using Ecng.Xaml.Charting.Numerics.PointResamplers;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.Model.DataSeries
{
	/// <summary>
	/// A generic DataSeries which provides data in Min, Lower Quartile, Median, Upper Quartile, Max format for consumption 
	/// by the <seealso cref="BoxPlotDataSeries{TX,TY}"/>. 
	/// </summary>
	/// <typeparam name="TX">The type of the X-data</typeparam>
	/// <typeparam name="TY">Tye type of the Y-data</typeparam>
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
	public class BoxPlotDataSeries<TX, TY> : DataSeries<TX, TY>
		where TX : IComparable
		where TY : IComparable
	{
		private ISeriesColumn<TY> _minimumColumn = new SeriesColumn<TY>();
		private ISeriesColumn<TY> _lowerQuartileColumn = new SeriesColumn<TY>();
		private ISeriesColumn<TY> _upperQuartileColumn = new SeriesColumn<TY>();
		private ISeriesColumn<TY> _maximumColumn = new SeriesColumn<TY>();

		/// <summary>
		/// Initializes a new instance of the <see cref="BoxPlotDataSeries{TX,TY}" /> class.
		/// </summary>
		public BoxPlotDataSeries()
		{
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
					   && _minimumColumn.HasValues
					   && _lowerQuartileColumn.HasValues
					   && _yColumn.HasValues
					   && _upperQuartileColumn.HasValues 
					   && _maximumColumn.HasValues;
			}
		}

		/// <summary>
		/// Gets the <see cref="DataSeriesType"/> for this DataSeries
		/// </summary>
		public override DataSeriesType DataSeriesType
		{
			get { return DataSeriesType.Box; }
		}

	    public IList<TY> MedianValues { get { return YValues; }}
        public IList<TY> MinimumValues { get { return _minimumColumn; } }
        public IList<TY> MaximumValues { get { return _maximumColumn; } }
        public IList<TY> UpperQuartileValues { get { return _upperQuartileColumn; } }
        public IList<TY> LowerQuartileValues { get { return _lowerQuartileColumn; } }

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
				_minimumColumn = new FifoSeriesColumn<TY>(size);
				_lowerQuartileColumn = new FifoSeriesColumn<TY>(size);				
				_upperQuartileColumn = new FifoSeriesColumn<TY>(size);
				_maximumColumn = new FifoSeriesColumn<TY>(size);
			}
			else
			{
				_xColumn = new SeriesColumn<TX>();
                _yColumn = new SeriesColumn<TY>();
				_minimumColumn = new SeriesColumn<TY>();
				_lowerQuartileColumn = new SeriesColumn<TY>();				
				_upperQuartileColumn = new SeriesColumn<TY>();
				_maximumColumn = new SeriesColumn<TY>();
			}

			((ICollection<TX>)_xColumn).Clear();
            ((ICollection<TY>)_yColumn).Clear();
			((ICollection<TY>)_minimumColumn).Clear();
			((ICollection<TY>)_lowerQuartileColumn).Clear();			
			((ICollection<TY>)_upperQuartileColumn).Clear();
			((ICollection<TY>)_maximumColumn).Clear();
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

				// If removed y-value was YMax or YMin, recalculate YMax/YMin
				//bool needsYRecalc = y.CompareTo(YMax) == 0 || y.CompareTo(YMin) == 0;
				//
				//var high = ((IList<TY>)_maximumColumn)[index];
				//var low = ((IList<TY>)_minimumColumn)[index];
				//
				//// If removed high/low-value was YMax or YMin, recalculate YMax/YMin
				//needsYRecalc |= high.CompareTo(YMax) == 0 || low.CompareTo(YMin) == 0;

				((IList<TY>)_maximumColumn).RemoveAt(index);
				((IList<TY>)_minimumColumn).RemoveAt(index);

				//_xMin = XValues.Count > 0 ? XValues[0] : _xMath.MaxValue;
				//_xMax = XValues.Count > 0 ? XValues[XValues.Count - 1] : _xMath.MinValue;
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
			_xColumn.RemoveRange(startIndex, count);
			_yColumn.RemoveRange(startIndex, count);

			_maximumColumn.RemoveRange(startIndex, count);
			_upperQuartileColumn.RemoveRange(startIndex, count);
			_yColumn.RemoveRange(startIndex, count);
			_lowerQuartileColumn.RemoveRange(startIndex, count);
			_minimumColumn.RemoveRange(startIndex, count);

			//_xMin = XValues.Count > 0 ? XValues[0] : _xMath.MaxValue;
			//_xMax = XValues.Count > 0 ? XValues[XValues.Count - 1] : _xMath.MinValue;
			//RecalculateXPositiveMinimum();
			//
			//RecalculateYMinMaxFull();

			OnDataSeriesChanged(DataSeriesUpdate.DataChanged);
			DataDistributionCalculator.UpdateDataDistributionFlagsWhenRemovedXValues();
		}


		/// <summary>
		/// Creates a deep copy of a DataSeries
		/// </summary>
		/// <returns></returns>
		public override IDataSeries<TX, TY> Clone()
		{
			lock (SyncRoot)
			{
				var dataSeries = new BoxPlotDataSeries<TX, TY>();
				dataSeries.FifoCapacity = FifoCapacity;
                dataSeries.AcceptsUnsortedData = AcceptsUnsortedData;

				dataSeries.Append(XValues, YValues, _minimumColumn, _lowerQuartileColumn, _upperQuartileColumn, _maximumColumn);
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
		public override IPointSeries ToPointSeries(ResamplingMode resamplingMode, IndexRange pointRange, int viewportWidth, bool isCategoryAxis, bool? dataIsDisplayedAs2D, IRange visibleXRange, IPointResamplerFactory factory, object pointSeriesArg = null)
		{
			lock (SyncRoot)
			{
				// Either Mid or None. Cannot have mix MinMax with Max/Min as they result in different numbers of points
				var midResample = resamplingMode == ResamplingMode.None ? ResamplingMode.None : ResamplingMode.Mid;
				var hResample = resamplingMode == ResamplingMode.None ? ResamplingMode.None : ResamplingMode.Max;
				var lResample = resamplingMode == ResamplingMode.None ? ResamplingMode.None : ResamplingMode.Min;

				var r = factory.GetPointResampler<TX, TY>();
				var medianPoints = r.Execute(midResample, pointRange, viewportWidth, IsFifo, isCategoryAxis, _xColumn, _yColumn, DataDistributionCalculator.DataIsSortedAscending, DataDistributionCalculator.DataIsEvenlySpaced, dataIsDisplayedAs2D, visibleXRange);
				var upperQuartilePoints = r.Execute(midResample, pointRange, viewportWidth, IsFifo, isCategoryAxis, _xColumn, _upperQuartileColumn, DataDistributionCalculator.DataIsSortedAscending, DataDistributionCalculator.DataIsEvenlySpaced, dataIsDisplayedAs2D, visibleXRange);
				var lowerQuartilePoints = r.Execute(midResample, pointRange, viewportWidth, IsFifo, isCategoryAxis, _xColumn, _lowerQuartileColumn, DataDistributionCalculator.DataIsSortedAscending, DataDistributionCalculator.DataIsEvenlySpaced, dataIsDisplayedAs2D, visibleXRange);
				var maxPoints = r.Execute(hResample, pointRange, viewportWidth, IsFifo, isCategoryAxis, _xColumn, _maximumColumn, DataDistributionCalculator.DataIsSortedAscending, DataDistributionCalculator.DataIsEvenlySpaced, dataIsDisplayedAs2D, visibleXRange);
				var minPoints = r.Execute(lResample, pointRange, viewportWidth, IsFifo, isCategoryAxis, _xColumn, _minimumColumn, DataDistributionCalculator.DataIsSortedAscending, DataDistributionCalculator.DataIsEvenlySpaced, dataIsDisplayedAs2D, visibleXRange);

				return new BoxPointSeries(medianPoints, minPoints, lowerQuartilePoints, upperQuartilePoints, maxPoints);
			}
		}

		/// <summary>
		/// When overridden in a derived class, returns a <see cref="HitTestInfo"/> instance containing data about the data-point at the specified index
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
					hitTestInfo.Minimum = ((IList<TY>) _minimumColumn)[index];
					hitTestInfo.Maximum = ((IList<TY>) _maximumColumn)[index];
					hitTestInfo.Median = ((IList<TY>) _yColumn)[index];

					hitTestInfo.LowerQuartile = ((IList<TY>) _lowerQuartileColumn)[index];
					hitTestInfo.UpperQuartile = ((IList<TY>) _upperQuartileColumn)[index];
				}

				return hitTestInfo;
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
			const int expectedYValuesCount = 5;

			if (yValues.Length != expectedYValuesCount)
			{
				ThrowWhenAppendInvalid(expectedYValuesCount);
			}

			Append(x, yValues[0], yValues[1], yValues[2], yValues[3], yValues[4]);
		}

		/// <summary>
		/// Appends a list of X, Y points to the series
		/// </summary>
		/// <exception cref="InvalidOperationException">Exception will be thrown if the count of x and each y differ</exception>
		/// <param name="x">The list of X points</param>
		/// <param name="yValues">Lists of Y points (depends on series type)</param>
		public override void Append(IEnumerable<TX> x, params IEnumerable<TY>[] yValues)
		{
			const int expectedYValuesCount = 5;

			if (yValues.Length != expectedYValuesCount)
			{
				ThrowWhenAppendInvalid(expectedYValuesCount);
			}

			Append(x, yValues[0], yValues[1], yValues[2], yValues[3], yValues[4]);
		}

		/// <summary>
		/// Appends an Box-plot point to the series, including X-value, Minimum, Lower Quartile, Median, Upper Quartile, Maximum value
		/// </summary>
		/// <param name="x">The X value</param>
		/// <param name="median">The median.</param>
		/// <param name="minimum">The minimum.</param>
		/// <param name="lowerQuartile">The lower quartile.</param>
		/// <param name="upperQuartile">The upper quartile.</param>
		/// <param name="maximum">The maximum.</param>
		private void Append(TX x, TY median, TY minimum, TY lowerQuartile, TY upperQuartile, TY maximum)
		{
			lock (SyncRoot)
			{
				_xColumn.Add(x);
				_yColumn.Add(median);
				_minimumColumn.Add(minimum);
				_lowerQuartileColumn.Add(lowerQuartile);
				_upperQuartileColumn.Add(upperQuartile);
				_maximumColumn.Add(maximum);

				//_yMax = ComputeMax(_yMax, maximum, _maximumColumn);
				//_yMin = ComputeMin(_yMin, minimum, _minimumColumn);
				//
				//var min = ComputeMin(_yMinPositive, minimum, _yColumn);
				//_yMinPositive = GetPositiveMin(_yMinPositive, min);
				//
				//_xMin = ((IList<TX>)_xColumn)[0];
				//_xMax = x;
				//
				//var xMin = ComputeMin(_xMinPositive, x, _xColumn);
				//_xMinPositive = GetPositiveMin(_xMinPositive, xMin);

				OnDataSeriesChanged(DataSeriesUpdate.DataChanged);
				DataDistributionCalculator.OnAppendXValue(_xColumn, x, AcceptsUnsortedData);
			}
		}

		/// <summary>
		/// Appends a collection of Box-plot points to the series, including X-values, Minimum, Lower Quartile, Median, Upper Quartile, Maximum values
		/// </summary>
		/// <param name="x">The X value</param>
		/// <param name="median">The median.</param>
		/// <param name="minimum">The minimum.</param>
		/// <param name="lowerQuartile">The lower quartile.</param>
		/// <param name="upperQuartile">The upper quartile.</param>
		/// <param name="maximum">The maximum.</param>
		public void Append(IEnumerable<TX> x, IEnumerable<TY> median, IEnumerable<TY> minimum, IEnumerable<TY> lowerQuartile, IEnumerable<TY> upperQuartile, IEnumerable<TY> maximum)
		{
			if (x.IsEmpty())
                return;

            lock (SyncRoot)
			{
				var countBeforeAppending = ((ISeriesColumn) _xColumn).Count;
				_xColumn.AddRange(x);
				_yColumn.AddRange(median);
				_minimumColumn.AddRange(minimum);
				_lowerQuartileColumn.AddRange(lowerQuartile);
				_upperQuartileColumn.AddRange(upperQuartile);
				_maximumColumn.AddRange(maximum);

				OnDataSeriesChanged(DataSeriesUpdate.DataChanged);
                DataDistributionCalculator.OnAppendXValues(_xColumn, countBeforeAppending, x, AcceptsUnsortedData);
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
			TY high = ((IList<TY>)_maximumColumn)[index];
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
			TY low = ((IList<TY>)_minimumColumn)[index];
			return YMath.IsNaN(low) == false ? YMath.Min(existingYMin, low) : existingYMin;
		}
	}
}
