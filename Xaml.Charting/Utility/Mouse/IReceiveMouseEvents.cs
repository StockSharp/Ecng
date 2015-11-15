// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IReceiveMouseEvents.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.ChartModifiers;

namespace Ecng.Xaml.Charting.Utility.Mouse
{
    /// <summary>
    /// Defines the interface to a type which receives unified Mouse Events (cross-platform WPF and Silverlight).
    /// </summary>
    public interface IReceiveMouseEvents
    {
        /// <summary>
        /// Gets or sets whether the mouse target is enabled.
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets a Mouse Event Group, an ID used to share mouse events across multiple targets.
        /// </summary>
        string MouseEventGroup { get; set; }

        /// <summary>
        /// Returns a value indicating whether mouse events should be propagated to the mouse target.
        /// </summary>
        bool CanReceiveMouseEvents();

        /// <summary>
        /// Called when a Mouse DoubleClick occurs.
        /// </summary>
        /// <param name="e">Arguments detailing the mouse button operation.</param>
        void OnModifierDoubleClick(ModifierMouseArgs e);

        /// <summary>
        /// Called when a Mouse Button is pressed.
        /// </summary>
        /// <param name="e">Arguments detailing the mouse button operation.</param>
        void OnModifierMouseDown(ModifierMouseArgs e);

        /// <summary>
        /// Called when the Mouse is moved.
        /// </summary>
        /// <param name="e">Arguments detailing the mouse move operation.</param>
        void OnModifierMouseMove(ModifierMouseArgs e);

        /// <summary>
        /// Called when a Mouse Button is released.
        /// </summary>
        /// <param name="e">Arguments detailing the mouse button operation.</param>
        void OnModifierMouseUp(ModifierMouseArgs e);

        /// <summary>
        /// Called when the Mouse Wheel is scrolled.
        /// </summary>
        /// <param name="e">Arguments detailing the mouse wheel operation.</param>
        void OnModifierMouseWheel(ModifierMouseArgs e);

        /// <summary>
        /// Called when a manipulation is started.
        /// </summary>
        /// <param name="e">Arguments detailing the manipulation operation.</param>
        void OnModifierTouchDown(ModifierTouchManipulationArgs e);

        /// <summary>
        /// Called after each touch position change during a manipulation.
        /// </summary>
        /// <param name="e">Arguments detailing the manipulation operation.</param>
        void OnModifierTouchMove(ModifierTouchManipulationArgs e);

        /// <summary>
        /// Called when a manipulation is complete.
        /// </summary>
        /// <param name="e">Arguments detailing the manipulation operation.</param>
        void OnModifierTouchUp(ModifierTouchManipulationArgs e);

        /// <summary>
        /// Called when the MouseLeave event is fired for a Master of current <see cref="MouseEventGroup"/>.
        /// </summary>
        /// <param name="e">Arguments detailing the manipulation operation.</param>
        void OnMasterMouseLeave(ModifierMouseArgs e);
    }
}
