// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// TouchManipulationEventArgs.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Ecng.Xaml.Charting.Visuals.Events
{
    /// <summary>
    /// EventArgs to store a list of <see cref="TouchPoint"/> TouchPoints
    /// </summary>
    public class TouchManipulationEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="TouchManipulationEventArgs"/> is handled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if handled; otherwise, <c>false</c>.
        /// </value>
        public bool Handled{ get; set;}

        /// <summary>
        /// Gets or sets the touch points.
        /// </summary>
        /// <value>
        /// The touch points.
        /// </value>
        public IEnumerable<TouchPoint> TouchPoints { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TouchManipulationEventArgs"/> class.
        /// </summary>
        /// <param name="touchPoints">The touch points.</param>
        public TouchManipulationEventArgs(IEnumerable<TouchPoint> touchPoints)
        {
            TouchPoints = touchPoints;
        }
    }
}
