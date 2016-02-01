// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// Heatmap2DArrayDataSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Reflection;
using System.Windows.Media;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Numerics;
using Ecng.Xaml.Charting.Numerics.PointResamplers;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.Model.DataSeries
{
    /// <summary>
    /// Represents static 2D array as IDataSeries to be displayed by FastHeatmapRenderableSeries
    /// Converts data into color
    /// </summary>
    [Obfuscation(Exclude = true, ApplyToMembers = true)] // Problem with Obfuscation of this class, exclude
    public sealed class Heatmap2DArrayDataSeries<TX, TY, TZ> : IDataSeries<TX, TY>, IHeatmap2DArrayDataSeriesInternal
        where TX : IComparable
        where TY : IComparable
        where TZ : IComparable
    {
        private readonly Func<int, TX> _xMapping;
        private readonly Func<int, TY> _yMapping;
        private readonly TZ[,] _array2D;

        private double[,] _cachedArray2D = null;


        private int[,] _cachedArgbColorArray2D;
        private DoubleToColorMappingSettings _cachedMappingSettings;

        readonly object _syncRoot = new object();

        private IUltrachartSurface _parentSurface;


        /// <summary>
        /// Creates an instance of Heatmap2dArrayDataSeries from 2D array
        /// </summary>
        /// <param name="array2D">2D array with data. First dimension is considered as Y, second one is X</param>
        /// <param name="xMapping">Delegate which returns X value for X index in 2D array</param>
        /// <param name="yMapping">Delegate which returns Y value for Y index in 2D array</param>
        public Heatmap2DArrayDataSeries(TZ[,] array2D, Func<int, TX> xMapping, Func<int, TY> yMapping)
        {
            if (array2D == null) throw new ArgumentNullException();
            AcceptsUnsortedData = false;
            _array2D = array2D;
            _xMapping = xMapping;
            _yMapping = yMapping;
        }

        event EventHandler<DataSeriesChangedEventArgs> IDataSeries.DataSeriesChanged
        {
            add { }
            remove { }
        }

        public object SyncRoot
        {
            get { return _syncRoot; }
        }

        public bool AcceptsUnsortedData { get; set; }

        public bool IsEvenlySpaced
        {
            get { return true; }
        }

        IUltrachartSurface IDataSeries.ParentSurface
        {
            get { return _parentSurface; }
            set { _parentSurface = value; }
        }

        public string SeriesName { get; set; }

        IRange IDataSeries.XRange
        {
            get { return new DoubleRange(_xMapping(0).ToDouble(), _xMapping(ArrayWidth-1).ToDouble()); }
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
        IComparable IDataSeries.XMax
        {
            get { return _xMapping(ArrayWidth - 1); }
        }
        bool IDataSeries.IsFifo
        {
            get { return false; }
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

        private int ArrayHeight
        {
            get { return _array2D.GetLength(0); }
        }

        int IHeatmap2DArrayDataSeries.ArrayWidth
        {
            get { return ArrayWidth; }
        }

        int IHeatmap2DArrayDataSeries.ArrayHeight
        {
            get { return ArrayHeight; }
        }


        IList IDataSeries.XValues
        {
            get { return (IList) ((IDataSeries<TX, TY>) this).XValues; }
        }

        IList IDataSeries.YValues
        {
            get { return (IList) ((IDataSeries<TX, TY>) this).YValues; }
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
                var a = new TY[ArrayHeight];
                for (int i = 0; i < a.Length; i++)
                    a[i] = _yMapping(i);
                return a;
            }
        }

        
        int[,] IHeatmap2DArrayDataSeriesInternal.GetArgbColorArray2D(DoubleToColorMappingSettings mappingSettings)
        {
            if (_cachedArgbColorArray2D == null || !mappingSettings.Equals(_cachedMappingSettings))
            {
                int height = ArrayHeight;
                int width = ArrayWidth;

                _cachedArgbColorArray2D = new int[height, width];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        _cachedArgbColorArray2D[y, x] = DoubleToArgbColor(_array2D[y, x].ToDouble(), mappingSettings);
                    }
                }

                _cachedMappingSettings = mappingSettings;
            }

            return _cachedArgbColorArray2D;
        }

        private static int DoubleToArgbColor(double x, DoubleToColorMappingSettings mappingSettings)
        {
            //TODO: this has to be optimized
            x -= mappingSettings.Minimum;
            x *= mappingSettings.ScaleFactor;

            if (mappingSettings.CachedMap == null)
            {
                mappingSettings.CachedMap = new int[1000]; // 1000 elements should be enough to represent gradient brush
                for (int j = 0; j < mappingSettings.CachedMap.Length; j++)
                {
                    double v = (double)j / mappingSettings.CachedMap.Length;
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
                                        color1.A, color1.R, color1.G, color1.B, color2.A, color2.R, color2.G, color2.B);
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

            var index = (int)(x * mappingSettings.CachedMap.Length);

            if (index < 0) index = 0;
            else if (index >= mappingSettings.CachedMap.Length) index = mappingSettings.CachedMap.Length - 1;

            return mappingSettings.CachedMap[index];
        }

        private static int DoubleToArgbColor(double x, int a1, int r1, int g1, int b1, int a2, int r2, int g2, int b2)
        {
            if (x > 1) x = 1;
            if (x < 0) x = 0;
            int a = a1 + (int)((a2 - a1) * x);
            int r = r1 + (int)((r2 - r1) * x);
            int g = g1 + (int)((g2 - g1) * x);
            int b = b1 + (int)((b2 - b1) * x);

            return (a << 24) | (r << 16) | (g << 8) | (b);
        }

        double[,] IHeatmap2DArrayDataSeries.GetArray2D()
        {
            if (_cachedArray2D == null)
            {
                int h = _array2D.GetLength(0);
                int w = _array2D.GetLength(1);
                _cachedArray2D = new double[h, w];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                        _cachedArray2D[y, x] = _array2D[y, x].ToDouble();
            }
            return _cachedArray2D;
        }


        
        [Obsolete("IsAttached is obsolete because there is no DataSeriesSet now")]
        bool IDataSeries.IsAttached
        {
            get { throw new NotImplementedException(); }
        }

        IRange IDataSeries.YRange
        {
            get { return new DoubleRange(_yMapping(0).ToDouble(), _yMapping(ArrayHeight-1).ToDouble()); }
        }

        DataSeriesType IDataSeries.DataSeriesType
        {
            get { return DataSeriesType.Heatmap; }
        }

        IComparable IDataSeries.XMinPositive
        {
            get { throw new NotImplementedException(); }
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
        
        public int FindClosestLine(int start, int count, IComparable x, IComparable y, double xyScaleRatio, double maxXDistance)
        {
            throw new NotImplementedException();
        }

        public int FindClosestPoint(int start, int count, IComparable x, IComparable y, double xyScaleRatio, double maxXDistance)
        {
            throw new NotImplementedException();
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

        void IDataSeries.Clear()
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

        IPointSeries IDataSeries.ToPointSeries(System.Collections.IList column, ResamplingMode resamplingMode, IndexRange pointRange, int viewportWidth, bool isCategoryAxis)
        {
            throw new NotImplementedException();
        }

        int IDataSeries.FindIndex(IComparable x, SearchMode searchMode)
        {
            throw new NotImplementedException();
        }

        void IDataSeries.InvalidateParentSurface(RangeMode rangeMode)
        {
            throw new NotImplementedException();
        }

        public IDataSeries ToStackedDataSeriesComponent(IEnumerable<IDataSeries> previousDataSeriesInSameGroup)
        {
            throw new NotImplementedException();
        }

        public int FindClosestPoint(IComparable x, IComparable y, double xyScaleRatio, double maxXDistance)
        {
            throw new NotImplementedException();
        }

        public int FindClosestLine(IComparable x, IComparable y, double xyScaleRatio, double maxXDistance, LineDrawMode drawNanAs)
        {
            throw new NotImplementedException();
        }

        public void OnBeginRenderPass() { }

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


        IndexRange IDataSeries.GetIndicesRange(IRange visibleRange)
        {
            return new IndexRange(0, ArrayWidth - 1);
        }

        IPointSeries IDataSeries.ToPointSeries(ResamplingMode resamplingMode, IndexRange pointRange, int viewportWidth, bool isCategoryAxis, bool? dataIsDisplayedAs2D, IRange visibleXRange, IPointResamplerFactory factory, object pointSeriesArg = null)
        {
            return new Array2DPointSeries<TX, TY>(this, _xMapping, _yMapping);
        }

        IRange IDataSeries.GetWindowedYRange(IndexRange xIndexRange)
        {
            return GetYRange();
        }

        private IRange GetYRange()
        {
            return new DoubleRange(_yMapping(0).ToDouble(), _yMapping(ArrayHeight).ToDouble());
        }

        IRange IDataSeries.GetWindowedYRange(IndexRange xIndexRange, bool getPositiveRange)
        {
            return GetYRange();
        }

        IRange IDataSeries.GetWindowedYRange(IRange xRange, bool getPositiveRange)
        {
            return GetYRange();
        }

        IRange IDataSeries.GetWindowedYRange(IRange xRange)
        {
            return GetYRange();
        }

        HitTestInfo IDataSeries.ToHitTestInfo(int index)
        {
            return GetHitTestInfo(index, index);
        }

        private HitTestInfo GetHitTestInfo(int? xIndex, int? yIndex)
        {
            lock (SyncRoot)
            {
                var isHit = xIndex != null && yIndex != null;

                IComparable zValue = null;
                if (isHit)
                {
                    zValue = _array2D[yIndex.Value, xIndex.Value];
                }

                var hitTestInfo = new HitTestInfo
                {
                    DataSeriesName = SeriesName,
                    DataSeriesType = DataSeriesType.Heatmap,
                    ZValue = zValue,
                    IsHit = isHit,
                    IsWithinDataBounds = isHit,
                    IsVerticalHit = isHit,
                };

                return hitTestInfo;
            }
        }

        public HitTestInfo ToHitTestInfo(double xValue, double yValue, bool interpolateXy = true)
        {
            var xIndex = GetIndex(_xMapping, xValue, ArrayWidth);
            var yIndex = GetIndex(_yMapping, yValue, ArrayHeight);

            var hitTestInfo = GetHitTestInfo(xIndex, yIndex);

            if (interpolateXy)
            {
                hitTestInfo.XValue = xValue;
                hitTestInfo.YValue = yValue;
            }
            else
            {                
                if (hitTestInfo.IsHit)
                {
                    hitTestInfo.XValue = _xMapping((int) xIndex.GetValueOrDefault(-1));
                    hitTestInfo.YValue = _yMapping((int) yIndex.GetValueOrDefault(-1));
                }
            }

            return hitTestInfo;
        }

        /// <summary>
        /// Reverse mapping
        /// </summary>
        private int? GetIndex<T>(Func<int, T> mapping, double value, int dimension)
            where T : IComparable
        {
            for (var index = 0; index < dimension; index++)
            {
                if (mapping(index + 1).ToDouble() >= value &&
                    mapping(index).ToDouble() < value)
                {
                    return index;
                }
            }

            return null;
        }
    }
}