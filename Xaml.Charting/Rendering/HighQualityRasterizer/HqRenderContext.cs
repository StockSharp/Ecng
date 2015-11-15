// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// AggSharpRenderContext.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Rendering.Common;
using MatterHackers.Agg;
using MatterHackers.Agg.Image;
using MatterHackers.Agg.VertexSource;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using Image = System.Windows.Controls.Image;
using Point = System.Windows.Point;

namespace Ecng.Xaml.Charting.Rendering.HighQualityRasterizer
{
	internal class HqRenderContext : RenderContextBase
	{
		private readonly WriteableBitmap _bmp;
		private readonly uint[] _emptyStrideRow;
		protected readonly Graphics2D _graphics2D;
		private readonly Image _image;
		private readonly ImageBuffer _imageBuffer;
		private readonly RenderOperationLayers _renderLayers = new RenderOperationLayers();
		private readonly List<IDisposable> _resourcesToDispose = new List<IDisposable>();
		protected readonly Size _viewportSize;

        TextureCache TextureCache {get {return (TextureCache)_textureCache;}}

		public HqRenderContext(Image image, WriteableBitmap bmp, uint[] emptyStrideRow, ImageBuffer imageBuffer,
									 Graphics2D graphics2D, TextureCacheBase textureCache) : base(textureCache)
		{
			if (textureCache == null) throw new ArgumentNullException();
			_viewportSize = new Size(bmp.PixelWidth, bmp.PixelHeight);
			_image = image;
			_bmp = bmp;
			_emptyStrideRow = emptyStrideRow;
			_imageBuffer = imageBuffer;
			_graphics2D = graphics2D;

			_graphics2D.Rasterizer.reset();
		}

		public override RenderOperationLayers Layers
		{
			get { return _renderLayers; }
		}

		public override Size ViewportSize
		{
			get { return _viewportSize; }
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public
#if !SILVERLIGHT
		unsafe  
#endif
		override void Dispose()
		{
			if (_viewportSize.Width == 0 || _viewportSize.Height == 0) return;

#if !SILVERLIGHT
			_bmp.Lock();
			fixed (byte* src = &_imageBuffer.GetBuffer()[0])
			{
				var dest = (byte*) _bmp.BackBuffer.ToPointer();
				NativeMethods.CopyUnmanagedMemory(src, 0, dest, 0, (int) (_viewportSize.Width*_viewportSize.Height*4));
			}
			_bmp.AddDirtyRect(new Int32Rect(0, 0, _bmp.PixelWidth, _bmp.PixelHeight));
			_bmp.Unlock();
#else
			Buffer.BlockCopy(_imageBuffer.GetBuffer(), 0, _bmp.Pixels, 0, (int)(_viewportSize.Width * _viewportSize.Height * 4));
			_bmp.Invalidate();
#endif
			if (!ReferenceEquals(_bmp, _image.Source))
				_image.Source = _bmp;

			foreach (IDisposable disposable in _resourcesToDispose)
			{
				disposable.Dispose();
			}
		}

		public override IBrush2D CreateBrush(Color color, double opacity = 1d, bool? alphaBlend = null)
		{
			return new HqBrush(color, true, opacity);
		}

		public override IBrush2D CreateBrush(Brush brush, double opacity = 1d, TextureMappingMode mappingMode = TextureMappingMode.PerScreen)
		{
            if (brush == null)
                return CreateBrush(Color.FromArgb(0x00, 0x00, 0x00, 0x00));

			if (brush is SolidColorBrush)
				return CreateBrush(((SolidColorBrush) brush).Color, opacity, true);
			else
				return new TextureBrush(brush, mappingMode, TextureCache);
		}

		public override IPen2D CreatePen(Color color, bool antiAliasing, float strokeThickness, double opacity, double[] strokeDashArray = null, PenLineCap strokeEndLineCap = PenLineCap.Round)
		{
			return new HqPen(color, strokeThickness, strokeEndLineCap, antiAliasing, opacity, strokeDashArray);
		}

		public override ISprite2D CreateSprite(FrameworkElement fe)
		{
			return new HqSprite2D(TextureCache.GetWriteableBitmapTexture(fe));
		}

		public override void Clear()
		{
			for (int i = 0; i < _viewportSize.Height; i++)
			{
				Buffer.BlockCopy(_emptyStrideRow, 0, _imageBuffer.GetBuffer(), (int) (4*i*_viewportSize.Width),
								 4*_emptyStrideRow.Length);
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

            _graphics2D.Rasterizer.reset();

            var rect = new Rect(new Point(pt1.X, pt1.Y), new Point(pt2.X, pt2.Y));
            var e = new RoundedRect(rect.Left, rect.Bottom, rect.Right, rect.Top, 0.0);
            _graphics2D.Rasterizer.add_path(e);

            if (brush is TextureBrush)
            {
                var primitiveRect = new Rect(pt1, pt2);
                var textureBrush = (TextureBrush)brush;
                byte[] textureArray = textureBrush.GetByteTexture(ViewportSize);
                new ScanlineRenderer().render_scanlines_aa_solid(_graphics2D.DestImage, _graphics2D.Rasterizer,
                                                                 _graphics2D.ScanlineCache,
                                                                 (x, y) =>
                                                                 {
                                                                     // get custom color from texture brush by X and Y
                                                                     int offset = textureBrush.GetByteOffsetConsideringMappingMode(x, y, primitiveRect, gradientRotationAngle);
                                                                     return new RGBA_Bytes(
                                                                         textureArray[offset + 2],
                                                                         textureArray[offset + 1],
                                                                         textureArray[offset + 0],
                                                                         textureArray[offset + 3]);
                                                                 });
            }
            else
                new ScanlineRenderer().render_scanlines_aa_solid(_graphics2D.DestImage, _graphics2D.Rasterizer,
                                                                 _graphics2D.ScanlineCache, ToRgbaBytes(brush.Color));
		}

	    public override void FillArea(IBrush2D brush, IEnumerable<Tuple<Point, Point>> lines, bool isVerticalChart = false, double gradientRotationAngle = 0)
		{
			foreach (var areaSegment in lines.SplitMultilineByGaps())
			{
				var pathStorage = new PathStorage();
				_graphics2D.Rasterizer.reset();

				var clippedAreaPoints = ClipArea(areaSegment).ToArray();
				if (clippedAreaPoints.Length < 2)
					return;

				pathStorage.MoveTo(clippedAreaPoints[0].X, clippedAreaPoints[0].Y);
				for (int i = 1; i < clippedAreaPoints.Length; i++)
				{
					pathStorage.LineTo(clippedAreaPoints[i].X, clippedAreaPoints[i].Y);
				}

				_graphics2D.Rasterizer.add_path(pathStorage);
				_graphics2D.Rasterizer.close_polygon();

				if (brush is TextureBrush)
				{
					var textureBrush = (TextureBrush) brush;
					byte[] textureArray = textureBrush.GetByteTexture(ViewportSize);
					new ScanlineRenderer().render_scanlines_aa_solid(_graphics2D.DestImage, _graphics2D.Rasterizer,
						_graphics2D.ScanlineCache,
						(x, y) =>
						{
							// get custom color from texture brush by X and Y
							var offset =
								textureBrush.
									GetByteOffsetNotConsideringMappingMode
									(x, y, gradientRotationAngle);
							return
								new RGBA_Bytes(
									textureArray[offset + 2],
									textureArray[offset + 1],
									textureArray[offset + 0],
									textureArray[offset + 3]);
						});
				}
				else
					new ScanlineRenderer().render_scanlines_aa_solid(_graphics2D.DestImage, _graphics2D.Rasterizer,
						_graphics2D.ScanlineCache,
						ToRgbaBytes(brush.Color));
			}
		}

		public void FillTriangle(Point point, Point point1, Point point2, Color color)
		{
			var ps = new PathStorage();
			ps.MoveTo(point.X, point.Y);
			ps.LineTo(point1.X, point1.Y);
			ps.LineTo(point2.X, point2.Y);
			ps.ClosePolygon();

			_graphics2D.Rasterizer.add_path(ps);
			new ScanlineRenderer().render_scanlines_aa_solid(_graphics2D.DestImage, _graphics2D.Rasterizer,
															 _graphics2D.ScanlineCache, ToRgbaBytes(color));
		}
		public void DrawTriangle(Point point, Point point1, Point point2, float thickness, Color color)
		{
			double w = thickness;
			var lineProfile = new LineProfileAnitAlias(w, new gamma_none());
			var outlineRenderer = new OutlineRenderer(_graphics2D.DestImage, lineProfile);
			var rasterizer = new rasterizer_outline_aa(outlineRenderer);

			rasterizer.line_join(w > 2.0f
									 ? rasterizer_outline_aa.outline_aa_join_e.outline_round_join
									 : rasterizer_outline_aa.outline_aa_join_e.outline_no_join);

			rasterizer.round_cap(w > 2.0f);

			var ps = new PathStorage();

			ps.MoveTo(point.X, point.Y);
			ps.LineTo(point1.X, point1.Y);
			ps.LineTo(point2.X, point2.Y);
			ps.LineTo(point.X, point.Y);

			var colors = new[] {ToRgbaBytes(color)};
			var pathIndex = new[] {0};
			rasterizer.RenderAllPaths(ps, colors, pathIndex, 1);
		}

		public override void DisposeResourceAfterDraw(IDisposable disposable)
		{
			if (disposable != null)
			{
				_resourcesToDispose.Add(disposable);
			}
		}

		private RGBA_Bytes ToRgbaBytes(Color color)
		{
			return new RGBA_Bytes(color.R, color.G, color.B, color.A);
		}

		private int CoerceValues(ref int coord, ref double measure)
		{
			int offset = 0;

			if (coord < 0 && coord + measure > 0)
			{
				offset = -coord;

				measure -= offset;
				coord += offset;
			}

			return offset;
		}

		public override void DrawSprite(ISprite2D sprite, Rect srcRect, Point destPoint)
		{
			var sourceImage = sprite as HqSprite2D;
			if (sourceImage == null)
				throw new ArgumentException(string.Format("Input Sprite must be of type {0}", typeof (HqSprite2D)));

			var destImage = ((ImageProxy) _graphics2D.DestImage).LinkedImage as ImageBuffer;
			var sourceBlender = //sourceImage.GetBlender(); 
				new BlenderBGRA();
			var destBlender = new BlenderPreMultBGRA();

			byte[] sourceBuffer = sourceImage.GetBuffer();
			byte[] destBuffer = destImage.GetBuffer();

			var yCoord = (int) destPoint.Y;
			var xCoord = (int) destPoint.X;

			double srcWidth = sourceImage.Width;
			double srcHeight = sourceImage.Height;

			int xOffset = CoerceValues(ref xCoord, ref srcWidth);
			int yOffset = CoerceValues(ref yCoord, ref srcHeight);

			// don't draw if point is out of viewport
			if (yCoord < 0 || xCoord < 0)
			{
				return;
			}

			for (int j = 0; j < srcHeight && j + yCoord < destImage.Height && xCoord < destImage.Width; j++)
			{
				int sourceOffset = sourceImage.GetBufferOffsetXY(xOffset, j + yOffset);
				int destOffset = destImage.GetBufferOffsetXY(xCoord, j + yCoord);

				for (int i = 0; i < srcWidth && i + xCoord < destImage.Width; i++)
				{
					byte alpha = sourceBuffer[sourceOffset + ImageBuffer.OrderA];
					if (alpha == 0)
					{
						sourceOffset += 4;
						destOffset += 4;
						continue;
					}

					if (alpha == 255)
					{
						destBuffer[destOffset++] = sourceBuffer[sourceOffset++];
						destBuffer[destOffset++] = sourceBuffer[sourceOffset++];
						destBuffer[destOffset++] = sourceBuffer[sourceOffset++];
						destBuffer[destOffset++] = sourceBuffer[sourceOffset++];
						continue;
					}

					RGBA_Bytes sourceColor = sourceBlender.PixelToColorRGBA_Bytes(sourceBuffer, sourceOffset);

					// Equivalent for calling destImage.BlendPixel(xCoord + i, j + yCoord, sourceColor, 1), however uses 
					// PremultBGRA blender as destination and BGRA blender as source to get rid of dodgy discoloured edges 
					int bufferOffset;
					byte[] buffer = destImage.GetPixelPointerXY(xCoord + i, j + yCoord, out bufferOffset);
					destBlender.BlendPixel(buffer, bufferOffset, sourceColor);

					sourceOffset += 4;
					destOffset += 4;
				}
			}
		}

		public void DrawSprite(Rect destRect, ISprite2D sprite)
		{
			var sourceImage = sprite as HqSprite2D;
			if (sourceImage == null)
				throw new ArgumentException(string.Format("Input Sprite must be of type {0}", typeof (HqSprite2D)));

			var destImage = ((ImageProxy) _graphics2D.DestImage).LinkedImage as ImageBuffer;
			var sourceBlender = //sourceImage.GetBlender(); 
				new BlenderBGRA();
			var destBlender = new BlenderPreMultBGRA();

			byte[] sourceBuffer = sourceImage.GetBuffer();
			byte[] destBuffer = destImage.GetBuffer();
			double srcWidth = sourceImage.Width;
			double srcHeight = sourceImage.Height;
			var dstLeft = (int) destRect.Left;
			var dstTop = (int) destRect.Top;
			var dstWidth = (int) destRect.Width;
			var dstHeight = (int) destRect.Height;

			for (int dstXIndex = 0; dstXIndex < dstWidth; dstXIndex++)
				for (int dstYIndex = 0; dstYIndex < dstHeight; dstYIndex++)
				{
					if (dstXIndex + dstLeft < 0) continue;
					if (dstXIndex + dstLeft >= destImage.Width) continue;
					if (dstYIndex + dstTop < 0) continue;
					if (dstYIndex + dstTop >= destImage.Height) continue;

					var srcX = (int) ((double) dstXIndex/dstWidth*srcWidth);
					var srcY = (int) ((double) dstYIndex/dstHeight*srcHeight);

					int sourceOffset = sourceImage.GetBufferOffsetXY(srcX, srcY);

					byte alpha = sourceBuffer[sourceOffset + ImageBuffer.OrderA];
					if (alpha == 0)
						continue;
					if (alpha == 255)
					{
						int destOffset = destImage.GetBufferOffsetXY(dstXIndex + dstLeft, dstYIndex + dstTop);
						destBuffer[destOffset++] = sourceBuffer[sourceOffset++];
						destBuffer[destOffset++] = sourceBuffer[sourceOffset++];
						destBuffer[destOffset++] = sourceBuffer[sourceOffset++];
						destBuffer[destOffset++] = sourceBuffer[sourceOffset++];
						continue;
					}

					RGBA_Bytes sourceColor = sourceBlender.PixelToColorRGBA_Bytes(sourceBuffer, sourceOffset);

					// Equivalent for calling destImage.BlendPixel(xCoord + i, j + yCoord, sourceColor, 1), however uses 
					// PremultBGRA blender as destination and BGRA blender as source to get rid of dodgy discoloured edges 
					int bufferOffset;
					byte[] buffer = destImage.GetPixelPointerXY(dstXIndex + dstLeft, dstYIndex + dstTop,
																out bufferOffset);
					destBlender.BlendPixel(buffer, bufferOffset, sourceColor);
				}
		}

		public override void DrawSprites(ISprite2D sprite, Rect srcRect, IEnumerable<Point> points)
		{
			foreach (Point pt in points)
			{
				DrawSprite(sprite, srcRect, pt);
			}
		}

		public override void DrawSprites(ISprite2D sprite2D, IEnumerable<Rect> dstRects)
		{
			foreach (Rect dstRect in dstRects)
			{
				DrawSprite(dstRect, sprite2D);
			}
		}

		public override void DrawEllipse(IPen2D strokePen, IBrush2D fillBrush, Point center, double width, double height)
		{
			if (!IsInBounds(center)) return;

			lock (_syncRoot) // todo it is not good to lock entire procedure. - no sense in MT
			{
				_graphics2D.Rasterizer.reset(); // without this aggsharp renderer draws strange big triangles
				var e = new Ellipse(center.X, center.Y, width/2, height/2);
				_graphics2D.Rasterizer.add_path(e);
				new ScanlineRenderer().render_scanlines_aa_solid(_graphics2D.DestImage, _graphics2D.Rasterizer,
																 _graphics2D.ScanlineCache, ToRgbaBytes(fillBrush.Color));


				var stroke = new Stroke(e);
				stroke.line_join(LineJoin.Round);
				stroke.inner_join(InnerJoin.Round);
				stroke.line_cap(LineCap.Butt);
				stroke.width(strokePen.StrokeThickness);
				_graphics2D.Rasterizer.add_path(stroke);
				new ScanlineRenderer().render_scanlines_aa_solid(_graphics2D.DestImage, _graphics2D.Rasterizer,
																 _graphics2D.ScanlineCache, ToRgbaBytes(strokePen.Color));


				_graphics2D.Rasterizer.reset(); // without this aggsharp renderer draws strange big triangles
			}
		}
		object _syncRoot = new object();

		public override void DrawEllipses(IPen2D strokePen, IBrush2D fillBrush, IEnumerable<Point> centres, double width, double height)
		{
			foreach (var center in centres)
			{
				DrawEllipse(strokePen, fillBrush, center, width, height);
			}
		}

		public override void DrawPixelsVertically(int x0, int yStartBottom, int yEndTop, IList<int> pixelColorsArgb, double opacity, bool yAxisIsFlipped)
		{
			var t = Math.Max(yStartBottom, yEndTop);
			yEndTop = Math.Min(yStartBottom, yEndTop);
			yStartBottom = t;

			var destImage = ((ImageProxy)_graphics2D.DestImage).LinkedImage as ImageBuffer;
			byte[] destBuffer = destImage.GetBuffer();
			
			var h = destImage.Height;
			if (yStartBottom == yEndTop) return;
			byte opacityA = (byte)(opacity * 256);
			int yStartBottomLimited = Math.Min(yStartBottom, h);

			for (int y = yStartBottomLimited; y >= yEndTop && y >= 0; y--)
			{
				if (y < 0 || y >= h) continue;
				int pixelIndex = (yStartBottom - y) * pixelColorsArgb.Count / (yStartBottom - yEndTop);
				if (yAxisIsFlipped)
					pixelIndex = pixelColorsArgb.Count - 1 - pixelIndex;


				if (pixelIndex >= 0 && pixelIndex < pixelColorsArgb.Count)
				{
					var rgbaColor = pixelColorsArgb[pixelIndex];
					int destOffset = destImage.GetBufferOffsetXY(x0, y);
                    byte aA = (byte)(((rgbaColor & 0xFF000000) >> 24) * opacity);

                    if (aA < 0xFF)
					{
						byte rA = (byte)((rgbaColor & 0x00FF0000) >> 16);
						byte gA = (byte)((rgbaColor & 0x0000FF00) >> 8);
						byte bA = (byte)((rgbaColor & 0x000000FF));
						byte rB = destBuffer[destOffset + ImageBuffer.OrderR];
						byte gB = destBuffer[destOffset + ImageBuffer.OrderG];
						byte bB = destBuffer[destOffset + ImageBuffer.OrderB];
						byte aB = destBuffer[destOffset + ImageBuffer.OrderA];


						int rOut = (rA*aA/255) + (rB*aB*(255 - aA)/(255*255));
						int gOut = (gA*aA/255) + (gB*aB*(255 - aA)/(255*255));
						int bOut = (bA*aA/255) + (bB*aB*(255 - aA)/(255*255));
						int aOut = aA + (aB*(255 - aA)/255);

						destBuffer[destOffset + ImageBuffer.OrderR] = (byte) rOut;
						destBuffer[destOffset + ImageBuffer.OrderG] = (byte) gOut;
						destBuffer[destOffset + ImageBuffer.OrderB] = (byte) bOut;
						destBuffer[destOffset + ImageBuffer.OrderA] = (byte) aOut;
					}
					else
					{
						destBuffer[destOffset++] = (byte) (rgbaColor & 0x0000FF);
						destBuffer[destOffset++] = (byte) ((rgbaColor & 0x00FF00) >> 8);
						destBuffer[destOffset++] = (byte) ((rgbaColor & 0xFF0000) >> 16);
						destBuffer[destOffset] = (byte) ((rgbaColor & 0xFF000000) >> 24);
						
					}
				}
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
			return new HqLineDrawingContext((HqPen)pen, this, startX, startY);
		}

        /// <summary>
        /// Begins a filled Polygon segment, returning the <see cref="IPathDrawingContext" />. This is the fastest way to draw polygon and simply a proxy to <see cref="FillArea" /> method.
        /// </summary>
        /// <param name="brush">The brush for the polygon fill</param>
        /// <param name="startX">The start X coordinate (pixel coord)</param>
        /// <param name="startY">The start Y coordinate (pixel coord)</param>
        /// <returns>
        /// The <see cref="IPathDrawingContext" /> to continue the polygon
        /// </returns>
        public sealed override IPathDrawingContext BeginPolygon(IBrush2D brush, double startX, double startY, double gradientRotationAngle = 0)
        {
            return new HqFillDrawingContext(brush, this, startX, startY) {GradientRotationAngle = gradientRotationAngle};
        }

        #region HqLineDrawingContext Nested Class        
        internal sealed class HqLineDrawingContext : IPathDrawingContext
		{
			private readonly HqRenderContext _context;
			private HqPen _pen;
			private double _lastX;
			private double _lastY;
			private Size _viewportSize;
			private readonly ImageBuffer _destImage;
			private bool _useAggMethod;
			private PathStorage _ps;
			private rasterizer_outline_aa _rasterizer;
            private Stroke _pg;

            public HqLineDrawingContext(HqPen pen, HqRenderContext context, double x, double y)
			{
				_context = context;
				

				_viewportSize = _context.ViewportSize;                
				_destImage = ((ImageProxy)_context._graphics2D.DestImage).LinkedImage as ImageBuffer;

				this.Begin(pen, x, y);
			}

            public IPathDrawingContext Begin(IPathColor pen, double x, double y)
            {
                _pen = (HqPen)pen;
                _useAggMethod = _pen.Antialiased || _pen.StrokeThickness > 1.0f;

				_lastX = x;
				_lastY = y;                

				if (_useAggMethod)
				{
                    _ps = new PathStorage();
                    
				    if (_pen.HasDashes)
				    {
                        // slower method, supports dash
                        var lineCap = _pen.StrokeEndLineCap == PenLineCap.Square ? LineCap.Square : LineCap.Round;
                        _pg = new Stroke(_ps, _pen.StrokeThickness);
                        _pg.line_join(LineJoin.Round);
                        _pg.inner_join(InnerJoin.Round);
                        _pg.line_cap(lineCap);

                        _context._graphics2D.Rasterizer.reset();   
				    }
				    else
				    {
                        // Faster method, does not support dash
                        IGammaFunction gammaFunction = null;
                        if (_pen.Antialiased)
                            gammaFunction = new gamma_none();
                        else
                            gammaFunction = new gamma_threshold(0.5);

                        double w = _pen.StrokeThickness;

                        var lineProfile = new LineProfileAnitAlias(w, gammaFunction);
                        var outlineRenderer = new OutlineRenderer(_context._graphics2D.DestImage, lineProfile);
                        _rasterizer = new rasterizer_outline_aa(outlineRenderer);

                        bool isSmoothLine = w >= 2.0f;
                        _rasterizer.line_join(isSmoothLine
                                                 ? rasterizer_outline_aa.outline_aa_join_e.outline_round_join
                                                 : rasterizer_outline_aa.outline_aa_join_e.outline_no_join);

                        _rasterizer.round_cap(isSmoothLine);
				    }


                    _ps.MoveTo(x, y);					
				}

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
				// AA   Stroke Thickness    Which algorithm?
				// T    1                   AggSharp Implementation
				// T    2                   AggSharp Implementation
				// F    1                   Bresenham
				// F    2                   AggSharp Implementation
				
				if (_useAggMethod)
				{
					_ps.LineTo(x, y);                   
				}
				else
				{
					// Workaround to prevent the thick non-AA lines that AggSharp draws when 
					// stroke thickness is 1.0                
					DrawLineBresenham(
						_destImage,
						_destImage.GetBuffer(),
						(int) _viewportSize.Width,
						(int) _viewportSize.Height,
						(int) _lastX.ClipToInt(),
						(int) _lastY.ClipToInt(),
						(int) x.ClipToInt(),
						(int) y.ClipToInt(),
						_pen.Color);
				}

				_lastX = x;
				_lastY = y;
			}

			public void End()
			{
				// Sometimes lines are drawn with Bresenham, e.g. 1px, in that case no need to forward PathStorage to Scanline Renderer
				if (!_useAggMethod) return;

			    if (_pen.HasDashes)
			    {
                    // Slower method, supports dashes
                    var graphics2D = _context._graphics2D;

                    graphics2D.Rasterizer.add_path(_pg);
                    if (!_pen.Antialiased)
                    {
                        graphics2D.Rasterizer.gamma(new gamma_threshold(0.5));
                    }

                    new ScanlineRenderer().render_scanlines_aa_solid(graphics2D.DestImage, graphics2D.Rasterizer,
                                                                     graphics2D.ScanlineCache, _context.ToRgbaBytes(_pen.Color));
                    if (!_pen.Antialiased)
                        graphics2D.Rasterizer.gamma(new gamma_none());
			    }
			    else
			    {
                    // Faster method, does not support dashes
                    var colors = new[] { _context.ToRgbaBytes(_pen.Color) };
                    var pathIndex = new[] { 0 };
                    _rasterizer.RenderAllPaths(_ps, colors, pathIndex, 1);   
			    }                
			}

            void IDisposable.Dispose()
            {
                this.End();
            }
		}
        #endregion

        #region HqFillDrawingContext Nested Class

        internal sealed class HqFillDrawingContext : IPathDrawingContext
	    {
	        private IBrush2D _brush;
	        private readonly HqRenderContext _hqRenderContext;
	        private PathStorage _ps;

	        public HqFillDrawingContext(IBrush2D brush, HqRenderContext hqRenderContext, double startX, double startY)
	        {
	            _hqRenderContext = hqRenderContext;

	            Begin(brush, startX, startY);
	        }

            public double GradientRotationAngle { get; set; }

            public IPathDrawingContext Begin(IPathColor pen, double x, double y)
            {
                _brush = (IBrush2D)pen;
                _ps = new PathStorage();

                _hqRenderContext._graphics2D.Rasterizer.reset();
                _ps.MoveTo(x, y);

                return this;
            }

            public IPathDrawingContext MoveTo(double x, double y)
	        {
	            _ps.LineTo(x, y);
	            return this;
	        }

            public void End()
            {
                _hqRenderContext._graphics2D.Rasterizer.add_path(_ps);
                _hqRenderContext._graphics2D.Rasterizer.close_polygon();

                var textureBrush = _brush as TextureBrush;
                if (textureBrush != null)
                {
                    byte[] textureArray = textureBrush.GetByteTexture(_hqRenderContext.ViewportSize);
                    new ScanlineRenderer().render_scanlines_aa_solid(_hqRenderContext._graphics2D.DestImage, _hqRenderContext._graphics2D.Rasterizer,
                                                                     _hqRenderContext._graphics2D.ScanlineCache,
                														(x, y) =>
                															{
                																// get custom color from texture brush by X and Y
                																int offset = textureBrush.GetByteOffsetNotConsideringMappingMode(x, y, GradientRotationAngle);
                																return new RGBA_Bytes(
                																	textureArray[offset + 2],
                																	textureArray[offset + 1],
                																	textureArray[offset + 0],
                																	textureArray[offset + 3]);
                															});
                }
                else
                    new ScanlineRenderer().render_scanlines_aa_solid(_hqRenderContext._graphics2D.DestImage, _hqRenderContext._graphics2D.Rasterizer,
                                                                     _hqRenderContext._graphics2D.ScanlineCache, _hqRenderContext.ToRgbaBytes(_brush.Color));
            }

            void IDisposable.Dispose()
            {
                End();
            }
	    }
        #endregion

        private class gamma_noaa : IGammaFunction
		{
			
			public double GetGamma(double x)
			{
				return 1.0;
			}

		}


		private static void DrawPixel(ImageBuffer dest, byte[] buffer, int w, int h, int x1, int y1, Color color)
		{
			// Set pixel
			if (y1 < h && y1 >= 0 && x1 < w && x1 >= 0)
			{
				BlendPixel(dest, buffer, w, color, y1, x1);
			}
		}

		private static void DrawLineBresenham(ImageBuffer dest, byte[] buffer, int w, int h, int x1, int y1, int x2, int y2, Color color)
		{
			// Edge case where lines that went out of vertical bounds clipped instead of dissapear
			if ((y1 < 0 && y2 < 0) || (y1 > h && y2 > h))
				return;

			if (x1 == x2 && y1 == y2)
			{
				DrawPixel(dest, buffer, w, h, x1, y1, color);
				return;
			}

			// Perform cohen-sutherland clipping if either point is out of the viewport
			if (!WriteableBitmapExtensions.CohenSutherlandLineClip(new Rect(0, 0, w, h), ref x1, ref y1, ref x2, ref y2)) return;

			// Distance start and end point
			int dx = x2 - x1;
			int dy = y2 - y1;

			// Determine sign for direction x
			int incx = 0;
			if (dx < 0)
			{
				dx = -dx;
				incx = -1;
			}
			else if (dx > 0)
			{
				incx = 1;
			}

			// Determine sign for direction y
			int incy = 0;
			if (dy < 0)
			{
				dy = -dy;
				incy = -1;
			}
			else if (dy > 0)
			{
				incy = 1;
			}

			// Which gradient is larger
			int pdx, pdy, odx, ody, es, el;
			if (dx > dy)
			{
				pdx = incx;
				pdy = 0;
				odx = incx;
				ody = incy;
				es = dy;
				el = dx;
			}
			else
			{
				pdx = 0;
				pdy = incy;
				odx = incx;
				ody = incy;
				es = dx;
				el = dy;
			}

			// Init start
			int x = x1;
			int y = y1;
			int error = el >> 1;
			if (y < h && y >= 0 && x < w && x >= 0)
			{
				BlendPixel(dest, buffer, w, color, y, x);
			}

			// Walk the line!
			for (int i = 0; i < el; i++)
			{
				// Update error term
				error -= es;

				// Decide which coord to use
				if (error < 0)
				{
					error += el;
					x += odx;
					y += ody;
				}
				else
				{
					x += pdx;
					y += pdy;
				}

				// Set pixel
				if (y < h && y >= 0 && x < w && x >= 0)
				{
					BlendPixel(dest, buffer, w, color, y, x);
				}
			}
		}

		private static void BlendPixel(ImageBuffer dest, byte[] buffer, int w, Color color, int y, int x)
		{
			int index = (y*w + x)*4;

			byte ca = color.A;
			byte cr = color.R;
			byte cg = color.G;
			byte cb = color.B;

			if (color.A == 255)
			{
				buffer[index + ImageBuffer.OrderA] = ca;
				buffer[index + ImageBuffer.OrderR] = cr;
				buffer[index + ImageBuffer.OrderG] = cg;
				buffer[index + ImageBuffer.OrderB] = cb;
				return;
			}

			byte sa = buffer[index + ImageBuffer.OrderA];
			byte sr = buffer[index + ImageBuffer.OrderR];
			byte sg = buffer[index + ImageBuffer.OrderG];
			byte sb = buffer[index + ImageBuffer.OrderB];

//            if (sa == 0)
//            {
//                sr = cr;
//                sg = cg;
//                sb = cb;
//            }

			// taken from http://stackoverflow.com/questions/1944095/how-to-mix-two-argb-pixels/1944193#1944193
			var rOut = (sr*sa/255) + (cr*ca*(255 - sa)/(255*255));
			var gOut = (sg*sa/255) + (cg*ca*(255 - sa)/(255*255));
			var bOut = (sb*sa/255) + (cb*ca*(255 - sa)/(255*255));
			var aOut = sa + (ca*(255 - sa)/255);

			// original code:
			//sa = (byte) (((sa * ca) * 0x8081) >> 23);
			//sr = (byte) ((((((sr * cr) * 0x8081) >> 23) * ca) * 0x8081) >> 23);
			//sg = (byte) ((((((sg * cg) * 0x8081) >> 23) * ca) * 0x8081) >> 23);
			//sb = (byte) ((((((sb * cb) * 0x8081) >> 23) * ca) * 0x8081) >> 23);
			//buffer[index + ImageBuffer.OrderA] = sa;
			//buffer[index + ImageBuffer.OrderR] = sr;
			//buffer[index + ImageBuffer.OrderG] = sg;
			//buffer[index + ImageBuffer.OrderB] = sb;

			buffer[index + ImageBuffer.OrderA] = (byte)aOut;
			buffer[index + ImageBuffer.OrderR] = (byte)rOut;
			buffer[index + ImageBuffer.OrderG] = (byte)gOut;
			buffer[index + ImageBuffer.OrderB] = (byte)bOut;

//            int destPixel = sa << 24 | sb << 16 | sg << 8 | sr;
//
//            var da = (destPixel >> 24);
//            var dg = ((destPixel >> 8) & 0xff);
//            var drb = destPixel & 0x00FF00FF;
//            
//            // blend with high-quality alpha and lower quality but faster 1-off RGBs 
//            int result = (int)(
//                ((sa + ((da * (255 - sa) * 0x8081) >> 23)) << 24) | // alpha 
//                (((sg - dg) * sa + (dg << 8)) & 0xFFFFFF00) | // green 
//                (((((srb - drb) * sa) >> 8) + drb) & 0x00FF00FF) // red and blue 
//            );

			//            var destPixel = (uint)pixels[index];
			//
			//            var da = (destPixel >> 24);
			//            var dg = ((destPixel >> 8) & 0xff);
			//            var drb = destPixel & 0x00FF00FF;
			//
			//            // blend with high-quality alpha and lower quality but faster 1-off RGBs 
			//            pixels[index] = (int)(
			//               ((sa + ((da * (255 - sa) * 0x8081) >> 23)) << 24) | // alpha 
			//               (((sg - dg) * sa + (dg << 8)) & 0xFFFFFF00) | // green 
			//               (((((srb - drb) * sa) >> 8) + drb) & 0x00FF00FF) // red and blue 
			//            );


//            var sourceBlender = new BlenderPreMultBGRA();
//
//            RGBA_Bytes sourceColor = sourceBlender.PixelToColorRGBA_Bytes(buffer, index);
//            //RGBA_Bytes sourceColor = new RGBA_Bytes(buffer[index + ImageBuffer.OrderR], buffer[index + ImageBuffer.OrderG], buffer[index + ImageBuffer.OrderB], buffer[index + ImageBuffer.OrderA]);
//
//            // Equivalent for calling destImage.BlendPixel(xCoord + i, j + yCoord, sourceColor, 1), however uses 
//            // PremultBGRA blender as destination and BGRA blender as source to get rid of dodgy discoloured edges 
////            int bufferOffset;
////            byte[] bfer = dest.GetPixelPointerXY(x, y, 
////                                                        out bufferOffset);
//            var destBlender = new BlenderPreMultBGRA();
//            destBlender.BlendPixel(buffer, index, sourceColor);
		}

//        private static void AlphaBlendNormalOnPremultiplied(byte[] pixels, int index, int sa, uint srb, uint sg)
//        {
//            var destPixel = (uint)pixels[index];
//
//            var da = (destPixel >> 24);
//            var dg = ((destPixel >> 8) & 0xff);
//            var drb = destPixel & 0x00FF00FF;
//
//            // blend with high-quality alpha and lower quality but faster 1-off RGBs 
//            pixels[index] = (int)(
//               ((sa + ((da * (255 - sa) * 0x8081) >> 23)) << 24) | // alpha 
//               (((sg - dg) * sa + (dg << 8)) & 0xFFFFFF00) | // green 
//               (((((srb - drb) * sa) >> 8) + drb) & 0x00FF00FF) // red and blue 
//            );
//        }
	}
}