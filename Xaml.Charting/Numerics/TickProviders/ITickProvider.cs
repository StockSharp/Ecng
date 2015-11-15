// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ITickProvider.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections.Generic;
using System.Text;
using Ecng.Xaml.Charting.Numerics;

namespace Ecng.Xaml.Charting.Visuals.Axes
{
    /// <summary>
    /// Provides base interface for tick providers
    /// </summary>
    public interface ITickProvider
    {
        /// <summary>
        /// Called when the tick provider is initialized as it is attached to the parent axis, with the parent axis instance
        /// </summary>
        /// <param name="axis">The parent <see cref="IAxis"/> instance</param>
        void Init(IAxis axis);

        /// <summary>
        /// Returns double representation of major ticks array for <see cref="IAxis"/>
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        double[] GetMajorTicks(IAxisParams axis);

        /// <summary>
        /// Returns double representation of minor ticks array for <see cref="IAxis"/>
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        double[] GetMinorTicks(IAxisParams axis);
    }

    /// <summary>
    /// Provides interface for tick providers
    /// </summary>
    public interface ITickProvider<T> : ITickProvider where T : IComparable
    {
        /// <summary>
        /// Returns array of major ticks from tick provider for <see cref="IAxis"/>
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        new T[] GetMajorTicks(IAxisParams axis);

        /// <summary>
        /// Returns array of minor ticks from tick provider for <see cref="IAxis"/>
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        new T[] GetMinorTicks(IAxisParams axis);
    }
}
