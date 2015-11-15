// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// StringToDoubleRangeTypeConverter.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace Ecng.Xaml.Charting.Common.Databinding
{
    /// <summary>
    /// TypeConverter to allow conversion of a string value to <see cref="DoubleRange"/>. Used to allow succinct Markup syntax e.g. 
    /// 
    /// &lt;NumericAxis VisibleRange=&quot;10, 20&quot;/&gt;
    /// </summary>
    public class StringToDoubleRangeTypeConverter : TypeConverter
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
            return sourceType == typeof (string);
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
        /// <exception cref="System.FormatException">Unable to convert the string {0} into a DoubleRange. Please use the format '1.234,5.678'</exception>
        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if (value == null)
                return null;

            if (!CanConvertFrom(context, value.GetType()))
            {
                throw new FormatException(string.Format("Unable to convert the object type {0} into a DoubleRange. Please use a string with format '1.234, 5.678' or '1.234'", value.GetType()));
            }

            string str = (string)value;

            try
            {                
                int commaIndex = str.IndexOf(',');

                if (commaIndex == -1)
                {
                    var doubleValue = double.Parse(str, CultureInfo.InvariantCulture);

                    return new DoubleRange(doubleValue, doubleValue);
                }
                else
                {
                    return new DoubleRange(
                        double.Parse(str.Substring(0, commaIndex), CultureInfo.InvariantCulture),
                        double.Parse(str.Substring(commaIndex + 1, str.Length - commaIndex - 1), CultureInfo.InvariantCulture));
                }
            }
            catch (Exception)
            {
                throw new FormatException("Unable to convert the string {0} into a DoubleRange. Please use the format '1.234,5.678' or '1.234'");
            }            
        }
    }
}
