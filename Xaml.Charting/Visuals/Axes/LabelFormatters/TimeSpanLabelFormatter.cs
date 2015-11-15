// *************************************************************************************
// ULTRACHART © Copyright ulc software Services Ltd. 2011-2013. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: info@ulcsoftware.co.uk
// 
// TimeSpanLabelFormatter.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Common.Extensions;

namespace Ecng.Xaml.Charting.Visuals.Axes
{
    /// <summary>
    /// The DateTimeLabelFormatter is a pass-through which uses the <see cref="AxisBase.TextFormatting"/> and <see cref="AxisBase.CursorTextFormatting"/> properties
    /// to format axis and cursor label texts. 
    /// </summary>
    public class TimeSpanLabelFormatter : LabelFormatterBase
    {
        /// <summary>
        /// Formats a label for the cursor, from the specified data-value passed in
        /// </summary>
        /// <param name="dataValue">The data-value to format</param>
        /// <returns>
        /// The formatted cursor label string
        /// </returns>
        public override string FormatCursorLabel(IComparable dataValue)
        {
            // Only DateTime's allow text-formatting like HH:mm:ss 
            return dataValue.ToDateTime().ToString(ParentAxis.CursorTextFormatting);
        }

        /// <summary>
        /// Formats a label for the axis from the specified data-value passed in
        /// </summary>
        /// <param name="dataValue">The data-value to format</param>
        /// <returns>
        /// The formatted label string
        /// </returns>
        /// <exception cref="System.InvalidOperationException">The DateTimeLabelFormatter is only valid on instances of DateTimeAxis</exception>
        public override string FormatLabel(IComparable dataValue)
        {
            var timeSpanAxis = ParentAxis as TimeSpanAxis;
            if (timeSpanAxis == null)
            {
                throw new InvalidOperationException("The TimeSpanLabelFormatter is only valid on instances of TimeSpanAxis");
            }

            // Only DateTime's allow text-formatting like HH:mm:ss 
            return dataValue.ToDateTime().ToString(timeSpanAxis.TextFormatting);
        }
    }
}