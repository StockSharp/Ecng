// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ModifierTouchManipulationArgs.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows;
using System.Windows.Input;
using Ecng.Xaml.Charting.Utility.Mouse;

namespace Ecng.Xaml.Charting.ChartModifiers
{
    /// <summary>
    /// Defines a cross-platform Manipulation event args, used by <see cref="IChartModifier"/> derived types to process manipulation events.
    /// </summary>
    public class ModifierTouchManipulationArgs : ModifierEventArgsBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModifierTouchManipulationArgs"/> class.
        /// </summary>
        public ModifierTouchManipulationArgs()
        {            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModifierTouchManipulationArgs"/> class.
        /// </summary>
        /// <param name="touchPoints">The touch points.</param>
        /// <param name="isMaster">if set to <c>true</c>, this is a Master event, else Slave event.</param>
        /// <param name="master">The master instance in the case where charts are synchronized using <see cref="MouseManager.MouseEventGroupProperty"/>.</param>
        public ModifierTouchManipulationArgs(IEnumerable<TouchPoint> touchPoints,  bool isMaster, IReceiveMouseEvents master = null)
            : base(master, isMaster)
        {
            Manipulators = touchPoints;
        }

        /// <summary>
        /// Gets a collection of objects that represents the touch contacts for the manipulation.
        /// </summary>
        public IEnumerable<TouchPoint> Manipulators { get; set; }
    }
}
