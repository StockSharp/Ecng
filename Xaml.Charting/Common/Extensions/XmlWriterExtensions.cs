// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// XmlWriterExtensions.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows;
using System.Windows.Media;
using System.Xml;

namespace Ecng.Xaml.Charting.Common.Extensions
{
    internal static class XmlWriterExtensions
    {
        public static void SerializeProperty(this XmlWriter writer, object element, string propertyName)
        {
            var propertyInfo = element.GetType().GetProperties().First(property=>property.Name == propertyName);

            var propertyValue = propertyInfo.GetValue(element, null);
            if (propertyValue == null)
                return;

            var value = GetValueString(propertyValue);

            writer.WriteAttributeString(propertyName, value);
        }

        private static string GetValueString(object propertyValue)
        {
            string value;
            if (propertyValue is Brush)
            {
                var color = ((Brush) propertyValue).ExtractColor();

                value = String.Format("{0:X},{1:X},{2:X},{3:X}", color.A, color.R, color.G, color.B);
            }
            else if (propertyValue is Color)
            {
                var color = (Color) propertyValue;

                value = String.Format("{0:X},{1:X},{2:X},{3:X}", color.A, color.R, color.G, color.B);
            }
            else if (propertyValue is IRange)
            {
                var rangeType = propertyValue.GetType();
                var range = ((IRange) propertyValue).AsDoubleRange();

                value = String.Format("{0},{1},{2}", rangeType.FullName, range.Min, range.Max);
            }
            else if (propertyValue is Thickness)
            {
                var t = (Thickness) propertyValue;

                value = String.Format("{0},{1},{2},{3}", t.Left, t.Top,t.Right,t.Bottom);
            }
            else
            {
                value = propertyValue.ToString();
            }

            return value;
        }
    }
}
