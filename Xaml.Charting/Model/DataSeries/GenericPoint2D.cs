// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// GenericPoint2D.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
namespace Ecng.Xaml.Charting.Model.DataSeries
{
    /// <summary>
    /// Defines a generic base class for a Series Point, an internally used structure which contains transformed points to render Y-values on the <see cref="Ecng.Xaml.Charting.Rendering.Common.RenderSurfaceBase"/>
    /// </summary>
    /// <typeparam name="TY">The Type of the Y-Values</typeparam>
    public class GenericPoint2D<TY> : IPoint where TY : ISeriesPoint<double>
    {
        private double _x;
        private TY _y;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericPoint2D{TY}" /> class.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        public GenericPoint2D(double x, TY y)
        {
            _x = x;
            _y = y;
        }

        /// <summary>
        /// Gets the X-Value
        /// </summary>
        public double X
        {
            get { return _x; }
        }

        /// <summary>
        /// Gets the Y-value
        /// </summary>
        public double Y
        {
            get { return _y.Y; }
        }

        /// <summary>
        /// Gets the Y values.
        /// </summary>
        /// <value>
        /// The Y values.
        /// </value>
        public TY YValues { get { return _y; } }
    }
}