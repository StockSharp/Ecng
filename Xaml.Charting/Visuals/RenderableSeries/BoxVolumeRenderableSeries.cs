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

        public static readonly DependencyProperty Timeframe2Property = DependencyProperty.Register("Timeframe2", typeof(TimeSpan?), typeof(BoxVolumeRenderableSeries), new PropertyMetadata(TimeSpan.FromMinutes(5), OnInvalidateParentSurface, CoerceHigherTimeframe));
        public static readonly DependencyProperty Timeframe3Property = DependencyProperty.Register("Timeframe3", typeof(TimeSpan?), typeof(BoxVolumeRenderableSeries), new PropertyMetadata(TimeSpan.FromMinutes(15), OnInvalidateParentSurface, CoerceHigherTimeframe));

        public static readonly DependencyProperty Timeframe2ColorProperty = DependencyProperty.Register("Timeframe2Color", typeof(Color), typeof(BoxVolumeRenderableSeries), new PropertyMetadata(Color.FromRgb(36,36,36), OnInvalidateParentSurface));
        public static readonly DependencyProperty Timeframe2FrameColorProperty = DependencyProperty.Register("Timeframe2FrameColor", typeof(Color), typeof(BoxVolumeRenderableSeries), new PropertyMetadata(Color.FromRgb(255,102,0), OnInvalidateParentSurface));
        public static readonly DependencyProperty Timeframe3ColorProperty = DependencyProperty.Register("Timeframe3Color", typeof(Color), typeof(BoxVolumeRenderableSeries), new PropertyMetadata(Color.FromRgb(0,55,24), OnInvalidateParentSurface));

        public static readonly DependencyProperty CellFontColorProperty = DependencyProperty.Register("CellFontColor", typeof(Color), typeof(BoxVolumeRenderableSeries), new PropertyMetadata(Color.FromRgb(90,90,90), OnInvalidateParentSurface));

        public static readonly DependencyProperty HighVolColorProperty = DependencyProperty.Register("HighVolColor", typeof(Color), typeof(BoxVolumeRenderableSeries), new PropertyMetadata(Colors.LawnGreen, OnInvalidateParentSurface));

        public TimeSpan? Timeframe2 {
            get { return (TimeSpan?)GetValue(Timeframe2Property); }
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

        public TimeSpan? Timeframe3 {
            get { return (TimeSpan?)GetValue(Timeframe3Property); }
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
            var tf = (TimeSpan?)newVal;

            if(tf == null)
                return newVal;

            if(ser.Timeframe == null)
                return null;

            if(tf.Value < ser.Timeframe.Value || tf.Value.Ticks % ser.Timeframe.Value.Ticks != 0)
                return ser.Timeframe;

            return newVal;
        }

        public override IndexRange GetExtendedXRange(IndexRange range) {
            var tf = Timeframe;

            if(tf == null)
                return range;

            var offsetCandles = (int)(Math.Max(Timeframe2?.Ticks ?? 0, Timeframe3?.Ticks ?? 0) / tf.Value.Ticks);

            var result = new IndexRange(range.Min - offsetCandles + 1, range.Max + offsetCandles - 1);

            //UltrachartDebugLogger.Instance.WriteLine("GetExtendedXRange: ({0},{1}) => ({2},{3})", range.Min, range.Max, result.Min, result.Max);

            return result;
        }

        //readonly PerformanceAnalyzer _perf = new PerformanceAnalyzer();

        protected override void OnDrawImpl(IRenderContext2D renderContext, IRenderPassData renderPassData) {
            var series = DataSeries as TimeframeSegmentDataSeries;
            if(series == null) return;

            var xCalc = CurrentRenderPassData.XCoordinateCalculator;
            var yCalc = CurrentRenderPassData.YCoordinateCalculator;
            var tf1 = Timeframe;
            var tf2 = tf1 > TimeSpan.Zero ? Timeframe2 : null;
            var tf3 = tf1 > TimeSpan.Zero ? Timeframe3 : null;
            var fontColor = CellFontColor;
            var highVolColor = HighVolColor;
            var screenHeight = renderContext.ViewportSize.Height;
            //var screenWidth = renderContext.ViewportSize.Width;
            var points = (TimeframeSegmentPointSeries) CurrentRenderPassData.PointSeries;
            var segments = points.Segments;
            var numSegments = segments.Length;
            var priceStep = points.PriceStep;
            var visibleRange = points.VisibleRange;

            if (tf1.HasValue)
            {
                if(tf2 < tf1 || tf3 < tf1 ||
                    (tf2.HasValue && tf2.Value.Ticks % tf1.Value.Ticks != 0) ||
                    (tf3.HasValue && tf3.Value.Ticks % tf1.Value.Ticks != 0))
                {
                    throw new InvalidOperationException($"invalid timeframes. tf1={tf1}, tf2={tf2}, tf3={tf3}");
                }
            }

            //_perf.Checkpoint("writeline");
            UltrachartDebugLogger.Instance.WriteLine("BoxVolume: started render {0} segments. Indexes: {1}-{2}, VisibleRange: {3}-{4}", segments.Length, segments[0].Segment.Index, segments[segments.Length-1].Segment.Index, visibleRange.Min, visibleRange.Max);

            //_perf.Checkpoint("writeline done");

            var segmentWidth = Math.Abs(xCalc.GetCoordinate(1) - xCalc.GetCoordinate(0));
            var segmentHeight = Math.Abs(yCalc.GetCoordinate(segments[0].Segment.MinPrice) - yCalc.GetCoordinate(segments[0].Segment.MinPrice + priceStep));
            var halfSegmentHeight = segmentHeight / 2;
            var halfSegmentWidth = segmentWidth / 2;

            var maxDrawPrice = yCalc.GetDataValue(-segmentHeight);
            var minDrawPrice = yCalc.GetDataValue(screenHeight + segmentHeight);
            var buf = new List<TimeframeSegmentWrapper>((int)Math.Max(tf2?.Ticks / tf1?.Ticks ?? 1, tf3?.Ticks / tf1?.Ticks ?? 1));

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

                if (tf3.HasValue)
                {
                    var tf = tf3;
                    for(var i = 0; i < numSegments; ++i) {
                        FillPeriodSegments(buf, segments, i, tf.Value);
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
                }

                #endregion

                //_perf.Checkpoint("start tf2");

                #region draw Timeframe2 grid

                if (tf2.HasValue)
                {
                    var tf = tf2;
                    for(var i = 0; i < numSegments; ++i) {
                        FillPeriodSegments(buf, segments, i, tf.Value);
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

                        var minX = xCalc.GetCoordinate(segment.X) - halfSegmentWidth;

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
                else if (!tf2.HasValue && !tf3.HasValue)
                {
                    for(var i = 0; i < numSegments; ++i) {
                        var segment = segments[i];

                        var minX = xCalc.GetCoordinate(segment.X) - halfSegmentWidth;

                        if(segment.Segment.MinPrice > maxDrawPrice || segment.Segment.MaxPrice < minDrawPrice)
                            continue;

                        var yMinPrice = yCalc.GetCoordinate(segment.Segment.MinPrice);
                        var yMaxPrice = yCalc.GetCoordinate(segment.Segment.MaxPrice);

                        var rect = new Rect(minX, yMaxPrice, Math.Max(segmentWidth, 1), Math.Max(Math.Abs(yMaxPrice - yMinPrice), 1));

                        renderContext.FillRectangle(textBrush, rect.TopLeft, rect.BottomRight);
                    }
                }

                #endregion
            }
        }
    }
}
