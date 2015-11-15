// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// MouseButtons.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

namespace Ecng.Xaml.Charting.ChartModifiers
{
    /// <summary>
    /// Specifies the Modifier button pressed at the time of a mouse operation
    /// </summary>
    [Flags]
    public enum MouseModifier
    {
        /// <summary>
        /// No modifiers were pressed
        /// </summary>
        None = 0x0, 

        /// <summary>
        /// The SHIFT button was pressed
        /// </summary>
        Shift = 0x1, 

        /// <summary>
        /// The CTRL button was pressed
        /// </summary>
        Ctrl = 0x2, 

        /// <summary>
        /// The ALT button was pressed
        /// </summary>
        Alt = 0x4
    }

    /// <summary>
    /// Specifies the MouseButtons pressed at the time of a mouse operation
    /// </summary>
    [Flags]
    public enum MouseButtons
    {
        /// <summary>
        /// No buttons were pressed
        /// </summary>
        None = 0x0,

        /// <summary>
        /// The LEFT button was pressed
        /// </summary>
        Left = 0x1,

        /// <summary>
        /// The MIDDLE button was pressed
        /// </summary>
        Middle = 0x2,

        /// <summary>
        /// The RIGHT button was pressed
        /// </summary>
        Right = 0x4
    }
}