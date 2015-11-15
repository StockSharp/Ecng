// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// SelectedRangeChangedEventArgs.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Visuals;

namespace Ecng.Xaml.Charting
{
    /// <summary>
    /// Enumeration constants to define the possible event type during scrolling of <see cref="UltrachartScrollbar"/>
    /// </summary>
    public enum SelectedRangeEventType
    {
        /// <summary>
        /// <see cref="UltrachartScrollbar.SelectedRange"/> was changed externally
        /// </summary>
        ExternalSource,

        /// <summary>
        /// <see cref="UltrachartScrollbar"/> viewport was dragged without resizing
        /// </summary>
        Drag,

        /// <summary>
        /// <see cref="UltrachartScrollbar"/> viewport was resized with one of resizing grips
        /// </summary>
        Resize,

        /// <summary>
        /// <see cref="UltrachartScrollbar"/> viewport was moved after click on non selected area
        /// </summary>
        Moved,
    }

    /// <summary>
    /// Event arguments for the <see cref="UltrachartScrollbar.SelectedRangeChanged"/> event
    /// </summary>
    public class SelectedRangeChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the range for the event args
        /// </summary>
        public IRange SelectedRange
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the <see cref="EventType"/> value which describes current event type
        /// </summary>
        public SelectedRangeEventType EventType
        {
            get; 
            private set;
        }

        /// <summary>
        /// sets the range start and range stop for the event args
        /// </summary>
        /// <param name="newRangeStart">The new range start set</param>
        /// <param name="newRangeStop">The new range stop set</param>
        internal SelectedRangeChangedEventArgs(IComparable newRangeStart, IComparable newRangeStop, SelectedRangeEventType eventType)
        {
            SelectedRange = RangeFactory.NewRange(newRangeStart, newRangeStop);
            EventType = eventType;
        }

        /// <summary>
        /// sets the range start and range stop for the event args
        /// </summary>
        internal SelectedRangeChangedEventArgs(IRange newRange, SelectedRangeEventType eventType): this(newRange.Min, newRange.Max, eventType)
        {}
    }
}
