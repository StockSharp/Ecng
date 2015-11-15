// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IAxisInteractivityHelper.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

namespace Ecng.Xaml.Charting.Visuals.Axes
{
    /// <summary>
    /// Defines a set of operations which allows to interact with axis that owns current instance of <see cref="IAxisInteractivityHelper"/> 
    /// </summary>
    public interface IAxisInteractivityHelper
    {
        /// <summary>
        /// Calculates and returns a Zoomed Range on current <see cref="IAxis"/>, using <paramref name="fromCoord"/> as a coordinate of new range start and
        /// <paramref name="toCoord"/> as a coordinate of new range end
        /// </summary>
        /// <param name="initialRange">Initial range</param>
        /// <param name="fromCoord">The coordinate of new range start in pixels</param>
        /// <param name="toCoord">The coordinate of new range end in pixels</param>
        IRange Zoom(IRange initialRange, double fromCoord, double toCoord);

        /// <summary>
        /// Calculates and returns a Zoomed Range on current <see cref="IAxis"/>, using <paramref name="minFraction"/> as a multiplier of range start and
        /// <paramref name="maxFraction"/> as a multiplier of range end
        /// </summary>
        /// <param name="initialRange">Initial range</param>
        /// <param name="minFraction">The multiplier of range start</param>
        /// <param name="maxFraction">The multiplier of range end</param>
        IRange ZoomBy(IRange initialRange, double minFraction, double maxFraction);

        /// <summary>
        /// From the starting <see cref="IRange"/>, scrolls to a new range by the specified number of pixels in Min direction
        /// </summary>
        /// <param name="rangeToScroll">The start range</param>
        /// <param name="pixels">Scroll N pixels from the start visible range</param>
        IRange ScrollInMinDirection(IRange rangeToScroll, double pixels);

        /// <summary>
        /// From the starting <see cref="IRange"/>, scrolls to a new range by the specified number of pixels in Max direction
        /// </summary>
        /// <param name="rangeToScroll">The start range</param>
        /// <param name="pixels">Scroll N pixels from the start visible range</param>
        IRange ScrollInMaxDirection(IRange rangeToScroll, double pixels);

        /// <summary>
        /// From the starting <see cref="IRange"/>, scrolls to a new range by the specified number of pixels
        /// </summary>
        /// <param name="rangeToScroll">The start range</param>
        /// <param name="pixels">Scroll N pixels from the start visible range</param>
        IRange Scroll(IRange rangeToScroll, double pixels);

        /// <summary>
        /// Translates the passed range by the specified number of datapoints
        /// </summary>
        /// <param name="rangeToScroll">The start range</param>
        /// <param name="pointAmount">Amount of points that the start visible range is scrolled by</param>
        /// <remarks>For XAxis only,  is suitable for <see cref="CategoryDateTimeAxis"/>, <see cref="DateTimeAxis"/> and <see cref="NumericAxis"/>
        /// where data is regularly spaced</remarks>
        [Obsolete("The ScrollBy method is Obsolete as it is only really possible to implement on Category Axis. For this axis type just update the IndexRange (visibleRange) by N to scroll the axis", true)]
        IRange ScrollBy(IRange rangeToScroll, int pointAmount);

        /// <summary>
        /// Performs clipping of passed <paramref name="rangeToClip"/> using <paramref name="clipMode"/>
        /// </summary>
        /// <param name="rangeToClip"></param>
        /// <param name="maximumRange"></param>
        /// <param name="clipMode"></param>
        /// <returns></returns>
        IRange ClipRange(IRange rangeToClip, IRange maximumRange, ClipMode clipMode);
    }
}
