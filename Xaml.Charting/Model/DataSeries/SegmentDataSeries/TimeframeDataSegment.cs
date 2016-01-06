using System;
using System.Collections.Generic;
using System.Linq;
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting.Model.DataSeries.SegmentDataSeries {
    public class TimeframeDataSegment : IPoint {
        public class PriceLevel {
            public PriceLevel(double price) { Price = price; }

            public double Price {get;}
            public long Value {get; private set;}
            public int Digits {get; private set;}

            public void AddValue(long val) {
                Value += val;
                Digits = Value.NumDigitsInPositiveNumber();
            }

            public void UpdateValue(long val) {
                Value = val;
                Digits = Value.NumDigitsInPositiveNumber();
            }
        }

        readonly UltraList<PriceLevel> _levels = new UltraList<PriceLevel>(4);
        double _minPrice, _maxPrice;

        bool IsEmtpy => _levels.Count == 0;

        public IEnumerable<PriceLevel> Values => _levels;
        public IEnumerable<double> AllPrices  => _levels.Select(l => l.Price);

        public double PriceStep {get;}
        public int Index {get;}
        public DateTime Time {get;}

        public double MinPrice => _minPrice;
        public double MaxPrice => _maxPrice;

        public long MaxValue {get; private set;}
        public int MaxDigits {get; private set;}

        public double X => Time.Ticks;
        public double Y => !IsEmtpy ? _maxPrice : double.NaN;

        public TimeframeDataSegment(DateTime time, double priceStep, int index) {
            if(time.Second != 0 || time.Millisecond != 0)
                throw new InvalidOperationException("invalid time");

            Time = time;
            PriceStep = priceStep;
            Index = index;

            _minPrice = double.MaxValue;
            _maxPrice = double.MinValue;
        }

        PriceLevel GetPriceLevel(double normPrice) {
            if(normPrice < MinPrice) _minPrice = normPrice;
            if(normPrice > MaxPrice) _maxPrice = normPrice;

            var numElements = 1 + (int)Math.Round((MaxPrice - MinPrice) / PriceStep);

            var levelsArrSizeChanged = _levels.EnsureMinSize(numElements);

            var arr = _levels.ItemsArray;

            if(numElements == 1)
                return arr[0] ?? (arr[0] = new PriceLevel(normPrice));

            var first = arr[0];
            if(first == null) {
                var errMsg = $"ERROR: GetPriceLevel({normPrice}): first item is null for time={Time}";
                UltrachartDebugLogger.Instance.WriteLine(errMsg);
                throw new InvalidOperationException(errMsg);
            }

            var index = (int)Math.Round((normPrice - first.Price) / PriceStep);
            if(index >= 0) {
                if(levelsArrSizeChanged) {
                    for(var i = 0; i < _levels.Count; ++i)
                        if(arr[i] == null)
                            arr[i] = new PriceLevel((MinPrice + i * PriceStep).NormalizePrice(PriceStep));
                }

                return arr[index];
            }

            index = -index;
            for(var i = numElements - 1; i >= 0; --i) {
                var price = (MinPrice + i * PriceStep).NormalizePrice(PriceStep);

                arr[i] = i >= index ? 
                         (arr[i - index] ?? new PriceLevel(price)) : 
                         new PriceLevel(price);

            }

            return arr[0];
        }

        public void AddPoint(double price, long volume) {
            if(volume == 0) return;

            price = price.NormalizePrice(PriceStep);

            var level = GetPriceLevel(price);

            level.AddValue(volume);

            if(level.Value > MaxValue) {
                MaxValue = level.Value;
                MaxDigits = level.Digits;
            }
        }

        public void UpdatePoint(double price, long volume) {
            price = price.NormalizePrice(PriceStep);

            var level = GetPriceLevel(price);

            var oldValue = level.Value;

            level.UpdateValue(volume);

            if(level.Value > oldValue && level.Value > MaxValue) {
                MaxValue = volume;
                MaxDigits = level.Digits;
            } else if(level.Value < oldValue) {
                MaxValue = _levels.Max(l => l.Value);
                MaxDigits = MaxValue.NumDigitsInPositiveNumber();
            }
        }

        public long GetValueByPrice(double price) {
            if(_levels.Count == 0) return 0;

            price = price.NormalizePrice(PriceStep);

            if(price < MinPrice || price > MaxPrice) return 0;

            var arr = _levels.ItemsArray;
            var index = (int)Math.Round((price - arr[0].Price) / PriceStep);

            if(index < 0 || index >= _levels.Count)
                return 0;

            var pv = arr[index];
            return pv?.Value ?? 0;
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

                priceStep = seg.PriceStep;
            }

            if(priceStep > 0)
                numCellsY = 1 + (int)Math.Round((maxPrice - minPrice) / priceStep);
        }
    }

    public class TimeframeSegmentWrapper : IPoint {
        public TimeframeDataSegment Segment {get;}

        public double X {get;}
        public double Y => Segment.Y;

        public TimeframeSegmentWrapper(TimeframeDataSegment segment, double index) {
            Segment = segment;
            X = index;
        }
    }
}
