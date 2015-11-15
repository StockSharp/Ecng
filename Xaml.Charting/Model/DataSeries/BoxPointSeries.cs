// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// BoxPointSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
    /// The <see cref="BoxPointSeries"/> specifically is used when resampling and rendering Box Plot points
    /// </summary>
    /// <seealso cref="FastBoxPlotRenderableSeries"/>
    /// <seealso cref="BoxPlotDataSeries{TX,TY}"/>
    public class BoxPointSeries : GenericPointSeriesBase<BoxSeriesPoint>
    {
        private readonly IPointSeries _yPoints;
        private readonly IPointSeries _minPoints;
        private readonly IPointSeries _lowerPoints;
        private readonly IPointSeries _upperPoints;
        private readonly IPointSeries _maxPoints;

        /// <summary>
        /// Initializes a new instance of the <see cref="BoxPointSeries" /> class.
        /// </summary>
        /// <param name="yPoints">The resampled y points.</param>
        /// <param name="minPoints">The resampled min points.</param>
        /// <param name="lowerPoints">The resampled lower quartile points.</param>
        /// <param name="upperPoints">The resampled upper quartile points.</param>
        /// <param name="maxPoints">The max points.</param>
        public BoxPointSeries(IPointSeries yPoints, IPointSeries minPoints, IPointSeries lowerPoints, IPointSeries upperPoints, IPointSeries maxPoints) : base(yPoints)
        {
            _yPoints = yPoints;
            _minPoints = minPoints;
            _lowerPoints = lowerPoints;
            _upperPoints = upperPoints;
            _maxPoints = maxPoints;
        }

        /// <summary>
        /// Gets the number of <see cref="IPoint" /> points that this series contains
        /// </summary>
        public override int Count
        {
            get { return _yPoints.Count; }
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
                var pt = new GenericPoint2D<BoxSeriesPoint>(
                    _yPoints[index].X,
                    new BoxSeriesPoint(
                        _yPoints[index].Y,
                        _minPoints[index].Y,
                        _lowerPoints[index].Y, 
                        _upperPoints[index].Y, 
                        _maxPoints[index].Y));

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
                double currentHigh = _maxPoints[i].Y;
                double currentLow = _minPoints[i].Y;
                if (double.IsNaN(currentHigh) || double.IsNaN(currentLow)) continue;
                min = min < currentLow ? min : currentLow;
                max = max > currentHigh ? max : currentHigh;
                count = _minPoints.Count;
            }

            return new DoubleRange(min, max);
        }
    }
}