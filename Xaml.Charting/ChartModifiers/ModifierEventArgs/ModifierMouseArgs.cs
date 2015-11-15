// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ModifierMouseArgs.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows;
using Ecng.Xaml.Charting.Utility.Mouse;
using Ecng.Xaml.Charting.Visuals;

namespace Ecng.Xaml.Charting.ChartModifiers
{
    /// <summary>
    /// Defines a cross-platform Mouse event args, used by <see cref="IChartModifier"/> derived types to process mouse events
    /// </summary>
    public class ModifierMouseArgs : ModifierEventArgsBase
    {
        /// <summary>
        /// Gets or sets the mouse wheel delta
        /// </summary>
        public int Delta { get; set; }

        /// <summary>
        /// Gets or sets the mouse point that this event occurred at
        /// </summary>
        public Point MousePoint { get; set; }

        /// <summary>
        /// Gets or sets the MouseButtons that were pressed at the time of the event
        /// </summary>
        public MouseButtons MouseButtons { get; set; }

        /// <summary>
        /// Gets or sets the Modifier Key that was pressed at the time of the event
        /// </summary>
        public MouseModifier Modifier { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModifierMouseArgs"/> class.
        /// </summary>
        /// <remarks></remarks>
        public ModifierMouseArgs()
        {            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModifierMouseArgs"/> class.
        /// </summary>
        /// <param name="mousePoint">The mouse point that this event occurred at relative to the <see cref="UltrachartSurface.RootGrid"/>.</param>
        /// <param name="mouseButtons">The mouse buttons clicked.</param>
        /// <param name="modifier">The modifier key pressed.</param>
        /// <param name="isMaster">If True, then this mouse event occurred on a master <see cref="ChartModifierBase"/>. 
        /// Used to process which modifier was the source of an event when multiple modifiers are linked</param>
        /// <param name="master">The instance of the master <see cref="ChartModifierBase"/> which sourced the event. Default value is null</param>
        /// <remarks></remarks>
        public ModifierMouseArgs(Point mousePoint, MouseButtons mouseButtons, MouseModifier modifier, bool isMaster, IReceiveMouseEvents master) 
            : this(mousePoint, mouseButtons, modifier, 0, isMaster, master)
        {
            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModifierMouseArgs"/> class.
        /// </summary>
        /// <param name="mousePoint">The mouse point that this event occurred at relative to the <see cref="UltrachartSurface.RootGrid"/>.</param>
        /// <param name="mouseButtons">The mouse buttons clicked.</param>
        /// <param name="modifier">The modifier key pressed.</param>
        /// <param name="wheelDelta">The mouse wheel delta.</param>
        /// <param name="isMaster">If True, then this mouse event occurred on a master <see cref="ChartModifierBase"/>. 
        /// Used to process which modifier was the source of an event when multiple modifiers are linked</param>
        /// <param name="master">The instance of the master <see cref="ChartModifierBase"/> which sourced the event. Default value is null</param>
        /// <remarks></remarks>
        public ModifierMouseArgs(Point mousePoint, MouseButtons mouseButtons, MouseModifier modifier, int wheelDelta, bool isMaster, IReceiveMouseEvents master = null)
            :base(master, isMaster)
        {
            MousePoint = mousePoint;
            MouseButtons = mouseButtons;
            Modifier = modifier;
            Delta = wheelDelta;
        }
    }
}
