using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Ecng.Xaml.Charting.Numerics;
using Ecng.Xaml.Charting.Numerics.PointResamplers;

// ReSharper disable once CheckNamespace
namespace Ecng.Xaml.Charting.Model.DataSeries.SegmentDataSeries {
    /// <summary>
    /// Data series for BoxVolume chart
    /// </summary>
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public class BoxVolumeDataSeries : TimeframeSegmentDataSeries<PriceVolumeDataPoint, MinVolumeSegment> {
        public override DataSeriesType DataSeriesType {get {return DataSeriesType.BoxVolume;}}

        /// <summary>
        /// Create box volume data series.
        /// </summary>
        public BoxVolumeDataSeries(int timeframe, double priceStep, bool sumTicks = false) : base(timeframe, priceStep, sumTicks) {
        }

        protected override MinVolumeSegment CreateSegment(DateTime periodStart) {
            return new MinVolumeSegment(this, periodStart, PriceStep, Count);
        }

        protected override void OnNewPoint(PriceVolumeDataPoint point) {
        }

        BoxVolumePointSeries _lastPointSeries;

        public override IPointSeries ToPointSeries(ResamplingMode resamplingMode, IndexRange pointRange, int viewportWidth, bool isCategoryAxis, bool? dataIsDisplayedAs2D, IRange visibleXRange, IPointResamplerFactory factory) {
            if(!pointRange.IsDefined)
                return null;

            var pointSeries = new BoxVolumePointSeries(_lastPointSeries, Segments.ItemsArray, pointRange, visibleXRange, PriceStep);

            _lastPointSeries = pointSeries;

            return pointSeries;
        }

        public IEnumerable<BoxBytes> GetByteSeries()
        {
            return Segments.SelectMany(seg => seg.Values.Select(pv => 
                new BoxBytes {
                    Dtm = (seg.Time).Ticks, 
                    Price = PriceDataPoint.NormalizePrice(pv.Price, PriceStep), 
                    Volume = pv.Value
            })).ToArray();
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BoxBytes {
        public long Dtm;
        public double Price;
        public int Volume;
    }
}
