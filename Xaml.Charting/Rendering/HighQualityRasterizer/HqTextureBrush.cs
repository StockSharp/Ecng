// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// AggSharpTextureBrush.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Rendering.Common;
using System.Windows.Media;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Ecng.Xaml.Charting.Rendering.HighQualityRasterizer
{
    internal class HqTextureBrush : IBrush2D
	{
		readonly Brush _brush;

        public HqTextureBrush(Brush brush) // todo use oopacity
		{
            IsTransparent = brush.Opacity == 0 || ((brush is SolidColorBrush) && ((SolidColorBrush)brush).Color.A == 0);
			_brush = brush;
		}
		public System.Windows.Media.Color Color
		{
			get { throw new NotImplementedException(); }
		}
		public int ColorCode
		{
			get { throw new NotImplementedException(); }
		}
		public bool AlphaBlend
		{
			get { throw new NotImplementedException(); }
		}

        public bool IsTransparent { get; private set; }

        public void Dispose()
		{
		}
		
		Size _cachedViewportSize = Size.Empty;
        byte[] _cachedTexture;
#if !SILVERLIGHT
        unsafe
#endif
        public byte[] GetTexture(Size viewportSize) // todo clean code
        {
            if (_cachedViewportSize != viewportSize || _cachedTexture == null)
            {
                Rectangle element = new Rectangle { Width = viewportSize.Width, Height = viewportSize.Height };
                element.Fill = _brush;
                element.MeasureArrange();
                var wbmp = element.RenderToBitmap((int) viewportSize.Width, (int) viewportSize.Height);

                _cachedTexture = new byte[wbmp.PixelWidth * wbmp.PixelHeight * 4];
               using (var bmpContext = wbmp.GetBitmapContext(ReadWriteMode.ReadOnly))
               {
#if !SILVERLIGHT
                   fixed (byte* dest = _cachedTexture)
                    {
                        var src = (byte*)bmpContext.Pixels;
                        NativeMethods.CopyUnmanagedMemory(src, 0, dest, 0, (int)(wbmp.PixelWidth * wbmp.PixelHeight * 4));
                    }
#else
                    System.Buffer.BlockCopy(wbmp.Pixels, 0, _cachedTexture, 0, (int)(wbmp.PixelWidth * wbmp.PixelHeight * 4));
#endif
               }

                _cachedViewportSize = viewportSize;
            }
            return _cachedTexture;
        }
    }
}
