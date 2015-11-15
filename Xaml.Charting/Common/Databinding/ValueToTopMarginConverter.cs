// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ValueToTopMarginConverter.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Data;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting
{
    public class ValueToTopMarginConverter : IValueConverter
    {
        private TextBlock _measurement = new TextBlock();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var exponent = value as TextBlock;
            var dataContext = exponent != null && exponent.DataContext != null ? exponent.DataContext as NumericTickLabelViewModel : null;

            var margin = new Thickness(0, 0, 0, 0);
            if (dataContext != null && dataContext.HasExponent)
            {
                var coef = Double.Parse((string)parameter, CultureInfo.InvariantCulture);

                _measurement.FontSize = exponent.FontSize;
                _measurement.MeasureArrange();

                margin.Top = _measurement.ActualHeight * coef;
            }

            return margin;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
