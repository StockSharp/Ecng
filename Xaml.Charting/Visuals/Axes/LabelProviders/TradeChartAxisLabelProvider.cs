// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// TradeChartAxisLabelProvider.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections;
using System.Linq;
using Ecng.Xaml.Charting.Common.Extensions;

namespace Ecng.Xaml.Charting.Visuals.Axes
{
    /// <summary>
    /// A LabelFormatter instance to use on stock charts. Designed to be used with the <see cref="CategoryDateTimeAxis"/> and applied by default on the <see cref="UltraStockChart"/> control
    /// </summary>
    public class TradeChartAxisLabelProvider : LabelProviderBase
    {
        private readonly string[] _cursorFormatStrings = new[]
            {
                "MMM {0}",
                "yyyy {0}",
                "{0} yyyy",
                "dd MMM {0}", 
            };

        private readonly string[] _majorFormatStrings = new[]
            {
                "yyyy",
                "MMM",
                "dd MMM",
                "HH:mm"
            };

        private int _formatIndex;

        /// <summary>
        /// Called when the label formatted is initialized, with the parent axis instance
        /// </summary>
        /// <param name="parentAxis">The parent <see cref="IAxis" /> instance</param>
        /// <exception cref="System.InvalidOperationException">The TradeChartAxisLabelFormatter is only valid on instances of CategoryDateTimeAxis</exception>
        public override void Init(IAxis parentAxis)
        {
            var catAxis = parentAxis as CategoryDateTimeAxis;
            if (catAxis == null)
            {
                throw new InvalidOperationException("The TradeChartAxisLabelFormatter is only valid on instances of CategoryDateTimeAxis");
            }

            base.Init(parentAxis);
        }

        /// <summary>
        /// Called at the start of an axis render pass, before any labels are formatted for the current draw operation
        /// </summary>
        public override void OnBeginAxisDraw()
        {
            var catAxis = (CategoryDateTimeAxis)ParentAxis;

            var barTimeFrame = catAxis.GetBarTimeFrame();
            var visibleRangeAsDateRange = catAxis.ToDateRange((IndexRange)catAxis.VisibleRange);

            _formatIndex = 3;

            if (visibleRangeAsDateRange != null && visibleRangeAsDateRange.IsDefined)
            {
                long ticksInViewport = visibleRangeAsDateRange.Diff.Ticks;

                if (ticksInViewport > TimeSpanExtensions.FromYears(2).Ticks)
                {
                    _formatIndex = 0;
                }
                else if (ticksInViewport > TimeSpan.FromDays(14).Ticks || barTimeFrame >= TimeSpan.FromDays(1).Ticks)
                {
                    _formatIndex = -1;
                }
            }

            base.OnBeginAxisDraw();
        }

        /// <summary>
        /// Formats a label for the cursor, from the specified data-value passed in
        /// </summary>
        /// <param name="dataValue">The data-value to format</param>
        /// <returns>
        /// The formatted cursor label string
        /// </returns>
        public override string FormatCursorLabel(IComparable dataValue)
        {
            string formattedText;

            var dateTime = dataValue.ToDateTime();
            if (ParentAxis.CursorTextFormatting.IsNullOrEmpty())
            {
                int index = GetFormattingIndex(dateTime, true);

                formattedText = string.Format(dateTime.ToString(_cursorFormatStrings[index]),
                                     dateTime.ToString(_majorFormatStrings[index]));
            }
            else
            {
                formattedText = dateTime.ToString(ParentAxis.CursorTextFormatting);
            }

            return formattedText;
        }

        /// <summary>
        /// Formats a label for the axis from the specified data-value passed in
        /// </summary>
        /// <param name="dataValue">The data-value to format</param>
        /// <returns>
        /// The formatted label string
        /// </returns>
        public override string FormatLabel(IComparable dataValue)
        {
            var dateTime = dataValue.ToDateTime();

            var index = GetFormattingIndex(dateTime);

            var formatString = _majorFormatStrings[index];
            var formattedValue = dateTime.ToString(formatString);

            return formattedValue;
        }

        private int GetFormattingIndex(DateTime dataValue, bool forCursor = false)
        {
            var index = _formatIndex;

            if (index < 0)
            {
                index = 2;

                if (dataValue.Day == 1 && !forCursor)
                {
                    index = 1;
                }
            }

            return index;
        }
    }
}