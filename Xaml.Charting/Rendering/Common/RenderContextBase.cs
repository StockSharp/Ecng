// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// RenderContextBase.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace Ecng.Xaml.Charting.Rendering.Common
{
    /// <summary>
    /// Defines the base class for <see cref="IRenderContext2D"/> implementors, allowing drawing, blitting and creation of pens and brushes on the <see cref="Ecng.Xaml.Charting.Rendering.Common.RenderSurfaceBase"/>
    /// </summary>
    public abstract class RenderContextBase : IRenderContext2D
    {
        protected readonly TextureCacheBase _textureCache;

        private readonly DashSplitter _dashSplitter = new DashSplitter();

        protected RenderContextBase(TextureCacheBase textureCache) {
            _textureCache = textureCache;
        }

        internal DashSplitter DashSplitter { get { return _dashSplitter; }}

        /// <summary>
        /// Gets a collection of <see cref="RenderOperationLayer"/> layers, which allow rendering operations to be posted to a layered queue for later
        /// execution in order (and correct Z-ordering). 
        /// </summary>
        /// <seealso cref="RenderLayer"></seealso>
        /// <seealso cref="RenderOperationLayer"></seealso>
        /// <seealso cref="RenderSurfaceBase"></seealso>
        /// <example>
        /// 	<code title="RenderOperationLayers Example" description="Demonstrates how to enqueue operations to the RenderOperationLayers collection and later flush to ensure rendering operations get processed in the correct Z-order" lang="C#">
        /// RenderOperationLayers layers = renderContext.Layers;
        ///  
        /// // Enqueue some operations in the layers in any order
        /// layers[RenderLayer.AxisMajorGridlines].Enqueue(() =&gt; renderContext.DrawLine(/* .. */));
        /// layers[RenderLayer.AxisBands].Enqueue(() =&gt; renderContext.DrawRectangle(/* .. */));
        /// layers[RenderLayer.AxisMinorGridlines].Enqueue(() =&gt; renderContext.DrawLine(/* .. */));
        ///  
        /// // Processes all layers by executing enqueued operations in order of adding, 
        /// // and in Z-order of layers
        /// layers.Flush();</code>
        /// </example>
        public abstract RenderOperationLayers Layers { get; }

        /// <summary>
        /// Gets the size of the current viewport for this render operation
        /// </summary>
        public abstract Size ViewportSize { get; }

        /// <summary>
        /// enables/disables primitves chaching optimization ( Direct3D renderer only )
        /// </summary>
        public virtual void SetPrimitvesCachingEnabled(bool bEnabled)
        {
        }

        /// <summary>
        /// Creates a <see cref="IBrush2D" /> valid for the current render pass. Use this to draw rectangles, polygons and shaded areas
        /// </summary>
        /// <param name="color">The color of the brush, supports transparency</param>
        /// <param name="opacity">The opacity of the brush</param>
        /// <param name="alphaBlend">If true, use alphablending when shading. If null, auto-detect from the Color</param>
        /// <returns>
        /// The <see cref="IBrush2D" /> instance
        /// </returns>
        public abstract IBrush2D CreateBrush(Color color, double opacity = 1, bool? alphaBlend = null);

        /// <summary>
        /// Creates a <see cref="IBrush2D" /> from WPF Brush valid for the current render pass. Use this to draw rectangles, polygons and shaded areas
        /// </summary>
        /// <param name="brush">The WPF Brush to use as a source, e.g. this can be a <seealso cref="SolidColorBrush" />, or it can be a <seealso cref="LinearGradientBrush" />. Note that solid colors support transparency and are faster than gradient brushes</param>
        /// <param name="opacity">The opacity of the brush</param>
        /// <param name="textureMappingMode"></param>
        /// <returns>
        /// The <see cref="IBrush2D" /> instance
        /// </returns>
        public abstract IBrush2D CreateBrush(Brush brush, double opacity = 1, TextureMappingMode textureMappingMode = TextureMappingMode.PerScreen);

        /// <summary>
        /// Creates a <see cref="IPen2D" /> valid for the current render pass. Use this to draw outlines, quads and lines
        /// </summary>
        /// <param name="color">The color of the pen, supports transparency</param>
        /// <param name="antiAliasing">If true, use antialiasing</param>
        /// <param name="strokeThickness">The strokethickness, default=1.0</param>
        /// <param name="opacity">The opecity of the pen</param>
        /// <param name="strokeDashArray"></param>
        /// <param name="strokeEndLineCap"></param>
        /// <returns>
        /// The <see cref="IPen2D" /> instance
        /// </returns>
        public abstract IPen2D CreatePen(Color color, bool antiAliasing, float strokeThickness, double opacity = 1, double[] strokeDashArray = null, PenLineCap strokeEndLineCap = PenLineCap.Round);

        /// <summary>
        /// Creates a Sprite from FrameworkElement by rendering to bitmap. This may be used in the <see cref="DrawSprite" /> method
        /// to draw to the screen repeatedly
        /// </summary>
        /// <param name="fe"></param>
        /// <returns></returns>
        public abstract ISprite2D CreateSprite(FrameworkElement fe);

        /// <summary>
        /// Clears the <see cref="IRenderSurface2D" />
        /// </summary>
        public abstract void Clear();

        /// <summary>
        /// Blits the source image onto the <see cref="IRenderSurface2D" />
        /// </summary>
        /// <param name="srcSprite">The source sprite to render</param>
        /// <param name="srcRect">The source rectangle</param>
        /// <param name="destPoint">The destination point, which will be the top-left coordinate of the sprite after blitting</param>
        public abstract void DrawSprite(ISprite2D srcSprite, Rect srcRect, Point destPoint);

        /// <summary>
        /// Batch draw of the source sprite onto the <see cref="IRenderSurface2D" />
        /// </summary>
        /// <param name="sprite2D">The sprite to render</param>
        /// <param name="srcRect">The source rectangle</param>
        /// <param name="points">The points to draw sprites at</param>
        public abstract void DrawSprites(ISprite2D sprite2D, Rect srcRect, IEnumerable<Point> points);

        /// <summary>
        /// Batch draw of the source sprite onto the <see cref="IRenderSurface2D" />
        /// </summary>
        /// <param name="sprite2D">The sprite to render</param>
        /// <param name="dstRects">The destination rectangles to draw sprites at</param>
        public abstract void DrawSprites(ISprite2D sprite2D, IEnumerable<Rect> dstRects);

        /// <summary>
        /// Fills a rectangle on the <see cref="IRenderSurface2D" /> using the specified <see cref="IBrush2D" />
        /// </summary>
        /// <param name="brush">The brush</param>
        /// <param name="pt1"></param>
        /// <param name="pt2"></param>
        /// <param name="gradientRotationAngle">The angle which the brush is rotated by</param>
        public abstract void FillRectangle(IBrush2D brush, Point pt1, Point pt2, double gradientRotationAngle = 0);

        /// <summary>
        /// Fills an area, limited by two line segments, e.g. as in a stacked mountain chart, using the specified <see cref="IBrush2D" />
        /// </summary>
        /// <param name="brush">The brush</param>
        /// <param name="lines">The list of lines representing polygon segments</param>
        /// <param name="isVerticalChart">Value, indicates whether chart is vertical</param>
        /// <param name="gradientRotationAngle">The angle which the brush is rotated by</param>
        public abstract void FillArea(IBrush2D brush, IEnumerable<Tuple<Point, Point>> lines, bool isVerticalChart = false, double gradientRotationAngle = 0);

        /// <summary>
        /// Draws an Ellipse on the <see cref="IRenderSurface2D" /> using the specified outline <see cref="IPen2D">Pen</see> and fill <see cref="IBrush2D">Brush</see>
        /// </summary>
        /// <param name="strokePen">The stroke pen</param>
        /// <param name="fillBrush">The fill brush</param>
        /// <param name="center">The center of the ellipse in pixels</param>
        /// <param name="width">The width of the ellipse in pixels</param>
        /// <param name="height">The height of the ellipse in pixels</param>
        public abstract void DrawEllipse(IPen2D strokePen, IBrush2D fillBrush, Point center, double width, double height);

        /// <summary>
        /// Draws 0..N Ellipses at the points passed in with the same width, height, pen and brush
        /// </summary>
        /// <param name="strokePen"></param>
        /// <param name="fillBrush"></param>
        /// <param name="centres">The points to draw ellipses at</param>
        /// <param name="width">The common width for all ellipses</param>
        /// <param name="height">The common height for all ellipses</param>
        public abstract void DrawEllipses(IPen2D strokePen, IBrush2D fillBrush, IEnumerable<Point> centres, double width, double height);

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public abstract void Dispose();

        /// <summary>
        /// Call this method, passing in <see cref="IDisposable" /> instance to dispose after the render pass completes.
        /// Called internally by Ultrachart to lazy-dispose of Direct2D and Direct3D brushes and textures
        /// </summary>
        /// <param name="disposable"></param>
        public abstract void DisposeResourceAfterDraw(IDisposable disposable);

        /// <summary>
        /// Draws vertical scan line for heatmap
        /// from bottom to top, from yStart to yEnd
        /// </summary>
        /// <param name="x">Screen X coordinate where to draw pixels</param>
        /// <param name="yStartBottom">Screen Y coordinate of vertical scan line's bottom.
        /// Can be located outdide of visible area, in this case not all pixels in list are rendered</param>
        /// <param name="yEndTop">Screen Y coordinate of vertical scan line's top.
        /// Can be located outdide of visible area, in this case not all pixels in list are rendered</param>
        /// <param name="pixelColorsArgb">The colors to apply to the vertical scanline</param>
        /// <param name="opacity">The opacity of the vertical scaline, from 0.0 to 1.0</param>
        /// <param name="yAxisIsFlipped">if set to <c>true</c> then y axis is flipped.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public abstract void DrawPixelsVertically(int x, int yStartBottom, int yEndTop, IList<int> pixelColorsArgb, double opacity, bool yAxisIsFlipped);

        /// <summary>
        /// Draws a Quad on the <see cref="IRenderSurface2D" /> using the specified <see cref="IPen2D" />
        /// </summary>
        /// <param name="pen">The Pen</param>
        /// <param name="pt1"></param>
        /// <param name="pt2"></param>
        public virtual void DrawQuad(IPen2D pen, Point pt1, Point pt2)
        {
            // is it outside of viewport?
            if (pt1.X < 0 && pt2.X < 0) return;
            if (pt1.Y < 0 && pt2.Y < 0) return;
            if (pt1.Y > ViewportSize.Height && pt2.Y > ViewportSize.Height) return;
            if (pt1.X > ViewportSize.Width && pt2.X > ViewportSize.Width) return;

            ClipRectangle(ref pt1, ref pt2, 1, 1);

            using (var lineContext = this.BeginLine(pen, pt2.X, pt1.Y))
            {
                lineContext.MoveTo(pt2.X, pt2.Y);
                lineContext.MoveTo(pt1.X, pt2.Y);
                lineContext.MoveTo(pt1.X, pt1.Y);
                lineContext.MoveTo(pt2.X, pt1.Y);
            }
        }

        /// <summary>
        /// Draws a single line on the 
        /// <see cref="IRenderSurface2D" /> using the specified 
        /// <see cref="IPen2D" />.
        /// Note for a faster implementation in some rasterizers, use DrawLines
        /// </summary>
        /// <param name="pen">The pen</param>
        /// <param name="pt1">The start of the line in pixels</param>
        /// <param name="pt2">The end of the line in pixels</param>
        public virtual void DrawLine(IPen2D pen, Point pt1, Point pt2)
        {
            using (var lineContext = this.BeginLine(pen, pt1.X, pt1.Y))
            {
                lineContext.MoveTo(pt2.X, pt2.Y);
            }
        }

        /// <summary>
        /// Draws a multi-point line on the <see cref="IRenderSurface2D" /> using the specified <see cref="IPen2D" />
        /// </summary>
        /// <param name="pen">The pen</param>
        /// <param name="points">The points.</param>
        public virtual void DrawLines(IPen2D pen, IEnumerable<Point> points)
        {
            var itr = points.GetEnumerator();
            itr.MoveNext();
            var firstPoint = itr.Current;

            using (var lineContext = this.BeginLine(pen, firstPoint.X, firstPoint.Y))
            {
                while (itr.MoveNext())
                {
                    var currentPoint = itr.Current;

                    lineContext.MoveTo(currentPoint.X, currentPoint.Y);
                }
            }
        }

        /// <summary>
        /// Fills a polygon on the <see cref="IRenderSurface2D" /> using the specifie <see cref="IBrush2D" />
        /// </summary>
        /// <param name="brush">The brush</param>
        /// <param name="points">The list of points defining the closed polygon, where X,Y coordinates in clockwise direction</param>
        public virtual void FillPolygon(IBrush2D brush, IEnumerable<Point> points)
        {
            var itr = points.GetEnumerator();
            itr.MoveNext();
            var firstPoint = itr.Current;

            using (var fillContext = this.BeginPolygon(brush, firstPoint.X, firstPoint.Y))
            {
                while (itr.MoveNext())
                {
                    var currentPoint = itr.Current;

                    fillContext.MoveTo(currentPoint.X, currentPoint.Y);
                }
            }
        }

        public void TextDrawDimensions(string text, float fontSize, Color foreColor, out float width, out float height, string fontFamily = null, FontWeight fontWeight = default(FontWeight)) {
            TextDrawDimensionsInternal(text, fontSize, foreColor, out width, out height, fontFamily, fontWeight);
        }

        /// <summary>
        /// Draws text if it does not go outside
        /// </summary>
        /// <param name="dstBoundingRect"></param>
        /// <param name="alignY"></param>
        /// <param name="foreColor"></param>
        /// <param name="fontSize"></param>
        /// <param name="text"></param>
        /// <param name="fontFamily"></param>
        /// <param name="fontWeight"></param>
        /// <param name="alignX"></param>
        public void DrawText(string text, Rect dstBoundingRect, AlignmentX alignX, AlignmentY alignY, Color foreColor, float fontSize, string fontFamily = null, FontWeight fontWeight = default(FontWeight)) {
            float width, height;

            var characterSprites = TextDrawDimensionsInternal(text, fontSize, foreColor, out width, out height, fontFamily, fontWeight);

            if(width > dstBoundingRect.Width) return;
            if(height > dstBoundingRect.Height) return;

            var startPoint = GetStartPoint(dstBoundingRect, new Rect(new Size(width, height)), alignX, alignY);
            var x = startPoint.X;

            foreach (var sprite in characterSprites) {
                DrawSprite(sprite, new Rect(0, 0, sprite.Width, sprite.Height), new Point(x, startPoint.Y));
                x += sprite.Width;
            }
        }

        /// <summary>
        /// Draws text relative to base point 
        /// </summary>
        public void DrawText(string text, Point basePoint, AlignmentX alignX, AlignmentY alignY, Color foreColor, float fontSize, string fontFamily = null, FontWeight fontWeight = default(FontWeight)) {
            float width, height;

            var characterSprites = TextDrawDimensionsInternal(text, fontSize, foreColor, out width, out height, fontFamily, fontWeight);

            var startPoint = GetStartPoint(basePoint, new Rect(new Size(width, height)), alignX, alignY);
            var x = startPoint.X;

            foreach (var sprite in characterSprites) {
                DrawSprite(sprite, new Rect(0, 0, sprite.Width, sprite.Height), new Point(x, startPoint.Y));
                x += sprite.Width;
            }
        }

        public Size DigitMaxSize(float fontSize, string fontFamily = null, FontWeight fontWeight = default(FontWeight)) {
            if(fontFamily == null) fontFamily = "Tahoma";
            if(fontWeight == default(FontWeight)) fontWeight = FontWeights.Regular;

            fontSize = fontSize.Round(0.5f);
            var key = Tuple.Create(fontFamily, fontSize, fontWeight);
            Size size;
            if(_textureCache.MaxDigitSizeDict.TryGetValue(key, out size))
                return size;

            double maxw, maxh;

            maxw = double.MinValue;
            maxh = double.MinValue;

            var digits = new[] {'0', '1', '2', '3', '4', '5', '6', '7', '8', '9'};
            foreach(var sprite in digits.Select(d => GetCharSprite(d, fontFamily, fontSize, fontWeight, Colors.White))) {
                if(sprite.Width > maxw) maxw = sprite.Width;
                if(sprite.Height > maxh) maxh = sprite.Height;
            }

            size = new Size(maxw, maxh);
            _textureCache.MaxDigitSizeDict[key] = size;

            return size;
        }

        public const float FontSizeStep = 0.5f;

        IEnumerable<ISprite2D> TextDrawDimensionsInternal(string text, float fontSize, Color foreColor, out float width, out float height, string fontFamily = null, FontWeight fontWeight = default(FontWeight)) {
            if(fontFamily == null) fontFamily = "Tahoma";
            if(fontWeight == default(FontWeight)) fontWeight = FontWeights.Regular;

            fontSize = fontSize.Round(FontSizeStep);

            var characterSprites = text.Select(character => GetCharSprite(character, fontFamily, fontSize, fontWeight, foreColor)).ToArray();

            if(characterSprites.Length == 0) {
                width = height = 0;
                return characterSprites;
            }

            width = characterSprites.Sum(x => x.Width);
            height = characterSprites.Max(x => x.Height);

            return characterSprites;
        }

        ISprite2D GetCharSprite(char character, string fontFamily, float fontSize, FontWeight fontWeight, Color color) {
            var key = new CharSpriteKey {Character = character, ForeColor = color, FontFamily = fontFamily, FontWeight = fontWeight, FontSize = fontSize};
            ISprite2D sprite;

            if(!_textureCache.FontCache.TryGetValue(key, out sprite)) {
                sprite = CreateSprite(new TextBlock {
                    Text = new string(character, 1),
                    Foreground = new SolidColorBrush(color),
                    FontFamily = GetFontByName(fontFamily),
                    FontSize = fontSize,
                    FontWeight = fontWeight,
                    Margin = new Thickness(0)
                });

                _textureCache.FontCache.Add(key, sprite);
            }

            return sprite;
        }

        /// <summary>
        /// Begins a Polyline segment, returning the <see cref="IPathDrawingContext" />. This is the fastest way to draw lines and simply a proxy to <see cref="DrawLines" /> method.
        /// </summary>
        /// <param name="pen">The pen for the line segment</param>
        /// <param name="startX">The start X coordinate (pixel coord)</param>
        /// <param name="startY">The start Y coordinate (pixel coord)</param>
        /// <returns>
        /// The <see cref="IPathDrawingContext" /> to continue the line
        /// </returns>
        public abstract IPathDrawingContext BeginLine(IPen2D pen, double startX, double startY);

        /// <summary>
        /// Begins a filled Polygon segment, returning the <see cref="IPathDrawingContext" />. This is the fastest way to draw polygon and simply a proxy to <see cref="FillArea" /> method.
        /// </summary>
        /// <param name="brush">The brush for the polygon fill</param>
        /// <param name="startX">The start X coordinate (pixel coord)</param>
        /// <param name="startY">The start Y coordinate (pixel coord)</param>
        /// <param name="gradientRotationAngle">The angle which the <param name="brush"></param> is rotated by</param>
        /// <returns>
        /// The <see cref="IPathDrawingContext" /> to continue the polygon
        /// </returns>
        public abstract IPathDrawingContext BeginPolygon(IBrush2D brush, double startX, double startY, double gradientRotationAngle = 0d);

        /// <returns>false if line is outside of visible area</returns>
        internal static bool ClipLine(ref Point pt1, ref Point pt2, Size viewportSize)
        {
            Rect viewportRect = new Rect(0, 0, viewportSize.Width, viewportSize.Height);

            if (!pt1.IsInBounds(viewportSize) || !pt2.IsInBounds(viewportSize))
            {
                // Fixes issue when points have infinity values ( such as #SC-1899:Log(0) issue )
                var pt1X = pt1.X.ClipToInt();
                var pt1Y = pt1.Y.ClipToInt();
                var pt2X = pt2.X.ClipToInt();
                var pt2Y = pt2.Y.ClipToInt();

                var r = WriteableBitmapExtensions.CohenSutherlandLineClip(
                    viewportRect,
                    ref pt1X, ref pt1Y, ref pt2X, ref pt2Y);
                pt1.X = pt1X; pt1.Y = pt1Y; pt2.X = pt2X; pt2.Y = pt2Y;
                return r;
            }

            return true;
        }

        protected void ClipRectangle(ref Point pt1, ref Point pt2, int yExtension, int xExtension)
        {
            pt1 = pt1.ClipPoint(ViewportSize, yExtension, xExtension);
            pt2 = pt2.ClipPoint(ViewportSize, yExtension, xExtension);
        }

        protected void ClipRectangle(ref Point pt1, ref Point pt2)
        {
            pt1 = pt1.ClipPoint(ViewportSize);
            pt2 = pt2.ClipPoint(ViewportSize);
        }

        /// <summary>
        /// Used internally: Clips the zero line (e.g. in mountain fills) to the viewport
        /// </summary>
        protected double ClipZeroLineForArea(double zeroLine, bool isVerticalChart)
        {
            if (zeroLine < 0) return 0;
            if (isVerticalChart)
            {
                if (zeroLine > ViewportSize.Width) return ViewportSize.Width;
            }
            else
            {
                if (zeroLine > ViewportSize.Height) return ViewportSize.Height;
            }
            return zeroLine;
        }

        /// <summary>
        /// Used internally to clip a polygon or line-segment to the viewport
        /// </summary>
        protected IEnumerable<Point> ClipArea(IEnumerable<Point> points, int xExtension = 0,
            int yExtension = 0)
        {
            return PointUtil.ClipPolygon(points, ViewportSize, xExtension, yExtension);
        }

        /// <summary>
        /// Returns true if the point is inside the viewport
        /// </summary>
        protected bool IsInBounds(Point pt)
        {
            return !(pt.X < 0 || pt.X > ViewportSize.Width || pt.Y < 0 || pt.Y > ViewportSize.Height);
        }

        /// <summary>
        /// Used internally to clip the area of a StackedMountainSeries to the viewport
        /// </summary>
        /// <param name="lines">Collection of lines, which represent bounds of a polygon segment</param>
        protected IEnumerable<Point> ClipArea(IEnumerable<Tuple<Point, Point>> lines)
        {
            var linesArray = lines.ToArray();
            var polyline = ClipArea(linesArray.Select(line => line.Item2)).Concat(ClipArea(linesArray.Select(line => line.Item1)).Reverse());

            return polyline;
        }

        static Point GetStartPoint(Rect outerRect, Rect innerRect, AlignmentX alignX, AlignmentY alignY) {
            if(innerRect.Width > outerRect.Width || innerRect.Height > outerRect.Height)
                throw new ArgumentOutOfRangeException("innerRect");

            double x, y;

            switch(alignX) {
                case AlignmentX.Left:   x = outerRect.Left; break;
                case AlignmentX.Right:  x = outerRect.Right - innerRect.Width; break;
                default:                x = outerRect.Left + outerRect.Width / 2 - innerRect.Width / 2; break;
            }

            switch(alignY) {
                case AlignmentY.Top:    y = outerRect.Top; break;
                case AlignmentY.Bottom: y = outerRect.Bottom - innerRect.Height; break;
                default:                y = outerRect.Top + outerRect.Height / 2 - innerRect.Height / 2; break;
            }

            return new Point(x, y);
        }

        static Point GetStartPoint(Point basePoint, Rect rect, AlignmentX alignX, AlignmentY alignY) {
            double x, y;

            switch(alignX) {
                case AlignmentX.Left:   x = basePoint.X; break;
                case AlignmentX.Right:  x = basePoint.X - rect.Width; break;
                default:                x = basePoint.X - rect.Width / 2; break;
            }

            switch(alignY) {
                case AlignmentY.Top:    y = basePoint.Y; break;
                case AlignmentY.Bottom: y = basePoint.Y - rect.Height; break;
                default:                y = basePoint.Y - rect.Height / 2; break;
            }

            return new Point(x, y);
        }

        static readonly Dictionary<string, FontFamily> _fonts = new Dictionary<string, FontFamily>(); 
        static FontFamily GetFontByName(string fontName) {
            FontFamily font;
            if(_fonts.TryGetValue(fontName, out font))
                return font;

            font = new FontFamily(fontName);
            _fonts.Add(fontName, font);
            return font;
        }
    }
}
