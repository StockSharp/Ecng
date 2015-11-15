// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// BitmapPrintingHelper.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Visuals;

namespace Ecng.Xaml.Charting.Common.Helpers
{
    internal static class BitmapPrintingHelper
    {
        public static BitmapSource ExportToBitmapSource(this UIElement source)
        {
            var actualHeight = source.RenderSize.Height;
            var actualWidth = source.RenderSize.Width;

            var renderTarget = 
#if !SILVERLIGHT
                new RenderTargetBitmap((int) actualWidth, (int) actualHeight, 96, 96, PixelFormats.Pbgra32);

            // Fix #SC-1971: rendering chart inside panel using RenderTargetBitmap
            // http://blogs.msdn.com/b/jaimer/archive/2009/07/03/rendertargetbitmap-tips.aspx
            var element = (UIElement)source.GetVisualChildren().Single();

            renderTarget.Render(element);
#else
                new WriteableBitmap((int) actualWidth, (int) actualHeight);
            
            renderTarget.Render(source, null);
            renderTarget.Invalidate();
#endif

            return renderTarget;
        }

#if !SILVERLIGHT
        public static void SaveToFile(BitmapSource bitmap, string fileName, ExportType imageType)
        {
            var encoder = GetEncoder(imageType);
            using (var stream = new FileStream(fileName, FileMode.Create))
            {
                bitmap.WriteToStream(stream,encoder);
            }
        }

        private static BitmapEncoder GetEncoder(ExportType exportType)
        {
            if (exportType == ExportType.Png)
                return new PngBitmapEncoder();

            if(exportType == ExportType.Bmp)
                return new BmpBitmapEncoder();

            if (exportType == ExportType.Jpeg)
                return new JpegBitmapEncoder();
            
            throw new InvalidEnumArgumentException("Unsupported ExportType");
        }


        private static void WriteToStream(this BitmapSource bitmap, Stream stream, BitmapEncoder encoder)
        {
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            encoder.Save(stream);
        }
#endif
    }
}
