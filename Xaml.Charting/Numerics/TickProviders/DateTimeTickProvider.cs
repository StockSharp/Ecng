// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// DateTimeTickProvider.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Common.Extensions;

namespace Ecng.Xaml.Charting.Visuals.Axes
{
    /// <summary>
    /// Provides tick coordinates for the <see cref="DateTimeAxis"/>
    /// </summary>
    public class DateTimeTickProvider : TimeSpanTickProviderBase
    {
        protected override double GetTicks(IComparable value)
        {
            return value.ToDateTime().Ticks;
        }

        /// <summary>
        /// When overriden in a derived class, Rounds up the <see cref="IComparable" /> to the nearest TimeSpan specified by <paramref name="delta" />
        /// </summary>
        /// <param name="current">The current value.</param>
        /// <param name="delta">The delta.</param>
        /// <returns>
        /// The rounded value
        /// </returns>
        protected override IComparable RoundUp(IComparable current, TimeSpan delta)
        {
            var dateTime = current.ToDateTime();

            return DateUtil.RoundUp(dateTime, delta);
        }

        /// <summary>
        /// Determines whether addition is valid between the current <see cref="IComparable" /> and the TimeSpan specified by <paramref name="delta" />
        /// </summary>
        /// <param name="current">The current.</param>
        /// <param name="delta">The delta.</param>
        /// <returns>
        /// If True, addition is valid
        /// </returns>
        protected override bool IsAdditionValid(IComparable current, TimeSpan delta)
        {
            var dateTime = current.ToDateTime();

            return dateTime.IsAdditionValid(delta);
        }

        /// <summary>
        /// When overriden in a derived class, Adds the <see cref="IComparable" /> to the nearest TimeSpan specified by <paramref name="delta" />
        /// </summary>
        /// <param name="current">The current value.</param>
        /// <param name="delta">The delta.</param>
        /// <returns>
        /// The addition result
        /// </returns>
        protected override IComparable AddDelta(IComparable current, TimeSpan delta)
        {
            var dateTime = current.ToDateTime();

            return dateTime.AddDelta(delta);
        }

        /// <summary>
        /// When overriden in a derived class, Determines whether the <see cref="IComparable" /> is divisible by the TimeSpan specified by <paramref name="delta" />
        /// </summary>
        /// <param name="current">The current value.</param>
        /// <param name="delta">The delta.</param>
        /// <returns>If True, IsDivisibleBy</returns>
        protected override bool IsDivisibleBy(IComparable current, TimeSpan delta)
        {
            var dateTime = current.ToDateTime();

            return DateUtil.IsDivisibleBy(dateTime, delta);
        }
    }
}
