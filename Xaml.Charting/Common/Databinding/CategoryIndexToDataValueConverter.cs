// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// CategoryIndexToDataValueConverter.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Linq;
using System.Globalization;
using System.Windows.Data;
using Ecng.Xaml.Charting.Numerics;
using Ecng.Xaml.Charting.Numerics.CoordinateCalculators;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.Annotations;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting.Common.Databinding
{
    internal class CategoryIndexToDataValueConverter : IValueConverter
    {
        private readonly LineAnnotationWithLabelsBase _annotationSource;

        internal CategoryIndexToDataValueConverter(LineAnnotationWithLabelsBase annotationSource)
        {
            Guard.NotNull(annotationSource, "annotationSource");
            _annotationSource = annotationSource;
        }

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
        /// <exception cref="System.NotImplementedException"></exception>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || !_annotationSource.XAxes.Any())
                return value;

            var axis = _annotationSource.GetUsedAxis();

            if (axis == null)
                return null;

            if (axis is ICategoryAxis)
            {
                var catCoordCalculator = axis.GetCurrentCoordinateCalculator() as ICategoryCoordinateCalculator;
                if (catCoordCalculator != null)
                {
                    var index = (int)(value is int ? value : System.Convert.ChangeType(value, typeof(int), CultureInfo.InvariantCulture));

                    value = catCoordCalculator.TransformIndexToData(index);
                }
            }            

            // (SC-2620) Workaround to return the original value, not a string
            var coord = axis.GetCoordinate((IComparable)value);
            value = axis.GetDataValue(coord);

            return value;
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
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
