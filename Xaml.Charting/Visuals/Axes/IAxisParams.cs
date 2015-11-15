// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IAxisParams.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
    /// Defines the interface used to pass the set of parameters to <see cref="ITickProvider"/>.
    /// </summary>
    public interface IAxisParams
    {
        /// <summary>
        /// Gets or sets the VisibleRange of the Axis. In the case of XAxis, this will cause an align to X-Axis operation to take place
        /// </summary>
        /// <remarks>Setting the VisibleRange will cause the axis to redraw</remarks>
        IRange VisibleRange { get; set; }

        /// <summary>
        /// Gets or sets the GrowBy Factor. e.g. GrowBy(0.1, 0.2) will increase the axis extents by 10% (min) and 20% (max) outside of the data range
        /// </summary>
        IRange<double> GrowBy { get; set; }

        /// <summary>
        /// Gets or sets the Minor Delta
        /// </summary>
        IComparable MinorDelta { get; set; }

        /// <summary>
        /// Gets or sets the Major Delta
        /// </summary>
        IComparable MajorDelta { get; set; }

        /// <summary>
        /// Gets the maximum range of the axis, based on the data-range of all series
        /// </summary>
        /// <returns></returns>
        IRange GetMaximumRange();
    }
}
