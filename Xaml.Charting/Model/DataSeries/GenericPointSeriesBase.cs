// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// GenericPointSeriesBase.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections;
using System.Collections.Generic;
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting.Model.DataSeries
{
    /// <summary>
    /// A generic abstract base class to store <see cref="IPoint"/> instances, used as the resampling output of higher-order DataSeries, e.g. <see cref="XyyPointSeries"/>,  <see cref="XyzPointSeries"/>
    /// </summary>
    /// <typeparam name="TY">The type of the Y-data</typeparam>
    public abstract class GenericPointSeriesBase<TY> : IGenericPointSeries<TY> where TY: IComparable
    {
        private readonly IPointSeries _yPoints;

        protected GenericPointSeriesBase(IPointSeries yPoints)
        {
            _yPoints = yPoints;
        }

        /// <summary>
        /// Gets the y points.
        /// </summary>
        /// <value>
        /// The y points.
        /// </value>
        public IPointSeries YPoints { get { return _yPoints; }}

        /// <summary>
        /// Gets the Raw X-Values for the PointSeries
        /// </summary>
        public IUltraList<double> XValues
        {
            get { return _yPoints.XValues; }
        }

        /// <summary>
        /// Gets the Raw Y-Values for the PointSeries
        /// </summary>
        public IUltraList<double> YValues
        {
            get { return _yPoints.YValues; }
        }

        /// <summary>
        /// Gets the number of <see cref="IPoint"/> points that this series contains
        /// </summary>
        public abstract int Count { get; }

        /// <summary>
        /// Gets or sets the <see cref="IPoint" /> at the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="IPoint" />.
        /// </value>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public abstract IPoint this[int index]
        {
            get;
        }

        /// <summary>
        /// Gets the min, max range in the Y-Direction
        /// </summary>
        /// <returns>
        /// A <see cref="DoubleRange" /> defining the min, max in the Y-direction
        /// </returns>
        public abstract DoubleRange GetYRange();
    }
}
