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

        readonly UltraList<TimeframeDataSegment> _segments = new UltraList<TimeframeDataSegment>();
        readonly UltraList<DateTime> _segmentDates = new UltraList<DateTime>();
        IUltraReadOnlyList<DateTime> SegmentDates => _segmentDates.AsReadOnly();

        TimeframeSegmentPointSeries _lastPointSeries;

        public DataSeriesType DataSeriesType => DataSeriesType.TimeframeSegment;

        public int Timeframe {get;}
        public double PriceStep {get;}

        /// <summary>
        /// Event raised whenever points are added to, removed or one or more DataSeries properties changes
        /// </summary>
        /// <remarks></remarks>
        public event EventHandler<DataSeriesChangedEventArgs> DataSeriesChanged;

        #region sync objects

        readonly object _syncRoot = new object();

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
        double[] YValues => _segments.Select(s => s.Y).ToArray();

        IList IDataSeries.XValues => (IList)XValues;
        IList<DateTime> IDataSeries<DateTime, double>.XValues => XValues;

        IList IDataSeries.YValues => YValues;
        IList<double> IDataSeries<DateTime, double>.YValues => YValues;

        public TimeframeSegmentDataSeries(int timeframe, double priceStep) {
            if(timeframe < 1 || timeframe > MaxTimeframe) throw new ArgumentOutOfRangeException(nameof(timeframe));
            if(priceStep <= 0d || priceStep.IsNaN()) throw new ArgumentOutOfRangeException(nameof(priceStep));

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

            UltrachartDebugLogger.Instance.WriteLine("GetIndicesRange(boxvol): Min={0}, Max={1}", indexRange.Min, indexRange.Max);

            return indexRange;
        }

        public static Tuple<DateTime, DateTime, int> GetTimeframePeriod(DateTime dt, int periodMinutes) {
            if(periodMinutes < 1 || periodMinutes > MaxTimeframe)
                throw new ArgumentOutOfRangeException(nameof(periodMinutes));

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

        internal static double[] GeneratePrices(double min, double max, double step) {
            min = min.NormalizePrice(step);
            max = max.NormalizePrice(step);

            var result = new double[1 + (int)Math.Round((max - min) / step)];

            for (var i = 0; i < result.Length; ++i)
                result[i] = (min + i * step).NormalizePrice(step);

            return result;
        }

        public void Append(DateTime time, double price, int volume) {
            lock(SyncRoot) {
                var period = GetTimeframePeriod(time, Timeframe);

                if(_segments.Count > 0 && _segments[_segments.Count - 1].Time > period.Item1)
                    throw new ArgumentOutOfRangeException(nameof(time), "data must be ordered by time");

                AddOrUpdateSegment(period.Item1, price, volume);
            }

            DataSeriesChanged?.Invoke(this, new DataSeriesChangedEventArgs(DataSeriesUpdate.DataChanged));
        }

        void AddOrUpdateSegment(DateTime periodStart, double price, int volume) {
            if(PriceStep <= 0 || PriceStep.IsNaN()) return;

            TimeframeDataSegment segment;

            if(_segments.Count == 0 || _segments[_segments.Count - 1].Time != periodStart) {
                segment = new TimeframeDataSegment(periodStart, PriceStep, _segments.Count);

                _segments.Add(segment);
                _segmentDates.Add(segment.Time);
            } else {
                segment = _segments[_segments.Count - 1];
            }

            segment.AddPoint(price, volume);

            //OnNewPoint(point); // todo aggregation
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

        public IPointSeries ToPointSeries(ResamplingMode resamplingMode, IndexRange pointRange, int viewportWidth, bool isCategoryAxis, bool? dataIsDisplayedAs2D, IRange visibleXRange, IPointResamplerFactory factory) {
            if(!pointRange.IsDefined)
                return null;

            var pointSeries = new TimeframeSegmentPointSeries(_lastPointSeries, Segments.ItemsArray, pointRange, visibleXRange, PriceStep);

            _lastPointSeries = pointSeries;

            return pointSeries;
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
}
