using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using Ecng.Xaml.Charting.Common;
using Ecng.Xaml.Charting.Licensing;
using Ecng.Xaml.Charting.Model.DataSeries.SegmentDataSeries;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Licensing.Core;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries {
    [UltrachartLicenseProvider(typeof(RenderableSeriesUltrachartLicenseProvider))]
    abstract public class TimeframeSegmentRenderableSeries : BaseRenderableSeries {
        protected const string _defaultFontFamily = "Tahoma";

        public double PriceScale {get { return ((TimeframeSegmentDataSeries)DataSeries).Return(ser => ser.PriceStep, 10d); }}
        public int Timeframe {get { return ((TimeframeSegmentDataSeries)DataSeries).Return(ser => ser.Timeframe, 1); }}

        protected internal override bool IsPartOfExtendedFeatures => true;

        protected override HitTestInfo HitTestInternal(Point rawPoint, double hitTestRadius, bool interpolate) {
            return HitTestInfo.Empty;
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

        protected void FillPeriodSegments<T>(List<TimeframeSegmentWrapper<T>> buf, TimeframeSegmentWrapper<T>[] arr, int index, int tf) where T : TimeframeDataSegment {
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

    class NoResetObservableCollection<T> : ObservableCollection<T> {
        /// <summary>Clears all items in the collection by removing them individually.</summary>
        protected sealed override void ClearItems() {
            var items = new List<T>(this);
            foreach(var item in items)
                Remove(item);
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
