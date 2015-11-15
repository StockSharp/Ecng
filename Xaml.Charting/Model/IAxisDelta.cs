// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IAxisDelta.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
    /// Defines the interface to a Delta type, which defines Major and Minor deltas as used in <see cref="AxisBase"/>
    /// </summary>
    public interface IAxisDelta
    {
        /// <summary>
        /// Gets or sets the Major Delta
        /// </summary>
        IComparable MajorDelta { get; }

        /// <summary>
        /// Gets or sets the Minor Delta
        /// </summary>
        IComparable MinorDelta { get; }
    }

    /// <summary>
    /// Defines the Typed interface to a Delta type, which defines Major and Minor deltas as used in <see cref="AxisBase"/>
    /// </summary>
    /// <typeparam name="T">The typeparameter of this Delta, e.g. <see cref="System.Double"/></typeparam>
    public interface IAxisDelta<T> : IAxisDelta, ICloneable where T : IComparable
    {
        /// <summary>
        /// Gets or sets the Major Delta
        /// </summary>
        new T MajorDelta { get; set; }

        /// <summary>
        /// Gets or sets the Minor Delta
        /// </summary>
        new T MinorDelta { get; set; }
    }
}