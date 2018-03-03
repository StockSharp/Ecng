using System;
using System.Windows;
using Ecng.Xaml.Charting.Common;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Licensing;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Licensing.Core;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    [UltrachartLicenseProvider(typeof(RenderableSeriesUltrachartLicenseProvider))]
    public class FastXORenderableSeries : FastOhlcRenderableSeries
    {
        public static readonly DependencyProperty XOBoxSizeProperty = DependencyProperty.Register("XOBoxSize", typeof(double), typeof(FastXORenderableSeries), new PropertyMetadata(1d, OnInvalidateParentSurface));

        public double XOBoxSize { get => (double) GetValue(XOBoxSizeProperty); set => SetValue(XOBoxSizeProperty, value);}

        public FastXORenderableSeries()
        {
            DefaultStyleKey = typeof (FastXORenderableSeries);
            SetCurrentValue(DataPointWidthProperty, 1d);
        }

        protected override void DrawVanilla(IRenderContext2D renderContext, IRenderPassData renderPassData, IPenManager penManager)
        {
            var isVerticalChart = renderPassData.IsVerticalChart;
            var ohlcPoints = CurrentRenderPassData.PointSeries;

            var boxSize = XOBoxSize;
            int setCount = ohlcPoints.Count;
            if (setCount == 1 || boxSize <= 0d)
                return;

            var xCalc = renderPassData.XCoordinateCalculator;
            var yCalc = renderPassData.YCoordinateCalculator;

            var xoWidth = GetDatapointWidth(xCalc, ohlcPoints, DataPointWidth);
            var xoHeight = Math.Abs(yCalc.GetCoordinate(boxSize) - yCalc.GetCoordinate(0));
            const double minSize = 3d;
            var drawBlocks = xoWidth < minSize || xoHeight < minSize;

            var paletteProvider = PaletteProvider;
            
            var drawingHelper = SeriesDrawingHelpersFactory.GetSeriesDrawingHelper(renderContext, CurrentRenderPassData);

            var upBrush = renderContext.CreateBrush(UpWickColor, Opacity);
            var downBrush = renderContext.CreateBrush(DownWickColor, Opacity);

            for (int i = 0; i < setCount; i++)
            {
                var ohlcPoint = (GenericPoint2D<OhlcSeriesPoint>)ohlcPoints[i];

                var high = NumberUtil.RoundUp(ohlcPoint.YValues.High, boxSize);
                var low = NumberUtil.RoundDown(ohlcPoint.YValues.Low, boxSize);
                var isUp = ohlcPoint.YValues.Close >= ohlcPoint.YValues.Open;

                var xCentre = renderPassData.XCoordinateCalculator.GetCoordinate(ohlcPoint.X).ClipToIntValue();
                var xLeft = (xCentre - (xoWidth * 0.5)).ClipToIntValue();
                var xRight = (xCentre + (xoWidth * 0.5)).ClipToIntValue();

                var yHigh = yCalc.GetCoordinate(high).ClipToIntValue();
                var yLow = yCalc.GetCoordinate(low).ClipToIntValue();

                var wickPen = isUp ? _upWickPen : _downWickPen;
                var brush = isUp ? upBrush : downBrush;

                paletteProvider.Do(pp =>
                {
                    var overrideColor = pp.OverrideColor(this, ohlcPoint.X,
                                                               ohlcPoint.YValues.Open,
                                                               ohlcPoint.YValues.High,
                                                               ohlcPoint.YValues.Low,
                                                               ohlcPoint.YValues.Close);

                    if (overrideColor.HasValue)
                    {
                        wickPen = penManager.GetPen(overrideColor.Value);
                        brush = renderContext.CreateBrush(overrideColor.Value, Opacity);
                    }
                });

                var yMax = Math.Max(yLow, yHigh);
                var yMin = Math.Min(yLow, yHigh);

                if (drawBlocks)
                {
                    renderContext.FillRectangle(brush, 
                                                TransformPoint(new Point(xLeft, yLow), isVerticalChart), 
                                                TransformPoint(new Point(xRight, yHigh), isVerticalChart),
                                                0);
                }
                else if (isUp)
                {
                    for (var y = (double)yMax; y > yMin; y -= xoHeight)
                    {
                        var y1 = Math.Max(y - xoHeight, yMin);
                        if (y - y1 >= minSize)
                        {
                            drawingHelper.DrawLine(TransformPoint(new Point(xLeft,  y), isVerticalChart), TransformPoint(new Point(xRight, y1), isVerticalChart), wickPen);
                            drawingHelper.DrawLine(TransformPoint(new Point(xRight, y), isVerticalChart), TransformPoint(new Point(xLeft,  y1), isVerticalChart), wickPen);
                        }
                    }
                }
                else
                {
                    for (var y = (double)yMin; y < yMax; y += xoHeight)
                    {
                        var height = Math.Min(y + xoHeight, yMax) - y;
                        if(height >= minSize)
                            renderContext.DrawEllipse(wickPen, null, new Point(xCentre, y + height/2), xoWidth, height);
                    }
                }
            }
        }
    }
}