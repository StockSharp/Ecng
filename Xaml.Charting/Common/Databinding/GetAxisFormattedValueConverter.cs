using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using Ecng.Xaml.Charting.Visuals.Annotations;

namespace Ecng.Xaml.Charting.Common.Databinding
{
    public class GetAxisFormattedValueConverter: IValueConverter
    {
        private readonly LineAnnotationWithLabelsBase _parentAnnotation;

        public GetAxisFormattedValueConverter(LineAnnotationWithLabelsBase parentAnnotation)
        {
            _parentAnnotation = parentAnnotation;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var labelValue = value as IComparable;
            var formattedValue = value == null ? String.Empty : value.ToString();

            if (_parentAnnotation != null && labelValue != null)
            {
                var axis = _parentAnnotation.GetUsedAxis();

                // Check if LabelTextFormatting is not set by a user
                var defaultBinding =
                    _parentAnnotation.GetBindingExpression(LineAnnotationWithLabelsBase.LabelTextFormattingProperty);

                var isDefault = defaultBinding != null;
                if (isDefault)
                {
                    isDefault = defaultBinding.ParentBinding.Path.Path == "DefaultTextFormatting";
                }

                // Use the axis formatting by default and LabelTextFormatting otherwise
                if (isDefault && axis != null)
                {
                    formattedValue = axis.FormatCursorText(labelValue);
                }
                else
                {
                    formattedValue = String.Format("{0:" + _parentAnnotation.LabelTextFormatting + "}", labelValue);
                }
            }

            return formattedValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}