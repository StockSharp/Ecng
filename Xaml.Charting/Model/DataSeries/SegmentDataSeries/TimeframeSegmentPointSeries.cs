using System.Collections.Generic;
using System.Linq;
using Ecng.Xaml.Charting.Common.Extensions;

namespace Ecng.Xaml.Charting.Model.DataSeries.SegmentDataSeries {
    class TimeframeSegmentPointSeries : IPointSeries {
        readonly TimeframeSegmentWrapper[] _segments;
        readonly DoubleRange _yRange;
        IUltraList<double> _xValues;

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

        public TimeframeSegmentPointSeries(TimeframeSegmentPointSeries lastPointSeries, TimeframeDataSegment[] segments, IndexRange dataRange, IRange visibleRange, double priceStep) {
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

            if(lastPointSeries != null && lastPointSeries.DataRange.Min == DataRange.Min && lastPointSeries.DataRange.Max == DataRange.Max) {
                // диапазон сегментов не изменился с прошлого рендера => значит мог измениться только последний сегмент =>
                // => оптимизация агрегации (берем все агрегаторы из предыдущей пойнт-серии и обновляем только то, что изменилось в последнем сегменте)

                var lastSegmentIndex = DataRange.Max;

                var newAllPrices = new HashSet<double>();
                lastPointSeries.AllPrices.ForEachDo(price => newAllPrices.Add(price));
                segments[lastSegmentIndex].Values.ForEachDo(pd => newAllPrices.Add(pd.Price));

                AllPrices = newAllPrices.ToArray();

            } else { // иначе создаем пойнт-серию заново (без инициализированных агрегаторов)

                var prices = new HashSet<double>();

                foreach(var d in Segments.SelectMany(seg => seg.Segment.Values.Where(pd => pd != null)))
                    prices.Add(d.Price);

                AllPrices = prices.ToArray();
            }

        }

        public DoubleRange GetYRange() { return _yRange; }
    }
}
