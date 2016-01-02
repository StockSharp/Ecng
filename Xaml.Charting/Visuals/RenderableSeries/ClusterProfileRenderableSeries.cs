using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Ecng.Xaml.Charting.Licensing;
using Ecng.Xaml.Charting.Model.DataSeries.SegmentDataSeries;
using Ecng.Xaml.Charting.Numerics.CoordinateCalculators;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Licensing.Core;
using MatterHackers.VectorMath;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries {
    [UltrachartLicenseProvider(typeof(RenderableSeriesUltrachartLicenseProvider))]
    public class ClusterProfileRenderableSeries : TimeframeSegmentRenderableSeries {
        #region dependency properties

        public static readonly DependencyProperty LineColorProperty = DependencyProperty.Register("LineColor", typeof(Color), typeof(ClusterProfileRenderableSeries), new PropertyMetadata(Colors.DarkGray, OnInvalidateParentSurface));
        public static readonly DependencyProperty TextColorProperty = DependencyProperty.Register("TextColor", typeof(Color), typeof(ClusterProfileRenderableSeries), new PropertyMetadata(Color.FromRgb(90,90,90), OnInvalidateParentSurface));
        public static readonly DependencyProperty ClusterColorProperty = DependencyProperty.Register("ClusterColor", typeof(Color), typeof(ClusterProfileRenderableSeries), new PropertyMetadata(Colors.DarkGreen, OnInvalidateParentSurface));
        public static readonly DependencyProperty ClusterMaxColorProperty = DependencyProperty.Register("ClusterMaxColor", typeof(Color), typeof(ClusterProfileRenderableSeries), new PropertyMetadata(Colors.LimeGreen, OnInvalidateParentSurface));

        public Color LineColor { get { return (Color)GetValue(LineColorProperty); } set { SetValue(LineColorProperty, value); }}
        public Color TextColor { get { return (Color)GetValue(TextColorProperty); } set { SetValue(TextColorProperty, value); }}
        public Color ClusterColor { get { return (Color)GetValue(ClusterColorProperty); } set { SetValue(ClusterColorProperty, value); }}
        public Color ClusterMaxColor { get { return (Color)GetValue(ClusterMaxColorProperty); } set { SetValue(ClusterMaxColorProperty, value); }}

        #endregion

        public ClusterProfileRenderableSeries() {
            DefaultStyleKey = typeof (ClusterProfileRenderableSeries);
        }

        //readonly PerformanceAnalyzer _perf = new PerformanceAnalyzer();

        class LocalRenderContext {
            public ICoordinateCalculator<double> XCalc;
            public ICoordinateCalculator<double> YCalc;
            public int Timeframe;
            public double ScreenWidth;
            public double ScreenHeight;
            public double SegmentWidth;
            public double HalfSegmentWidth;
            public double PriceLevelHeight;
            public double HalfPriceLevelHeight;
            public Color DefaultFontColor;

            public IRenderContext2D RenderContext;
            public IPen2D BarSeparatorPen;
        }

        enum DrawStartPoint {
            //  0deg       90deg      180deg    270deg (counterclockwise)
            BottomLeft, BottomRight, TopRight, TopLeft
        }

        protected override void OnDrawImpl(IRenderContext2D renderContext, IRenderPassData renderPassData) {
            var series = DataSeries as TimeframeSegmentDataSeries;
            if(series == null) return;

            #region initialize

            var pointSeries = (TimeframeSegmentPointSeries)CurrentRenderPassData.PointSeries;
            var segments = pointSeries.Segments;
            var priceStep = pointSeries.PriceStep;

            var ctx = new LocalRenderContext {
                XCalc = CurrentRenderPassData.XCoordinateCalculator,
                YCalc = CurrentRenderPassData.YCoordinateCalculator,
                Timeframe = Timeframe,
                ScreenHeight = renderContext.ViewportSize.Height,
                ScreenWidth = renderContext.ViewportSize.Width,
                RenderContext = renderContext,
                DefaultFontColor = TextColor,
            };

            ctx.SegmentWidth = Math.Abs(ctx.XCalc.GetCoordinate(1) - ctx.XCalc.GetCoordinate(0));
            ctx.PriceLevelHeight = Math.Abs(ctx.YCalc.GetCoordinate(segments[0].Segment.MinPrice) - ctx.YCalc.GetCoordinate(segments[0].Segment.MinPrice + priceStep));
            ctx.HalfSegmentWidth = ctx.SegmentWidth / 2;
            ctx.HalfPriceLevelHeight = ctx.PriceLevelHeight / 2;

            var mainColor = ClusterColor;
            var maxColor = ClusterMaxColor;
            var textColor = TextColor;
            var minDrawPrice = ctx.YCalc.GetDataValue(ctx.ScreenHeight + ctx.PriceLevelHeight);
            var maxDrawPrice = ctx.YCalc.GetDataValue(-ctx.PriceLevelHeight);
            var visibleRange = pointSeries.VisibleRange;

            #region some checks

            if(segments.Length < 1)
                return;

            if(ctx.Timeframe < 1)
                throw new InvalidOperationException($"invalid timeframes. tf1={ctx.Timeframe}");

            if(minDrawPrice > maxDrawPrice)
                throw new InvalidOperationException($"minDrawPrice({minDrawPrice}) > maxDrawPrice({maxDrawPrice})");

            #endregion

            UltrachartDebugLogger.Instance.WriteLine("ClusterProfile: started render {0} segments. Indexes: {1}-{2}, VisibleRange: {3}-{4}", segments.Length, segments[0].Segment.Index, segments[segments.Length-1].Segment.Index, visibleRange.Min, visibleRange.Max);

            #endregion

            using(var penManager = new PenManager(renderContext, false, StrokeThickness, Opacity)) {
                var linePen = penManager.GetPen(LineColor);
                ctx.BarSeparatorPen = penManager.GetPen(Color.FromArgb(50, 0xff, 0xff, 0xff));

                #region draw clusters

                var visibleSegments = segments.Where(s => s.Segment.Index >= visibleRange.Min && s.Segment.Index <= visibleRange.Max);

                // ReSharper disable once PossibleMultipleEnumeration
                if(!visibleSegments.Any())
                    return;

                // ReSharper disable once PossibleMultipleEnumeration
                var maxValue = visibleSegments.Max(s => s.Segment.MaxValue);
                var drawClusters = ctx.SegmentWidth >= 3;

                // ReSharper disable once PossibleMultipleEnumeration
                foreach(var s in visibleSegments) {
                    var segment = s.Segment;
                    var x1 = ctx.XCalc.GetCoordinate(s.X);
                    var x2 = x1 + ctx.SegmentWidth;

                    var vertLineX = x1;

                    if(s.Segment.MinPrice > maxDrawPrice || segment.MaxPrice < minDrawPrice)
                        continue;

                    var y1 = ctx.YCalc.GetCoordinate(segment.MaxPrice) - ctx.HalfPriceLevelHeight;
                    var y2 = ctx.YCalc.GetCoordinate(segment.MinPrice) + ctx.HalfPriceLevelHeight;

                    var data = segment.Values;

                    renderContext.DrawLine(linePen, new Point(vertLineX, y1 + 1), new Point(vertLineX, y2));

                    if(drawClusters) {
                        var localMaxVal = segment.MaxValue;

                        var iter = new BarIterator(data.Length, maxValue, it => {
                            var priceData = data[it.Index];
                            it.Coord = priceData.Price;
                            it.Value = priceData.Value;

                            // ReSharper disable once AccessToDisposedClosure
                            it.BarBrush = penManager.GetBrush(it.Value == localMaxVal ? maxColor : mainColor);
                        });

                        iter.FontColor = textColor;

                        var maxSize = ctx.SegmentWidth - 1;

                        DrawHistogram(iter, ctx, vertLineX + 1, maxSize, DrawStartPoint.TopLeft, 0f);
                    }
                }

                #endregion
            }
        }

        class BarIterator {
            readonly int _count;
            readonly int _maxValue;
            readonly Action<BarIterator> _onNextBar;
            int _index;

            public int Index {get {return _index;}}

            public BarIterator(int count, int maxValue, Action<BarIterator> onNextBar) {
                _count = count;
                _maxValue = maxValue;
                _onNextBar = onNextBar;
            }

            public void Reset() {
                _index = -1;
            }

            public bool NextBar() {
                if(++_index < _count) {
                    _onNextBar(this);
                    return true;
                }
                return false;
            }

            public double Coord {get; set;}
            public int Value {get; set;}
            public IBrush2D BarBrush {get; set;}
            public Color FontColor {get; set;}
            public int MaxValue {get {return _maxValue;}}
        }

        void DrawHistogram(BarIterator bars, LocalRenderContext ctx, double baselineCoord, double barMaxHeight, DrawStartPoint orientation, float minFontSize) {
            if(barMaxHeight <= 1d)
                return;

            ICoordinateCalculator<double> coordCalc;
            int xMultiplier, yMultiplier;
            bool isHorizontal;

            if(orientation == DrawStartPoint.BottomLeft || orientation == DrawStartPoint.TopRight) {
                isHorizontal = true;
                coordCalc = ctx.XCalc;
                xMultiplier = 1;
                yMultiplier = orientation == DrawStartPoint.BottomLeft ? -1 : 1;
            } else {
                isHorizontal = false;
                coordCalc = ctx.YCalc;
                xMultiplier = orientation == DrawStartPoint.TopLeft ? 1 : -1;
                yMultiplier = -1;
            }

            bars.Reset();

            if(isHorizontal) { // horizontal histogram, vertical bars
                var canDrawText = minFontSize > 0f || _fontCalculator.CanDrawText(new Size(ctx.SegmentWidth, _fontCalculator.MinFontHeight), 1);
                var textAlignment = orientation == DrawStartPoint.BottomLeft ? AlignmentY.Top : AlignmentY.Bottom;
                var maxVal = bars.MaxValue;

                while(bars.NextBar()) {
                    var val = bars.Value;
                    var x1 = coordCalc.GetCoordinate(bars.Coord);
                    var y1 = baselineCoord;
                    var x2 = x1 + ctx.SegmentWidth - 1;
                    var y2 = y1 + yMultiplier * (barMaxHeight * val / maxVal);

                    if(x1 > x2) MathHelper.Swap(ref x1, ref x2);
                    if(y1 > y2) MathHelper.Swap(ref y1, ref y2);

                    if(y2 - y1 < 0.5d) continue;

                    DrawBar(ctx, new Point(x1, y1), new Point(x2, y2), bars.BarBrush, false, 
                            canDrawText ? val.ToString(CultureInfo.InvariantCulture) : null,
                            AlignmentX.Center, textAlignment, bars.FontColor, minFontSize, barMaxHeight);
                }
            } else { // vertical histogram, horizontal bars
                var canDrawText = minFontSize > 0f || ctx.PriceLevelHeight >= _fontCalculator.MinFontHeight && barMaxHeight >= _fontCalculator.MinDigitWidth;
                var textAlignment = orientation == DrawStartPoint.TopLeft ? AlignmentX.Left : AlignmentX.Right;
                var maxVal = bars.MaxValue;

                while(bars.NextBar()) {
                    var val = bars.Value;
                    var x1 = baselineCoord;
                    var y1 = coordCalc.GetCoordinate(bars.Coord) - yMultiplier * ctx.HalfPriceLevelHeight;
                    var x2 = x1 + (maxVal > 0 ? xMultiplier * (barMaxHeight * val / maxVal) : 0);
                    var y2 = y1 + yMultiplier * ctx.PriceLevelHeight - yMultiplier * 1;

                    if(x1 > x2) MathHelper.Swap(ref x1, ref x2);
                    if(y1 > y2) MathHelper.Swap(ref y1, ref y2);

                    if(x2 - x1 < 0.5d)
                        continue;

                    DrawBar(ctx, new Point(x1, y1), new Point(x2, y2), bars.BarBrush, true, 
                            canDrawText ? val.ToString(CultureInfo.InvariantCulture) : null,
                            textAlignment, AlignmentY.Center, bars.FontColor, minFontSize, barMaxHeight);
                }
            }
        }

        void DrawBar(LocalRenderContext ctx, Point pt1, Point pt2, IBrush2D brush, bool isHorizontalBar,
                     string text, AlignmentX txtAlignX, AlignmentY txtAlignY, Color fontColor, float minFontSize, double barMaxHeight) {
            
            ctx.RenderContext.FillRectangle(brush, pt1, pt2);

            if(isHorizontalBar) {
                if(pt2.Y - pt1.Y >= 2)
                    ctx.RenderContext.DrawLine(ctx.BarSeparatorPen, pt1, new Point(pt2.X, pt1.Y));
            } else {
                if(pt2.X - pt1.X >= 2)
                    ctx.RenderContext.DrawLine(ctx.BarSeparatorPen, pt1, new Point(pt1.X, pt2.Y));
            }

            if(text != null) {
                var rectText = new Rect(pt1, pt2);
                var fontInfo = _fontCalculator.GetFont(rectText.Size, text.Length, minFontSize);

                if(fontInfo != null && fontInfo.Item3) {
                    ctx.RenderContext.DrawText(text, rectText, txtAlignX, txtAlignY, fontColor, fontInfo.Item1, _fontCalculator.FontFamily, fontInfo.Item2);
                }
            }
        }
    }
}
