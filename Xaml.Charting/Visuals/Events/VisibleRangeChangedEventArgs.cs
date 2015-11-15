// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// VisibleRangeChangedEventArgs.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting
{
    /// <summary>
    /// Event Args used by the <see cref="AxisBase.VisibleRangeChanged"/> event
    /// </summary>
    public class VisibleRangeChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the old <see cref="IAxisParams.VisibleRange"/> before the operation
        /// </summary>
        public IRange OldVisibleRange { get; private set; }

        /// <summary>
        /// Gets the new <see cref="IAxisParams.VisibleRange"/> before the operation
        /// </summary>
        public IRange NewVisibleRange { get; private set; }

        /// <summary>
        /// Gets the value, indicating whether the current notification was caused by animation
        /// </summary>
        public bool IsAnimating { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="VisibleRangeChangedEventArgs" /> class.
        /// </summary>
        /// <param name="oldRange">The old range.</param>
        /// <param name="newRange">The new range.</param>
        /// <param name="isAnimationChange">The value, indicating whether the notification is fired during animation</param>
        public VisibleRangeChangedEventArgs(IRange oldRange, IRange newRange, bool isAnimationChange)
        {
            OldVisibleRange = oldRange;
            NewVisibleRange = newRange;

            IsAnimating = isAnimationChange;
        }
    }
}
