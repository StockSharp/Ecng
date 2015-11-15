// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// XyyPointSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.Model.DataSeries
{
    /// <summary>
    /// An internal concept - the <see cref="IPointSeries"/> provides a sequence of <see cref="ISeriesPoint{T}"/> derived 
    /// types, which represent resampled data immediately before rendering. 
    /// 
    /// The <see cref="XyyPointSeries"/> specifically is used when resampling and rendering points for <see cref="FastBandRenderableSeries"/>, or 
    /// any other series-type which requires one X-value and two Y-values
    /// </summary>
    /// <seealso cref="FastBandRenderableSeries"/>
    /// <seealso cref="XyyDataSeries{TX,TY}"/>
    public class XyyPointSeries : GenericPointSeriesBase<XyySeriesPoint>
    {
        private readonly IPointSeries _yPoints;
        private readonly IPointSeries _y1Points;

        /// <summary>
        /// Initializes a new instance of the <see cref="XyyPointSeries" /> class.
        /// </summary>
        /// <param name="yPoints">The y points.</param>
        /// <param name="y1Points">The y1 points.</param>
        public XyyPointSeries(IPointSeries yPoints, IPointSeries y1Points) : base(yPoints)
        {
            _yPoints = yPoints;
            _y1Points = y1Points;
        }

        /// <summary>
        /// Gets the number of <see cref="IPoint" /> points that this series contains
        /// </summary>
        public override int Count
        {
            get { return _yPoints.Count; }
        }

        /// <summary>
        /// Gets the Y points.
        /// </summary>
        public IPointSeries YPoints { get { return _yPoints; } }
        /// <summary>
        /// Gets the y1 points.
        /// </summary>
        public IPointSeries Y1Points { get { return _y1Points; } }

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
                var pt = new GenericPoint2D<XyySeriesPoint>(
                    _yPoints[index].X,
                    new XyySeriesPoint(
                        _yPoints != null ? _yPoints[index].Y : 0,
                        _y1Points[index].Y));

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
                var y1 = _y1Points[i].Y;
                var y = _yPoints[i].Y;

                if (double.IsNaN(y) || double.IsNaN(y1)) continue;

                double lowest = Math.Min(y1, y);
                double highest = Math.Max(y1, y);
                
                min = min < lowest ? min : lowest;
                max = max > highest ? max : highest;
            }

            return new DoubleRange(min, max);
        }
    }
}