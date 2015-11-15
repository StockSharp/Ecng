// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ThemeAppliedEventArgs.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows;
using Ecng.Xaml.Charting.Themes;

namespace Ecng.Xaml.Charting
{
    /// <summary>
    /// EventArgs used when the <see cref="ThemeManager.ThemeApplied"/> event is raised
    /// </summary>
    /// <seealso cref="ThemeManager"/>
    /// <seealso cref="IThemeProvider"/>
    public class ThemeAppliedEventArgs : EventArgs
    {
        private readonly FrameworkElement _control;
        private readonly string _newTheme;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThemeAppliedEventArgs" /> class.
        /// </summary>
        /// <param name="control">The control which has the theme applied</param>
        /// <param name="newTheme">The new theme string</param>
        public ThemeAppliedEventArgs(FrameworkElement control, string newTheme)
        {
            _control = control;
            _newTheme = newTheme;
        }

        /// <summary>
        /// Gets the control which has the theme applied
        /// </summary>
        public FrameworkElement Control
        {
            get { return _control; }
        }

        /// <summary>
        /// Gets the new theme name
        /// </summary>
        public string NewTheme
        {
            get { return _newTheme; }
        }
    }
}