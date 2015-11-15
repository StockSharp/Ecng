// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// WriteableBitmapPen.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Rendering.Common;

namespace Ecng.Xaml.Charting.Rendering.HighSpeedRasterizer
{
    internal class HsPen : IPen2D
    {
        public int StrokeDashArrayIndex { get; set; }
		public double StrokeDashArrayItemPassedLength { get; set; }
        private readonly float _strokeThickness;
        private readonly bool _isAntialiased;
        private readonly Color _color;
        private readonly int _colorCode;
        private BitmapContext _pen;
        private double[] _strokeDashArray;
        private bool _isTransparent;

        internal BitmapContext Pen
        {
            get { return _pen; }
        }

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
            get { return _colorCode; }
        }

        public bool IsTransparent { get { return _isTransparent; } }

        public PenLineCap StrokeEndLineCap { get; private set; }

        public bool HasDashes { get; private set; }

        internal HsPen(Color color, int colorCode, float strokeThickness, bool isAntialiased, double opacity = 1.0, double[] strokeDashArray = null)
            : this(color, colorCode, strokeThickness, PenLineCap.Round, isAntialiased, opacity, strokeDashArray)
        {}

        internal HsPen(Color color, int colorCode, float strokeThickness, PenLineCap strokeEndLineCap, bool isAntialiased, double opacity = 1.0, double[] strokeDashArray = null)
        {
            _isTransparent = color.A == 0;
            _color = color;
            _colorCode = colorCode;
            _strokeThickness = strokeThickness;
            _isAntialiased = isAntialiased;
            _strokeDashArray = strokeDashArray;
            HasDashes = _strokeDashArray != null && _strokeDashArray.Length >= 2;

            CreateLineEndCap(strokeEndLineCap, opacity);
        }

        private void CreateLineEndCap(PenLineCap strokeEndLineCap, double opacity)
        {
            if (_strokeThickness > 1)
            {
                Shape lineEndShape = null;
                switch (strokeEndLineCap)
                {
                    case PenLineCap.Round:
                        {
                            lineEndShape = new Ellipse();
                            StrokeEndLineCap = PenLineCap.Round;
                            break;
                        }

                    case PenLineCap.Flat:
                    case PenLineCap.Square:
                    case PenLineCap.Triangle:
                    default:
                        {
                            lineEndShape = new Rectangle();
                            StrokeEndLineCap = PenLineCap.Square;
                            break;
                        }
                }

                lineEndShape.Width = _strokeThickness;
                lineEndShape.Height = _strokeThickness;
                lineEndShape.Opacity = opacity;

                lineEndShape.Fill = new SolidColorBrush(
                    Color.FromArgb((byte) (opacity*_color.A), _color.R, _color.G, _color.B));

                lineEndShape.Arrange(new Rect(0, 0, _strokeThickness, _strokeThickness));
                var bmp = lineEndShape.RenderToBitmap((int) _strokeThickness, (int) _strokeThickness);

                _pen = bmp.GetBitmapContext();
            }
        }

        public void Dispose()
        {
#if !SILVERLIGHT // Fixes bug where Silverlight crashes if pen disposed in SmartDisposable after finalizer
            var localPen = _pen;
            if (localPen.WriteableBitmap != null)
            {
                localPen.WriteableBitmap.Dispatcher.BeginInvokeIfRequired(localPen.Dispose);
            }
#endif
        }
    }
}