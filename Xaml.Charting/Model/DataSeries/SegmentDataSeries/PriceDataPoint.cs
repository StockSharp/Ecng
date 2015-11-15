using System;
using System.Collections.Generic;
using System.Linq;
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting.Model.DataSeries.SegmentDataSeries {
    public abstract class PriceDataPoint {
        readonly DateTime _time;
        readonly double _price;

        public DateTime Time {get {return _time;}}
        public double Price {get {return _price;}}

        protected PriceDataPoint(DateTime time, double price) {
            if(price <= 0) throw new ArgumentException("price");

            _time = time;
            _price = price;
        }

        internal double NormalizedPrice(double step) {
            return NormalizePrice(_price, step);
        }

        internal static double NormalizePrice(double price, double priceStep) {
            return price.Round(priceStep);
        }

        internal static double[] GeneratePrices(double min, double max, double step) {
            min = NormalizePrice(min, step);
            max = NormalizePrice(max, step);

            var result = new double[1 + (int)Math.Round((max - min) / step)];

            for(var i = 0; i < result.Length; ++i)
                result[i] = NormalizePrice(min + i * step, step);

            return result;
        }
    }

    public class PriceVolumeDataPoint : PriceDataPoint {
        readonly int _volume;

        public int Volume {get {return _volume;}}

        public PriceVolumeDataPoint(DateTime time, double price, int volume) : base(time, price) {
            if(volume <= 0) throw new ArgumentException("volume");

            _volume = volume;
        }
    }

    public class ClusterProfileDataPoint : PriceDataPoint {
        public const int NumParameters = 1;
        readonly int[] _params;

        public int[] Params {get {return _params;}} 

        public ClusterProfileDataPoint(DateTime time, double price, int volume) : base(time, price) {
            _params = new[] {volume};
        }

        public int this[int paramNum] {
            get {return _params[paramNum];}
        }
    }
}
