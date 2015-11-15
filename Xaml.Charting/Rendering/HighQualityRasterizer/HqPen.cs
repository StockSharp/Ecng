// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// AggSharpPen.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

namespace Ecng.Xaml.Charting.Rendering.HighQualityRasterizer
{
    internal class HqPen : IPen2D
    {
        public int StrokeDashArrayIndex { get; set; }
        public double StrokeDashArrayItemPassedLength { get; set; }
        private float _strokeThickness;
        private Color _color;
        private bool _isAntialiased;
        private readonly double[] _strokeDashArray;
        private bool _isTransparent;

        public double[] StrokeDashArray
        {
            get { return _strokeDashArray; }
        }

        public float StrokeThickness
        {
            get { return _strokeThickness; }
        }

        public bool Antialiased
        {
            get { return _isAntialiased; }
        }

        public Color Color
        {
            get { return _color; }
        }

        public int ColorCode
        {
            get { return -1; }
        }

        public bool HasDashes { get; private set; }

        public bool IsTransparent { get { return _isTransparent; } }

        public PenLineCap StrokeEndLineCap { get; private set; }

        internal HqPen(Color color, float strokeThickness, bool isAntialiased, double opacity = 1.0, double[] strokeDashArray = null)
            :this(color, strokeThickness, PenLineCap.Round, isAntialiased, opacity, strokeDashArray)
        {}

        internal HqPen(Color color, float strokeThickness, PenLineCap strokeEndLineCap, bool isAntialiased, double opacity = 1.0, double[] strokeDashArray = null)
        {
            _isTransparent = color.A == 0;
            _strokeThickness = strokeThickness;
            _color = Color.FromArgb((byte)(color.A * opacity), color.R, color.G, color.B);
            _isAntialiased = isAntialiased;
            _strokeDashArray = strokeDashArray;
            HasDashes = _strokeDashArray != null && _strokeDashArray.Length >= 2;

            StrokeEndLineCap = strokeEndLineCap;
        }

        public void Dispose()
        {
        }
    }
}
