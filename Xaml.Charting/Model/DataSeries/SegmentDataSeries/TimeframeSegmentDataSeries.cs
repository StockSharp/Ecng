using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Numerics;
using Ecng.Xaml.Charting.Numerics.GenericMath;
using Ecng.Xaml.Charting.Numerics.PointResamplers;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.Model.DataSeries.SegmentDataSeries {
    /// <summary>
    /// base class for segmented data series
    /// </summary>
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public class TimeframeSegmentDataSeries : BindableObject, IDataSeries<DateTime, double> {
        public const double MinPriceStep = 0.000001d;
        public const int TimeframeOneDay = 60 * 24;
        public const int TimeframeOneWeek = 60 * 24 * 7;
        public const int MaxTimeframe = TimeframeOneWeek; // 1 week

        public static readonly IMath<DateTime> XMath = GenericMathFactory.New<DateTime>();

        string _seriesName;

        readonly Dictionary<double, long> _volumeByPrice = new Dictionary<double, long>();

        readonly UltraList<TimeframeDataSegment> _segments = new UltraList<TimeframeDataSegment>();
        readonly UltraList<DateTime> _segmentDates = new UltraList<DateTime>();
        readonly YValueList _yValues;
        IUltraReadOnlyList<DateTime> SegmentDates => _segmentDates.AsReadOnly();

        public DataSeriesType DataSeriesType => DataSeriesType.TimeframeSegment;

        public int Timeframe {get;}
        public double PriceStep {get;}

        /// <summary>
        /// Event raised whenever points are added to, removed or one or more DataSeries properties changes
        /// </summary>
        /// <remarks></remarks>
        public event EventHandler<DataSeriesChangedEventArgs> DataSeriesChanged;

        #region sync objects

        // операции добавления
        public object SyncRoot {get;} = new object();

        public bool AcceptsUnsortedData {get {return false;} set{ } }

        #endregion

        public IUltrachartSurface ParentSurface {get; set;}

        /// <summary>
        /// Gets or sets the name of this series
        /// </summary>
        /// <value>The name of the series.</value>
        /// <remarks></remarks>
        public string SeriesName {
            get { return _seriesName; }
            set {
                _seriesName = value;
                DataSeriesChanged?.Invoke(this, new DataSeriesChangedEventArgs(DataSeriesUpdate.DataChanged));
            }
        }

        public bool HasValues => _segmentDates.Count > 0;
        public int Count => _segmentDates.Count;

        public IComparable YMin => YRange.Min;
        public IComparable YMax => YRange.Max;

        IComparable IDataSeries.XMin => XRange.Min;
        IComparable IDataSeries.XMax => XRange.Max;

        bool IDataSeries.IsFifo => false;

        public bool IsSorted => true;

        internal new IUltraList<TimeframeDataSegment> Segments => _segments;

        protected IUltraReadOnlyList<TimeframeDataSegment> SegmentsReadOnly => _segments.AsReadOnly();

        /// <summary>
        /// Gets the Type of X-data points in this DataSeries. Used to check compatibility with Axis types
        /// </summary>
        public Type XType => typeof(DateTime);

        /// <summary>
        /// Gets the Type of Y-data points in this DataSeries. Used to check compatibility with Axis types
        /// </summary>
        public Type YType => typeof(double);

        /// <summary>
        /// Gets the latest Y-Value of the DataSeries
        /// </summary>
        public IComparable LatestYValue => null;

        IList<DateTime> XValues => SegmentDates;

        IList IDataSeries.XValues => (IList)XValues;
        IList<DateTime> IDataSeries<DateTime, double>.XValues => XValues;

        IList IDataSeries.YValues => _yValues;
        IList<double> IDataSeries<DateTime, double>.YValues => _yValues;

        public TimeframeSegmentDataSeries(int timeframe, double priceStep) {
            if(timeframe < 1 || timeframe > MaxTimeframe) throw new ArgumentOutOfRangeException(nameof(timeframe));
            if(priceStep <= 0d || priceStep.IsNaN()) throw new ArgumentOutOfRangeException(nameof(priceStep));

            _yValues = new YValueList(this);
            Timeframe = timeframe;
            PriceStep = priceStep.NormalizePrice(MinPriceStep);
        }

        public void Clear() {
            UltrachartDebugLogger.Instance.WriteLine("ERROR: TimeframeSegmentDataSeries.Clear(): not supported");
        }

        /// <summary>
        /// May be called to trigger a redraw on the parent <see cref="UltrachartSurface" />. This method is extremely useful
        /// when <see cref="IDataSeries" /> are in a ViewModel and bound via MVVM to <see cref="IRenderableSeries" />.
        /// Please see the <paramref name="rangeMode" /> parameter for invalidation options
        /// </summary>
        /// <param name="rangeMode">Provides <see cref="RangeMode" /> invalidation options for the parent surface</param>
        public void InvalidateParentSurface(RangeMode rangeMode) {
            if(ParentSurface != null) {
                switch (rangeMode) {
                    case RangeMode.None: ParentSurface.InvalidateElement(); break;
                    case RangeMode.ZoomToFit: ParentSurface.ZoomExtents(); break;
                    case RangeMode.ZoomToFitY: ParentSurface.ZoomExtentsY(); break;
                }
            }
        }

        public IndexRange GetIndicesRange(IRange visibleRange) {
            return GetIndicesRange(visibleRange, SearchMode.RoundDown, SearchMode.RoundUp);
        }

        private IndexRange GetIndicesRange(IRange range, SearchMode downSearchMode, SearchMode upSearchMode) {
            var result = new IndexRange(0, -1);

            if(_segmentDates.Count == 0)
                return result;

            var indexRange = range.Clone() as IndexRange;
            var indicesRange = indexRange ?? SearchDataIndexesOn(range, downSearchMode, upSearchMode);

            result = NormalizeIndexRange(indicesRange);

            return result;
        }

        IRange IDataSeries.GetWindowedYRange(IRange xRange) {
            var indexRange = SearchDataIndexesOn(xRange, SearchMode.RoundDown, SearchMode.RoundUp);

            return indexRange.IsDefined ? 
                    ((IDataSeries)this).GetWindowedYRange(indexRange) :
                    new DoubleRange(double.MinValue, double.MaxValue);
        }

        IRange IDataSeries.GetWindowedYRange(IndexRange indexRange, bool getPositiveRange) {
            return indexRange.IsDefined ? 
                    ((IDataSeries)this).GetWindowedYRange(indexRange) :
                    new DoubleRange(double.MinValue, double.MaxValue);
        }

        IRange IDataSeries.GetWindowedYRange(IRange xRange, bool getPositiveRange) {
            return ((IDataSeries)this).GetWindowedYRange(xRange);
        }

        public IRange XRange => _segmentDates.Any() ? 
                                    new DateRange(_segmentDates[0], _segmentDates[_segmentDates.Count - 1]).AsDoubleRange() :
                                    new DoubleRange(double.MinValue, double.MaxValue);

        public IRange YRange { get {
            if(_segments.Count == 0)
                return new DoubleRange(double.MinValue, double.MaxValue);

            double minPrice, maxPrice;
            minPrice = double.MaxValue;
            maxPrice = double.MinValue;

            foreach(var seg in _segments) {
                if(seg.MinPrice < minPrice)
                    minPrice = seg.MinPrice;
                if(seg.MaxPrice > maxPrice)
                    maxPrice = seg.MaxPrice;
            }

            return new DoubleRange(minPrice, maxPrice);
        }}

        IndexRange SearchDataIndexesOn(IRange range, SearchMode downSearchMode, SearchMode upSearchMode) {
            var indRange = range as IndexRange;
            if(indRange != null)
                return (IndexRange)indRange.Clone();

            var indicesRange = new IndexRange(-1, -1);

            DateRange dateRange;
            var dblRange = range as DoubleRange;
            if(dblRange != null) {
                dateRange = new DateRange(new DateTime((long)dblRange.Min), new DateTime((long)dblRange.Max));
            } else {
                dateRange = range as DateRange;
            }

            if(dateRange == null) {
                UltrachartDebugLogger.Instance.WriteLine("ERROR: SearchDataIndexesOn: unable to convert range type={0}", range.GetType().Name);
                return NormalizeIndexRange(indicesRange);
            }

            var dates = _segmentDates.ToArray();

            indicesRange.Min = ((IList)dates).FindIndex(true, dateRange.Min, downSearchMode);
            indicesRange.Max = ((IList)dates).FindIndex(true, dateRange.Max, upSearchMode);

            return NormalizeIndexRange(indicesRange);
        }

        IndexRange NormalizeIndexRange(IndexRange indexRange) {
            var count = _segmentDates.Count;

            if(indexRange.IsDefined) {
                if(indexRange.Min > count - 1 || indexRange.Max < 0) {
                    indexRange.Min = indexRange.Max = 0;
                } else {
                    indexRange.Min = Math.Max(indexRange.Min, 0);
                    indexRange.Max = Math.Max(Math.Min(indexRange.Max, count - 1), indexRange.Min);
                }
            }

            if(indexRange.Min.CompareTo(indexRange.Max) > 0)
                indexRange.Min = 0;

            UltrachartDebugLogger.Instance.WriteLine("GetIndicesRange(timeframesegment): Min={0}, Max={1}", indexRange.Min, indexRange.Max);

            return indexRange;
        }

        public static Tuple<DateTime, DateTime, int> GetTimeframePeriod(DateTime dt, int periodMinutes, Tuple<DateTime, DateTime, int> prevPeriod = null) {
            if(periodMinutes < 1 || periodMinutes > MaxTimeframe)
                throw new ArgumentOutOfRangeException(nameof(periodMinutes));

            if(prevPeriod != null && dt < prevPeriod.Item2 && dt >= prevPeriod.Item1)
                return prevPeriod;

            DateTime start, end;
            int index;

            if(periodMinutes < TimeframeOneDay) {
                index = (int)(dt.TimeOfDay.TotalMinutes / periodMinutes);
                start = dt.Date + TimeSpan.FromMinutes(index * periodMinutes);
                end = start + TimeSpan.FromMinutes(periodMinutes);
                if(end.Day != start.Day)
                    end = end.Date;
            } else if(periodMinutes == TimeframeOneDay) {
                start = new DateTime(dt.Year, dt.Month, dt.Day);
                end = start + TimeSpan.FromDays(1);
                index = start.DayOfYear - 1;
            } else if(periodMinutes == TimeframeOneWeek) {
                start = new DateTime(dt.Year, dt.Month, dt.Day) - TimeSpan.FromDays((int)dt.DayOfWeek);
                end = start + TimeSpan.FromDays(7);
                index = start.WeekNumber();
            } else if(periodMinutes > TimeframeOneWeek) {
                throw new NotImplementedException("periods more than one week are not supported");
            } else {
                // day < period < week
                var yearStart = new DateTime(dt.Year, 1, 1);
                var diff = yearStart - dt;
                index = (int)(diff.TotalMinutes / periodMinutes);
                start = yearStart + TimeSpan.FromMinutes(index * periodMinutes);
                end = start + TimeSpan.FromMinutes(periodMinutes);
                if(end.Year != start.Year)
                    end = new DateTime(end.Year, 1, 1);
            }

            return Tuple.Create(start, end, index);
        }

        internal static double[] GeneratePrices(double min, double max, double step) {
            min = min.NormalizePrice(step);
            max = max.NormalizePrice(step);

            var result = new double[1 + (int)Math.Round((max - min) / step)];

            for (var i = 0; i < result.Length; ++i)
                result[i] = (min + i * step).NormalizePrice(step);

            return result;
        }

        Tuple<DateTime, DateTime, int> _curPeriod;

        public void Append(DateTime time, double price, long volume) {
            bool updated;

            lock(SyncRoot) {
                _curPeriod = GetTimeframePeriod(time, Timeframe, _curPeriod);

                if(_segments.Count > 0 && _segments[_segments.Count - 1].Time > _curPeriod.Item1)
                    throw new ArgumentOutOfRangeException(nameof(time), "data must be ordered by time");

                updated = AddOrUpdateSegment(_curPeriod.Item1, price, volume, true);
            }

            if(updated)
                DataSeriesChanged?.Invoke(this, new DataSeriesChangedEventArgs(DataSeriesUpdate.DataChanged));
        }

        public void Update(DateTime time, double price, long volume) {
            bool updated;

            lock(SyncRoot) {
                _curPeriod = GetTimeframePeriod(time, Timeframe, _curPeriod);

                if(_segments.Count > 0 && _segments[_segments.Count - 1].Time > _curPeriod.Item1)
                    throw new ArgumentOutOfRangeException(nameof(time), "data must be ordered by time");

                updated = AddOrUpdateSegment(_curPeriod.Item1, price, volume, false);
            }

            if(updated)
                DataSeriesChanged?.Invoke(this, new DataSeriesChangedEventArgs(DataSeriesUpdate.DataChanged));
        }

        bool AddOrUpdateSegment(DateTime periodStart, double price, long volume, bool sum) {
            if(volume < 0)
                throw new ArgumentException(nameof(volume));

            if(sum && volume == 0)
                return false;

            var step = PriceStep;
            if(step <= 0 || step.IsNaN())
                return false;

            price = price.NormalizePrice(step);

            TimeframeDataSegment segment;

            if(_segments.Count == 0 || _segments[_segments.Count - 1].Time != periodStart) {
                segment = new TimeframeDataSegment(periodStart, step, _segments.Count);

                _segments.Add(segment);
                _segmentDates.Add(segment.Time);
            } else {
                segment = _segments[_segments.Count - 1];
            }

            long v;
            _volumeByPrice.TryGetValue(price, out v);

            if(sum) {
                segment.AddPoint(price, volume);
                _volumeByPrice[price] = v + volume;

                return true;
            }

            var oldVolume = segment.GetValueByPrice(price);

            if(volume == oldVolume)
                return false;

            segment.UpdatePoint(price, volume);
            _volumeByPrice[price] = v - oldVolume + volume;

            return true;
        }

        public double GetYMinAt(int index, double existingYMin) {
            return Math.Min(_segments[index].MinPrice, existingYMin);
        }

        public double GetYMaxAt(int index, double existingYMax) {
            return Math.Max(_segments[index].MaxPrice, existingYMax);
        }

        public IRange GetWindowedYRange(IndexRange xIndexRange) {
            double min, max;

            var range = (IndexRange)xIndexRange.Clone();

            if(range.Min < 0) range.Min = 0;
            if(range.Max >= _segments.Count) range.Max = _segments.Count - 1;

            TimeframeDataSegment.MinMax(_segments.Skip(range.Min).Take(range.Max - range.Min + 1), out min, out max);

            return new DoubleRange(min, max);
        }

        public IPointSeries ToPointSeries(ResamplingMode resamplingMode, IndexRange pointRange, int viewportWidth, bool isCategoryAxis, bool? dataIsDisplayedAs2D, IRange visibleXRange, IPointResamplerFactory factory, object pointSeriesArg = null) {
            return pointRange.IsDefined ? new TimeframeSegmentPointSeries(Segments.ItemsArray, pointRange, visibleXRange, PriceStep) : null;
        }

        public long GetVolumeByPrice(double price, double priceStep = 0d) {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if(priceStep == 0d) priceStep = PriceStep;

            price = price.NormalizePrice(priceStep);

            long vol;
            _volumeByPrice.TryGetValue(price, out vol);
            return vol;
        }

        #region suspender

        /// <summary>
        /// Gets a value indicating whether updates for the target are currently suspended
        /// </summary>
        public bool IsSuspended => UpdateSuspender.GetIsSuspended(this);

        /// <summary>
        /// Suspends drawing updates on the target until the returned object is disposed, when a final draw call will be issued
        /// </summary>
        /// <returns>
        /// The disposable Update Suspender
        /// </returns>
        public IUpdateSuspender SuspendUpdates() {
            var ps = ParentSurface;
            if (ps != null) {
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
        public void ResumeUpdates(IUpdateSuspender suspender) {
            if(suspender.ResumeTargetOnDispose)
                DataSeriesChanged?.Invoke(this, new DataSeriesChangedEventArgs(DataSeriesUpdate.DataChanged | DataSeriesUpdate.DataSeriesCleared));

            if(suspender.Tag != null) {
                // Synchronization object on the parent surface
                Monitor.Exit(suspender.Tag);
            }
        }

        public void DecrementSuspend() { }

        #endregion

        class YValueList : IList<double>, IList {
            readonly TimeframeSegmentDataSeries _parent;

            public int Count => _parent._segments.Count;
            public object SyncRoot => ((ICollection)_parent._segments).SyncRoot;
            public bool IsSynchronized => ((ICollection)_parent._segments).IsSynchronized;
            public bool IsReadOnly => true;
            public bool IsFixedSize => false;


            public YValueList(TimeframeSegmentDataSeries parent) {
                _parent = parent;
            }

            IEnumerator IEnumerable.GetEnumerator() {
                return GetEnumerator();
            }

            public IEnumerator<double> GetEnumerator() {
                return _parent._segments.Select(s => s.Y).GetEnumerator();
            }

            bool IList.Contains(object value) {
                if(!(value is double))
                    return false;

                return Contains((double)value);
            }

            public bool Contains(double value) {
                return _parent._segments.Select(s => s.Y).Contains(value);
            }

            int IList.IndexOf(object value) {
                if(!(value is double))
                    return -1;

                return IndexOf((double)value);
            }

            public int IndexOf(double value) {
                var count = _parent._segments.Count;
                for(var i=0; i < count; ++i)
                    if(_parent._segments[i].Y.DoubleEquals(value))
                        return i;

                return -1;
            }

            public double this[int index] {
                get { return _parent._segments[index].Y; }
                set { throw new NotImplementedException(); }
            }

            object IList.this[int index] {
                get { return this[index]; }
                set { throw new NotImplementedException(); }
            }

            #region not implemented

            public void CopyTo(double[] array, int arrayIndex) {
                throw new NotImplementedException();
            }

            public void CopyTo(Array array, int index) {
                throw new NotImplementedException();
            }

            public void Add(double value) {
                throw new NotImplementedException();
            }

            public void Clear() {
                throw new NotImplementedException();
            }

            public void Insert(int index, double value) {
                throw new NotImplementedException();
            }

            public bool Remove(double value) {
                throw new NotImplementedException();
            }

            public void RemoveAt(int index) {
                throw new NotImplementedException();
            }

            int IList.Add(object value) {
                throw new NotImplementedException();
            }

            void IList.Insert(int index, object value) {
                throw new NotImplementedException();
            }

            void IList.Remove(object value) {
                throw new NotImplementedException();
            }

            #endregion
        }

        #region not implemented for this type

        void IDataSeries<DateTime, double>.Append(DateTime dt, params double[] yValues) {throw new NotImplementedException();}
        public void Append(IEnumerable<DateTime> dtList, params IEnumerable<double>[] yValues) {throw new NotImplementedException();}

        [Obsolete("IsAttached is obsolete because there is no DataSeriesSet now")]
        bool IDataSeries.IsAttached { get { throw new NotImplementedException(); } }

        // obsolete
        IComparable IDataSeries.XMinPositive { get { throw new NotImplementedException(); }}
        int? IDataSeries.FifoCapacity { get { throw new NotImplementedException(); } set {throw new NotImplementedException();}}

        void IDataSeries<DateTime, double>.Remove(DateTime x) { throw new NotImplementedException(); }
        void IDataSeries<DateTime, double>.RemoveAt(int index) { throw new NotImplementedException(); }
        void IDataSeries<DateTime, double>.RemoveRange(int startIndex, int count) { throw new NotImplementedException(); }
        IDataSeries<DateTime, double> IDataSeries<DateTime, double>.Clone() { throw new NotImplementedException(); }
        int IDataSeries.FindIndex(IComparable x, SearchMode searchMode) { throw new NotImplementedException(); }
        int IDataSeries.FindClosestPoint(IComparable x, IComparable y, double xyScaleRatio, double maxXDistance) { throw new NotImplementedException(); }
        int IDataSeries.FindClosestLine(IComparable x, IComparable y, double xyScaleRatio, double maxXDistance, LineDrawMode drawNanAs) { throw new NotImplementedException(); }

        public virtual void OnBeginRenderPass() { }

        // obsolete
        IPointSeries IDataSeries.ToPointSeries(IList column, ResamplingMode resamplingMode, IndexRange pointRange, int viewportWidth, bool isCategoryAxis) { throw new NotImplementedException(); }
        HitTestInfo IDataSeries.ToHitTestInfo(int index) { return HitTestInfo.Empty; }

        // obsolete
        IComparable IDataSeries.YMinPositive {get {throw new NotImplementedException();}}

        #endregion
    }
}
