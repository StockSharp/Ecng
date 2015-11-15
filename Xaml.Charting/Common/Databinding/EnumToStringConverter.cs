using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace Ecng.Xaml.Charting.Common.Databinding {
    public class EnumToStringConverter : IValueConverter {
        public static readonly EnumToStringConverter Instance = new EnumToStringConverter();

        public object Convert(object value, Type targetType = null, object parameter = null, CultureInfo culture = null) {
            if(value != null) {
                var fi = value.GetType().GetField(value.ToString());

                var empty = (string)parameter == "1";

                if(fi != null) {
                    var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
                    return ((attributes.Length > 0) && (!String.IsNullOrEmpty(attributes[0].Description) || empty)) ? attributes[0].Description : value.ToString();
                }
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {throw new Exception("Cant convert back");}
    }
}
