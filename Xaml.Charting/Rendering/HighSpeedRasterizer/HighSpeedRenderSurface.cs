// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// HighSpeedRenderSurface.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows;
using Ecng.Xaml.Licensing.Core;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Ecng.Xaml.Charting.Licensing;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Rendering.HighQualityRasterizer;

namespace Ecng.Xaml.Charting.Rendering.HighSpeedRasterizer
{
    internal static class WriteableBitmapRenderSurfaceExtensions
    {
        internal static HsRenderContext GetRenderContext(this WriteableBitmap writeableBitmap, Image image, TextureCacheBase textureCache)
        {
            // Pass the image, bmp and size to the WriteableBitmapRenderContext
            // At the end of the draw call we assign the _renderWriteableBitmap to the _image.Source
            // to prevent flicker
            return new HsRenderContext(image, writeableBitmap,
                    new Size(writeableBitmap.PixelWidth, writeableBitmap.PixelHeight), textureCache);
        }
    }

    /// <summary>
    /// Provides a <see cref="RenderSurfaceBase"/> implementation that uses a High-Speed software rasterizer, capable of outputting many millions of points (line-series) 
    /// at interactive framerates. The downside is, the <see cref="HighSpeedRenderSurface"/> uses integer fixed-point math which results in jagged lines. 
    /// </summary>
    /// <seealso cref="HighQualityRenderSurface"/>
    /// <seealso cref="RenderSurfaceBase"/>
    /// <seealso cref="IRenderContext2D"/>
    [UltrachartLicenseProvider(typeof(RenderSurfaceLicenseProvider))]
    public class HighSpeedRenderSurface : RenderSurfaceBase
    {
        protected override TextureCacheBase CreateTextureCache() {
            return new TextureCache();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HighSpeedRenderSurface"/> class.
        /// </summary>
        public HighSpeedRenderSurface()
        {
            RecreateSurface();
        }

        /// <summary>
        /// When overridden in a derived class, returns a RenderContext valid for the current render pass
        /// </summary>
        /// <returns></returns>
        public override IRenderContext2D GetRenderContext()
        {
            return RenderWriteableBitmap.GetRenderContext(Image, TextureCache);
        }
    }
}
