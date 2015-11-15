// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ThemeManager.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Themes;
using Ecng.Xaml.Charting.Visuals;

namespace Ecng.Xaml.Charting
{
    /// <summary>
    /// Provides automatic themeing of <see cref="UltrachartSurface"/> via the Theme property.
    /// </summary>
    public static class ThemeManager
    {
        /// <summary>
        /// Defines the Theme dependency property.
        /// </summary>
        public static readonly DependencyProperty ThemeProperty =
            DependencyProperty.RegisterAttached("Theme", typeof(string), typeof(ThemeManager),
#if SILVERLIGHT
            new PropertyMetadata(string.Empty, OnThemeChanged)
#else
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.Inherits, OnThemeChanged)
#endif
            );

        /// <summary>
        /// Gets the value of the Theme Attached Property.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <returns>The current Theme.</returns>
        public static string GetTheme(DependencyObject d)
        {
            return (string)d.GetValue(ThemeProperty);
        }

        /// <summary>
        /// Sets the value of the Theme Attached property. For a list of All Themes, see the <see cref="AllThemes"/> property.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="value">The current theme.</param>
        public static void SetTheme(DependencyObject d, string value)
        {
            d.SetValue(ThemeProperty, value);
        }

        /// <summary>
        /// Raised when a new theme has been applied to the Ultrachart application.
        /// </summary>
        public static event EventHandler<ThemeAppliedEventArgs> ThemeApplied;

        private static readonly IDictionary<string, ResourceDictionary> _themes = new Dictionary<string, ResourceDictionary>();
        private static IThemeProvider _themeColorProvider;

        private static readonly Dictionary<string, IThemeProvider> ThemeProviders = new Dictionary<string, IThemeProvider>();

        static ThemeManager()
        {
            _allThemes = new List<string> { "BlackSteel", "BrightSpark", "Chrome", "Electric", "ExpressionDark", "ExpressionLight", "Oscilloscope" };

            // Default theme is ExpressionDark
            ThemeProvider.ApplyTheme(GetThemeResourceDictionary("ExpressionDark"));
        }

        public static IList<string> _allThemes;
        /// <summary>
        /// Gets a list of all available themes
        /// </summary>
        public static IList<string> AllThemes { get { return _allThemes; } }

        /// <summary>
        /// Gets <see cref="IThemeProvider"/> instance
        /// </summary>
        public static IThemeProvider ThemeProvider
        {
            get { return _themeColorProvider ?? (_themeColorProvider = new ThemeColorProvider()); }
        }

        /// <summary>
        /// Add new theme and themeProvider into _theme and ThemeProviders respectively
        /// </summary>
        /// <param name="theme"></param>
        /// <param name="dictionary"></param>
        public static void AddTheme(string theme, ResourceDictionary dictionary)
        {
            if (!_themes.ContainsKey(theme))
            {
                _themes.Add(theme, dictionary);

                IThemeProvider themeProvider = new ThemeColorProvider();
                themeProvider.ApplyTheme(dictionary);
                ThemeProviders.Add(theme, themeProvider);
                _allThemes.Add(theme);
            }
        }

        /// <summary>
        /// Add theme by string Key from _theme and appropriate themeProvider from ThemeProviders
        /// </summary>
        /// <param name="theme"></param>
        public static void RemoveTheme(string theme)
        {
            if (!_themes.ContainsKey(theme) && ThemeProviders.ContainsKey(theme))
            {
                _themes.Remove(theme);
                ThemeProviders.Remove(theme);
                _allThemes.Remove(theme);
            }
        }

        /// <summary>
        /// Gets a <see cref="IThemeProvider"/> instance for the named <paramref name="theme"/> 
        /// </summary>
        /// <param name="theme">the named <paramref name="theme"/> </param>
        /// <returns>a <see cref="IThemeProvider"/> instance</returns>
        public static IThemeProvider GetThemeProvider(string theme)
        {
            theme = String.IsNullOrEmpty(theme) ? "ExpressionDark" : theme;

            IThemeProvider themeProvider;
            if (!ThemeProviders.TryGetValue(theme, out themeProvider))
            {
                themeProvider = new ThemeColorProvider();
                themeProvider.ApplyTheme(GetThemeResourceDictionary(theme));
                ThemeProviders.Add(theme, themeProvider);
            }

            return themeProvider;
        }

        private static ResourceDictionary GetThemeResourceDictionary(string theme)
        {
            if (theme != null)
            {

                if (_themes.ContainsKey(theme))
                    return _themes[theme];

                var resourceDictionary = new ResourceDictionary{Source = GetThemeUri(theme)};
                
                _themes.Add(theme, resourceDictionary);

                return resourceDictionary;
            }
            return null;
        }

        private static Uri GetThemeUri(string theme)
        {
            if (theme.ToUpper(CultureInfo.InvariantCulture).Contains(";COMPONENT/"))
                return new Uri(theme, UriKind.Relative);

            var packUri =
#if SILVERLIGHT
                String.Format(@"/{0};component/Themes/{1}.xaml", typeof (UltrachartSurface).Assembly.GetDllName(), theme)
#else
                String.Format(@"/{0};component/Themes/{1}.xaml", typeof(UltrachartSurface).Assembly.GetDllName(), theme)
#endif
                ;
            var uri = new Uri(packUri, UriKind.Relative);

            return uri;
        }

        private static void ApplyTheme(this FrameworkElement control, string newTheme)
        {
            if (!string.IsNullOrEmpty(newTheme))
            {
                var resourceDictionary = GetThemeResourceDictionary(newTheme);

                ThemeProvider.ApplyTheme(resourceDictionary);

                OnThemeApplied(new ThemeAppliedEventArgs(control, newTheme));
            }
        }

        private static void OnThemeApplied(ThemeAppliedEventArgs e)
        {
            var handler = ThemeApplied;
            if (handler != null)
            {
                handler(null, e);
            }
        }

        private static void OnThemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var newTheme = e.NewValue as string;
            var oldTheme = e.OldValue as string;

            var control = d as FrameworkElement;

            if (string.IsNullOrEmpty(newTheme) || control == null)
                return;

            if (newTheme.ToUpper(CultureInfo.InvariantCulture) == "RANDOM")
            {
                var random = new Random();
                newTheme = AllThemes.ElementAt(random.Next(AllThemes.Count()));
            }

            var isThemeApplied = oldTheme != newTheme;
#if !SILVERLIGHT
            var valueSource = DependencyPropertyHelper.GetValueSource(control, ThemeProperty);

            isThemeApplied &= valueSource.BaseValueSource != BaseValueSource.Inherited;
#endif

            if (isThemeApplied)
            {
                control.ApplyTheme(newTheme);

                var invalidatable = control as IInvalidatableElement;
                if(invalidatable != null)
                {
                    invalidatable.InvalidateElement();
                }
            }            
        }
    }
}