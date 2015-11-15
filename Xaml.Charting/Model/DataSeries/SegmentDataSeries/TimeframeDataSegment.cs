using System;
using System.Collections.Generic;
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting.Model.DataSeries.SegmentDataSeries {
    abstract public class TimeframeDataSegment : IPoint {
        protected class PriceLevel {
            readonly double _price;

            public PriceLevel(double price) { _price = price; }

            public double Price {get {return _price;}}
        }

        protected readonly IUltraList<PriceLevel> _levels = new UltraList<PriceLevel>(4);
        readonly int _index;
        readonly double _priceStep;
        readonly DateTime _time;
        double _minPrice, _maxPrice;

        protected bool IsEmtpy {get {return _levels.Count == 0;}}

        internal int BandIndex {get; set;}

        public double[] AllPrices { get {
            if(_levels.Count == 0) return new double[0];

            var p0 = _levels[0].Price;
            var arr = new double[_levels.Count];
            for(var i = 0; i < _levels.Count; ++i)
                arr[i] = PriceDataPoint.NormalizePrice(p0 + i * PriceStep, PriceStep);

            return arr;
        }}

        public double PriceStep {get {return _priceStep;}}
        public int Index {get {return _index;}}
        public DateTime Time {get {return _time;}}

        public double MinPrice {get {return _minPrice;}}
        public double MaxPrice {get {return _maxPrice;}}

        public double X {get {return _time.Ticks;}}
        public double Y {get {return !IsEmtpy ? _maxPrice : double.NaN;}}

        protected TimeframeDataSegment(DateTime time, double priceStep, int index) {
            if(time.Second != 0 || time.Millisecond != 0)
                throw new InvalidOperationException("invalid time");

            _time = time;
            _priceStep = priceStep;
            _index = index;

            _minPrice = double.MaxValue;
            _maxPrice = double.MinValue;
        }

        protected PriceLevel GetPriceLevel(double normPrice, Func<double, PriceLevel> creator) {
            if(normPrice < MinPrice) _minPrice = normPrice;
            if(normPrice > MaxPrice) _maxPrice = normPrice;

            var numElements = 1 + (int)Math.Round((MaxPrice - MinPrice) / PriceStep);

            var levels = ((UltraList<PriceLevel>)_levels);
            var levelsArrSizeChanged = levels.EnsureMinSize(numElements);

            var arr = _levels.ItemsArray;

            if(numElements == 1)
                return arr[0] ?? (arr[0] = creator(normPrice));

            var first = arr[0];
            if(first == null) {
                var errMsg = string.Format("ERROR: GetPriceLevel({0}): first item is null for time={1}", normPrice, Time);
                UltrachartDebugLogger.Instance.WriteLine(errMsg);
                throw new InvalidOperationException(errMsg);
            }

            var index = (int)Math.Round((normPrice - first.Price) / PriceStep);
            if(index >= 0) {
                if(levelsArrSizeChanged) {
                    for(var i = 0; i < levels.Count; ++i)
                        if(arr[i] == null)
                            arr[i] = creator(PriceDataPoint.NormalizePrice(MinPrice + i * PriceStep, PriceStep));
                }

                return arr[index];
            }

            index = -index;
            for(var i = numElements - 1; i >= 0; --i) {
                var price = PriceDataPoint.NormalizePrice(MinPrice + i * PriceStep, PriceStep);

                arr[i] = i >= index ? 
                         (arr[i - index] ?? creator(price)) : 
                         creator(price);

            }

            return arr[0];
        }

        public static void MinMax(IEnumerable<TimeframeDataSegment> segments, out double minPrice, out double maxPrice) {
            int x;
            MinMax(segments, out minPrice, out maxPrice, out x);
        }

        public static void MinMax(IEnumerable<TimeframeDataSegment> segments, out double minPrice, out double maxPrice, out int numCellsY) {
            minPrice = double.MaxValue;
            maxPrice = double.MinValue;
            numCellsY = 0;
            double priceStep = 0;

            foreach(var seg in segments) {
                if(seg.MinPrice < minPrice) minPrice = seg.MinPrice;
                if(seg.MaxPrice > maxPrice) maxPrice = seg.MaxPrice;

                priceStep = seg._priceStep;
            }

            if(priceStep > 0)
                numCellsY = 1 + (int)Math.Round((maxPrice - minPrice) / priceStep);
        }
    }

    public abstract class TimeframeDataSegment<T> : TimeframeDataSegment where T : PriceDataPoint {
        protected TimeframeDataSegment(DateTime time, double priceStep, int index) : base(time, priceStep, index) {}

        public abstract void AddPoint(T point);
    }

    abstract public class TimeframeSegmentWrapper : IPoint {
        abstract public double X {get;}
        abstract public double Y {get;}
        abstract public TimeframeDataSegment BaseSegment {get;}
    }

    public class TimeframeSegmentWrapper<T> : TimeframeSegmentWrapper,IPoint where T:TimeframeDataSegment {
        readonly T _segment;
        readonly double _index;

        public override TimeframeDataSegment BaseSegment {get {return _segment;}}
        public T Segment {get {return _segment;}}

        public override double X {get {return _index;}}
        public override double Y {get {return _segment.Y;}}

        public TimeframeSegmentWrapper(T segment, double index) {
            _segment = segment;
            _index = index;
        }
    }
}
