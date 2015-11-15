// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IPublishMouseEvents.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
    /// Defines the interface to a class which publishes mouse events. 
    /// Used in conjunction with <see cref="IReceiveMouseEvents"/> and <see cref="MouseManager"/> 
    /// to provide cross-platform WPF and Silverlight mouse eventing
    /// </summary>
    public interface IPublishMouseEvents
    {
        
        /// <summary>
        /// Occurs when the left mouse button is pressed (or when the tip of the stylus touches the tablet) while the mouse pointer is over a <see cref="T:System.Windows.UIElement"/>.
        /// </summary>
        event MouseButtonEventHandler MouseLeftButtonDown;

        /// <summary>
        /// Occurs when the left mouse button is released (or the tip of the stylus is removed from the tablet) while the mouse (or the stylus) is over a <see cref="T:System.Windows.UIElement"/> (or while a <see cref="T:System.Windows.UIElement"/> holds mouse capture).
        /// </summary>
        event MouseButtonEventHandler MouseLeftButtonUp;

        /// <summary>
        /// Occurs when the right mouse button is pressed while the mouse pointer is over a <see cref="T:System.Windows.UIElement"/>.
        /// </summary>
        event MouseButtonEventHandler MouseRightButtonDown;

        /// <summary>
        /// Occurs when the right mouse button is released while the mouse pointer is over a <see cref="T:System.Windows.UIElement"/>. However, this event will only be raised if a caller marks the preceding <see cref="E:System.Windows.UIElement.MouseRightButtonDown"/> event as handled; see Remarks.
        /// </summary>
        event MouseButtonEventHandler MouseRightButtonUp;        

        /// <summary>
        /// Occurs when the coordinate position of the mouse (or stylus) changes while over a <see cref="T:System.Windows.UIElement"/> (or while a <see cref="T:System.Windows.UIElement"/> holds mouse capture).
        /// </summary>
        event MouseEventHandler MouseMove;

        /// <summary>
        /// Occurs when the user rotates the mouse wheel while the mouse pointer is over a <see cref="T:System.Windows.UIElement"/>, or the <see cref="T:System.Windows.UIElement"/> has focus.
        /// </summary>
        event MouseWheelEventHandler MouseWheel;

        /// <summary>
        /// Occurs when the mouse pointer leaves the bounds of this element
        /// </summary>
        event MouseEventHandler MouseLeave;

#if !SILVERLIGHT
        /// <summary>
        /// Occurs when the middle mouse button is pressed while the mouse pointer is over a <see cref="T:System.Windows.UIElement"/>.
        /// </summary>
        event MouseButtonEventHandler MouseMiddleButtonDown;

        /// <summary>
        /// Occurs when the middle mouse button is released while the mouse pointer is over a <see cref="T:System.Windows.UIElement"/>. However, this event will only be raised if a caller marks the preceding <see cref="E:System.Windows.UIElement.MouseRightButtonDown"/> event as handled; see Remarks.
        /// </summary>
        event MouseButtonEventHandler MouseMiddleButtonUp;        
#endif


        
        /// <summary>
        /// Occurs when an input device begins a manipulation on the <see cref="T:System.Windows.UIElement"/>.
        /// </summary>
        event EventHandler<TouchManipulationEventArgs> TouchDown;

        /// <summary>
        /// Occurs when an input device changes position during manipulation.
        /// </summary>
        event EventHandler<TouchManipulationEventArgs> TouchMove;

        /// <summary>
        /// Occurs when a manipulation and inertia on the <see cref="T:System.Windows.UIElement"/> object is complete.
        /// </summary>
        event EventHandler<TouchManipulationEventArgs> TouchUp;

    }
}