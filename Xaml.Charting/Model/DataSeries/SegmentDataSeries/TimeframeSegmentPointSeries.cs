using System.Collections.Generic;
using System.Linq;

namespace Ecng.Xaml.Charting.Model.DataSeries.SegmentDataSeries {
    class TimeframeSegmentPointSeries : IPointSeries {
        readonly TimeframeSegmentWrapper[] _segments;
        readonly DoubleRange _yRange;
        IUltraList<double> _xValues;

        readonly Dictionary<double, long> _volumeByPrice = new Dictionary<double, long>();

        public IUltraList<double> XValues => _xValues ?? (_xValues = new UltraList<double>(_segments.Select(s => s.X)));
        public IUltraList<double> YValues => null;

        public TimeframeSegmentWrapper[] Segments => _segments;
        protected IUltraReadOnlyList<TimeframeSegmentWrapper> SegmentsReadOnly => new UltraReadOnlyList<TimeframeSegmentWrapper>(_segments);

        public int Count => _segments.Length;

        public IEnumerable<double> AllPrices {get;}

        public IPoint this[int index] => _segments[index];

        public double PriceStep {get;}

        public IndexRange DataRange {get;}
        public IndexRange VisibleRange {get;}

        public TimeframeSegmentPointSeries(TimeframeDataSegment[] segments, IndexRange dataRange, IRange visibleRange, double priceStep) {
            PriceStep = priceStep;
            DataRange = (IndexRange)dataRange.Clone();
            VisibleRange = visibleRange as IndexRange ?? dataRange;

            _segments = new TimeframeSegmentWrapper[dataRange.Max - dataRange.Min + 1];

            for(var i = dataRange.Min; i <= dataRange.Max; ++i) {
                var seg = segments[i];
                _segments[i - dataRange.Min] = new TimeframeSegmentWrapper(seg, i);
            }

            double min, max;
            min = double.MaxValue;
            max = double.MinValue;

            foreach(var s in _segments) {
                if(s.Segment.MinPrice < min) min = s.Segment.MinPrice;
                if(s.Segment.MaxPrice > max) max = s.Segment.MaxPrice;
            }

            _yRange = new DoubleRange(min, max);

            var prices = new HashSet<double>();

            foreach(var pv in Segments.Where(seg => seg.Segment.Index >= VisibleRange.Min && seg.Segment.Index <= VisibleRange.Max)
                                      .SelectMany(seg => seg.Segment.Values.Where(pv => pv != null))) {

                prices.Add(pv.Price);

                long vol;
                _volumeByPrice.TryGetValue(pv.Price, out vol);
                _volumeByPrice[pv.Price] = pv.Value + vol;
            }

            AllPrices = prices.ToArray();
        }

        public DoubleRange GetYRange() { return _yRange; }

        public long GetVolumeByPrice(double normalizedPrice) {
            long vol;
            _volumeByPrice.TryGetValue(normalizedPrice, out vol);
            return vol;
        }
    }
}
