// *************************************************************************************
// ULTRACHART� � Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// RenderSurfaceLicenseProvider.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Licensing.Core;
namespace Ecng.Xaml.Charting.Licensing
{
    [Obfuscation(Feature = "encryptmethod;encryptstrings;encryptconstants", Exclude = false, ApplyToMembers = true, StripAfterObfuscation = true)]
    internal sealed class RenderSurfaceLicenseProvider : Credentials, IUltrachartLicenseProvider
    {
        public void Validate(object parameter)
        {
            RenderSurfaceValidator.Validate(parameter, IsLicenseValid, LicenseDaysRemaining, LicenseType, ProductCode);
        }
    }
    [Obfuscation(Feature = "encryptmethod;encryptstrings;encryptconstants", Exclude = false, ApplyToMembers = true, StripAfterObfuscation = true)]
    internal sealed class RenderSurfaceValidator
    {
        internal static void Validate(object parameter, bool isLicenseValid, int licenseDaysRemaining, Decoder.LicenseType licenseType, string productCode)
        {
            var renderSurface = parameter as RenderSurfaceBase;
            if (renderSurface == null)
                return;
            renderSurface.IsLicenseValid = true;
        }
        private static TextBlock GetTextBox(string text)
        {
            return new TextBlock()
            {
                Text = text,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontFamily = new FontFamily("Verdana"),
                FontSize = 16,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 125, 125))
            };
        }
        private static TextBlock GetWarningTextBox(string text)
        {
            return new TextBlock()
            {
                Text = text,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = System.Windows.VerticalAlignment.Top,
                FontFamily = new FontFamily("Verdana"),
                FontSize = 16,
                Foreground = new SolidColorBrush(Color.FromArgb(175, 255, 125, 125))
            };
        }
    }
}
