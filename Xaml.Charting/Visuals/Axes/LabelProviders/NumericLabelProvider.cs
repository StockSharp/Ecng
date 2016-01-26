﻿// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// NumericLabelProvider.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Globalization;

namespace Ecng.Xaml.Charting.Visuals.Axes
{
    /// <summary>
    /// The NumericLabelFormatter is a pass-through which uses the <see cref="AxisBase.TextFormatting"/> and <see cref="AxisBase.CursorTextFormatting"/> properties
    /// to format axis and cursor label texts
    /// </summary>
    public class NumericLabelProvider : LabelProviderBase
    {
        /// <summary>
        /// Creates a 
        /// <see cref="ITickLabelViewModel" /> instance, based on the data-value passed in.
        /// Invokes 
        /// <see cref="FormatLabel" /> to format the specified data-value passed in.
        /// </summary>
        /// <param name="dataValue">The data-value to format</param>
        /// <returns></returns>
        public override ITickLabelViewModel CreateDataContext(IComparable dataValue)
        {
            var label = new NumericTickLabelViewModel();

            return UpdateDataContext(label, dataValue);
        }

        /// <summary>
        /// Updates existing 
        /// <see cref="ITickLabelViewModel" />, based on the data-value passed in.
        /// Invokes 
        /// <see cref="FormatLabel" /> to format the specified data-value passed in.
        /// </summary>
        /// <param name="labelDataContext">The instance to update</param>
        /// <param name="dataValue">The data-value to format</param>
        /// <returns></returns>
        public override ITickLabelViewModel UpdateDataContext(ITickLabelViewModel labelDataContext, IComparable dataValue)
        {
            base.UpdateDataContext(labelDataContext, dataValue);

            var label = (NumericTickLabelViewModel) labelDataContext;
            var numericAxis = (NumericAxis) ParentAxis;

            var formatted = labelDataContext.Text;
            label.Text = formatted;

            var ind = formatted.IndexOfAny(new[] {'e', 'E'});
            label.HasExponent = numericAxis.ScientificNotation != ScientificNotation.None && ind >= 0;

            if (label.HasExponent)
            {
                label.Text = formatted.Substring(0, ind);
                label.Exponent = formatted.Substring(ind + 1);
                label.Separator = numericAxis.ScientificNotation == ScientificNotation.Normalized
                    ? "x10"
                    : formatted[ind].ToString(CultureInfo.InvariantCulture);
            }

            return label;
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
            var formattedText = String.IsNullOrEmpty(ParentAxis.CursorTextFormatting)
                                    ? FormatLabel(dataValue)
                                    : FormatText(dataValue, ParentAxis.CursorTextFormatting);

            return formattedText;
        }

        private string FormatText(IComparable dataValue, string format)
        {
            return string.Format("{0:" + format + "}", dataValue);
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
            return string.Format("{0:" + ParentAxis.TextFormatting + "}", dataValue);
        }
    }
}