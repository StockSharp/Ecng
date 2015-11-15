using System;
using System.Collections.Generic;
using System.Linq;
using Ecng.Xaml.Charting.Common.Extensions;

namespace Ecng.Xaml.Charting.Model.DataSeries.SegmentDataSeries {
    abstract class TimeframeSegmentPointSeries : IPointSeries {
        abstract public IUltraList<double> XValues {get;}
        abstract public IUltraList<double> YValues {get;}
        abstract public int Count {get;}
        abstract public IPoint this[int index] {get;}
        abstract public DoubleRange GetYRange();

        abstract public double PriceStep {get;}

        internal IUltraReadOnlyList<TimeframeSegmentWrapper> BaseSegments {get {return SegmentsReadOnly;}}

        abstract protected IUltraReadOnlyList<TimeframeSegmentWrapper> SegmentsReadOnly {get;}
    }

    abstract class TimeframeSegmentPointSeries<T> : TimeframeSegmentPointSeries, IPointSeries where T:TimeframeDataSegment {
        readonly TimeframeSegmentWrapper<T>[] _segments;
        readonly DoubleRange _yRange;
        readonly double _priceStep;
        IUltraList<double> _xValues;

        public override IUltraList<double> XValues {get {return _xValues ?? (_xValues = new UltraList<double>(_segments.Select(s => s.X)));}}
        public override IUltraList<double> YValues {get {return null;}}

        public TimeframeSegmentWrapper<T>[] Segments => _segments;
        protected override IUltraReadOnlyList<TimeframeSegmentWrapper> SegmentsReadOnly => new UltraReadOnlyList<TimeframeSegmentWrapper>(_segments);

        public override int Count {get {return _segments.Length;}}

        abstract public IEnumerable<double> AllPrices {get;}

        public override IPoint this[int index] { get { return _segments[index]; }}

        public override double PriceStep {get {return _priceStep;}}

        public IndexRange DataRange {get;}
        public IndexRange VisibleRange {get;}

        public TimeframeSegmentPointSeries(T[] segments, IndexRange dataRange, IRange visibleRange, double priceStep) {
            _priceStep = priceStep;
            DataRange = (IndexRange)dataRange.Clone();
            VisibleRange = visibleRange as IndexRange ?? dataRange;

            _segments = new TimeframeSegmentWrapper<T>[dataRange.Max - dataRange.Min + 1];

            for(var i = dataRange.Min; i <= dataRange.Max; ++i) {
                var seg = segments[i];
                _segments[i - dataRange.Min] = new TimeframeSegmentWrapper<T>(seg, i);
            }

            double min, max;
            min = double.MaxValue;
            max = double.MinValue;

            foreach(var s in _segments) {
                if(s.Segment.MinPrice < min) min = s.Segment.MinPrice;
                if(s.Segment.MaxPrice > max) max = s.Segment.MaxPrice;
            }

            _yRange = new DoubleRange(min, max);
        }

        public override DoubleRange GetYRange() { return _yRange; }
    }

    class BoxVolumePointSeries : TimeframeSegmentPointSeries<MinVolumeSegment> {
        readonly double[] _allPrices;

        public override IEnumerable<double> AllPrices {get {return _allPrices;}}

        public BoxVolumePointSeries(BoxVolumePointSeries lastPointSeries, MinVolumeSegment[] segments, IndexRange dataRange, IRange visibleRange, double priceStep) : base(segments, dataRange, visibleRange, priceStep) {
            if(lastPointSeries != null && lastPointSeries.DataRange.Min == DataRange.Min && lastPointSeries.DataRange.Max == DataRange.Max) {
                // диапазон сегментов не изменился с прошлого рендера => значит мог измениться только последний сегмент =>
                // => оптимизация агрегации (берем все агрегаторы из предыдущей пойнт-серии и обновляем только то, что изменилось в последнем сегменте)

                var lastSegmentIndex = DataRange.Max;

                var newAllPrices = new HashSet<double>();
                lastPointSeries.AllPrices.ForEachDo(price => newAllPrices.Add(price));
                segments[lastSegmentIndex].Values.ForEachDo(pd => newAllPrices.Add(pd.Price));

                _allPrices = newAllPrices.ToArray();

            } else { // иначе создаем пойнт-серию заново (без инициализированных агрегаторов)

                var prices = new HashSet<double>();

                foreach(var d in Segments.SelectMany(seg => seg.Segment.Values.Where(pd => pd != null)))
                    prices.Add(d.Price);

                _allPrices = prices.ToArray();
            }
        }
    }

    class ClusterProfilePointSeries : TimeframeSegmentPointSeries<ClusterProfileSegment> {
        readonly double[] _allPrices;

        public override IEnumerable<double> AllPrices => _allPrices;

        public ClusterProfilePointSeries(ClusterProfilePointSeries lastPointSeries, ClusterProfileSegment[] segments, IndexRange dataRange, IRange visibleRange, double priceStep) : base(segments, dataRange, visibleRange, priceStep) {
            if(lastPointSeries != null && lastPointSeries.DataRange.Min == DataRange.Min && lastPointSeries.DataRange.Max == DataRange.Max) {
                // диапазон сегментов не изменился с прошлого рендера => значит мог измениться только последний сегмент

                var lastSegmentIndex = DataRange.Max;

                var newAllPrices = new HashSet<double>();
                lastPointSeries.AllPrices.ForEachDo(price => newAllPrices.Add(price));
                segments[lastSegmentIndex].Data.ForEachDo(pd => newAllPrices.Add(pd.Price));

                _allPrices = newAllPrices.ToArray();

            } else { // иначе создаем пойнт-серию заново (без инициализированных агрегаторов)

                var prices = new HashSet<double>();

                foreach(var d in Segments.SelectMany(seg => seg.Segment.Data.Where(pd => pd != null)))
                    prices.Add(d.Price);

                _allPrices = prices.ToArray();
            }
        }
    }
}
