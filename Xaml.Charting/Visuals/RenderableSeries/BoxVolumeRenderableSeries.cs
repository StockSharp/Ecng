using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Ecng.Xaml.Charting.Licensing;
using Ecng.Xaml.Charting.Model.DataSeries.SegmentDataSeries;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Licensing.Core;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries {
    [UltrachartLicenseProvider(typeof(RenderableSeriesUltrachartLicenseProvider))]
    public class BoxVolumeRenderableSeries : TimeframeSegmentRenderableSeries {
        #region dependency properties

        public static readonly DependencyProperty Timeframe2Property = DependencyProperty.Register("Timeframe2", typeof(int), typeof(BoxVolumeRenderableSeries), new PropertyMetadata(5, OnInvalidateParentSurface, CoerceHigherTimeframe));
        public static readonly DependencyProperty Timeframe3Property = DependencyProperty.Register("Timeframe3", typeof(int), typeof(BoxVolumeRenderableSeries), new PropertyMetadata(15, OnInvalidateParentSurface, CoerceHigherTimeframe));

        public static readonly DependencyProperty Timeframe2ColorProperty = DependencyProperty.Register("Timeframe2Color", typeof(Color), typeof(BoxVolumeRenderableSeries), new PropertyMetadata(Color.FromRgb(36,36,36), OnInvalidateParentSurface));
        public static readonly DependencyProperty Timeframe2FrameColorProperty = DependencyProperty.Register("Timeframe2FrameColor", typeof(Color), typeof(BoxVolumeRenderableSeries), new PropertyMetadata(Color.FromRgb(255,102,0), OnInvalidateParentSurface));
        public static readonly DependencyProperty Timeframe3ColorProperty = DependencyProperty.Register("Timeframe3Color", typeof(Color), typeof(BoxVolumeRenderableSeries), new PropertyMetadata(Color.FromRgb(0,55,24), OnInvalidateParentSurface));

        public static readonly DependencyProperty CellFontColorProperty = DependencyProperty.Register("CellFontColor", typeof(Color), typeof(BoxVolumeRenderableSeries), new PropertyMetadata(Color.FromRgb(90,90,90), OnInvalidateParentSurface));

        public static readonly DependencyProperty HighVolColorProperty = DependencyProperty.Register("HighVolColor", typeof(Color), typeof(BoxVolumeRenderableSeries), new PropertyMetadata(Colors.LawnGreen, OnInvalidateParentSurface));

        public int Timeframe2 {
            get { return (int)GetValue(Timeframe2Property); }
            set { SetValue(Timeframe2Property, value); }
        }

        public Color Timeframe2Color {
            get { return (Color)GetValue(Timeframe2ColorProperty); }
            set { SetValue(Timeframe2ColorProperty, value); }
        }

        public Color Timeframe2FrameColor {
            get { return (Color)GetValue(Timeframe2FrameColorProperty); }
            set { SetValue(Timeframe2FrameColorProperty, value); }
        }

        public int Timeframe3 {
            get { return (int)GetValue(Timeframe3Property); }
            set { SetValue(Timeframe3Property, value); }
        }

        public Color Timeframe3Color {
            get { return (Color)GetValue(Timeframe3ColorProperty); }
            set { SetValue(Timeframe3ColorProperty, value); }
        }

        public Color CellFontColor {
            get { return (Color)GetValue(CellFontColorProperty); }
            set { SetValue(CellFontColorProperty, value); }
        }

        public Color HighVolColor {
            get { return (Color)GetValue(HighVolColorProperty); }
            set { SetValue(HighVolColorProperty, value); }
        }

        #endregion

        public BoxVolumeRenderableSeries() {
            DefaultStyleKey = typeof (BoxVolumeRenderableSeries);
        }

        protected override void OnDataSeriesDependencyPropertyChanged() {
            base.OnDataSeriesDependencyPropertyChanged();

            CoerceValue(Timeframe2Property);
            CoerceValue(Timeframe3Property);
        }

        static object CoerceHigherTimeframe(DependencyObject d, object newVal) {
            var ser = (BoxVolumeRenderableSeries)d;
            var tf = (int)newVal;

            if(tf < ser.Timeframe || tf % ser.Timeframe != 0)
                return ser.Timeframe;

            return newVal;
        }

        public override IndexRange GetExtendedXRange(IndexRange range) {
            var tf = Timeframe;
            if(tf < 1)
                return range;

            var offset = Math.Max(Timeframe2, Timeframe3) / tf;

            var result = new IndexRange(range.Min - offset + 1, range.Max + offset - 1);

            UltrachartDebugLogger.Instance.WriteLine("GetExtendedXRange: ({0},{1}) => ({2},{3})", range.Min, range.Max, result.Min, result.Max);

            return result;
        }

        //readonly PerformanceAnalyzer _perf = new PerformanceAnalyzer();

        protected override void OnDrawImpl(IRenderContext2D renderContext, IRenderPassData renderPassData) {
            //RenderContextBase.Test.Perf = _perf;
            //_perf.Restart("render start");

            var series = DataSeries as BoxVolumeDataSeries;
            if(series == null) return;

            var xCalc = CurrentRenderPassData.XCoordinateCalculator;
            var yCalc = CurrentRenderPassData.YCoordinateCalculator;
            var tf1 = Timeframe;
            var tf2 = Timeframe2;
            var tf3 = Timeframe3;
            var fontColor = CellFontColor;
            var highVolColor = HighVolColor;
            var screenHeight = renderContext.ViewportSize.Height;
            var screenWidth = renderContext.ViewportSize.Width;
            var points = (BoxVolumePointSeries) CurrentRenderPassData.PointSeries;
            var segments = points.Segments;
            var numSegments = segments.Length;
            var priceStep = points.PriceStep;
            var visibleRange = points.VisibleRange;

            if(tf1 < 1 || tf2 < tf1 || tf3 < tf1 || tf2 % tf1 != 0 || tf3 % tf1 != 0)
                throw new InvalidOperationException($"invalid timeframes. tf1={tf1}, tf2={tf2}, tf3={tf3}");

            //_perf.Checkpoint("writeline");
            UltrachartDebugLogger.Instance.WriteLine("BoxVolume: started render {0} segments. Indexes: {1}-{2}, VisibleRange: {3}-{4}", segments.Length, segments[0].Segment.Index, segments[segments.Length-1].Segment.Index, visibleRange.Min, visibleRange.Max);

            //_perf.Checkpoint("writeline done");

            var segmentWidth = Math.Abs(xCalc.GetCoordinate(1) - xCalc.GetCoordinate(0));
            var segmentHeight = Math.Abs(yCalc.GetCoordinate(segments[0].Segment.MinPrice) - yCalc.GetCoordinate(segments[0].Segment.MinPrice + priceStep));
            var halfSegmentHeight = segmentHeight / 2;
            var halfSegmentWidth = segmentWidth / 2;

            var maxDrawPrice = yCalc.GetDataValue(-segmentHeight);
            var minDrawPrice = yCalc.GetDataValue(screenHeight + segmentHeight);
            var buf = new List<TimeframeSegmentWrapper<MinVolumeSegment>>(Math.Max(tf2, tf3));

            if(minDrawPrice > maxDrawPrice)
                throw new InvalidOperationException($"minDrawPrice({minDrawPrice}) > maxDrawPrice({maxDrawPrice})");

            //_perf.Checkpoint("before penmanager");

            using(var penManager = new PenManager(renderContext, false, StrokeThickness, Opacity)) {
                var pen3 = penManager.GetPen(Timeframe3Color);
                var pen2 = penManager.GetPen(Timeframe2Color);

                var ccc = Timeframe2Color;
                var brushTf2 = penManager.GetBrush(Color.FromArgb((byte)(ccc.A / 2), ccc.R, ccc.G, ccc.B));

                ccc = Timeframe3Color;
                var brushTf3 = penManager.GetBrush(Color.FromArgb((byte)(ccc.A / 2), ccc.R, ccc.G, ccc.B));

                var pen2frame = penManager.GetPen(Timeframe2FrameColor);
                var drawingHelper = SeriesDrawingHelpersFactory.GetSeriesDrawingHelper(renderContext, CurrentRenderPassData);

                //_perf.Checkpoint("start tf3");

                #region draw Timeframe3 grid

                var tf = tf3;
                for(var i = 0; i < numSegments; ++i) {
                    FillPeriodSegments(buf, segments, i, tf);
                    i += (buf.Count - 1);

                    double minPrice, maxPrice;
                    int numCellsY;
                    TimeframeDataSegment.MinMax(buf.Select(w => w.Segment), out minPrice, out maxPrice, out numCellsY);

                    var minX = xCalc.GetCoordinate(buf[0].X) - halfSegmentWidth;
                    var maxX = xCalc.GetCoordinate(buf[buf.Count-1].X) + halfSegmentWidth;
                    var minY = yCalc.GetCoordinate(maxPrice) - halfSegmentHeight;
                    var maxY = yCalc.GetCoordinate(minPrice) + halfSegmentHeight;

                    DrawGrid(renderContext, drawingHelper, new Point(minX, minY), new Point(maxX, maxY), buf.Count, numCellsY, pen3, pen3, brushTf3);
                }

                #endregion

                //_perf.Checkpoint("start tf2");

                #region draw Timeframe2 grid

                tf = tf2;
                for(var i = 0; i < numSegments; ++i) {
                    FillPeriodSegments(buf, segments, i, tf);
                    i += (buf.Count - 1);

                    double minPrice, maxPrice;
                    int numCellsY;
                    TimeframeDataSegment.MinMax(buf.Select(w => w.Segment), out minPrice, out maxPrice, out numCellsY);

                    var minX = xCalc.GetCoordinate(buf[0].X) - halfSegmentWidth;
                    var maxX = xCalc.GetCoordinate(buf[buf.Count-1].X) + halfSegmentWidth;
                    var minY = yCalc.GetCoordinate(maxPrice) - halfSegmentHeight;
                    var maxY = yCalc.GetCoordinate(minPrice) + halfSegmentHeight;

                    DrawGrid(renderContext, drawingHelper, new Point(minX, minY), new Point(maxX, maxY), buf.Count, numCellsY, pen2frame, pen2, brushTf2);
                }

                #endregion

                //_perf.Checkpoint("begin text dimensions");

                #region draw volumes

                var maxDigits = segments.Max(s => s.Segment.MaxDigits);
                var cellSize = new Size(segmentWidth, segmentHeight);
                var canDrawDigits = _fontCalculator.CanDrawText(cellSize, maxDigits);

                //_perf.Checkpoint("draw text/box vols");

                var textBrush = penManager.GetBrush(fontColor);
                var brushHigh = penManager.GetBrush(highVolColor);

                if(segmentWidth >= 2 && segmentHeight >= 2) {
                    for(var i = 0; i < numSegments; ++i) {
                        var segment = segments[i];

                        var center = xCalc.GetCoordinate(segment.X) - halfSegmentWidth;
                        var minX = center;

                        foreach(var pv in segment.Segment.Values.Where(v => v != null && v.Price > minDrawPrice && v.Price < maxDrawPrice)) {
                            var centerY = yCalc.GetCoordinate(pv.Price);

                            var rectText = new Rect(minX, centerY - halfSegmentHeight, segmentWidth, segmentHeight);
                            var rectFill = new Rect(minX + 1, centerY - halfSegmentHeight + 1, segmentWidth - 2, segmentHeight - 2);

                            if(pv.Value > 0) {
                                if(canDrawDigits) {
                                    var str = pv.Value.ToString(CultureInfo.InvariantCulture);

                                    var fontInfo = _fontCalculator.GetFont(cellSize, pv.Digits);

                                    var color = pv.Value == segment.Segment.MaxValue ? highVolColor : fontColor;
                                    renderContext.DrawText(str, rectText, AlignmentX.Center, AlignmentY.Center, color, fontInfo.Item1, _fontCalculator.FontFamily, fontInfo.Item2);
                                } else {
                                    renderContext.FillRectangle(pv.Value == segment.Segment.MaxValue ? brushHigh : textBrush, rectFill.TopLeft, rectFill.BottomRight);
                                }
                            }
                        }
                    }
                }

                #endregion
            }
        }
    }
}
