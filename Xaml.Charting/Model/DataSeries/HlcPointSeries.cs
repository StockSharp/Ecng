// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// HlcPointSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.Model.DataSeries
{
    /// <summary>
    /// An internal concept - the <see cref="IPointSeries"/> provides a sequence of <see cref="ISeriesPoint{T}"/> derived 
    /// types, which represent resampled data immediately before rendering. 
    /// 
    /// The <see cref="HlcPointSeries"/> specifically is used when resampling and rendering points for Error Bars and HLC charts
    /// </summary>
    /// <seealso cref="FastErrorBarsRenderableSeries"/>
    /// <seealso cref="HlcDataSeries{TX,TY}"/>
    public class HlcPointSeries : GenericPointSeriesBase<HlcSeriesPoint>
    {
        private readonly IPointSeries _yErrorHighPoints;
        private readonly IPointSeries _yErrorLowPoints;

        /// <summary>
        /// Initializes a new instance of the <see cref="HlcPointSeries" /> class.
        /// </summary>
        /// <param name="yPoints">The y points.</param>
        /// <param name="yErrorHighPoints">The y error high points.</param>
        /// <param name="yErrorLowPoints">The y error low points.</param>
        public HlcPointSeries(IPointSeries yPoints, IPointSeries yErrorHighPoints, IPointSeries yErrorLowPoints)
            : base(yPoints)
        {
            _yErrorHighPoints = yErrorHighPoints;
            _yErrorLowPoints = yErrorLowPoints;
        }

        /// <summary>
        /// Gets the number of <see cref="IPoint" /> points that this series contains
        /// </summary>
        public override int Count
        {
            get { return YPoints.Count; }
        }

        /// <summary>
        /// Gets the <see cref="IPoint" /> at the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="IPoint" />.
        /// </value>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public override IPoint this[int index]
        {
            get
            {
                var pt = new GenericPoint2D<HlcSeriesPoint>(
                    YPoints[index].X,
                    new HlcSeriesPoint(
                        YPoints[index].Y,
                        _yErrorHighPoints[index].Y,
                        _yErrorLowPoints[index].Y));

                return pt;
            }
        }

        /// <summary>
        /// Gets the min, max range in the Y-Direction
        /// </summary>
        /// <returns>
        /// A <see cref="DoubleRange" /> defining the min, max in the Y-direction
        /// </returns>
        public override DoubleRange GetYRange()
        {
            int count = Count;

            double min = double.MaxValue;
            double max = double.MinValue;

            for (int i = 0; i < count; i++)
            {
                double currentHigh = _yErrorHighPoints[i].Y;
                double currentLow = _yErrorLowPoints[i].Y;
                if (double.IsNaN(currentHigh) || double.IsNaN(currentLow)) continue;
                min = min < currentLow ? min : currentLow;
                max = max > currentHigh ? max : currentHigh;
                count = _yErrorHighPoints.Count;
            }

            return new DoubleRange(min, max);
        }
    }
}