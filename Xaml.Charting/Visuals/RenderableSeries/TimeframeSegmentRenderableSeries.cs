using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using Ecng.Xaml.Charting.Common;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Licensing;
using Ecng.Xaml.Charting.Model.DataSeries.SegmentDataSeries;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Licensing.Core;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries {
    [UltrachartLicenseProvider(typeof(RenderableSeriesUltrachartLicenseProvider))]
    abstract public class TimeframeSegmentRenderableSeries : BaseRenderableSeries {
        protected const string _defaultFontFamily = "Tahoma";
        static readonly Brush _defaultVerticalVolumeBrush = new LinearGradientBrush(Color.FromRgb(0, 128, 0), Color.FromRgb(0, 15, 0), 90);

        Dictionary<double, Tuple<double, long>> _horizVolsWidths = new Dictionary<double, Tuple<double, long>>();

        public static readonly DependencyProperty LocalHorizontalVolumesProperty = DependencyProperty.Register(nameof(LocalHorizontalVolumes), typeof(bool), typeof(TimeframeSegmentRenderableSeries), new PropertyMetadata(false, OnInvalidateParentSurface));
        public static readonly DependencyProperty ShowHorizontalVolumesProperty = DependencyProperty.Register(nameof(ShowHorizontalVolumes), typeof(bool), typeof(TimeframeSegmentRenderableSeries), new PropertyMetadata(true, OnInvalidateParentSurface));
        public static readonly DependencyProperty HorizontalVolumeWidthFractionProperty = DependencyProperty.Register(nameof(HorizontalVolumeWidthFraction), typeof(double), typeof(TimeframeSegmentRenderableSeries), new PropertyMetadata(0.15d, OnInvalidateParentSurface, CoerceHorizontalVolumeWidthFraction));
        public static readonly DependencyProperty VolumeBarsBrushProperty = DependencyProperty.Register(nameof(VolumeBarsBrush), typeof(Brush), typeof(TimeframeSegmentRenderableSeries), new PropertyMetadata(_defaultVerticalVolumeBrush, OnInvalidateParentSurface));
        public static readonly DependencyProperty VolBarsFontColorProperty = DependencyProperty.Register(nameof(VolBarsFontColor), typeof(Color), typeof(TimeframeSegmentRenderableSeries), new PropertyMetadata(Colors.White, OnInvalidateParentSurface));

        public bool LocalHorizontalVolumes { get { return (bool)GetValue(LocalHorizontalVolumesProperty); } set { SetValue(LocalHorizontalVolumesProperty, value); }}
        public bool ShowHorizontalVolumes { get { return (bool)GetValue(ShowHorizontalVolumesProperty); } set { SetValue(ShowHorizontalVolumesProperty, value); }}
        public double HorizontalVolumeWidthFraction { get { return (double)GetValue(HorizontalVolumeWidthFractionProperty); } set { SetValue(HorizontalVolumeWidthFractionProperty, value); }}
        public Brush VolumeBarsBrush { get { return (Brush)GetValue(VolumeBarsBrushProperty); } set { SetValue(VolumeBarsBrushProperty, value); }}
        public Color VolBarsFontColor { get { return (Color)GetValue(VolBarsFontColorProperty); } set { SetValue(VolBarsFontColorProperty, value); }}

        public double PriceScale {get { return ((TimeframeSegmentDataSeries)DataSeries).Return(ser => ser.PriceStep, 10d); }}
        public int Timeframe {get { return ((TimeframeSegmentDataSeries)DataSeries).Return(ser => ser.Timeframe, 1); }}

        protected internal override bool IsPartOfExtendedFeatures => true;

        protected override HitTestInfo HitTestInternal(Point rawPoint, double hitTestRadius, bool interpolate) {
            return base.HitTestInternal(rawPoint, hitTestRadius, false);
        }

        static object CoerceHorizontalVolumeWidthFraction(DependencyObject d, object newVal) {
            var f = (double)newVal;

            return f < 0d ? 0d : 
                  (f > 1d ? 1d : newVal);
        }

        abstract protected void OnDrawImpl(IRenderContext2D renderContext, IRenderPassData renderPassData);

        protected FontCalculator _fontCalculator;

        //readonly PerformanceAnalyzer _perf = new PerformanceAnalyzer();
        protected override void InternalDraw(IRenderContext2D renderContext, IRenderPassData renderPassData) {
            if(renderPassData.IsVerticalChart)
                throw new NotSupportedException("vertical charts not supported");

            if(CurrentRenderPassData.XCoordinateCalculator.HasFlippedCoordinates || CurrentRenderPassData.YCoordinateCalculator.HasFlippedCoordinates)
                throw new NotSupportedException("flipped axes not supported");

            if(CurrentRenderPassData.PointSeries.Count < 1)
                return;

            var series = DataSeries as TimeframeSegmentDataSeries;
            if(series == null) return;

            if(_fontCalculator == null) {
                var font = FontFamily != null ? FontFamily.Source : _defaultFontFamily;
                var fontSize = ((float)FontSize).Round(0.5f);
                var fontWeight = FontWeight;

                _fontCalculator = new FontCalculator(renderContext, font, fontSize, fontWeight);
            }

            OnDrawImpl(renderContext, renderPassData);

            if(ShowHorizontalVolumes) {
                DrawHorizontalVolumes(renderContext, renderPassData);
            } else {
                _horizVolsWidths = new Dictionary<double, Tuple<double, long>>();
            }
        }

        protected void DrawGrid(IRenderContext2D renderContext, ISeriesDrawingHelper drawingHelper, Point pt1, Point pt2, int xCount, int yCount, IPen2D framePen, IPen2D gridPen, IBrush2D fillBrush) {
            if(pt1.X >= pt2.X || pt1.Y >= pt2.Y)
                return;

            var w = pt2.X - pt1.X;
            var h = pt2.Y - pt1.Y;
            var xDiff = (pt2.X - pt1.X) / xCount;
            var yDiff = (pt2.Y - pt1.Y) / yCount;

            if(w > 1 && h > 1) {
                if(xDiff > 2 && yDiff > 2) {
                    for(var i = 1; i < xCount; ++i) {
                        var x = pt1.X + i * xDiff;
                        drawingHelper.DrawLine(new Point(x, pt1.Y), new Point(x, pt2.Y), gridPen);
                    }

                    for(var i = 1; i < yCount; ++i) {
                        var y = pt1.Y + i * yDiff;
                        drawingHelper.DrawLine(new Point(pt1.X, y), new Point(pt2.X, y), gridPen);
                    }
                } else {
                    renderContext.FillRectangle(fillBrush, pt1, pt2);
                }
            }

            drawingHelper.DrawQuad(framePen, pt1, pt2);
        }

        protected void FillPeriodSegments(List<TimeframeSegmentWrapper> buf, TimeframeSegmentWrapper[] arr, int index, int tf) {
            buf.Clear();
            var segment = arr[index];

            var period = TimeframeSegmentDataSeries.GetTimeframePeriod(segment.Segment.Time, tf);

            do {
                buf.Add(arr[index]);
                if(index >= arr.Length - 1)
                    break;

                var nextSeg = arr[++index];

                if(nextSeg.Segment.Time >= period.Item2)
                    break;
            } while(true);
        }

        void DrawHorizontalVolumes(IRenderContext2D renderContext, IRenderPassData renderPassData) {
            const float MinHistTextSize = 9f;

            var series = DataSeries as TimeframeSegmentDataSeries;
            if(series == null) return;

            var localHorizVolume = LocalHorizontalVolumes;
            var points = (TimeframeSegmentPointSeries) CurrentRenderPassData.PointSeries;
            var priceStep = points.PriceStep;
            var segments = points.Segments;
            //var xCalc = CurrentRenderPassData.XCoordinateCalculator;
            var yCalc = CurrentRenderPassData.YCoordinateCalculator;
            var screenWidth = renderContext.ViewportSize.Width;
            var screenHeight = renderContext.ViewportSize.Height;
            var segmentHeight = Math.Abs(yCalc.GetCoordinate(segments[0].Segment.MinPrice) - yCalc.GetCoordinate(segments[0].Segment.MinPrice + priceStep));
            var halfSegmentHeight = segmentHeight / 2;
            var maxDrawPrice = yCalc.GetDataValue(-segmentHeight);
            var minDrawPrice = yCalc.GetDataValue(screenHeight + segmentHeight);

            var allPrices = localHorizVolume ? 
                points.AllPrices.Where(p => p > minDrawPrice && p < maxDrawPrice) :
                TimeframeSegmentDataSeries.GeneratePrices(minDrawPrice, maxDrawPrice, priceStep);

            var hvolumes = localHorizVolume ?
                allPrices.ToDictionary(price => price, points.GetVolumeByPrice) :
                allPrices.ToDictionary(price => price, price => series.GetVolumeByPrice(price, priceStep));

            var horizVolumes = hvolumes.Where(kv => kv.Key > minDrawPrice && kv.Key < maxDrawPrice && kv.Value > 0).ToArray();
            var volBarsBrush = VolumeBarsBrush;
            var volBarsFontColor = VolBarsFontColor;
            var volumeLinearBrush = volBarsBrush as LinearGradientBrush;
            var newHorizVolsWidths = new Dictionary<double, Tuple<double, long>>();

            if(!horizVolumes.Any())
                return;

            using(var penManager = new PenManager(renderContext, false, StrokeThickness, Opacity)) {
                var volBrightLinePen = penManager.GetPen(volumeLinearBrush?.GradientStops[0].Color ?? volBarsFontColor);
                var volBarBrush = penManager.GetBrush(volBarsBrush);
                var maxHorizVolume = horizVolumes.Max(kv => kv.Value);
                var widthFraction = HorizontalVolumeWidthFraction;

                foreach(var kv in horizVolumes) {
                    var yTop = yCalc.GetCoordinate(kv.Key) - halfSegmentHeight;
                    var yBottom = yTop + segmentHeight;
                    var width = screenWidth * kv.Value * widthFraction / maxHorizVolume;

                    var pt1 = new Point(0, yTop);
                    var pt2 = new Point(width, yBottom);

                    if(segmentHeight > 1.5) {
                        renderContext.DrawLine(volBrightLinePen, pt1, new Point(width, yTop));
                        renderContext.FillRectangle(volBarBrush, new Point(0, yTop + 1), pt2, 3 * Math.PI / 2);
                    } else {
                        renderContext.FillRectangle(volBarBrush, pt1, pt2, 3 * Math.PI / 2);
                    }

                    newHorizVolsWidths[kv.Key] = Tuple.Create(width, kv.Value);

                    var text = kv.Value.ToString(CultureInfo.InvariantCulture);
                    var rectText = new Rect(new Point(1, yTop), pt2);
                    var fontInfo = _fontCalculator.GetFont(rectText.Size, kv.Value.NumDigitsInPositiveNumber(), MinHistTextSize);

                    if(fontInfo.Item3) {
                        renderContext.DrawText(text, rectText, AlignmentX.Left, AlignmentY.Center, volBarsFontColor, fontInfo.Item1, _fontCalculator.FontFamily, fontInfo.Item2);
                    }
                }

                _horizVolsWidths = newHorizVolsWidths;
            }
        }

        protected override HitTestInfo NearestHitResult(Point mouseRawPoint, double hitTestRadiusInPixels, SearchMode searchMode, bool considerYCoordinateForDistanceCalculation) {
            var dataSeries = DataSeries as TimeframeSegmentDataSeries;
            if(dataSeries == null || dataSeries.Count < 1)
                return HitTestInfo.Empty;

            var priceStep = dataSeries.PriceStep;
            var xCalc = CurrentRenderPassData.XCoordinateCalculator;
            var yCalc = CurrentRenderPassData.YCoordinateCalculator;
            var index = (int)xCalc.GetDataValue(mouseRawPoint.X);
            var yValue = yCalc.GetDataValue(mouseRawPoint.Y);
            var price = yValue.NormalizePrice(priceStep);
            var segmentWidth = Math.Abs(xCalc.GetCoordinate(1) - xCalc.GetCoordinate(0));
            var segmentHeight = Math.Abs(yCalc.GetCoordinate(price) - yCalc.GetCoordinate(price + priceStep));

            if(segmentWidth < 1 || segmentHeight < 1)
                return HitTestInfo.Empty;

            if(ShowHorizontalVolumes) {
                Tuple<double, long> t;

                if(_horizVolsWidths.TryGetValue(price, out t) && mouseRawPoint.X <= t.Item1)
                    return new HitTestInfo {
                        DataSeriesName = dataSeries.SeriesName,
                        DataSeriesType = dataSeries.DataSeriesType,
                        YValue = price,
                        Volume = t.Item2,
                        IsHit = true,
                        HitTestPoint = new Point(t.Item1, yCalc.GetCoordinate(price)),
                    };
            }

            if(index < 0 || index >= dataSeries.Count)
                return HitTestInfo.Empty;

            var segment = dataSeries.Segments[index];
            var vol = segment.GetValueByPrice(yValue);
            if(vol == 0)
                return HitTestInfo.Empty;

            var hitTestInfo = new HitTestInfo {
                DataSeriesName = dataSeries.SeriesName,
                DataSeriesType = dataSeries.DataSeriesType,
                XValue = segment.Time,
                YValue = price,
                DataSeriesIndex = index,
                Volume = vol,
                IsHit = true,
                HitTestPoint = new Point(xCalc.GetCoordinate(index), yCalc.GetCoordinate(price)),
                //HitTestPoint = mouseRawPoint,
            };

            return HitTestSeriesWithBody(mouseRawPoint, hitTestInfo, hitTestRadiusInPixels);
        }

        protected override HitTestInfo HitTestSeriesWithBody(Point rawPoint, HitTestInfo nearestHitPoint, double hitTestRadius) {
            return nearestHitPoint;
        }

        protected class FontCalculator {
            const float MinFontSize = 7f;
            const float MaxFontSize = 32f;
            const float FontSizeStep = 0.5f;

            class FontInfo {
                readonly float _fontSize;
                readonly FontWeight _fontWeight;
                readonly Size _symbolDimensions;

                public float FontSize {get {return _fontSize;}}
                public FontWeight FontWeight {get {return _fontWeight;}}
                public Size SymbolDimensions {get {return _symbolDimensions;}}

                public FontInfo(float size, FontWeight weight, Size dimensions) {
                    _fontSize = size.Round(FontSizeStep);
                    _fontWeight = weight;
                    _symbolDimensions = dimensions;
                }
            }

            readonly FontInfo[] _fontInfos;
            readonly FontInfo _biggestFont, _smallestFont;
            readonly string _fontFamily;
            readonly Dictionary<Tuple<int, int>, FontInfo> _fontInfosDict = new Dictionary<Tuple<int, int>, FontInfo>(); 
            readonly Dictionary<float, FontInfo> _fontInfoBySizeDict = new Dictionary<float, FontInfo>(); 

            public string FontFamily {get {return _fontFamily;}}
            public double MinFontHeight {get {return _smallestFont.SymbolDimensions.Height;}}
            public double MinDigitWidth {get {return _smallestFont.SymbolDimensions.Width;}}

            public FontCalculator(IRenderContext2D renderContext, string fontFamily, float maxFontSize, FontWeight weightAtMaxSize) {
                _watch.Restart();

                _fontFamily = fontFamily;
                maxFontSize = Math.Min(MaxFontSize, maxFontSize).Round(FontSizeStep);

                _fontInfos = new FontInfo[1 + (int)Math.Round((maxFontSize - MinFontSize) / FontSizeStep)];

                for(var size = MinFontSize; size <= maxFontSize; size += FontSizeStep) {
                    var weight = size <= 8.5f ? FontWeights.ExtraLight : size <= 10f ? FontWeights.Light : weightAtMaxSize;
                    var dimensions = renderContext.DigitMaxSize(size, _fontFamily, weight);
                    var fontIndex = (int)Math.Round((size - MinFontSize) / FontSizeStep);
                    _fontInfos[fontIndex] = new FontInfo(size, weight, dimensions);
                }

                _smallestFont = _fontInfos[0];
                _biggestFont = _fontInfos[_fontInfos.Length - 1];

                double dMinW, dMaxW;
                double dMinH, dMaxH;

                dMinW = dMinH = double.MaxValue;
                dMaxW = dMaxH = double.MinValue;

                foreach(var info in _fontInfos) {
                    var w = info.SymbolDimensions.Width;
                    var h = info.SymbolDimensions.Height;

                    if(w < dMinW) dMinW = w;
                    if(w > dMaxW) dMaxW = w;
                    if(h < dMinH) dMinH = h;
                    if(h > dMaxH) dMaxH = h;

                    _fontInfosDict[Tuple.Create((int)Math.Round(w), (int)Math.Round(h))] = info;
                    _fontInfoBySizeDict[info.FontSize] = info;
                }

                var minWidth = (int)Math.Round(dMinW);
                var maxWidth = (int)Math.Round(dMaxW);
                var minHeight = (int)Math.Round(dMinH);
                var maxHeight = (int)Math.Round(dMaxH);

                var reversedIndexes = Enumerable.Range(0, _fontInfos.Length).Reverse().ToArray();

                for(var w = minWidth; w <= maxWidth; ++w)
                    for(var h = minHeight; h <= maxHeight; ++h) {
                        var key = Tuple.Create(w, h);
                        if(_fontInfosDict.ContainsKey(key))
                            continue;

                        _fontInfosDict[key] = reversedIndexes.Select(i => _fontInfos[i]).First(fi => fi.SymbolDimensions.Width <= w && fi.SymbolDimensions.Height <= h);
                    }

                _watch.Stop();

                UltrachartDebugLogger.Instance.WriteLine("Initialized font calculator. Font={0}, MaxSize={1}, MaxWeight={2}, dict.Size={3}, initTime={4:F3}ms", fontFamily, maxFontSize, weightAtMaxSize, _fontInfosDict.Count, _watch.Elapsed.TotalMilliseconds);
            }

            public Tuple<float, FontWeight, bool> GetFont(Size area, int numSymbols, float minFontSize = 0f) {
//                ++_numCalls;
//                _watch.Start();
                try {
                    var symWidth = area.Width / numSymbols;

                    if(symWidth < _smallestFont.SymbolDimensions.Width || area.Height < _smallestFont.SymbolDimensions.Height) {
                        // ReSharper disable once CompareOfFloatsByEqualityOperator
                        if(minFontSize == 0f)
                            return null;

                        var fi = _fontInfoBySizeDict[minFontSize.Round(FontSizeStep)];
                        return Tuple.Create(fi.FontSize, fi.FontWeight, false);
                    }

                    if(symWidth >= _biggestFont.SymbolDimensions.Width && area.Height >= _biggestFont.SymbolDimensions.Height)
                        return Tuple.Create(_biggestFont.FontSize, _biggestFont.FontWeight, true);

                    var w = (int)Math.Floor(Math.Min(symWidth, _biggestFont.SymbolDimensions.Width));
                    var h = (int)Math.Floor(Math.Min(area.Height, _biggestFont.SymbolDimensions.Height));

                    var info = _fontInfosDict[Tuple.Create(w, h)];

                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if(minFontSize == 0f || info.FontSize >= minFontSize)
                        return Tuple.Create(info.FontSize, info.FontWeight, true);

                    info = _fontInfoBySizeDict[minFontSize.Round(FontSizeStep)];
                    return Tuple.Create(info.FontSize, info.FontWeight, false);
                } catch(Exception e) {
                    UltrachartDebugLogger.Instance.WriteLine("GetFont({0}x{1}, {2}, {3:0.##}) error: {4}", area.Width, area.Height, numSymbols, minFontSize, e);
                    var info = _fontInfoBySizeDict[minFontSize.Round(FontSizeStep)];
                    return Tuple.Create(info.FontSize, info.FontWeight, false);
                }

//                } finally {
//                    _watch.Stop();
//                }
            }

            public bool CanDrawText(Size area, int numSymbols) {
                return GetFont(area, numSymbols) != null;
            }

            readonly Stopwatch _watch = new Stopwatch();
//            int _numCalls;
//            public void ResetWatch() {
//                _numCalls = 0;
//                _watch.Reset();
//            }
//            public TimeSpan WorkTime {get {return _watch.Elapsed;}}
//            public int NumCalls {get {return _numCalls;}}
        }

    }

    public class PerformanceAnalyzer {
        readonly Stopwatch _watch = new Stopwatch();
        readonly List<Tuple<int, TimeSpan, string>> _messages = new List<Tuple<int, TimeSpan, string>>(100);

        public void Restart(string msg = null) {
            _messages.Clear();
            _watch.Restart();

            if(msg != null) Checkpoint(msg);
        }

        public void Checkpoint(string msg) {
            _messages.Add(Tuple.Create(Thread.CurrentThread.ManagedThreadId, _watch.Elapsed, msg));
        }

        public void Stop(string msg = null) {
            _watch.Stop();
            if(msg != null) Checkpoint(msg);
        }

        public IEnumerable<string> Report() {
            return _messages.Select(t => string.Format("{0} - {1:0.###}ms - {2}", t.Item1, t.Item2.TotalMilliseconds, t.Item3));
        } 

        public string Report(string separator) {
            return string.Join(separator, Report());
        } 
    }
}
