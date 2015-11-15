using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Ecng.Xaml.Charting.Common;
using Ecng.Xaml.Charting.Common.Helpers;
using Ecng.Xaml.Charting.Numerics.TickCoordinateProviders;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Rendering.HighSpeedRasterizer;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting.Themes
{
    /// <summary>
    /// A panel providing a Bitmap and Labels Canvas for use on <see cref="PolarXAxis"/> within Ultrachart
    /// </summary>
    public class PolarAxisPanel : AxisPanel
    {
        private PolarPanel _polarPanel;
        private double _radius;

        private TickCoordinates _lastDrawnTickCoordinates;
        private float _lastDrawnOffset;

        private PolarCartesianTransformationHelper _transformationHelper;

        protected override Size MeasureOverride(Size availableSize)
        {
            AddTickLabels(AddLabels);

            _polarPanel = Children[0] as PolarPanel;

            _polarPanel.Measure(availableSize);

            foreach (UIElement child in _polarPanel.Children)
            {
                var image = child as Image;
                if (image != null)
                {
                    _axisImage = image;
                    _axisImage.SizeChanged -= OnAxisImageSizeChanged;
                    _axisImage.SizeChanged += OnAxisImageSizeChanged;
                }

                var grid = child as Grid;
                if (grid != null)
                {
                    _labelsContainer = grid;
                }
            }

            return _polarPanel.DesiredSize;
        }

        private void OnAxisImageSizeChanged(object sender, SizeChangedEventArgs sizeChangedEventArgs)
        {
            DrawTicks(_lastDrawnTickCoordinates, _lastDrawnOffset);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _polarPanel.Arrange(new Rect(new Point(0, 0), finalSize));

            return finalSize;
        }

        /// <summary>
        /// Draws ticks on axis
        /// </summary>
        /// <param name="tickCoords"></param>
        /// <param name="offset"></param>
        public override void DrawTicks(TickCoordinates tickCoords, float offset)
        {
            base.DrawTicks(tickCoords, offset);

            _lastDrawnTickCoordinates = tickCoords;
            _lastDrawnOffset = offset;
        }

        /// <summary>
        /// Draws ticks on axis bitmap
        /// </summary>
        /// <param name="renderContext"></param>
        /// <param name="tickStyle"></param>
        /// <param name="tickSize"></param>
        /// <param name="tickCoords"></param>
        /// <param name="offset"></param>
        protected override void DrawTicks(IRenderContext2D renderContext, Style tickStyle, double tickSize, float[] tickCoords, float offset)
        {
            LineToStyle.Style = tickStyle;
            ThemeManager.SetTheme(LineToStyle, ThemeManager.GetTheme(this));

            using (var linePen = renderContext.GetStyledPen(LineToStyle, true))
            {
                foreach (var coord in tickCoords)
                {
                    DrawTick(renderContext, linePen, coord, tickSize);
                }
            }
        }

        private void DrawTick(IRenderContext2D renderContext, IPen2D tickPen, float coord, double tickSize)
        {
            var r = _radius - MajorTickSize;

            var pt1 = _transformationHelper.ToCartesian(coord, r );
            var pt2 = _transformationHelper.ToCartesian(coord, r + tickSize);

            renderContext.DrawLine(tickPen, pt1, pt2);
        }

        /// <summary>
        /// Gets size of image for ticks drawing
        /// </summary>
        /// <returns></returns>
        protected override Size GetRenderContextSize()
        {
            var renderContextSize = AxisImage.RenderSize;

            _transformationHelper = new PolarCartesianTransformationHelper(renderContextSize.Width, renderContextSize.Height);
            _radius = PolarUtil.CalculateViewportRadius(renderContextSize);
            
            return renderContextSize;
        }
    }
}
