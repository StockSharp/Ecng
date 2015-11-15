// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// Point2D.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
    /// A struct representing a 2D point
    /// </summary>
    public struct Point2D : IPoint
    {
        private readonly double _x;
        private readonly double _y;

        /// <summary>
        /// Initializes a new instance of the <see cref="Point2D" /> struct.
        /// </summary>
        /// <param name="x">The x value.</param>
        /// <param name="y">The y value.</param>
        public Point2D(double x, double y)
        {
            _x = x;
            _y = y;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("X={0};Y={1}", _x, _y);
        } 

        /// <summary>
        /// Gets the X value
        /// </summary>        
        public double X
        {
            get { return _x; }
        }

        /// <summary>
        /// Gets the Y value
        /// </summary>
        public double Y
        {
            get { return _y; }
        }
    }
}