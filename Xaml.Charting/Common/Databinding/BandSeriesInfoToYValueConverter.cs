// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// BandSeriesInfoToYValueConverter.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using Ecng.Xaml.Charting.ChartModifiers;
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting
{
    /// <summary>
    /// A converter used to output Y-values from <see cref="BandSeriesInfo"/>, used as part of the Hit-Test API and 
    /// in the data-templates for <see cref="RolloverModifier"/> and <see cref="CursorModifier"/>
    /// </summary>
    public class BandSeriesInfoToYValueConverter: IValueConverter
    {
        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var bandSeriesInfo = value as BandSeriesInfo;

            var result = bandSeriesInfo.FormattedYValue;
            string lower, greater;

            if(bandSeriesInfo.YValue.CompareTo(bandSeriesInfo.Y1Value) >= 0)
            {
                greater = bandSeriesInfo.FormattedYValue;
                lower = bandSeriesInfo.FormattedY1Value;
            }
            else
            {
                greater = bandSeriesInfo.FormattedY1Value;
                lower = bandSeriesInfo.FormattedYValue;
            }

            if (parameter != null)
            {
                switch (parameter.ToString().ToUpperInvariant())
                {
                    case "1":
                        result = bandSeriesInfo.FormattedY1Value;
                        break;
                    case "GREATER":
                        result = greater;
                        break;
                    case "LOWER":
                        result = lower;
                        break;
                }
            }

            return result;
        }

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
