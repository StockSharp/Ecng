// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// TickCoordinates.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
namespace Ecng.Xaml.Charting.Numerics.TickCoordinateProviders
{
    /// <summary>
    /// A structure contaning tick coordinates. Used internally when drawing tick marks and grid lines.
    /// </summary>
    public struct TickCoordinates
    {
        private readonly double[] _minorTicks;
        private readonly double[] _majorTicks;

        private readonly float[] _minorTickCoordinates;
        private readonly float[] _majorTickCoordinates;

        /// <summary>
        /// Initializes a new instance of the <see cref="TickCoordinates"/> struct.
        /// </summary>
        /// <param name="minorTicks">The minor ticks represented in chart coordinates.</param>
        /// <param name="majorTicks">The major ticks represented in chart coordinates.</param>
        /// <param name="minorCoords">The minor ticks represented in pixel coordinates.</param>
        /// <param name="majorCoords">The major ticks represented in pixel coordinates.</param>
        public TickCoordinates(double[] minorTicks, double[] majorTicks, float[] minorCoords, float[] majorCoords)
        {
            _minorTicks = minorTicks;
            _majorTicks = majorTicks;

            _minorTickCoordinates = minorCoords;
            _majorTickCoordinates = majorCoords;
        }

        /// <summary>
        /// Returns a value indicating whether there are any tick coordinates.
        /// </summary>
        public bool IsEmpty
        {
            get { return _majorTickCoordinates == null || _minorTickCoordinates == null; }
        }

        /// <summary>
        /// Returns minor ticks in chart coordinates.
        /// </summary>
        public double[] MinorTicks
        {
            get { return _minorTicks; }
        }

        /// <summary>
        /// Returns major ticks in chart coordinates.
        /// </summary>
        public double[] MajorTicks
        {
            get { return _majorTicks; }
        }

        /// <summary>
        /// Returns major ticks in pixels.
        /// </summary>
        public float[] MinorTickCoordinates
        {
            get { return _minorTickCoordinates; }
        }

        /// <summary>
        /// Returns major ticks in pixels.
        /// </summary>
        public float[] MajorTickCoordinates
        {
            get { return _majorTickCoordinates; }
        }
    }
}
