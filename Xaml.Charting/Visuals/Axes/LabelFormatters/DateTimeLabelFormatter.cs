// *************************************************************************************
// ULTRACHART © Copyright ulc software Services Ltd. 2011-2013. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: info@ulcsoftware.co.uk
// 
// DateTimeLabelFormatter.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
    /// to format axis and cursor label texts. It also uses the <see cref="DateTimeAxis.SubDayTextFormatting"/> property to alter text-formatting when the date-range 
    /// switches to intra-day
    /// </summary>
    public class DateTimeLabelFormatter : LabelFormatterBase
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
            var dateTime = dataValue.ToDateTime();
            var formattedText = ParentAxis.CursorTextFormatting.IsNullOrEmpty()
                                       ? FormatLabel(dataValue)
                                       : dateTime.ToString(ParentAxis.CursorTextFormatting);

            return formattedText;
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
            var dtAxis = ParentAxis as DateTimeAxis;
            if (dtAxis == null)
            {
                throw new InvalidOperationException("The DateTimeLabelFormatter is only valid on instances of DateTimeAxis");
            }

            var dt = dataValue.ToDateTime();
            var range = ParentAxis.VisibleRange as DateRange;

            if (range.Diff.Ticks > TimeSpan.FromDays(1).Ticks)
                return dt.ToString(dtAxis.TextFormatting);

            return dt.ToString(dtAxis.SubDayTextFormatting);
        }
    }
}