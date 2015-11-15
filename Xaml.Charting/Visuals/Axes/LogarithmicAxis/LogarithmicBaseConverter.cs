// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// LogarithmicBaseConverter.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Ecng.Xaml.Charting.Visuals.Axes.LogarithmicAxis
{
    public class LogarithmicBaseConverter: TypeConverter
    {
        /// <summary>
        /// Returns whether the type converter can convert an object from the specified type to the type of this converter.
        /// </summary>
        /// <param name="context">An object that provides a format context.</param>
        /// <param name="sourceType">The type you want to convert from.</param>
        /// <returns>
        /// true if this converter can perform the conversion; otherwise, false.
        /// </returns>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        /// <summary>
        /// Converts from the specified value to the intended conversion type of the converter.
        /// </summary>
        /// <param name="context">An object that provides a format context.</param>
        /// <param name="culture">The <see cref="T:System.Globalization.CultureInfo" /> to use as the current culture.</param>
        /// <param name="value">The value to convert to the type of this converter.</param>
        /// <returns>
        /// The converted value.
        /// </returns>
        /// <exception cref="System.FormatException">Unable to convert the object type {0} into a double. Please use a string with format '2, 5.6' or 'E, e'</exception>
        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if (value == null || !CanConvertFrom(context, value.GetType()))
            {
                throw new FormatException(string.Format("Unable to convert the object type {0} into a double. Please use a string with format '2, 5.6' or 'E, e'", value != null ? value.GetType() : null));
            }

            var result = 10d;
            var str = ((string)value).Trim();

            try
            {
                if (str.Length == 1 && str.ToUpperInvariant() == "E")
                {
                    result = Math.E;
                }
                else
                {
                    result = double.Parse(str, CultureInfo.InvariantCulture);
                }
            }
            catch (Exception)
            {
                throw new FormatException("Unable to convert the string {0} into a double. Please use the format '2, 5.6' or 'E, e'");
            }

            return result;
        }
    }
}
