using System;
using System.Reflection;
using Ecng.Xaml.Charting.Numerics;
using Ecng.Xaml.Charting.Numerics.PointResamplers;

namespace Ecng.Xaml.Charting.Model.DataSeries.SegmentDataSeries {
    /// <summary>
    /// Data series for ClusterProfile chart
    /// </summary>
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public class ClusterProfileDataSeries : TimeframeSegmentDataSeries<ClusterProfileDataPoint, ClusterProfileSegment> {

        public override DataSeriesType DataSeriesType {get {return DataSeriesType.ClusterProfile;}}

        /// <summary>
        /// Create box volume data series.
        /// </summary>
        public ClusterProfileDataSeries(int timeframe, double priceStep, bool sumTicks = false) : base(timeframe, priceStep, sumTicks) {
        }

        protected override ClusterProfileSegment CreateSegment(DateTime periodStart) {
            return new ClusterProfileSegment(this, periodStart, PriceStep, Count);
        }

        protected override void OnNewPoint(ClusterProfileDataPoint point) {
        }

        ClusterProfilePointSeries _lastPointSeries;

        public override IPointSeries ToPointSeries(ResamplingMode resamplingMode, IndexRange pointRange, int viewportWidth, bool isCategoryAxis, bool? dataIsDisplayedAs2D, IRange visibleXRange, IPointResamplerFactory factory) {
            if(!pointRange.IsDefined) return null;

            var pointSeries = new ClusterProfilePointSeries(_lastPointSeries, Segments.ItemsArray, pointRange, visibleXRange, PriceStep);

            _lastPointSeries = pointSeries;

            return pointSeries;
        }
    }
}
