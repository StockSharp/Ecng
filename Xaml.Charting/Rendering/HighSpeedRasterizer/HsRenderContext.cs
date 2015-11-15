// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// WriteableBitmapRenderContext.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Rendering.Common;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using Image = System.Windows.Controls.Image;

namespace Ecng.Xaml.Charting.Rendering.HighSpeedRasterizer
{
	internal class HsRenderContext : RenderContextBase
	{
		private BitmapContext _bitmapContext;
		private readonly Image _image;
		private readonly RenderOperationLayers _renderLayers = new RenderOperationLayers();
		private readonly WriteableBitmap _renderWriteableBitmap;
		private readonly List<IDisposable> _resourcesToDispose = new List<IDisposable>();
		private Size _viewportSize;

        TextureCache TextureCache {get {return (TextureCache)_textureCache;}}

		public HsRenderContext(Image image, WriteableBitmap renderWriteableBitmap, Size viewportSize,
											TextureCacheBase textureCache) : base(textureCache)
		{
			_image = image;
			_renderWriteableBitmap = renderWriteableBitmap;
			_viewportSize = viewportSize;
			_bitmapContext = _renderWriteableBitmap.GetBitmapContext();
		}        

		public override RenderOperationLayers Layers
		{
			get { return _renderLayers; }
		}

		public override Size ViewportSize
		{
			get { return _viewportSize; }
		}

		public override IBrush2D CreateBrush(Color seriesColor, double opacity = 1, bool? alphaBlend = null)
		{
			return new HsBrush(seriesColor, WriteableBitmapExtensions.ConvertColor(opacity, seriesColor),
											alphaBlend ?? (seriesColor.A * opacity) < 255);
		}

		public override IBrush2D CreateBrush(Brush brush, double opacity = 1d, TextureMappingMode textureMappingMode = TextureMappingMode.PerScreen)
		{
            if (brush == null)
                return CreateBrush(Color.FromArgb(0x00, 0x00, 0x00, 0x00));

			if (brush is SolidColorBrush)
				return CreateBrush(((SolidColorBrush) brush).Color, opacity);
			else
				return new TextureBrush(brush, textureMappingMode, TextureCache);
		}

		public override IPen2D CreatePen(Color seriesColor, bool antiAliasing, float strokeThickness, double opacity = 1, double[] strokeDashArray = null, PenLineCap strokeEndLineCap = PenLineCap.Round)
		{
			return new HsPen(
				seriesColor,
				WriteableBitmapExtensions.ConvertColor(opacity, seriesColor),
				strokeThickness,
				strokeEndLineCap,
				antiAliasing,
				opacity,
				strokeDashArray);
		}

		public override ISprite2D CreateSprite(FrameworkElement fe)
		{
			return new HsSprite2D(TextureCache.GetWriteableBitmapTexture(fe));
		}

		public override void Clear()
		{
			_bitmapContext.Clear();
		}

		public override void DrawSprite(ISprite2D sprite2D, Rect srcRect, Point destPoint)
		{
			_bitmapContext.WriteableBitmap.Blit(new Rect(destPoint.X, destPoint.Y, sprite2D.Width, sprite2D.Height),
												(sprite2D as HsSprite2D).WriteableBitmap,
												srcRect);
		}

		public override void DrawSprites(ISprite2D sprite2D, Rect srcRect, IEnumerable<Point> points)
		{
			var destRect = new Rect(0, 0, sprite2D.Width, sprite2D.Height);
			WriteableBitmap bmp = (sprite2D as HsSprite2D).WriteableBitmap;
			foreach (Point pt in points)
			{
				destRect.X = pt.X;
				destRect.Y = pt.Y;
				_bitmapContext.WriteableBitmap.Blit(destRect,
													bmp,
													srcRect);
			}
		}

		public override void DrawSprites(ISprite2D sprite2D, IEnumerable<Rect> dstRects)
		{
			WriteableBitmap bmp = (sprite2D as HsSprite2D).WriteableBitmap;
			foreach (Rect dstRect in dstRects)
			{
				_bitmapContext.WriteableBitmap.Blit(dstRect,
													bmp,
													new Rect(0, 0, sprite2D.Width, sprite2D.Height));
			}
		}

		public override void FillRectangle(IBrush2D brush, Point pt1, Point pt2, double gradientRotationAngle = 0)
		{
			// is it outside of viewport?
			if (pt1.X < 0 && pt2.X < 0) return;
			if (pt1.Y < 0 && pt2.Y < 0) return;
			if (pt1.Y > ViewportSize.Height && pt2.Y > ViewportSize.Height) return;
			if (pt1.X > ViewportSize.Width && pt2.X > ViewportSize.Width) return;

			ClipRectangle(ref pt1, ref pt2);

			var primitiveRect = new Rect(pt1, pt2);

			if (brush is TextureBrush)
			{
				var textureBrush = (TextureBrush) brush;
				int[] texture = textureBrush.GetIntTexture(ViewportSize);
				_bitmapContext.WriteableBitmap.FillRectangle((int) primitiveRect.Left, (int) primitiveRect.Top,
															 (int) primitiveRect.Right, (int) primitiveRect.Bottom,
															 (x, y) =>
																 {
																	 int offset =
																		 textureBrush.GetIntOffsetConsideringMappingMode
																			 (x, y, primitiveRect, gradientRotationAngle);
																	 return texture[offset];
																 },
															 brush.AlphaBlend
																 ? WriteableBitmapExtensions.BlendMode.Alpha
																 : WriteableBitmapExtensions.BlendMode.None);
			}
			else
				_bitmapContext.WriteableBitmap.FillRectangle((int) primitiveRect.Left, (int) primitiveRect.Top,
															 (int) primitiveRect.Right, (int) primitiveRect.Bottom,
															 brush.ColorCode,
															 brush.AlphaBlend
																 ? WriteableBitmapExtensions.BlendMode.Alpha
																 : WriteableBitmapExtensions.BlendMode.None);
		}

	    public override void FillArea(IBrush2D brush, IEnumerable<Tuple<Point, Point>> lines, bool isVerticalChart = false, double gradientRotationAngle = 0)
		{
			foreach (var areaSegments in lines.SplitMultilineByGaps())
			{
				var  clippedAreaPoints = ClipArea(areaSegments).ToArray();
				var renderCoords = new int[clippedAreaPoints.Length*2 + 2];

				int i = 0, j = 0;
				for (; i < clippedAreaPoints.Length; i++)
				{
					renderCoords[j++] = (int)clippedAreaPoints[i].X;
					// -1 for fixing http://ulcsoftware.myjetbrains.com/youtrack/issue/SC-907
					renderCoords[j++] = (int)clippedAreaPoints[i].Y - 1;
				}

				renderCoords[j++] = (int) clippedAreaPoints[0].X;
				renderCoords[j] = (int) clippedAreaPoints[0].Y - 1;

				if (brush is TextureBrush)
				{
					var textureBrush = (TextureBrush) brush;
					int[] texture = textureBrush.GetIntTexture(ViewportSize);
					_bitmapContext.WriteableBitmap.FillPolygon(renderCoords,
															   (x, y) =>
																   {
																	   var offset =
																		   textureBrush
																			   .GetIntOffsetNotConsideringMappingMode(
																				   x, y, gradientRotationAngle);
																	   return texture[offset];
																   },
															   brush.AlphaBlend
																   ? WriteableBitmapExtensions.BlendMode.Alpha
																   : WriteableBitmapExtensions.BlendMode.None);
				}
				else
					_bitmapContext.WriteableBitmap.FillPolygon(renderCoords,
															   brush.ColorCode,
															   brush.AlphaBlend
																   ? WriteableBitmapExtensions.BlendMode.Alpha
																   : WriteableBitmapExtensions.BlendMode.None);
			}
		}

		public override void DisposeResourceAfterDraw(IDisposable disposable)
		{
			if (disposable != null)
				_resourcesToDispose.Add(disposable);
		}

		public void Blit(Rect destRect, WriteableBitmap srcImage, Rect srcRect)
		{
			_bitmapContext.WriteableBitmap.Blit(destRect, srcImage, srcRect);
		}

		// todo: is it same as        public void FillRectangle(IBrush2D brush, Point pt1, Point pt2)   ?
		// if so, it is confusing, need to remove one copy
		public void FillRectangle(IBrush2D fillBrush, int x1, int y1, int x2, int y2)
		{
			_bitmapContext.WriteableBitmap.FillRectangle(x1, y1, x2, y2, fillBrush.ColorCode, fillBrush.AlphaBlend ? WriteableBitmapExtensions.BlendMode.Alpha : WriteableBitmapExtensions.BlendMode.None);
		}        

		public override void DrawEllipse(IPen2D strokePen, IBrush2D fillBrush, Point center, double width, double height)
		{
			if (!IsInBounds(center)) return;

			if (width <= 1.0 && height <= 1.0)
			{
				WriteableBitmapExtensions.DrawPixel(_bitmapContext, (int) ViewportSize.Width, (int) ViewportSize.Height, (int) center.X, (int) center.Y, fillBrush.ColorCode);
				return;
			}

			if (fillBrush != null && !fillBrush.IsTransparent)
			{
				WriteableBitmapExtensions.FillEllipseCentered(_bitmapContext, (int)center.X, (int)center.Y, (int)width / 2,
																   (int)height / 2, fillBrush.ColorCode);
			}

			if (strokePen != null && !strokePen.IsTransparent)
			{
				WriteableBitmapExtensions.DrawEllipseCentered(_bitmapContext, (int)center.X, (int)center.Y, (int)width / 2,
																   (int)height / 2, strokePen.ColorCode,
																   (int)strokePen.StrokeThickness);
			}
		}

		public override void DrawEllipses(IPen2D strokePen, IBrush2D fillBrush, IEnumerable<Point> centres, double width, double height)
		{
			foreach (var pt in centres)
			{
				DrawEllipse(strokePen, fillBrush, pt, width, height);
			}
		}

		public override void DrawPixelsVertically(int x, int yStartBottom, int yEndTop, IList<int> pixelColorsArgb, double opacity, bool yAxisIsFlipped)
		{
			_bitmapContext.WriteableBitmap.DrawPixelsVertically(x, yStartBottom, yEndTop, pixelColorsArgb, opacity, yAxisIsFlipped);
		}
			 
		public override void Dispose()
		{
			_bitmapContext.Dispose();
			if (_image != null && !ReferenceEquals(_image.Source, _renderWriteableBitmap))
			{
				_image.Source = _renderWriteableBitmap;
			}

			foreach (IDisposable disposable in _resourcesToDispose)
			{
				disposable.Dispose();
			}            
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
		public sealed override IPathDrawingContext BeginLine(IPen2D pen, double startX, double startY)
		{
			return new WbxLineDrawingContext((HsPen)pen, this, startX, startY);
		}

		/// <summary>
		/// Begins a filled Polygon segment, returning the <see cref="IFillDrawingContext" />. This is the fastest way to draw polygon and simply a proxy to <see cref="FillArea" /> method.
		/// </summary>
		/// <param name="brush">The brush for the polygon fill</param>
		/// <param name="startX">The start X coordinate (pixel coord)</param>
		/// <param name="startY">The start Y coordinate (pixel coord)</param>
        /// /// <param name="gradientRotationAngle">The angle which the <param name="brush"></param> is rotated by</param>
		/// <returns>
		/// The <see cref="IPathDrawingContext" /> to continue the polygon
		/// </returns>
	    public sealed override IPathDrawingContext BeginPolygon(IBrush2D brush, double startX, double startY,
	        double gradientRotationAngle = 0d)
	    {
            return new WbxPolygonDrawingContext(brush, this, startX, startY){GradientRotationAngle = gradientRotationAngle};
	    }			

		#region WbxPolygonDrawingContext Nested Class
        internal sealed class WbxPolygonDrawingContext : IPathDrawingContext
		{
			private IBrush2D _brush;
			private readonly HsRenderContext _renderContext;
			private List<int> _points = new List<int>();

			public WbxPolygonDrawingContext(IBrush2D brush, HsRenderContext renderContext, double startX, double startY)
			{				
				_renderContext = renderContext;

			    Begin(brush, startX, startY);
			}

			public double GradientRotationAngle { get; set; }

            public IPathDrawingContext Begin(IPathColor fill, double x, double y)
            {
                _brush = (IBrush2D)fill;
                _points.Add((int)x.ClipToInt());
                _points.Add((int)y.ClipToInt());
                return this;
            }

            public IPathDrawingContext MoveTo(double x, double y)
			{
				_points.Add((int)x.ClipToInt());
				_points.Add((int)y.ClipToInt());
			    return this;
			}

            public void End()
            {
                var blendMode = _brush.AlphaBlend
                    ? WriteableBitmapExtensions.BlendMode.Alpha
                    : WriteableBitmapExtensions.BlendMode.None;

                var textureBrush = _brush as TextureBrush;
                if (textureBrush != null)
                {
                    int[] texture = textureBrush.GetIntTexture(_renderContext.ViewportSize);
                    _renderContext._bitmapContext.WriteableBitmap.FillPolygon(_points.ToArray(),
                        (x, y) =>
                        {
                            int offset = textureBrush.GetIntOffsetNotConsideringMappingMode(x, y, GradientRotationAngle);
                            return texture[offset];
                        },
                        blendMode);
                }
                else
                {
                    _renderContext._bitmapContext.WriteableBitmap.FillPolygon(
                        _points.ToArray(),
                        _brush.ColorCode,
                        blendMode);
                }
            }

            void IDisposable.Dispose()
            {
                End();
            }
		}
		#endregion

		#region WbxLineDrawingContext Nested Class
		private sealed class WbxLineDrawingContext : IPathDrawingContext
		{
			private readonly HsRenderContext _context;
			private HsPen _pen;
			private double _lastX;
			private double _lastY;
			private readonly BitmapContext _bitmapContext;
			private Size _viewportSize;

            private int _previousLineEndX = -1; // is used to solve problem of overlapping lines
            private int _previousLineEndY = -1;

			public WbxLineDrawingContext(HsPen pen, HsRenderContext context, double x, double y)
			{
				_context = context;            				
				_bitmapContext = _context._bitmapContext;
				_viewportSize = _context.ViewportSize;

			    this.Begin(pen, x, y);
			}

		    public IPathDrawingContext Begin(IPathColor pen, double x, double y)
		    {
                _pen = (HsPen)pen;    
                _lastX = x;
                _lastY = y;

		        return this;
		    }

			public IPathDrawingContext MoveTo(double x, double y)
			{
				if (!_pen.HasDashes)
				{
					// Non dashed implemented, just draw the line
					LineToImplementation(x, y);
					_lastX = x;
					_lastY = y;
					return this;
				}

				// Dashed implementation, dashes have been implemented per-segment since Ultrachart 2.x
				var dashStart = new Point(_lastX, _lastY);
				var dashEnd = new Point(x, y);

				// Clip the start/end to the viewport, required before using DashSplitter
			    RenderContextBase.ClipLine(ref dashStart, ref dashEnd, _viewportSize);

				var ds = _context.DashSplitter;
				ds.Reset(dashStart, dashEnd, _viewportSize, _pen);

				while (ds.MoveNext())
				{
					var segment = ds.Current;

                    // Trick the LineDrawingContext					
                    this.End(); // End the line
                    this.Begin(_pen, segment.X1, segment.Y1); // Restart the line

					// Perform the dash line draw
					LineToImplementation(segment.X2, segment.Y2);
				}

				_lastX = x;
				_lastY = y;
				return this;
			}

			private void LineToImplementation(double x, double y)
			{
				if (_pen.StrokeThickness > 1.0f)
				{
					// Thick line implementation
					WriteableBitmapExtensions.DrawPennedLine(
						_bitmapContext,
                        (int)_viewportSize.Width,
                        (int)_viewportSize.Height,
                        (int)_lastX.ClipToInt(),
                        (int)_lastY.ClipToInt(),
                        (int)x.ClipToInt(),
                        (int)y.ClipToInt(),
						_pen.Pen);
				}
				else if (_pen.Antialiased)
				{
					// Thin anti aliased line implementation
					var pt1X = (int)_lastX.ClipToInt();
                    var pt1Y = (int)_lastY.ClipToInt();
                    var pt2X = (int)x.ClipToInt();
                    var pt2Y = (int)y.ClipToInt();
                    var skipFirstPixel = pt1X == _previousLineEndX && pt1Y == _previousLineEndY;
					WriteableBitmapExtensions.DrawLineAa(
						_bitmapContext,
						(int)_viewportSize.Width,
						(int)_viewportSize.Height,
						pt1X,
						pt1Y,
						pt2X,
						pt2Y,
						_pen.ColorCode, skipFirstPixel);
                    _previousLineEndX = pt2X;
                    _previousLineEndY = pt2Y;
				}
				else
				{
					// Thin, no AA implementation
					WriteableBitmapExtensions.DrawLineBresenham(
						_bitmapContext,
						(int)_viewportSize.Width,
						(int)_viewportSize.Height,
                        (int)_lastX.ClipToInt(),
                        (int)_lastY.ClipToInt(),
                        (int)x.ClipToInt(),
                        (int)y.ClipToInt(),
						_pen.ColorCode);
				}

				_lastX = x;
				_lastY = y;
			}

			public void End()
			{
			}

            void IDisposable.Dispose()
            {
                this.End();
            }
		}
		#endregion
	}    
}