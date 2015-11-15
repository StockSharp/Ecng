// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// WriteableBitmapBrush.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Media;
using Ecng.Xaml.Charting.Rendering.Common;

namespace Ecng.Xaml.Charting.Rendering.HighSpeedRasterizer
{
    internal struct HsBrush : IBrush2D
    {
        private Color _color;
        private int _colorCode;
        private bool _alphaBlend;
        private bool _isTransparent;

        internal HsBrush(Color color, int colorCode, bool alphaBlend) : this()
        {
            _isTransparent = color.A == 0;
            _color = color;
            _colorCode = colorCode;
            _alphaBlend = alphaBlend;
        }

        public Color Color
        {
            get { return _color; }
        }

        public int ColorCode
        {
            get { return _colorCode; }
        }

        public bool AlphaBlend
        {
            get { return _alphaBlend; }
        }

        public bool IsTransparent
        {
            get { return _isTransparent; }
        }

        public void Dispose()
        {
        }
    }
}