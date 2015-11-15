using System;
using System.Linq;
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting.Model.DataSeries.SegmentDataSeries {
    public class MinVolumeSegment : TimeframeDataSegment<PriceVolumeDataPoint> {
        readonly BoxVolumeDataSeries _series;

        int _maxValue, _maxDigits;

        public int MaxValue {get {return _maxValue;}}
        public int MaxDigits {get {return _maxDigits;}}

        public IPriceValue[] Values {get {return _levels.Cast<IPriceValue>().ToArray();}}

        public MinVolumeSegment(BoxVolumeDataSeries series, DateTime time, double priceStep, int index) : base(time, priceStep, index) {
            _series = series;
        }

        public override void AddPoint(PriceVolumeDataPoint point) {
            var price = point.NormalizedPrice(PriceStep);

            if(point.Volume == 0) return;

            var pv = (PriceData)GetPriceLevel(price, p => new PriceData(p));

            var oldValue = pv.Value;

            if(_series.SumTicks)
                pv.AddValue(point.Volume);
            else
                pv.UpdateValue(point.Volume);

            if(pv.Value > oldValue && pv.Value > _maxValue) {
                _maxValue = pv.Value;
                _maxDigits = pv.Digits;
            } else if(pv.Value < oldValue) {
                _maxValue = _levels.Cast<IPriceValue>().Max(pd => pd.Value);
                _maxDigits = _maxValue.NumDigitsInPositiveNumber();
            }
        }

        public int GetValueByPrice(double price) {
            if(_levels.Count == 0) return 0;

            price = PriceDataPoint.NormalizePrice(price, PriceStep);

            if(price < MinPrice || price > MaxPrice) return 0;

            var arr = _levels.ItemsArray;
            var index = (int)Math.Round((price - arr[0].Price) / PriceStep);

            if(index < 0 || index >= _levels.Count)
                return 0;

            var pv = arr[index];
            return pv == null ? 0 : ((PriceData)pv).Value;
        }
    
        public interface IPriceValue {
            double Price {get;} 
            int Value {get;}
            int Digits {get;}
        }

        class PriceData : PriceLevel, IPriceValue {
            public PriceData(double price) : base(price) { }

            public int Value {get; private set;}
            public int Digits {get; private set;}

            public void UpdateValue(int val) {
                Value = val;
                Digits = val.NumDigitsInPositiveNumber();
            }

            public void AddValue(int val) {
                Value += val;
                Digits = Value.NumDigitsInPositiveNumber();
            }
        }
    }
}