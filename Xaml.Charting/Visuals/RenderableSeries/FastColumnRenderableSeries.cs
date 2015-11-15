// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// FastColumnRenderableSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
// For full terms and conditions of the license, see http://www.ultrachart.com/ultrachart-eula/
// 
// This source code is protected by international copyright law. Unauthorized
// reproduction, reverse-engineering, or distribution of all or any portion of
// this source code is strictly prohibited.
// 
// This source code contains confidential and proprietary trade secrets of
// ulc software Services Ltd., and should at no time be copied, transferred, sold,
// distributed or made available without express written permission.
// *************************************************************************************
using System;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Licensing;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Licensing.Core;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    /// <summary>
    /// Provides Fast Column (Bar) series rendering
    /// </summary>
    [UltrachartLicenseProvider(typeof(RenderableSeriesUltrachartLicenseProvider))]
    public class FastColumnRenderableSeries : BaseColumnRenderableSeries
    {
        /// <summary>
        /// Initializes a new instance of the <seealso cref="FastColumnRenderableSeries" /> class.
        /// </summary>
        public FastColumnRenderableSeries()
        {
            DefaultStyleKey = typeof(FastColumnRenderableSeries);
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="BaseRenderableSeries"/> is valid for drawing.
        /// </summary>
        protected override bool GetIsValidForDrawing()
        {
            var isValid = base.GetIsValidForDrawing() &&
                          ((SeriesColor.A != 0 && StrokeThickness > 0) || FillBrush != null);
            return isValid;
        }

        /// <summary>
        /// Returns the data range of the assosiated <see cref="IDataSeries"/> on Y direction
        /// <param name="xRange">The X-Axis Range currently in view</param>
        /// <param name="getPositiveRange">Indicates whether to return positive YRange only</param>
        /// </summary>
        public override IRange GetYRange(IRange xRange, bool getPositiveRange)
        {
            var yRange = base.GetYRange(xRange, getPositiveRange);
            double zeroLineY = ZeroLineY;

            // In case of log axis
            if (getPositiveRange && zeroLineY <= 0d)
            {
                // Do not allow ZeroLineY 
                zeroLineY = yRange.Min.ToDouble();
            }

            return RangeFactory.NewRange(Math.Min(yRange.Min.ToDouble(), zeroLineY), Math.Max(yRange.Max.ToDouble(), zeroLineY));
        }

        protected override double GetSeriesBodyLowerDataBound(HitTestInfo nearestHitPoint)
        {
            var yValue = (IComparable)DataSeries.YValues[nearestHitPoint.DataSeriesIndex];
            var lowerBound = yValue.ToDouble().CompareTo(ZeroLineY) > 0 ? ZeroLineY : yValue.ToDouble();

            return lowerBound;
        }

        protected override double GetSeriesBodyUpperDataBound(HitTestInfo nearestHitPoint)
        {
            var yValue = (IComparable)DataSeries.YValues[nearestHitPoint.DataSeriesIndex];
            var upperBound = yValue.ToDouble().CompareTo(ZeroLineY) > 0 ? yValue.ToDouble() : ZeroLineY;

            return upperBound;
        }

        /// <summary>
        /// When overriden in a derived class, computes the width of the columns, which depends on the input data,
        /// any spacing and the current viewport dimensions
        /// </summary>
        /// <param name="points">The <see cref="IPointSeries" /> containing resampled data to render</param>
        /// <param name="renderPassData">The <see cref="IRenderPassData" /> containing information about the current render pass</param>
        /// <returns>
        /// The width of the column
        /// </returns>
        protected override double GetColumnWidth(IPointSeries points, IRenderPassData renderPassData)
        {
            return GetDatapointWidth(renderPassData.XCoordinateCalculator, points, DataPointWidth);
        }

        public override IRange GetXRange()
        {
            var range = base.GetXRange();

            if (range.IsDefined == false)
            {
                return DoubleRange.UndefinedRange;
            }

            var count = DataSeries.Count;
            var dRange = range.AsDoubleRange();

            var additionalValue = count > 1
                ? dRange.Diff / (count - 1) / 2 * DataPointWidth
                : DataPointWidth / 2;

            dRange.Max += additionalValue;
            dRange.Min -= additionalValue;

            return range;
        }
    }
}