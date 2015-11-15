// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// XmlReaderExtensions.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml;

namespace Ecng.Xaml.Charting.Common.Extensions
{
    internal static class XmlReaderExtensions
    {
        private static readonly FontFamily DefaultFontFamily = new TextBlock().FontFamily;

        private static readonly IDictionary<string, FontWeight> _fontWeights = new Dictionary<string, FontWeight>()
        {
            { FontWeights.Bold.ToString(), FontWeights.Bold},
            { FontWeights.Black.ToString(), FontWeights.Black},
            { FontWeights.ExtraBlack.ToString(), FontWeights.ExtraBlack},
            { FontWeights.ExtraBold.ToString(), FontWeights.ExtraBold},
            { FontWeights.ExtraLight.ToString(), FontWeights.ExtraLight},
            { FontWeights.Light.ToString(), FontWeights.Light},
            { FontWeights.Medium.ToString(), FontWeights.Medium},
            { FontWeights.Normal.ToString(), FontWeights.Normal},
            { FontWeights.SemiBold.ToString(), FontWeights.SemiBold},
#if SILVERLIGHT
            { FontWeights.SemiLight.ToString(), FontWeights.SemiLight},
#endif
            { FontWeights.Thin.ToString(), FontWeights.Thin},
        };

        private static readonly IDictionary<string, FontStyle> _fontStyles = new Dictionary<string, FontStyle>()
        {
            { FontStyles.Italic.ToString(), FontStyles.Italic},
            { FontStyles.Normal.ToString(), FontStyles.Normal},
        };

        public static void DeserilizeProperty(this XmlReader reader, object element, string propertyName)
        {
            var propertyInfo = element.GetType().GetProperty(propertyName);

            var value = GetValue(reader, propertyName, propertyInfo.PropertyType);

            propertyInfo.SetValue(element, value, null);
        }

        public static object GetValue(this XmlReader reader, string attributeName, Type valueType)
        {
            var value = reader[attributeName];

            object newValue;
            if (valueType.IsEnum)
            {
                newValue = value == null ? null : Enum.Parse(valueType, value, false);
            }
            else if (valueType == typeof(Brush))
            {
                if (value == null) return null;
                var color = GetColorFromString(value);

                newValue = new SolidColorBrush(color);
            }
            else if (valueType == typeof(Color))
            {
                if (value == null) return Color.FromArgb(0x00, 0x00, 0x00, 0x00);
                newValue = GetColorFromString(value);
            }
            else if (valueType == typeof(Thickness))
            {
                if (value == null) return new Thickness(0, 0, 0, 0);
                newValue = GetThicknessFromString(value);
            }
            else if (valueType == typeof(FontFamily))
            {
                try
                {
                    newValue = string.IsNullOrEmpty(value) ? DefaultFontFamily : new FontFamily(value);
                }
                catch
                {
                    newValue = DefaultFontFamily;
                }
            }
            else if (valueType == typeof (FontWeight))
            {                
                if (value != null && _fontWeights.ContainsKey(value))
                    newValue = _fontWeights[value];
                else
                    newValue = FontWeights.Normal;
            }
            else if (valueType == typeof(FontStyle))
            {
                if (value != null && _fontStyles.ContainsKey(value))
                    newValue = _fontStyles[value];
                else
                    newValue = FontStyles.Normal;
            }
            else if (typeof(IRange).IsAssignableFrom(valueType))
            {
                if (value == null) return null;
                var rangeComponents = value.Split(',');

                var type = Type.GetType(rangeComponents[0]);
                var min = Convert.ToDouble(rangeComponents[1]);
                var max = Convert.ToDouble(rangeComponents[2]);

                newValue = RangeFactory.NewRange(type, min, max);
            }
            else if (valueType == typeof(TimeSpan))
            {
                if (value == null) return TimeSpan.Zero;
                newValue = TimeSpan.Parse(value);
            }
            else
            {
                if (value == null) return null;
                var type = Nullable.GetUnderlyingType(valueType) ?? valueType;

                newValue = Convert.ChangeType(value, type, CultureInfo.CurrentCulture);
            }

            return newValue;
        }

        private static Color GetColorFromString(string value)
        {
            var colorComponents = value.Split(',');

            var a = Convert.ToByte(colorComponents[0], 16);
            var r = Convert.ToByte(colorComponents[1], 16);
            var g = Convert.ToByte(colorComponents[2], 16);
            var b = Convert.ToByte(colorComponents[3], 16);

            return Color.FromArgb(a, r, g, b);
        }

        private static Thickness GetThicknessFromString(string value)
        {
            var tComponents = value.Split(',');

            var l = Convert.ToDouble(tComponents[0]);
            var t = Convert.ToDouble(tComponents[1]);
            var r = Convert.ToDouble(tComponents[2]);
            var b = Convert.ToDouble(tComponents[3]);

            return new Thickness(l,t,r,b);
        }
    }
}
