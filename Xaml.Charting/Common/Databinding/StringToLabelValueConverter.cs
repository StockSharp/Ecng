// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// StringToLabelValueConverter.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Visuals.Annotations;

namespace Ecng.Xaml.Charting
{
    /// <summary>
    /// TypeConverter to assist in transforming strings to <see cref="LineAnnotation"/> Labels
    /// </summary>
    internal class StringToLabelValueConverter : TypeConverter
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
            if (sourceType == typeof (String))
                return true;

            return base.CanConvertFrom(context, sourceType);
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
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var str = (string) value;

            double doubleResult;
            if (Double.TryParse(str, out doubleResult))
            {
                return doubleResult;
            }

            DateTime dateTimeResult;
            if (DateTime.TryParse(str, out dateTimeResult))
            {
                return dateTimeResult;
            }

            return str;
        }
    }
}
