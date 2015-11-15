// *************************************************************************************
// ULTRACHART� � Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// UltrachartSurfaceLicenseProvider.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Licensing.Core;
using Decoder = Ecng.Xaml.Licensing.Core.Decoder;
namespace Ecng.Xaml.Charting.Licensing
{
    [Obfuscation(Feature = "encryptmethod;encryptstrings;encryptconstants", Exclude = false, ApplyToMembers = true, StripAfterObfuscation = true)]
    internal sealed class UltrachartSurfaceLicenseProvider : Credentials, IUltrachartLicenseProvider
    {
        /// <summary>
        /// Validates the component
        /// </summary>
        /// <param name="parameter">The component to validate</param>
        public void Validate(object parameter)
        {
            var ultraChartSurface = parameter as UltrachartSurface;
            if (ultraChartSurface == null)
                return;
            ultraChartSurface.LicenseDaysRemaining = LicenseDaysRemaining;
            var mainGrid = ultraChartSurface.RootGrid as Grid;
            if (mainGrid == null)
                return;
            var button = mainGrid.Children.Cast<UIElement>().FirstOrDefault(x => x is Button);
            if (button != null)
            {
                mainGrid.Children.Remove(button);
            }

            return;

            if (LicenseType == Decoder.LicenseType.Trial || !IsLicenseValid)
            {
#if !SILVERLIGHT
                var sb = new StringBuilder();
                sb.Append(@"<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'");
                sb.Append(@" xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'");
                sb.Append(@"  TargetType='Button'>");
                sb.Append(@"  <ContentPresenter/>");
                sb.Append(@"</ControlTemplate>");
                var btn = new Button();
                using (Stream ms = new MemoryStream(Encoding.ASCII.GetBytes(sb.ToString())))
                {
                    var controlTemplate = XamlReader.Load(ms) as ControlTemplate;
                    btn.Template = controlTemplate;
                }
                var bitmapSource = new BitmapImage(UriUtil.PackUri);
                btn.Cursor = Cursors.Hand;
                btn.Click += (s, args) => Process.Start(UriUtil.ExtUri);
#else
                var btn = new HyperlinkButton();
                btn.Cursor = Cursors.Hand;
                btn.NavigateUri = UriUtil.ExtUri;
                btn.TargetName = "_blank";
                var bitmapSource = new BitmapImage(UriUtil.PackUri);
#endif
                btn.Content = new Image() { Source = bitmapSource, Stretch = Stretch.None };
                btn.Width = 256;
                btn.Height = 30;
                int chartRow = 3;
                int chartCol = 2;
                Grid.SetRow(btn, chartRow);
                Grid.SetColumn(btn, chartCol);
                btn.VerticalAlignment = VerticalAlignment.Bottom;
                btn.HorizontalAlignment = HorizontalAlignment.Right;
                btn.Margin = new Thickness(5);
                mainGrid.Children.Add(btn);
            }
        }
    }
}
