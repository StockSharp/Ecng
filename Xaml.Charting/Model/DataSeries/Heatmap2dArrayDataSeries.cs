// *************************************************************************************
// ULTRACHART © Copyright ulc software Services Ltd. 2011-2013. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: info@ulcsoftware.co.uk
// 
// Heatmap2dArrayDataSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Reflection;
using System.Windows.Media;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Numerics;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.Model.DataSeries
{
    /// <summary>
    /// Contains settings to translate double value into color for heatmap
    /// </summary>
    internal class DoubleToColorMappingSettings
    {
        public GradientStop[] GradientStops;
        public double Minimum, ScaleFactor;
		public override bool Equals(object obj)
		{
			if ((obj is DoubleToColorMappingSettings))
			{
				var obj2 = (DoubleToColorMappingSettings)obj;
				if (obj2.Minimum != this.Minimum) return false;
				if (obj2.ScaleFactor != this.ScaleFactor) return false;
				if (obj2.GradientStops.Length != this.GradientStops.Length) return false;
				for (int i = 0; i < this.GradientStops.Length; i++)
					if (obj2.GradientStops[i] != this.GradientStops[i]) return false;
				return true;
			}
			return false;
		}

		/// <summary>
		/// contains pre-calculated color values
		/// </summary>
	    public int[] CachedMap;
    }

    /// <summary>
    /// represents 2D color data for Array2DSegment 
    /// </summary>
    internal interface IHeatmap2dArrayDataSeries
    {
        int[,] GetArgbColorArray2D(DoubleToColorMappingSettings mappingSettings);
        double[,] GetArray2D();
        int ArrayHeight { get; }
        int ArrayWidth { get; }
        double? GetArrayValue(double xValue, double yValue, bool xAxisIsFlipped, bool yAxisIsFlipped);
    }

    /// <summary>
    /// Represents static 2D array as IDataSeries to be displayed by FastHeatmapRenderableSeries
    /// Converts data into color
    /// </summary>
    [Obfuscation(Exclude = true, ApplyToMembers = true)] // Problem with Obfuscation of this class, exclude
    public sealed class Heatmap2dArrayDataSeries<TX, TY, TZ> : IDataSeries<TX, TY>, IHeatmap2dArrayDataSeries
        where TX : IComparable
        where TY : IComparable
        where TZ : IComparable
    {

        /// <summary>
        /// Gets the Type of X-data points in this DataSeries. Used to check compatibility with Axis types
        /// </summary>
        public Type XType { get { return typeof(TX); } }

        /// <summary>
        /// Gets the Type of Y-data points in this DataSeries. Used to check compatibility with Axis types
        /// </summary>
        public Type YType { get { return typeof(TY); } }

        private int ArrayWidth
        {
            get { return _array2D.GetLength(1); }
        }
        int IHeatmap2dArrayDataSeries.ArrayWidth
        {
            get { return _array2D.GetLength(1); }
        }
        private int ArrayHeight
        {
            get { return _array2D.GetLength(0); }
        }
        int IHeatmap2dArrayDataSeries.ArrayHeight
        {
            get { return _array2D.GetLength(0); }
        }


        private readonly Func<int, TX> _xMapping;
        private readonly Func<int, TY> _yMapping;
        private readonly TZ[,] _array2D;

        /// <summary>
        /// Creates an instance of Heatmap2dArrayDataSeries from 2D array
        /// </summary>
        /// <param name="array2d">2D array with data. First dimension is considered as Y, second one is X</param>
        /// <param name="xMapping">Delegate which returns X value for X index in 2D array</param>
        /// <param name="yMapping">Delegate which returns Y value for Y index in 2D array</param>
        public Heatmap2dArrayDataSeries(TZ[,] array2d, Func<int, TX> xMapping, Func<int, TY> yMapping)
        {
            if (array2d == null) throw new ArgumentNullException();
            _array2D = array2d;
            _xMapping = xMapping;
            _yMapping = yMapping;
        }


        #region IHeatmap2dArrayDataSeries.GetArgbColorArray2d
        private int[,] _cachedArgbColorArray2d;
        private DoubleToColorMappingSettings _cachedMappingSettings;
        int[,] IHeatmap2dArrayDataSeries.GetArgbColorArray2D(DoubleToColorMappingSettings mappingSettings)
        {
            if (_cachedArgbColorArray2d == null || !mappingSettings.Equals(_cachedMappingSettings))
            {
                int h = _array2D.GetLength(0);
                int w = _array2D.GetLength(1);
                _cachedArgbColorArray2d = new int[h,w];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                        _cachedArgbColorArray2d[y, x] = DoubleToArgbColor(_array2D[y, x].ToDouble(), mappingSettings);
                _cachedMappingSettings = mappingSettings;
            }
            return _cachedArgbColorArray2d;
        }
        private static int DoubleToArgbColor(double x, DoubleToColorMappingSettings mappingSettings)
        { // this has to be optimized
            x -= mappingSettings.Minimum;
            x *= mappingSettings.ScaleFactor;

			if (mappingSettings.CachedMap == null)
			{
				mappingSettings.CachedMap = new int[1000]; // 1000 elements should be enough to represent gradient brush
				for (int j = 0; j < mappingSettings.CachedMap.Length; j++)
				{
					double v = (double)j/mappingSettings.CachedMap.Length;
					unchecked
					{
						int color;
						var gradientStops = mappingSettings.GradientStops;
						if (gradientStops.Length > 1)
						{
							var previousGradientStop = gradientStops[0];
							for (int i = 1; i < gradientStops.Length; i++)
							{
								var gradientStop = gradientStops[i];
								if (v < gradientStop.Offset)
								{
									var offset1 = previousGradientStop.Offset;
									var color1 = previousGradientStop.Color;
									var offset2 = gradientStop.Offset;
									var color2 = gradientStop.Color;

									color = DoubleToArgbColor((v - offset1) / (offset2 - offset1),
															 color1.R, color1.G, color1.B, color2.R, color2.G, color2.B);
									goto save_color_to_cache;
								}
								previousGradientStop = gradientStop;
							}
						}
						var c = mappingSettings.GradientStops[mappingSettings.GradientStops.Length - 1].Color;
						color = (255 << 24) | (c.R << 16) | (c.G << 8) | (c.B);

					save_color_to_cache:
						mappingSettings.CachedMap[j] = color;
					}
				}
			}

	        var index = (int) (x*mappingSettings.CachedMap.Length);
			if (index < 0) index = 0;
			else if (index >= mappingSettings.CachedMap.Length) index = mappingSettings.CachedMap.Length - 1;

	        return mappingSettings.CachedMap[index];
        }
        private static int DoubleToArgbColor(double x, int r1, int g1, int b1, int r2, int g2, int b2)
        {
            if (x > 1) x = 1;
            if (x < 0) x = 0;
            int r = r1 + (int)((r2 - r1) * x);
            int g = g1 + (int)((g2 - g1) * x);
            int b = b1 + (int)((b2 - b1) * x);

            return (255 << 24) | (r << 16) | (g << 8) | (b);
        }
        #endregion

        #region IHeatmap2dArrayDataSeries.GetArray2D
        private double[,] _cachedArray2d = null;
        double[,] IHeatmap2dArrayDataSeries.GetArray2D()
        {
            if (_cachedArray2d == null)
            {
                int h = _array2D.GetLength(0);
                int w = _array2D.GetLength(1);
                _cachedArray2d = new double[h, w];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                        _cachedArray2d[y, x] = _array2D[y, x].ToDouble();
            }
            return _cachedArray2d;
        }
        #endregion

        System.Collections.IList IDataSeries.XValues
        {
            get
            {
                var a = new TX[ArrayWidth];
                for (int i = 0; i < a.Length; i++)
                    a[i] = _xMapping(i);
                return a;
            }
        }

        System.Collections.IList IDataSeries.YValues
        {
            get
            {
                var a = new TY[ArrayWidth];
                for (int i = 0; i < a.Length; i++)
                    a[i] = _yMapping(ArrayHeight - 1);
                return a;
            }
        }

        /// <summary>
        /// Gets the latest Y-Value of the DataSeries
        /// </summary>
        public IComparable LatestYValue { get { return null; } }

        IList<TX> IDataSeries<TX, TY>.XValues
        {
            get
            {
                var a = new TX[ArrayWidth];
                for (int i = 0; i < a.Length; i++)
                    a[i] = _xMapping(i);
                return a;
            }
        }
        IList<TY> IDataSeries<TX, TY>.YValues
        {
            get
            {
                var a = new TY[ArrayWidth];
                for (int i = 0; i < a.Length; i++)
                    a[i] = _yMapping(ArrayHeight - 1);
                return a;
            }
        }
        void IDataSeries<TX, TY>.Append(TX x, params TY[] yValues)
        {
            throw new NotImplementedException();
        }
        void IDataSeries<TX, TY>.Append(IEnumerable<TX> x, params IEnumerable<TY>[] yValues)
        {
            throw new NotImplementedException();
        }
        void IDataSeries<TX, TY>.Remove(TX x)
        {
            throw new NotImplementedException();
        }
        void IDataSeries<TX, TY>.RemoveAt(int index)
        {
            throw new NotImplementedException();
        }
        void IDataSeries<TX, TY>.RemoveRange(int startIndex, int count)
        {
            throw new NotImplementedException();
        }
        void IDataSeries<TX, TY>.Clear()
        {
            throw new NotImplementedException();
        }
        IDataSeries<TX, TY> IDataSeries<TX, TY>.Clone()
        {
            throw new NotImplementedException();
        }
        TY IDataSeries<TX, TY>.GetYMinAt(int index, TY existingYMin)
        {
            throw new NotImplementedException();
        }
        TY IDataSeries<TX, TY>.GetYMaxAt(int index, TY existingYMax)
        {
            throw new NotImplementedException();
        }

#pragma warning disable 67
        private event EventHandler<DataSeriesChangedEventArgs> DataSeriesChanged;
#pragma warning restore 67

        event EventHandler<DataSeriesChangedEventArgs> IDataSeries.DataSeriesChanged
        {
            add { DataSeriesChanged += value; }
            remove { DataSeriesChanged -= value; }
        }

        private IUltrachartSurface _parentSurface;
        IUltrachartSurface IDataSeries.ParentSurface
        {
            get { return _parentSurface; }
            set { _parentSurface = value; }
        }
        [Obsolete("IsAttached is obsolete because there is no DataSeriesSet now")]
        bool IDataSeries.IsAttached
        {
            get { throw new NotImplementedException(); }
        }
        IRange IDataSeries.XRange
        {
            get { return new DoubleRange(_xMapping(0).ToDouble(), _xMapping(ArrayWidth - 1).ToDouble()); }
        }
        IRange IDataSeries.YRange
        {
            get { throw new NotImplementedException(); }
        }
        DataSeriesType IDataSeries.DataSeriesType
        {
            get { throw new NotImplementedException(); }
        }
		public string SeriesName
		{
			get;
			set;
		}
        IComparable IDataSeries.YMin
        {
            get { return _yMapping(0); }
        }
        IComparable IDataSeries.YMinPositive
        {
            get { return _yMapping(0); }
        }
        IComparable IDataSeries.YMax
        {
            get { return _yMapping(ArrayHeight - 1); }
        }
        IComparable IDataSeries.XMin
        {
            get { return _xMapping(0); }
        }
        IComparable IDataSeries.XMinPositive
        {
            get { throw new NotImplementedException(); }
        }
        IComparable IDataSeries.XMax
        {
            get { return _xMapping(ArrayWidth - 1); }
        }
        bool IDataSeries.IsFifo
        {
            get { return false; }
        }
        int? IDataSeries.FifoCapacity
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

		public int FindClosestLine(IComparable x, IComparable y, double xyScaleRatio, double maxXDistance, LineDrawMode drawNanAs)
        {
            throw new NotImplementedException();
        }
        public int FindClosestPoint(IComparable x, IComparable y, double xyScaleRatio, double maxXDistance)
        {
            throw new NotImplementedException();
        }
        bool IDataSeries.HasValues
        {
            get { return ArrayHeight != 0 && ArrayWidth != 0; }
        }
        int IDataSeries.Count
        {
            get { return ArrayWidth; }
        }
        bool IDataSeries.IsSorted
        {
            get { return true; }
        }
        readonly object _syncRoot = new object();
        object IDataSeries.SyncRoot
        {
            get { return _syncRoot; }
        }
        IndexRange IDataSeries.GetIndicesRange(IRange visibleRange)
        {
            return new IndexRange(0, ArrayWidth);
        }
        IPointSeries IDataSeries.ToPointSeries(ResamplingMode resamplingMode, IndexRange pointRange, int viewportWidth, bool isCategoryAxis, bool? dataIsDisplayedAs2d, IRange visibleXRange, IPointResamplerFactory factory)
        {
            return new Array2DPointSeries<TX, TY>(this, _xMapping, _yMapping);
        }
        IPointSeries IDataSeries.ToPointSeries(System.Collections.IList column, ResamplingMode resamplingMode, IndexRange pointRange, int viewportWidth, bool isCategoryAxis)
        {
            throw new NotImplementedException();
        }
        IRange IDataSeries.GetWindowedYRange(IRange xRange)
        {
            return new DoubleRange(_yMapping(0).ToDouble(), _yMapping(ArrayHeight - 1).ToDouble());
        }
        IRange IDataSeries.GetWindowedYRange(IndexRange xIndexRange)
        {
            return new DoubleRange(_yMapping(0).ToDouble(), _yMapping(ArrayHeight - 1).ToDouble());
        }
        IRange IDataSeries.GetWindowedYRange(IndexRange xIndexRange, bool getPositiveRange)
        {
            return new DoubleRange(_yMapping(0).ToDouble(), _yMapping(ArrayHeight - 1).ToDouble());
        }
        IRange IDataSeries.GetWindowedYRange(IRange xRange, bool getPositiveRange)
        {
            return new DoubleRange(_yMapping(0).ToDouble(), _yMapping(ArrayHeight - 1).ToDouble());
        }
        int IDataSeries.FindIndex(IComparable x, SearchMode searchMode)
        {
            throw new NotImplementedException();
        }
        HitTestInfo IDataSeries.ToHitTestInfo(int index)
		{
			return HitTestInfo.Empty;
		}

        void IDataSeries.InvalidateParentSurface(RangeMode rangeMode)
        {
            throw new NotImplementedException();
        }
        public IDataSeries ToStackedDataSeriesComponent(IEnumerable<IDataSeries> previousDataSeriesInSameGroup)
        {
            throw new NotImplementedException();
        }

        bool ISuspendable.IsSuspended
        {
            get { throw new NotImplementedException(); }
        }
        IUpdateSuspender ISuspendable.SuspendUpdates()
        {
            throw new NotImplementedException();
        }
        void ISuspendable.ResumeUpdates(IUpdateSuspender suspender)
        {
            throw new NotImplementedException();
        }
        void ISuspendable.DecrementSuspend()
        {
            throw new NotImplementedException();
        }
        public bool IsEvenlySpaced
        {
            get { return true; }
        }

		/// <summary>
		/// reverse mapping
		/// </summary>
		static int? GetIndex<T>(Func<int, T> mapping, double value, int maxIndexInclusive, bool axisIsFlipped)
			where T : IComparable
		{
            for (int index = 1; index <= maxIndexInclusive; index++)
                if (mapping(index).ToDouble() >= value)
                    if (mapping(index - 1).ToDouble() < value)
                        return index - 1;
                    else return null;
			return null;
		}
        public double? GetArrayValue(double xValue, double yValue, bool xAxisIsFlipped, bool yAxisIsFlipped)
		{
			var xIndex = GetIndex(_xMapping, xValue, ArrayWidth, xAxisIsFlipped);
			var yIndex = GetIndex(_yMapping, yValue, ArrayHeight, yAxisIsFlipped);
		    if (yIndex == null || xIndex == null) return null;
			var zValue = _array2D[yIndex.Value, xIndex.Value];
			return zValue.ToDouble();
		}
    }
}