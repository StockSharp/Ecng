// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// TextureBrush.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Media.Imaging;
using System.Windows;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Rendering.Common;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Ecng.Xaml.Charting.Rendering
{
    internal class TextureBrush: IBrush2D
    {
        private const int ByteOffset = 4;

        private readonly TextureCache _textureCache;
        readonly TextureMappingMode _mappingMode;

        int[] _cachedIntTexture;  
        byte[] _cachedByteTexture;

        readonly Brush _brush;
        private Color _color;

        // is used to discard cached texture aray when screen size changes
        Size _cachedTextureSize = Size.Empty;

        // todo use opacity
        public TextureBrush(Brush brush, TextureMappingMode mappingMode, TextureCache textureCache)
        {
            if (textureCache == null) throw new ArgumentNullException();

            IsTransparent = brush.IsTransparent();
            _textureCache = textureCache;
            _mappingMode = mappingMode;
			_brush = brush;
            _color = _brush.ExtractColor();
        }

		public Color Color
		{
			get { return _color; }
		}

		public int ColorCode
		{
			get { throw new NotImplementedException(); }
		}

		public bool AlphaBlend
		{
            get { return true; }
		}

        public bool IsTransparent { get; private set; }

        public void Dispose()
		{
		}
     
        /// <returns>int32 array for WriteableBitmap renderer</returns>
#if !SILVERLIGHT
        unsafe
#endif
        public int[] GetIntTexture(Size viewportSize)
        {
            if (_cachedIntTexture == null ||
                (_mappingMode == TextureMappingMode.PerScreen && _cachedTextureSize != viewportSize))           
            {
                _cachedIntTexture = _textureCache.GetIntTexture(viewportSize, _brush);
                if (_cachedIntTexture == null)
                {
                    Rectangle element = new Rectangle { Width = viewportSize.Width, Height = viewportSize.Height, Fill = _brush };                  
                    element.MeasureArrange();
                    var wbmp = element.RenderToBitmap((int)viewportSize.Width, (int)viewportSize.Height);
                    _cachedIntTexture = new int[wbmp.PixelWidth * wbmp.PixelHeight];
                    using (var bmpContext = wbmp.GetBitmapContext(ReadWriteMode.ReadOnly))
                    {
#if !SILVERLIGHT
                        fixed (int* dest = _cachedIntTexture)
                        {
                            var src = (int*)bmpContext.Pixels;
                            NativeMethods.CopyUnmanagedMemory(src, dest, (int)(wbmp.PixelWidth * wbmp.PixelHeight));
                        }
#else
                        System.Buffer.BlockCopy(wbmp.Pixels, 0, _cachedIntTexture, 0, (int)(wbmp.PixelWidth * wbmp.PixelHeight * ByteOffset));
#endif
                    }
                    _textureCache.AddTexture(viewportSize, _brush, _cachedIntTexture);
                }

                _cachedTextureSize = viewportSize;
            }
            return _cachedIntTexture;
        }

        /// <returns>BGRA byte array for AggSharp renderer</returns>
#if !SILVERLIGHT
        unsafe
#endif
 public byte[] GetByteTexture(Size viewportSize)
        {
            if (_cachedByteTexture == null ||
                (_mappingMode == TextureMappingMode.PerScreen && _cachedTextureSize != viewportSize))
            {
                _cachedByteTexture = _textureCache.GetByteTexture(viewportSize, _brush);
                if (_cachedByteTexture == null)
                {
                    var element = new Rectangle { Width = viewportSize.Width, Height = viewportSize.Height, Fill = _brush };
                    element.MeasureArrange();

                    var wbmp = element.RenderToBitmap((int)viewportSize.Width, (int)viewportSize.Height);
                    _cachedByteTexture = new byte[wbmp.PixelWidth * wbmp.PixelHeight * ByteOffset];
                    using (var bmpContext = wbmp.GetBitmapContext(ReadWriteMode.ReadOnly))
                    {
#if !SILVERLIGHT
                        fixed (byte* dest = _cachedByteTexture)
                        {
                            var src = (byte*)bmpContext.Pixels;
                            NativeMethods.CopyUnmanagedMemory(src, 0, dest, 0, (int)(wbmp.PixelWidth * wbmp.PixelHeight * ByteOffset));
                        }
#else
                        System.Buffer.BlockCopy(wbmp.Pixels, 0, _cachedByteTexture, 0, (int)(wbmp.PixelWidth * wbmp.PixelHeight * ByteOffset));
#endif
                    }
                    for (int offset = 0; offset < _cachedByteTexture.Length; offset += ByteOffset)
                    {
                        int a = Math.Max((int)_cachedByteTexture[offset + 3], 1);

                        // divide by alpha because it was pre-multiplied in RenderTargetBitmap
                        _cachedByteTexture[offset + 2] = (byte)((int)_cachedByteTexture[offset + 2] * 255 / a);
                        _cachedByteTexture[offset + 1] = (byte)((int)_cachedByteTexture[offset + 1] * 255 / a);
                        _cachedByteTexture[offset + 0] = (byte)((int)_cachedByteTexture[offset + 0] * 255 / a);
                    }
                    _textureCache.AddTexture(viewportSize, _brush, _cachedByteTexture);
                }
                _cachedTextureSize = viewportSize;
            }
            return _cachedByteTexture;
        }

        public int GetByteOffsetConsideringMappingMode(int screenX, int screenY, Rect primitiveBoundingRect, double gradiendRotationAngle)
        {
            return GetIntOffsetConsideringMappingMode(screenX, screenY, primitiveBoundingRect, gradiendRotationAngle) * ByteOffset;
        }

        public int GetByteOffsetConsideringMappingMode(int screenX, int screenY, double gradiendRotationAngle)
        {
            return GetIntOffsetConsideringMappingMode(screenX, screenY, gradiendRotationAngle) * ByteOffset;
        }

        public int GetByteOffsetNotConsideringMappingMode(int screenX, int screenY, double gradiendRotationAngle)
        {
            return GetIntOffsetNotConsideringMappingMode(screenX, screenY, gradiendRotationAngle) * ByteOffset;
        }

        public int GetIntOffsetConsideringMappingMode(int screenX, int screenY, Rect primitiveBoundingRect, double gradiendRotationAngle)
        {
            if (_mappingMode == TextureMappingMode.PerScreen) return GetIntOffsetNotConsideringMappingMode(screenX, screenY, gradiendRotationAngle);

            int w = (int)_cachedTextureSize.Width;
            int h = (int)_cachedTextureSize.Height;
            int mappedXOnTexture = (int)((screenX - primitiveBoundingRect.Left) / primitiveBoundingRect.Width * w);
            int mappedYOnTexture = (int)((screenY - primitiveBoundingRect.Top) / primitiveBoundingRect.Height * h);

            RotateAroundCenterOfTexture(ref mappedXOnTexture, ref mappedYOnTexture, gradiendRotationAngle);

            return (mappedYOnTexture * w + mappedXOnTexture);
        }

        /// <summary>
        /// Used for the mountain area filling.
        /// </summary>
        /// <param name="screenX"></param>
        /// <param name="screenY"></param>
        /// <param name="gradiendRotationAngle"></param>
        /// <returns></returns>
        public int GetIntOffsetNotConsideringMappingMode(int screenX, int screenY, double gradiendRotationAngle)
        {
            RotateAroundCenterOfTexture(ref screenX, ref screenY, gradiendRotationAngle);

            return (screenY * (int)_cachedTextureSize.Width + screenX);
        }

        void RotateAroundCenterOfTexture(ref int xOnTexture, ref int yOnTexture, double angle)
        {
            int w = (int)_cachedTextureSize.Width;
            int h = (int)_cachedTextureSize.Height;

            if (angle != 0)
            {
                var centerX = w / 2;
                var centerY = h / 2;

                double dx = (double)(xOnTexture - centerX) / w;
                double dy = (double)(yOnTexture - centerY) / h;

                // todo optimize for standard angles
                var dxRotated = (dx * Math.Cos(angle) - dy * Math.Sin(angle));
                var dyRotated = (dx * Math.Sin(angle) + dy * Math.Cos(angle));

                xOnTexture = centerX + (int)(dxRotated * w);
                yOnTexture = centerY + (int)(dyRotated * w);
            }


            if (xOnTexture >= w) xOnTexture = w - 1; else if (xOnTexture < 0) xOnTexture = 0;
            if (yOnTexture >= h) yOnTexture = h - 1; else if (yOnTexture < 0) yOnTexture = 0;
        }

        public int GetIntOffsetConsideringMappingMode(int screenX, int screenY, double gradiendRotationAngle)
        {
            if (_mappingMode == TextureMappingMode.PerScreen) return GetIntOffsetNotConsideringMappingMode(screenX, screenY, gradiendRotationAngle);

            var primitiveBoundingRect = new Rect(new Point(0, 0), _cachedTextureSize);

            int w = (int)_cachedTextureSize.Width;
            int h = (int)_cachedTextureSize.Height;
            int mappedXOnTexture = (int)((screenX - primitiveBoundingRect.Left) / primitiveBoundingRect.Width * w);
            int mappedYOnTexture = (int)((screenY - primitiveBoundingRect.Top) / primitiveBoundingRect.Height * h);

            RotateAroundCenterOfTexture(ref mappedXOnTexture, ref mappedYOnTexture, gradiendRotationAngle);

            return (mappedYOnTexture * w + mappedXOnTexture);
        }
    }
}
