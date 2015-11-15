using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Ecng.Xaml.Charting.Rendering.Common
{
    /// <summary>
    /// Defines a <see cref="IRenderContext2D"/> that does nothing, used to prevent charts drawing where the RenderSurface implemenation is unlicensed.
    /// </summary>
    public class NullRenderContext : IRenderContext2D
    {
        public RenderOperationLayers Layers
        {
            get { return new RenderOperationLayers(); }
        }

        public System.Windows.Size ViewportSize
        {
            get { return new System.Windows.Size(); }
        }

        public void SetPrimitvesCachingEnabled(bool bEnabled)
        {
           
        }

        public IBrush2D CreateBrush(System.Windows.Media.Color color, double opacity = 1, bool? alphaBlend = null)
        {
            return null;
        }

        public IBrush2D CreateBrush(System.Windows.Media.Brush brush, double opacity = 1, TextureMappingMode textureMappingMode = TextureMappingMode.PerScreen)
        {
            return null;
        }

        public IPen2D CreatePen(System.Windows.Media.Color color, bool antiAliasing, float strokeThickness, double opacity = 1.0, double[] strokeDashArray = null, System.Windows.Media.PenLineCap strokeEndLineCap = System.Windows.Media.PenLineCap.Round)
        {
            return null;
        }

        public ISprite2D CreateSprite(System.Windows.FrameworkElement fe)
        {
            return null;throw new NotImplementedException();
        }

        public void Clear()
        {
        }

        public void DrawSprite(ISprite2D srcSprite, System.Windows.Rect srcRect, System.Windows.Point destPoint)
        {
        }

        public void DrawSprites(ISprite2D sprite2D, System.Windows.Rect srcRect, IEnumerable<System.Windows.Point> points)
        {
        }

        public void DrawSprites(ISprite2D sprite2D, IEnumerable<System.Windows.Rect> dstRects)
        {
        }

        public void FillRectangle(IBrush2D brush, System.Windows.Point pt1, System.Windows.Point pt2, double gradientRotationAngle = 0)
        {
        }

        public void FillPolygon(IBrush2D brush, IEnumerable<System.Windows.Point> points)
        {
        }

        public void FillArea(IBrush2D brush, IEnumerable<Tuple<System.Windows.Point, System.Windows.Point>> lines, bool isVerticalChart = false, double gradientRotationAngle = 0)
        {
        }

        public void DrawQuad(IPen2D pen, System.Windows.Point pt1, System.Windows.Point pt2)
        {
        }

        public void DrawEllipse(IPen2D strokePen, IBrush2D fillBrush, System.Windows.Point center, double width, double height)
        {
        }

        public void DrawEllipses(IPen2D strokePen, IBrush2D fillBrush, IEnumerable<System.Windows.Point> centres, double width, double height)
        {
        }

        public void DrawLine(IPen2D pen, System.Windows.Point pt1, System.Windows.Point pt2)
        {
        }

        public void DrawLines(IPen2D pen, IEnumerable<System.Windows.Point> points)
        {
        }

        public void DisposeResourceAfterDraw(IDisposable disposable)
        {
        }

        public void DrawPixelsVertically(int x, int yStartBottom, int yEndTop, IList<int> pixelColorsArgb, double opacity, bool yAxisIsFlipped)
        {
        }

        public void TextDrawDimensions(string text, float fontSize, Color foreColor, out float width, out float height, string fontFamily = null, FontWeight fontWeight = new FontWeight()) {
            width = height = 0;
        }

        public Size DigitMaxSize(float fontSize, string fontFamily = null, FontWeight fontWeight = new FontWeight()) {
            return Size.Empty;
        }

        public void DrawText(string text, System.Windows.Rect dstBoundingRect, AlignmentX alignX, AlignmentY alignY, System.Windows.Media.Color foreColor, float fontSize, string fontFamily = null, FontWeight fontWeight = default(FontWeight)) {
        }

        public void DrawText(string text, Point basePoint, AlignmentX alignX, AlignmentY alignY, Color foreColor, float fontSize, string fontFamily = null, FontWeight fontWeight = new FontWeight()) {
        }

        public IPathDrawingContext BeginLine(IPen2D pen, double startX, double startY)
        {
            return null;
        }

        public IPathDrawingContext BeginPolygon(IBrush2D brush, double startX, double startY, double gradientRotationAngle = 0d)
        {
            return null;
        }

        public void Dispose()
        {
        }
    }
}
