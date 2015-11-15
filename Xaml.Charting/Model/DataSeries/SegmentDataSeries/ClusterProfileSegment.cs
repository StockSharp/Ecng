using System;
using System.Linq;
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting.Model.DataSeries.SegmentDataSeries {
    public class ClusterProfileSegment : TimeframeDataSegment<ClusterProfileDataPoint> {
        readonly ClusterProfileDataSeries _series;
        readonly int[] _maxValues = new int[ClusterProfileDataPoint.NumParameters];
        readonly bool[] _maxUpdateFlags = new bool[ClusterProfileDataPoint.NumParameters];

        internal IPriceData[] Data {get {return _levels.Cast<IPriceData>().ToArray();}}

        public ClusterProfileSegment(ClusterProfileDataSeries series, DateTime time, double priceStep, int index) : base(time, priceStep, index) {
            _series = series;
        }

        public override void AddPoint(ClusterProfileDataPoint point) {
            var price = point.NormalizedPrice(PriceStep);

            var pv = (PriceData)GetPriceLevel(price, p => new PriceData(p));

            if(_series.SumTicks) {
                pv.AddData(point);

                for(var i = 0; i < ClusterProfileDataPoint.NumParameters; ++i) {
                    var val = pv[i].Value;
                    if(val > _maxValues[i])
                        _maxValues[i] = val;
                }
            } else {
                for(var i = 0; i < ClusterProfileDataPoint.NumParameters; ++i) {
                    var val = point[i];
                    // если новое значение меньше старого, то для вычисления максимума нужно проходить по всему сегменту.
                    var needUpdate = val < pv[i].Value;
                    _maxUpdateFlags[i] = needUpdate;

                    if(!needUpdate && val > _maxValues[i])
                        _maxValues[i] = val;
                }

                pv.UpdateData(point);
            }
        }

        public int GetMaxValue(int index = 0) {
            if(_maxUpdateFlags[index]) {
                _maxUpdateFlags[index] = false;
                _maxValues[index] = _levels.Where(l => l != null).Cast<PriceData>().Select(pd => pd[index].Value).Max();
            }

            return _maxValues[index];
        }

        internal interface IPriceData {
            double Price {get;} 
            IntWithNumDigits this[int index] {get;}
        }

        class PriceData : PriceLevel, IPriceData {
            readonly IntWithNumDigits[] _data = new IntWithNumDigits[ClusterProfileDataPoint.NumParameters];

            public PriceData(double price) : base(price) { }

            public IntWithNumDigits this[int paramNum] {get {return _data[paramNum];}}

            public void UpdateData(ClusterProfileDataPoint point) {
                for(var i=0; i<ClusterProfileDataPoint.NumParameters; ++i)
                    _data[i] = new IntWithNumDigits(point[i]);
            }

            public void AddData(ClusterProfileDataPoint point) {
                for(var i=0; i<ClusterProfileDataPoint.NumParameters; ++i)
                    _data[i] = new IntWithNumDigits(point[i] + _data[i].Value);
            }
        }
    }

    struct IntWithNumDigits {
        public IntWithNumDigits(int val) {
            Value = val;
            Digits = val.NumDigitsInPositiveNumber();
        }
        public readonly int Value;
        public readonly byte Digits;
    }
}