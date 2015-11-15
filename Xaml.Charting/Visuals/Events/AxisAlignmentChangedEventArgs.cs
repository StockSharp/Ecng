// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// AxisAlignmentChangedEventArgs.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
    /// Event Args used by the <see cref="AxisBase.VisibleRangeChanged"/> event
    /// </summary>
    public class AxisAlignmentChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the old <see cref="AxisAlignment"/> value
        /// </summary>
        public AxisAlignment OldAlignment { get; private set; }

        /// <summary>
        /// Gets the new <see cref="AxisAlignment"/> value
        /// </summary>
        public AxisAlignment NewAlignment { get; private set; }

        /// <summary>
        /// Gets the Id of <see cref="IAxis"/>, which alignment has been changed
        /// </summary>
        public string AxisId { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AxisAlignmentChangedEventArgs" /> class.
        /// </summary>
        public AxisAlignmentChangedEventArgs(string axisId, AxisAlignment oldValue, AxisAlignment newValue)
        {
            OldAlignment = oldValue;
            NewAlignment = newValue;
            AxisId = axisId;
        }
    }
}

