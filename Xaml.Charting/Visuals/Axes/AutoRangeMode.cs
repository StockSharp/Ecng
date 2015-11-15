// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// AutoRangeMode.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
namespace Ecng.Xaml.Charting.Visuals.Axes
{
    /// <summary>
    /// Provides values which defines autorange behavior for <see cref="IAxis"/> implementers.
    /// </summary>
    public enum AutoRange
    {
        /// <summary>
        /// Allows the <see cref="IAxis"/> instance decide whether autorange or not when show <see cref="IAxis"/> first time, e.g. if the current VisibleRange is null or undefined
        /// </summary>
        Once,
        /// <summary>
        /// Autorange the <see cref="IAxis"/> instance always. In this case zooming is not allowed by user.  Only AxisDragModifier UI interaction is allowed.
        /// </summary>
        Always,
        /// <summary>
        /// Never autoranges the the <see cref="IAxis"/> instance
        /// </summary>
        Never
    }
}
