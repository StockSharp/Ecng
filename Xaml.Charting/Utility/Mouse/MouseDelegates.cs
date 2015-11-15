// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// MouseDelegates.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Input;
using Ecng.Xaml.Charting.Visuals.Events;

namespace Ecng.Xaml.Charting.Utility.Mouse
{
    /// <summary>
    /// Proxy class to handle mouse-events between a type which implements <see cref="IPublishMouseEvents"/> and <see cref="IReceiveMouseEvents"/>
    /// </summary>
    public class MouseDelegates
    {        
        /// <summary>
        /// The target element which will receive the notifications
        /// </summary>
        public IReceiveMouseEvents Target { get; set; }

        internal RenderSynchronizedMouseMove SynchronizedMouseMove { get; set; }

        /// <summary>
        /// A proxy delegate for Mouse Left Up events
        /// </summary>
        public MouseButtonEventHandler MouseLeftUpDelegate { get; set; }

        /// <summary>
        /// A proxy delegate for Mouse Left Down events
        /// </summary>
        public MouseButtonEventHandler MouseLeftDownDelegate { get; set; }

        /// <summary>
        /// A proxy delegate for Mouse Move events
        /// </summary>
        public MouseEventHandler MouseMoveDelegate { get; set; }

        /// <summary>
        /// A proxy delegate for Mouse Right Up events
        /// </summary>
        public MouseButtonEventHandler MouseRightUpDelegate { get; set; }

        /// <summary>
        /// A proxy delegate for Mouse Right Down events
        /// </summary>
        public MouseButtonEventHandler MouseRightDownDelegate { get; set; }

        /// <summary>
        /// A proxy delegate for Mouse Middle Down events
        /// </summary>
        public MouseButtonEventHandler MouseMiddleDownDelegate { get; set; }

        /// <summary>
        /// A proxy delegate for Mouse Middle Up events
        /// </summary>
        public MouseButtonEventHandler MouseMiddleUpDelegate { get; set; }

        /// <summary>
        /// A proxy delegate for Mouse Wheel events
        /// </summary>
        public MouseWheelEventHandler MouseWheelDelegate { get; set; }

        /// <summary>
        ///  A proxy delegate for Mouse Leave events
        /// </summary>
        public MouseEventHandler MouseLeaveDelegate { get; set; }

        /// <summary>
        /// A proxy delegate for Touch Down events
        /// </summary>
        public EventHandler<TouchManipulationEventArgs> TouchDownDelegate { get; set; }

        /// <summary>
        /// A proxy delegate for Touch Move events
        /// </summary>
        public EventHandler<TouchManipulationEventArgs> TouchMoveDelegate { get; set; }

        /// <summary>
        /// A proxy delegate for Touch Up events
        /// </summary>
        public EventHandler<TouchManipulationEventArgs> TouchUpDelegate { get; set; }
    }
}