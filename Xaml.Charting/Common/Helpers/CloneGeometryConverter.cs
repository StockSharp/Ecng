// *************************************************************************************
// ULTRACHART © Copyright ulc software Services Ltd. 2011-2013. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: info@ulcsoftware.co.uk
// 
// CloneGeometryConverter.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Ecng.Xaml.Charting
{
    public class CloneGeomenverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
                              object parameter, CultureInfo culture)
        {
#if SILVERLIGHT

            if (value == null)
            {
                return null;
            }

            var g = value as Geometry;

            if (g == null)
            {
                return null;
            }

            var clone = g.Clone();

            return clone;
#else
            return value;
#endif
        }

        public object ConvertBack(object value, Type targetType,
                                  object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}