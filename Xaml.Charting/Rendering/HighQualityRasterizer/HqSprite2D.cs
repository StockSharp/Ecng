// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// AggSharpSprite.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Media.Imaging;
using Ecng.Xaml.Charting.Rendering.Common;
using MatterHackers.Agg.Image;

namespace Ecng.Xaml.Charting.Rendering.HighQualityRasterizer
{
    internal
#if !SILVERLIGHT
    unsafe
#endif
    class HqSprite2D : ISprite2D
    {
        private readonly WriteableBitmap _bmp;
        private ImageBuffer _imageBuffer;

        public HqSprite2D(WriteableBitmap bmp)
        {
            _bmp = bmp;

            _imageBuffer = new ImageBuffer(bmp.PixelWidth, bmp.PixelHeight, 32, new BlenderBGRA());    

            using (var bmpContext = _bmp.GetBitmapContext(ReadWriteMode.ReadOnly))
            {
#if !SILVERLIGHT
                fixed (byte* dest = &_imageBuffer.GetBuffer()[0])
                {
                    var src = (byte*)bmpContext.Pixels;
                    NativeMethods.CopyUnmanagedMemory(src, 0, dest, 0, (int)(bmp.PixelWidth * bmp.PixelHeight * 4));
                }
#else
            System.Buffer.BlockCopy(_bmp.Pixels, 0, _imageBuffer.GetBuffer(), 0, (int)(bmp.PixelWidth * bmp.PixelHeight * 4));
#endif
            }

            Width = _bmp.PixelWidth;
            Height = _bmp.PixelHeight;
        }

        public byte[] GetBuffer()
        {
            return _imageBuffer.GetBuffer();
        }

        public int GetBufferOffsetXY(int i, int j)
        {
            return _imageBuffer.GetBufferOffsetXY(i, j);
        }

        public void Dispose()
        {
        }

        public float Width { get; private set; }
        public float Height { get; private set; }
    }
}
