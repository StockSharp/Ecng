using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Controls;
using Ecng.Xaml.Charting.Common;
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
    public abstract class TimeframeSegmentDataSeries : BindableObject, IDataSeries<DateTime, double> {
        public const double MinPriceStep = 0.000001d;
        public const int TimeframeOneDay = 60 * 24;
        public const int TimeframeOneWeek = 60 * 24 * 7;
        public const int MaxTimeframe = TimeframeOneWeek; // 1 week

        public static readonly IMath<DateTime> XMath = GenericMathFactory.New<DateTime>();

        string _seriesName;
        readonly double _priceStep;
        readonly int _timeframe;
        readonly bool _sumTicks;

        readonly UltraList<DateTime> _segmentDates = new UltraList<DateTime>();
        protected IUltraReadOnlyList<DateTime> SegmentDates => _segmentDates.AsReadOnly();

        internal IUltraReadOnlyList<TimeframeDataSegment> Segments => SegmentsReadOnly;

        abstract protected IUltraReadOnlyList<TimeframeDataSegment> SegmentsReadOnly {get;}
        abstract protected IList<DateTime> XValues {get;}
        abstract protected double[] YValues {get;}
        abstract public DataSeriesType DataSeriesType {get;}
        abstract public double GetYMinAt(int index, double existingYMin);
        abstract public double GetYMaxAt(int index, double existingYMax);
        abstract public IPointSeries ToPointSeries(ResamplingMode resamplingMode, IndexRange pointRange, int viewportWidth, bool isCategoryAxis, bool? dataIsDisplayedAs2D, IRange visibleXRange, IPointResamplerFactory factory);
        abstract public IRange GetWindowedYRange(IndexRange xIndexRange);
        abstract public IRange YRange {get;}

        public int Timeframe {get {return _timeframe;}}
        public double PriceStep {get {return _priceStep;}}
        public bool SumTicks {get {return _sumTicks;}}

        /// <summary>
        /// Event raised whenever points are added to, removed or one or more DataSeries properties changes
        /// </summary>
        /// <remarks></remarks>
        public event EventHandler<DataSeriesChangedEventArgs> DataSeriesChanged;

        #region sync objects

        readonly object _syncRoot = new object();
        readonly object _clearSyncRoot = new object();

        // лок, использующийся при очистке датасерии или полном пересчете (второй лок тоже используется при этих операциях)
        public object ClearSyncRoot {get { return _clearSyncRoot; }}

        // операции добавления
        public object SyncRoot {get { return _syncRoot; }}
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
                OnDataSeriesChanged(DataSeriesUpdate.DataChanged);
            }
        }

        public bool HasValues {get {return _segmentDates.Count > 0;}}
        public int Count { get { return _segmentDates.Count; }}

        public IComparable YMin { get { return YRange.Min; } }
        public IComparable YMax { get { return YRange.Max; } }

        IComparable IDataSeries.XMin { get { return XRange.Min; }}
        IComparable IDataSeries.XMax { get { return XRange.Max; }}

        bool IDataSeries.IsFifo { get { return false; }}

        public bool IsSorted { get { return true; } }
        /// <summary>
        /// Gets the Type of X-data points in this DataSeries. Used to check compatibility with Axis types
        /// </summary>
        public Type XType { get { return typeof(DateTime); } }

        /// <summary>
        /// Gets the Type of Y-data points in this DataSeries. Used to check compatibility with Axis types
        /// </summary>
        public Type YType { get { return typeof(double); } }

        /// <summary>
        /// Gets the latest Y-Value of the DataSeries
        /// </summary>
        public IComparable LatestYValue { get { return null; } }

        IList IDataSeries.XValues { get {return (IList)XValues;}}
        IList<DateTime> IDataSeries<DateTime, double>.XValues { get {return XValues;}}

        IList IDataSeries.YValues { get {return YValues;}}
        IList<double> IDataSeries<DateTime, double>.YValues { get {return YValues;}}

        protected TimeframeSegmentDataSeries(int timeframe, double priceStep, bool sumTicks) {
            if(timeframe < 1 || timeframe > MaxTimeframe) throw new ArgumentOutOfRangeException("timeframe");
            if(priceStep <= 0d || priceStep.IsNaN()) throw new ArgumentOutOfRangeException("priceStep");

            _sumTicks = sumTicks;
            _timeframe = timeframe;
            _priceStep = PriceDataPoint.NormalizePrice(priceStep, MinPriceStep);
        }

        protected virtual void OnNewSegment(TimeframeDataSegment segment) {
            _segmentDates.Add(segment.Time);
        }

        public void Clear() {
            UltrachartDebugLogger.Instance.WriteLine("TimeframeSegmentDataSeries.Clear(): not supported");
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

        public IRange XRange { get {
            return _segmentDates.Any() ? 
                new DateRange(_segmentDates[0], _segmentDates[_segmentDates.Count - 1]).AsDoubleRange() :
                new DoubleRange(double.MinValue, double.MaxValue);
        }}

        protected void OnDataSeriesChanged(DataSeriesUpdate dataSeriesUpdate) {
            var handler = DataSeriesChanged;
            if(handler != null)
                handler(this, new DataSeriesChangedEventArgs(dataSeriesUpdate));
        }

        protected IndexRange SearchDataIndexesOn(IRange range, SearchMode downSearchMode, SearchMode upSearchMode) {
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

        protected IndexRange NormalizeIndexRange(IndexRange indexRange) {
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

            UltrachartDebugLogger.Instance.WriteLine("GetIndicesRange(boxvol): Min={0}, Max={1}", indexRange.Min, indexRange.Max);

            return indexRange;
        }

        public static Tuple<DateTime, DateTime, int> GetTimeframePeriod(DateTime dt, int periodMinutes) {
            if(periodMinutes < 1 || periodMinutes > MaxTimeframe)
                throw new ArgumentOutOfRangeException("periodMinutes");

            DateTime start, end;
            int index;

            if(periodMinutes < TimeframeOneDay) {
                index = (int)(dt.TimeOfDay.TotalMinutes / periodMinutes);
                start = new DateTime(dt.Year, dt.Month, dt.Day) + TimeSpan.FromMinutes(index * periodMinutes);
                end = start + TimeSpan.FromMinutes(periodMinutes);
                if(end.Date != start.Date)
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

        #region suspender

        /// <summary>
        /// Gets a value indicating whether updates for the target are currently suspended
        /// </summary>
        public bool IsSuspended { get { return UpdateSuspender.GetIsSuspended(this); } }

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
            if (suspender.ResumeTargetOnDispose) {
                OnDataSeriesChanged(DataSeriesUpdate.DataChanged | DataSeriesUpdate.DataSeriesCleared);
            }

            if (suspender.Tag != null)
            {
                // Synchronization object on the parent surface
                Monitor.Exit(suspender.Tag);
            }
        }

        public void DecrementSuspend()
        {
        }

        #endregion

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
        // obsolete
        IPointSeries IDataSeries.ToPointSeries(IList column, ResamplingMode resamplingMode, IndexRange pointRange, int viewportWidth, bool isCategoryAxis) { throw new NotImplementedException(); }
        HitTestInfo IDataSeries.ToHitTestInfo(int index) { return HitTestInfo.Empty; }

        // obsolete
        IComparable IDataSeries.YMinPositive {get {throw new NotImplementedException();}}

        #endregion
    }

    /// <summary>
    /// base class for segmented data series
    /// </summary>
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    abstract public class TimeframeSegmentDataSeries<TPoint, TSegment> : TimeframeSegmentDataSeries
                                                                        where TPoint:PriceDataPoint
                                                                        where TSegment:TimeframeDataSegment<TPoint> {

        readonly UltraList<TSegment> _segments = new UltraList<TSegment>();
        internal new IUltraList<TSegment> Segments {get {return _segments;}}
        protected override IUltraReadOnlyList<TimeframeDataSegment> SegmentsReadOnly => (IUltraReadOnlyList<TimeframeDataSegment>)_segments.AsReadOnly();

        public TPoint LastTick {get; private set;}

        internal event Action<TSegment> NewSegment;

        /// <summary>
        /// Create data series.
        /// </summary>
        protected TimeframeSegmentDataSeries(int timeframe, double priceStep, bool sumTicks) : base(timeframe, priceStep, sumTicks) { }

        abstract protected TSegment CreateSegment(DateTime periodStart);
        abstract protected void OnNewPoint(TPoint point);

        public void Append(TPoint point) {
            lock(SyncRoot) {
                var period = GetTimeframePeriod(point.Time, Timeframe);

                if(_segments.Count > 0 && _segments[_segments.Count - 1].Time > period.Item1)
                    throw new ArgumentOutOfRangeException("point", "data must be ordered by time");

                AddOrUpdateSegment(period.Item1, point);
            }

            OnDataSeriesChanged(DataSeriesUpdate.DataChanged);
        }

        void AddOrUpdateSegment(DateTime periodStart, TPoint point) {
            if(PriceStep <= 0 || PriceStep.IsNaN()) return;

            TSegment segment;
            var newSegment = false;

            if(_segments.Count == 0 || _segments[_segments.Count - 1].Time != periodStart) {
                segment = CreateSegment(periodStart);

                newSegment = true;
                _segments.Add(segment);
                OnNewSegment(segment);
            } else {
                segment = _segments[_segments.Count - 1];
            }

            LastTick = point;

            segment.AddPoint(point);
            OnNewPoint(point);

            if(newSegment)
                NewSegment.SafeInvoke(segment);
        }

        protected override IList<DateTime> XValues { get { return SegmentDates; }}
        protected override double[] YValues { get { return _segments.Select(s => s.Y).ToArray(); }}

        public override double GetYMinAt(int index, double existingYMin) {
            return Math.Min(_segments[index].MinPrice, existingYMin);
        }

        public override double GetYMaxAt(int index, double existingYMax) {
            return Math.Max(_segments[index].MaxPrice, existingYMax);
        }

        public override IRange GetWindowedYRange(IndexRange xIndexRange) {
            double min, max;

            var range = (IndexRange)xIndexRange.Clone();

            if(range.Min < 0) range.Min = 0;
            if(range.Max >= _segments.Count) range.Max = _segments.Count - 1;

            TimeframeDataSegment.MinMax(_segments.Skip(range.Min).Take(range.Max - range.Min + 1), out min, out max);

            return new DoubleRange(min, max);
        }

        public override IRange YRange { get {
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
    }
}
