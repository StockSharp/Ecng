// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IRenderContext2D.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows; 
using System.Windows.Media;
using Ecng.Xaml.Charting.Visuals;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace Ecng.Xaml.Charting.Rendering.Common
{
	/// <summary>
	/// Defines the interface to a 2D RenderContext, allowing drawing, blitting and creation of pens and brushes on the <see cref="Ecng.Xaml.Charting.Rendering.Common.RenderSurfaceBase"/>
	/// </summary>
	/// <remarks>The <see cref="IRenderContext2D"/> is a graphics context valid for the current render pass. Any class which implements <see cref="IDrawable"/> has an OnDraw method
	/// in which an <see cref="IRenderContext2D"/> is passed in. Use this to draw penned lines, fills, rectangles, ellipses and blit graphics to the screen.</remarks>
	public interface IRenderContext2D : IDisposable
	{
		/// <summary>
		/// Gets a collection of <see cref="RenderOperationLayers"/>, which allow rendering operations to be posted to a layered queue for later
		/// execution in order (and correct Z-ordering). 
		/// </summary>
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
		RenderOperationLayers Layers { get; }

		/// <summary>
		/// Gets the current size of the viewport.
		/// </summary>
		Size ViewportSize { get; }

		/// <summary>
		/// enables/disables primitves chaching optimization ( Direct3D renderer only )
		/// </summary>
		void SetPrimitvesCachingEnabled( bool bEnabled );

		/// <summary>
		/// Creates a <see cref="IBrush2D"/> valid for the current render pass. Use this to draw rectangles, polygons and shaded areas 
		/// </summary>
		/// <param name="color">The color of the brush, supports transparency</param>
		/// <param name="opacity">The opacity of the brush</param>
		/// <param name="alphaBlend">If true, use alphablending when shading. If null, auto-detect</param>
		/// <returns>The <see cref="IBrush2D"/> instance</returns>
		IBrush2D CreateBrush(Color color, double opacity = 1, bool? alphaBlend = null);

		/// <summary>
		/// Creates a <see cref="IBrush2D"/> from WPF Brush valid for the current render pass. Use this to draw rectangles, polygons and shaded areas 
		/// </summary>
		/// <param name="brush">The WPF Brush to use as a source, e.g. this can be a <seealso cref="SolidColorBrush"/>, or it can be a <seealso cref="LinearGradientBrush"/>. Note that solid colors support transparency and are faster than gradient brushes</param>
		/// <param name="opacity">The opacity of the brush</param>
		/// <param name="textureMappingMode">Defines a <see cref="TextureMappingMode"/>, e.g. brushes share a texture per viewport or a new texture per primitive drawn</param>
		/// <returns>The <see cref="IBrush2D"/> instance</returns>
		IBrush2D CreateBrush(Brush brush, double opacity = 1, TextureMappingMode textureMappingMode = TextureMappingMode.PerPrimitive);

		/// <summary>
		/// Creates a <see cref="IPen2D"/> valid for the current render pass. Use this to draw outlines, quads and lines
		/// </summary>
		/// <param name="color">The color of the pen, supports transparency</param>
		/// <param name="antiAliasing">If true, use antialiasing</param>
		/// <param name="strokeThickness">The strokethickness, default=1.0</param>
		/// <param name="opacity">The opecity of the pen</param>
		/// <param name="strokeDashArray"> </param>
		/// <param name="strokeEndLineCap"> </param>
		/// <returns>The <see cref="IPen2D"/> instance</returns>
		IPen2D CreatePen(Color color, bool antiAliasing, float strokeThickness, double opacity = 1.0, double[] strokeDashArray = null, PenLineCap strokeEndLineCap=PenLineCap.Round);

		/// <summary>
		/// Creates a Sprite from FrameworkElement by rendering to bitmap. This may be used in the <see cref="DrawSprite"/> method
		/// to draw to the screen repeatedly
		/// </summary>
		/// <param name="fe"></param>
		/// <returns></returns>
		ISprite2D CreateSprite(FrameworkElement fe);

		/// <summary>
		/// Clears the <see cref="IRenderSurface2D"/>
		/// </summary>
		void Clear();

		/// <summary>
		/// Blits the source image onto the <see cref="IRenderSurface2D"/>
		/// </summary>
		/// <param name="srcSprite">The source sprite to render</param>
		/// <param name="srcRect">The source rectangle</param>
		/// <param name="destPoint">The destination point, which will be the top-left coordinate of the sprite after blitting</param>
		void DrawSprite(ISprite2D srcSprite, Rect srcRect, Point destPoint);

		/// <summary>
		/// Batch draw of the source sprite onto the <see cref="IRenderSurface2D"/>
		/// </summary>
		/// <param name="sprite2D">The sprite to render</param>
		/// <param name="srcRect">The source rectangle</param>
		/// <param name="points">The points to draw sprites at</param>
		void DrawSprites(ISprite2D sprite2D, Rect srcRect, IEnumerable<Point> points);

		/// <summary>
		/// Batch draw of the source sprite onto the <see cref="IRenderSurface2D"/>
		/// </summary>
		/// <param name="sprite2D">The sprite to render</param>
		/// <param name="dstRects">The destination rectangles to draw sprites at</param>
		void DrawSprites(ISprite2D sprite2D, IEnumerable<Rect> dstRects);

		/// <summary>
		/// Fills a rectangle on the <see cref="IRenderSurface2D"/> using the specified <see cref="IBrush2D"/>
		/// </summary>
		/// <param name="brush">The brush</param>
		/// <param name="pt2">The top-left point of the rectangle</param>
		/// <param name="pt1">The bottom-right point of the rectangle</param>
		/// <param name="gradientRotationAngle">The angle which the brush is rotated by, default is zero</param>        
		void FillRectangle(IBrush2D brush, Point pt1, Point pt2, double gradientRotationAngle = 0);

		/// <summary>
		/// Fills a polygon on the <see cref="IRenderSurface2D"/> using the specifie <see cref="IBrush2D"/>
		/// </summary>
		/// <param name="brush">The brush</param>
		/// <param name="points">The list of points defining the closed polygon, where X,Y coordinates in clockwise direction</param>
		void FillPolygon(IBrush2D brush, IEnumerable<Point> points);

	    /// <summary>
		/// Fills an area defined the the Points and Heights, e.g. as in a mountain chart, using the specifie <see cref="IBrush2D"/>
		/// </summary>
		/// <param name="brush">The brush</param>
		/// <param name="lines"></param>
		/// <param name="isVerticalChart">Value, indicates whether chart is vertical</param>
		/// <param name="gradientRotationAngle">The angle which the brush is rotated by</param>
		/// <param name="liness"></param>
		void FillArea(IBrush2D brush, IEnumerable<Tuple<Point, Point>> lines, bool isVerticalChart = false, double gradientRotationAngle = 0);

		/// <summary>
		/// Draws a Quad on the <see cref="IRenderSurface2D"/> using the specified <see cref="IPen2D"/>
		/// </summary>
		/// <param name="pen">The Pen</param>
		/// <param name="pt1">Left-top point in the quad</param>
		/// <param name="pt2">Bottom-right point in the quad</param>
		void DrawQuad(IPen2D pen, Point pt1, Point pt2);

		/// <summary>
		/// Draws an Ellipse on the <see cref="IRenderSurface2D"/> using the specified outline <see cref="IPen2D">Pen</see> and fill <see cref="IBrush2D">Brush</see>
		/// </summary>
		/// <param name="strokePen">The stroke pen</param>
		/// <param name="fillBrush">The fill brush</param>
		/// <param name="center">The center of the ellipse in pixels</param>
		/// <param name="width">The width of the ellipse in pixels</param>
		/// <param name="height">The height of the ellipse in pixels</param>
		void DrawEllipse(IPen2D strokePen, IBrush2D fillBrush, Point center, double width, double height);

		/// <summary>
		/// Draws 0..N Ellipses at the points passed in with the same width, height, pen and brush
		/// </summary>
		/// <param name="strokePen"></param>
		/// <param name="fillBrush"></param>
		/// <param name="centres">The points to draw ellipses at</param>
		/// <param name="width">The common width for all ellipses</param>
		/// <param name="height">The common height for all ellipses</param>
		void DrawEllipses(IPen2D strokePen, IBrush2D fillBrush, IEnumerable<Point> centres, double width, double height);

		/// <summary>
		/// Draws a single line on the <see cref="IRenderSurface2D"/> using the specified <see cref="IPen2D"/>. 
		/// Note for a faster implementation in some rasterizers, use DrawLines passing in an IEnumerable
		/// </summary>
		/// <param name="pen">The pen</param>
		/// <param name="pt1">The start of the line in pixels</param>
		/// <param name="pt2">The end of the line in pixels</param>
		void DrawLine(IPen2D pen, Point pt1, Point pt2);

		/// <summary>
		/// Draws a multi-point line on the <see cref="IRenderSurface2D"/> using the specified <see cref="IPen2D"/>
		/// </summary>
		/// <param name="pen">The pen</param>
		/// <param name="points">The points </param>
		/// <returns>The last point in the polyline drawn</returns>
		void DrawLines(IPen2D pen, IEnumerable<Point> points);

		/// <summary>
		/// Call this method, passing in <see cref="IDisposable"/> instance to dispose after the render pass completes. 
		/// Called internally by Ultrachart to lazy-dispose of Direct2D and Direct3D brushes and textures
		/// </summary>
		/// <param name="disposable"></param>
		void DisposeResourceAfterDraw(IDisposable disposable);

		/// <summary>
		/// Draws vertical scan line for heatmap
		/// from bottom to top, from yStart to yEnd
		/// </summary>
		/// <param name="x">Screen X coordinate where to draw pixels</param>
		/// <param name="yStartBottom">Screen Y coordinate of vertical scan line's bottom.
		/// Can be located outdide of visible area, in this case not all pixels in list are rendered</param>
		/// <param name="yEndTop">Screen Y coordinate of vertical scan line's top.
		/// Can be located outdide of visible area, in this case not all pixels in list are rendered</param>
		/// <param name="pixelColorsArgb">The list of pixel colors to draw</param>
		/// <param name="opacity">The Opacity of the line from 0.0 to 1.0</param>
		/// <param name="yAxisIsFlipped">if set to <c>true</c> then y axis is flipped.</param>
		void DrawPixelsVertically(int x, int yStartBottom, int yEndTop, IList<int> pixelColorsArgb, double opacity, bool yAxisIsFlipped);

		/// <summary>
		/// Calculate space needed to draw text
		/// </summary>
		void TextDrawDimensions(string text, float fontSize, Color foreColor, out float width, out float height, string fontFamily = null, FontWeight fontWeight = default(FontWeight));

        /// <summary>
        /// max digit size in pixels
        /// </summary>
	    Size DigitMaxSize(float fontSize, string fontFamily = null, FontWeight fontWeight = default(FontWeight));

		/// <summary>
		/// Draws text if it does not go outside 
		/// </summary>
		void DrawText(string text, Rect dstBoundingRect, AlignmentX alignX, AlignmentY alignY, Color foreColor, float fontSize, string fontFamily = null, FontWeight fontWeight = default(FontWeight));

		/// <summary>
		/// Draws text relative to base point 
		/// </summary>
		void DrawText(string text, Point basePoint, AlignmentX alignX, AlignmentY alignY, Color foreColor, float fontSize, string fontFamily = null, FontWeight fontWeight = default(FontWeight));

		/// <summary>
		/// Begins a Polyline segment, returning the <see cref="IPathDrawingContext"/>. This is the fastest way to draw lines and simply a proxy to <see cref="DrawLines"/> method. 
		/// </summary>
		/// <param name="pen">The pen for the line segment</param>
		/// <param name="startX">The start X coordinate (pixel coord)</param>
		/// <param name="startY">The start Y coordinate (pixel coord)</param>
		/// <returns>The <see cref="IPathDrawingContext"/> to continue the line</returns>
		IPathDrawingContext BeginLine(IPen2D pen, double startX, double startY);

		/// <summary>
        /// Begins a filled Polygon segment, returning the <see cref="IPathDrawingContext"/>. This is the fastest way to draw polygon and simply a proxy to <see cref="FillArea"/> method. 
		/// </summary>
		/// <param name="brush">The brush for the polygon fill</param>
		/// <param name="startX">The start X coordinate (pixel coord)</param>
		/// <param name="startY">The start Y coordinate (pixel coord)</param>
		/// <param name="gradientRotationAngle">The angle which the <param name="brush"></param> is rotated by</param>
		/// <returns>The <see cref="IPathDrawingContext"/> to continue the polygon</returns>
        IPathDrawingContext BeginPolygon(IBrush2D brush, double startX, double startY, double gradientRotationAngle = 0d);
	}

	/// <summary>
	/// Defines enumeration constants to describe how textures are mapped. 
	/// If textures are mapped <see cref="TextureMappingMode.PerScreen"/>, then a single
	/// large texture is shared for all elements that use this texture. Else, if <see cref="TextureMappingMode.PerPrimitive"/>
	/// then individual primitives have separate textures. 
	/// </summary>
	public enum TextureMappingMode
	{
		/// <summary>
		/// with this mode texture coordinates equal to screen coordinates
		/// </summary>
		PerScreen,
		/// <summary>
		/// with this mode entire texture is fit into single primitive
		/// </summary>
		PerPrimitive
	}
}
