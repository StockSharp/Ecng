// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// HighQualityRenderSurface.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using MatterHackers.Agg;
using Ecng.Xaml.Licensing.Core;
using MatterHackers.Agg.Image;
using Ecng.Xaml.Charting.Licensing;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Rendering.HighSpeedRasterizer;

namespace Ecng.Xaml.Charting.Rendering.HighQualityRasterizer
{
    /// <summary>
    /// Provides a <see cref="RenderSurfaceBase"/> implementation that uses a High-Quality software rasterizer, capable of rendering with sub-pixel accuracy. 
    /// The downside is, the <see cref="HighQualityRenderSurface"/> uses a more accurate floating-point math rendering engine which is slower than the <see cref="HighSpeedRenderSurface"/> counterpart
    /// </summary>
    /// <seealso cref="HighSpeedRenderSurface"/>
    /// <seealso cref="RenderSurfaceBase"/>
    /// <seealso cref="IRenderContext2D"/>
    [UltrachartLicenseProvider(typeof(RenderSurfaceLicenseProvider))]
    public class HighQualityRenderSurface : RenderSurfaceBase
    {
        private ImageBuffer _imageBuffer;
        private Graphics2D _graphics2D;

        protected internal uint[] _emptyStrideRow;

        /// <summary>
        /// Initializes a new instance of the <see cref="HighQualityRenderSurface"/> class.
        /// </summary>
        public HighQualityRenderSurface()
        {
            RecreateSurface();
        }

        /// <summary>
        /// Recreates the WriteableBitmap used by the Viewport
        /// </summary>
        /// <remarks></remarks>
        public override void RecreateSurface()
        {
            base.RecreateSurface();

            _emptyStrideRow = new uint[RenderWriteableBitmap.PixelWidth];
            _imageBuffer = new ImageBuffer(RenderWriteableBitmap.PixelWidth, RenderWriteableBitmap.PixelHeight, 32, new BlenderBGRA());
            _graphics2D = _imageBuffer.NewGraphics2D();
        }

        protected override TextureCacheBase CreateTextureCache() {
            return new TextureCache();
        }

        /// <summary>
        /// When overridden in a derived class, returns a RenderContext valid for the current render pass
        /// </summary>
        /// <returns></returns>
        public override IRenderContext2D GetRenderContext()
        {
            if (IsLicenseValid)
            {
                // Pass the image, bmp and graphics objects to the AggSharpRenderContext
                // At the end of the draw call we assign the _renderWriteableBitmap to the _image.Source
                // to prevent flicker
                return new HqRenderContext(Image, RenderWriteableBitmap, _emptyStrideRow, _imageBuffer, _graphics2D, TextureCache);
            }
            else
                return new NullRenderContext();
        }
    }
}
