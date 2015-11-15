// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ThemeBinding.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Markup;

using Ecng.Xaml.Charting.Themes;

namespace Ecng.Xaml.Charting.Common.MarkupExtensions
{
    /// <summary>
    /// Used to provide dynamic bindings to <see cref="IThemeProvider"/> resources (Brushes, Colors) inside a Ultrachart Theme. For an example of use, 
    /// see the Default.xaml file in the source code
    /// </summary>
    public class ThemeBinding : MarkupExtension
    {
        private static readonly IValueConverter Converter = new ThemeConverter();

        private static readonly RelativeSource Self =
#if SILVERLIGHT
            new RelativeSource(RelativeSourceMode.Self);
#else
            RelativeSource.Self;
#endif

        private static readonly object[] EmptyArray = new object[0];

        /// <summary>
        /// Initializes a new instance of the <see cref="ThemeBinding"/> class.
        /// </summary>
        public ThemeBinding()
        {
            Mode = BindingMode.OneWay;
        }

        /// <summary>
        /// Gets or sets the path to the property on a <see cref="IThemeProvider"/> instance
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the BindingMode.
        /// </summary>
        public BindingMode Mode { get; set; }

        /// <summary>
        /// When implemented in a derived class, returns an object that is provided as the value of the target property for this markup extension.
        /// </summary>
        /// <param name="serviceProvider">A service provider helper that can provide services for the markup extension.</param>
        /// <returns>
        /// The object value to set on the property where the extension is applied.
        /// </returns>
        /// <exception cref="System.Exception">Not a DependencyObject</exception>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var ipvt = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
            if (ipvt == null || ipvt.TargetObject.GetType().Name == "SharedDp")
                return this;
            var setter = ipvt.TargetObject as Setter;
            if (setter != null)
            {
                var binding = new Binding
                                  {
#if SILVERLIGHT
                                      Path = new PropertyPath(Path),
                                      Source = ThemeManager.ThemeProvider,
                                      Mode = BindingMode.OneWay
#else
                                      Path = new PropertyPath(ThemeManager.ThemeProperty),
                                      ConverterParameter = typeof(IThemeProvider).GetProperty(Path).GetGetMethod(),
                                      Converter = Converter,
                                      Mode = BindingMode.OneWay,
                                      RelativeSource = Self
#endif
                                  };
                return binding;
            }
            var depObj = ipvt.TargetObject as DependencyObject;
            if (depObj == null)
            {
                throw new Exception("Not a DependencyObject");
            }
            return ProvideValueForDependencyObject(depObj);
        }

        private object ProvideValueForDependencyObject(DependencyObject depObj)
        {
#if SILVERLIGHT
            return typeof(IThemeProvider).GetProperty(Path).GetValue(ThemeManager.ThemeProvider, EmptyArray);
#else
            var theme = ThemeManager.GetTheme(depObj);
            return typeof(IThemeProvider).GetProperty(Path).GetValue(ThemeManager.GetThemeProvider(theme), EmptyArray);
#endif
        }

        internal class ThemeConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                var getter = parameter as MethodInfo;
                var theme = value as string;
                return getter.Invoke(ThemeManager.GetThemeProvider(theme), EmptyArray);
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }
}
