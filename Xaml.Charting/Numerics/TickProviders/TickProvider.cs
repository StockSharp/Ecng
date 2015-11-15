// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// TickProvider.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Linq;
using Ecng.Xaml.Charting.Common.Extensions;

namespace Ecng.Xaml.Charting.Visuals.Axes
{
    /// <summary>
    /// Abstract base implementation of <see cref="ITickProvider"/>
    /// </summary>
    /// <typeparam name="T">The type of ticks for tick provider, e.g. <see cref="System.Double"/></typeparam>
    public abstract class TickProvider<T> : ITickProvider<T> where T : IComparable 
    {
        /// <summary>
        /// Called when the tick provider is initialized as it is attached to the parent axis, with the parent axis instance
        /// </summary>
        public IAxis ParentAxis { get; protected set; }

        /// <summary>
        /// Called when the tick provider is initialized as it is attached to the parent axis, with the parent axis instance
        /// </summary>
        /// <param name="axis">The parent <see cref="IAxis" /> instance</param>
        public virtual void Init(IAxis axis)
        {
            ParentAxis = axis;
        }

        /// <summary>
        /// Returns double representation of major ticks array
        /// </summary>
        /// <param name="axis">The AxisParams for the axis</param>
        /// <returns>The array of ticks to display (data values converted to double)</returns>
        double[] ITickProvider.GetMajorTicks(IAxisParams axis)
        {
            var ticks = GetMajorTicks(axis);

            return ConvertTicks(ticks);
        }

        /// <summary>
        /// Returns double representation of minor ticks array
        /// </summary>
        /// <param name="axis">The AxisParams for the axis</param>
        /// <returns>The array of ticks to display (data values converted to double)</returns>
        double[] ITickProvider.GetMinorTicks(IAxisParams axis)
        {
            var ticks = GetMinorTicks(axis);

            return ConvertTicks(ticks);
        }

        /// <summary>
        /// Converts ticks in generic format to Double, e.g. cast to double for numeric types, or cast DateTime.Ticks to double for DateTime types
        /// </summary>
        /// <param name="ticks"></param>
        /// <returns></returns>
        protected virtual double[] ConvertTicks(T[] ticks)
        {
            return ticks
                .Select(x => x.ToDouble())
                .ToArray();
        }

        /// <summary>
        /// Returns Generic-typed representation of major ticks array
        /// </summary>
        /// <param name="axis">The AxisParams for the axis</param>
        /// <returns>The array of ticks to display (data values converted to T)</returns>
        public abstract T[] GetMajorTicks(IAxisParams axis);

        /// <summary>
        /// Returns Generic-typed representation of minor ticks array
        /// </summary>
        /// <param name="axis">The AxisParams for the axis</param>
        /// <returns>The array of ticks to display (data values converted to T)</returns>
        public abstract T[] GetMinorTicks(IAxisParams axis);
    }
}