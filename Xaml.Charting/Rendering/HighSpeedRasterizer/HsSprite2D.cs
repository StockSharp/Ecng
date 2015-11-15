// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// WriteableBitmapSprite2D.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

namespace Ecng.Xaml.Charting.Rendering.HighSpeedRasterizer
{
    internal class HsSprite2D : ISprite2D
    {
        private readonly WriteableBitmap _wbmp;

        public HsSprite2D(WriteableBitmap wbmp)
        {
            _wbmp = wbmp;
            Width = _wbmp.PixelWidth;
            Height = _wbmp.PixelHeight;
        }

        public WriteableBitmap WriteableBitmap
        {
            get { return _wbmp; }
        }

        public void Dispose()
        {
        }

        public float Width { get; private set; }
        public float Height { get; private set; }
    }
}
