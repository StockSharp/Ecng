// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ITickCoordinatesProvider.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Numerics.TickCoordinateProviders;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting.Numerics.CoordinateProviders
{
    /// <summary>
    /// Provides the interface to a Tick Coordinate Provider, which converts arrays of major and minor ticks (data values) into pixel coordinates.
    /// </summary>
    public interface ITickCoordinatesProvider
    {
        /// <summary>
        /// Called when the <see cref="ITickCoordinatesProvider"/> is initialized as it is attached to the parent axis, with the parent axis instance
        /// </summary>
        /// <param name="parentAxis">The parent <see cref="IAxis"/> instance</param>
        void Init(IAxis parentAxis);

        /// <summary>
        /// Converts arrays of major and minor ticks (data values) into <see cref="TickCoordinates"/> structure containing pixel coordinates
        /// </summary>
        /// <param name="minorTicks">The minor ticks, cast to double</param>
        /// <param name="majorTicks">The major ticks, cast to double</param>
        /// <returns>The <see cref="TickCoordinates"/> structure containing pixel coordinates</returns>
        TickCoordinates GetTickCoordinates(double[] minorTicks, double[] majorTicks);
    }
}
