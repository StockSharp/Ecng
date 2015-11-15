//
//   Project:           WriteableBitmapEx - Silverlight WriteableBitmap extensions
//   Description:       Collection of draw extension methods for the Silverlight WriteableBitmap class.
//
//   Changed by:        $Author: unknown $
//   Changed on:        $Date: 2011-10-27 21:32:30 +0100 (Thu, 27 Oct 2011) $
//   Changed in:        $Revision: 82056 $
//   Project:           $URL: https://writeablebitmapex.svn.codeplex.com/svn/trunk/Source/WriteableBitmapEx/WriteableBitmapBaseExtensions.cs $
//   Id:                $Id: WriteableBitmapBaseExtensions.cs 82056 2011-10-27 20:32:30Z unknown $
//
//
//   Copyright © 2009-2011 Rene Schulte and WriteableBitmapEx Contributors
//
//   This Software is weak copyleft open source. Please read the License.txt for details.
//
using System.Collections.Generic;
namespace System.Windows.Media.Imaging
{
    /// <summary>
    /// Collection of draw extension methods for the Silverlight WriteableBitmap class.
    /// </summary>
    internal 
#if !SAFECODE
    #if !SILVERLIGHT 
        unsafe 
    #endif
#endif
    static partial class WriteableBitmapExtensions
    {
        
        internal const int SizeOfArgb = 4;


        
        
        internal static int ConvertColor(double opacity, Color color)
        {
            if (opacity < 0.0 || opacity > 1.0)
                throw new ArgumentOutOfRangeException("opacity", "Opacity must be between 0.0 and 1.0");
            color.A = (byte) (color.A*opacity);
            return ConvertColor(color);
        }        

        internal static int ConvertColor(Color color)
        {
            if (color.A == 0)
                return 0;

            var a = color.A + 1;
            var col = (color.A << 24)
                     | ((byte)((color.R * a) >> 8) << 16)
                     | ((byte)((color.G * a) >> 8) << 8)
                     | ((byte)((color.B * a) >> 8));
            return col;
        }

        /// <summary>
        /// Fills the whole WriteableBitmap with a color.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="color">The color used for filling.</param>
        internal static void Clear(this WriteableBitmap bmp, Color color)
        {
            // Add one to use mul and cheap bit shift for multiplicaltion
            var a = color.A + 1;
            var col = (color.A << 24)
                     | ((byte)((color.R * a) >> 8) << 16)
                     | ((byte)((color.G * a) >> 8) << 8)
                     | ((byte)((color.B * a) >> 8));
            
            using (var context = bmp.GetBitmapContext())            
            {
                var pixels = context.Pixels;
                var w = bmp.PixelWidth;
                var h = bmp.PixelHeight;
                var len = w*SizeOfArgb;

                // Fill first line
                for (var x = 0; x < w; x++)
                {
                    pixels[x] = col;
                }

                // Copy first line
                var blockHeight = 1;
                var y = 1;
                while (y < h)
                {
                    BitmapContext.BlockCopy(context, 0, context, y * len, blockHeight * len);
                    y += blockHeight;
                    blockHeight = Math.Min(2*blockHeight, h - y);
                }
            }
        }

        /// <summary>
        /// Fills the whole WriteableBitmap with an empty color (0).
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        internal static void Clear(this WriteableBitmap bmp)
        {
            using (var context = bmp.GetBitmapContext())
            {
                context.Clear();
            }            
        }

        /// <summary>
        /// Clones the specified WriteableBitmap.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <returns>A copy of the WriteableBitmap.</returns>
        internal static WriteableBitmap Clone(this WriteableBitmap bmp)
        {
            var result = BitmapFactory.New(bmp.PixelWidth, bmp.PixelHeight);
            using (var srcContext = bmp.GetBitmapContext(ReadWriteMode.ReadOnly))
            using (var destContext = result.GetBitmapContext())
            {
                BitmapContext.BlockCopy(srcContext, 0, destContext, 0, srcContext.Length * SizeOfArgb);
            }
            return result;
        }


        
        /// <summary>
        /// Applies the given function to all the pixels of the bitmap in 
        /// order to set their color.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="func">The function to apply. With parameters x, y and a color as a result</param>
        internal static void ForEach(this WriteableBitmap bmp, Func<int, int, Color> func)
        {
            using (var context = bmp.GetBitmapContext())
            {
                var pixels = context.Pixels;
                int w = bmp.PixelWidth;
                int h = bmp.PixelHeight;
                int index = 0;

                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        var color = func(x, y);
                        // Add one to use mul and cheap bit shift for multiplicaltion
                        var a = color.A + 1;
                        pixels[index++] = (color.A << 24)
                                          | ((byte) ((color.R*a) >> 8) << 16)
                                          | ((byte) ((color.G*a) >> 8) << 8)
                                          | ((byte) ((color.B*a) >> 8));
                    }
                }
            }
        }

        /// <summary>
        /// Applies the given function to all the pixels of the bitmap in 
        /// order to set their color.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="func">The function to apply. With parameters x, y, source color and a color as a result</param>
        internal static void ForEach(this WriteableBitmap bmp, Func<int, int, Color, Color> func)
        {
            using (var context = bmp.GetBitmapContext())
            {
                var pixels = context.Pixels;
                int w = bmp.PixelWidth;
                int h = bmp.PixelHeight;
                int index = 0;

                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        int c = pixels[index];
                        var color = func(x, y, Color.FromArgb((byte)(c >> 24), (byte)(c >> 16), (byte)(c >> 8), (byte)(c)));
                        // Add one to use mul and cheap bit shift for multiplicaltion
                        var a = color.A + 1;
                        pixels[index++] = (color.A << 24)
                                       | ((byte)((color.R * a) >> 8) << 16)
                                       | ((byte)((color.G * a) >> 8) << 8)
                                       | ((byte)((color.B * a) >> 8));
                    }
                }
            }
        }


        
        /// <summary>
        /// Gets the color of the pixel at the x, y coordinate as integer.  
        /// For best performance this method should not be used in iterative real-time scenarios. Implement the code directly inside a loop.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="x">The x coordinate of the pixel.</param>
        /// <param name="y">The y coordinate of the pixel.</param>
        /// <returns>The color of the pixel at x, y.</returns>
        internal static int GetPixeli(this WriteableBitmap bmp, int x, int y)
        {
            using (var context = bmp.GetBitmapContext())
            {
                return context.Pixels[y * bmp.PixelWidth + x];
            }
        }

        /// <summary>
        /// Gets the color of the pixel at the x, y coordinate as a Color struct.  
        /// For best performance this method should not be used in iterative real-time scenarios. Implement the code directly inside a loop.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="x">The x coordinate of the pixel.</param>
        /// <param name="y">The y coordinate of the pixel.</param>
        /// <returns>The color of the pixel at x, y as a Color struct.</returns>
        internal static Color GetPixel(this WriteableBitmap bmp, int x, int y)
        {
            using (var context = bmp.GetBitmapContext())
            {
                var c = context.Pixels[y*bmp.PixelWidth + x];
                var a = (byte) (c >> 24);

                // Prevent division by zero
                int ai = a;
                if (ai == 0)
                {
                    ai = 1;
                }

                // Scale inverse alpha to use cheap integer mul bit shift
                ai = ((255 << 8)/ai);
                return Color.FromArgb(a,
                                      (byte) ((((c >> 16) & 0xFF)*ai) >> 8),
                                      (byte) ((((c >> 8) & 0xFF)*ai) >> 8),
                                      (byte) ((((c & 0xFF)*ai) >> 8)));
            }
        }

        /// <summary>
        /// Gets the brightness / luminance of the pixel at the x, y coordinate as byte.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="x">The x coordinate of the pixel.</param>
        /// <param name="y">The y coordinate of the pixel.</param>
        /// <returns>The brightness of the pixel at x, y.</returns>
        internal static byte GetBrightness(this WriteableBitmap bmp, int x, int y)
        {
            using (var context = bmp.GetBitmapContext(ReadWriteMode.ReadOnly))
            {
                // Extract color components
                var c = context.Pixels[y*bmp.PixelWidth + x];
                var r = (byte) (c >> 16);
                var g = (byte) (c >> 8);
                var b = (byte) (c);

                // Convert to gray with constant factors 0.2126, 0.7152, 0.0722
                return (byte) ((r*6966 + g*23436 + b*2366) >> 15);
            }
        }


        
        
        /// <summary>
        /// Sets the color of the pixel using a precalculated index (faster). 
        /// For best performance this method should not be used in iterative real-time scenarios. Implement the code directly inside a loop.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="index">The coordinate index.</param>
        /// <param name="r">The red value of the color.</param>
        /// <param name="g">The green value of the color.</param>
        /// <param name="b">The blue value of the color.</param>
        internal static void SetPixeli(this WriteableBitmap bmp, int index, byte r, byte g, byte b)
        {
            using (var context = bmp.GetBitmapContext())
            {
                context.Pixels[index] = (255 << 24) | (r << 16) | (g << 8) | b;
            }
        }

        /// <summary>
        /// Sets the color of the pixel. 
        /// For best performance this method should not be used in iterative real-time scenarios. Implement the code directly inside a loop.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="x">The x coordinate (row).</param>
        /// <param name="y">The y coordinate (column).</param>
        /// <param name="r">The red value of the color.</param>
        /// <param name="g">The green value of the color.</param>
        /// <param name="b">The blue value of the color.</param>
        internal static void SetPixel(this WriteableBitmap bmp, int x, int y, byte r, byte g, byte b)
        {
            using (var context = bmp.GetBitmapContext())
            {
                context.Pixels[y*bmp.PixelWidth + x] = (255 << 24) | (r << 16) | (g << 8) | b;
            }
        }


        
        /// <summary>
        /// Sets the color of the pixel including the alpha value and using a precalculated index (faster). 
        /// For best performance this method should not be used in iterative real-time scenarios. Implement the code directly inside a loop.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="index">The coordinate index.</param>
        /// <param name="a">The alpha value of the color.</param>
        /// <param name="r">The red value of the color.</param>
        /// <param name="g">The green value of the color.</param>
        /// <param name="b">The blue value of the color.</param>
        internal static void SetPixeli(this WriteableBitmap bmp, int index, byte a, byte r, byte g, byte b)
        {
            using (var context = bmp.GetBitmapContext())
            {
                context.Pixels[index] = (a << 24) | (r << 16) | (g << 8) | b;
            }
        }

        /// <summary>
        /// Sets the color of the pixel including the alpha value. 
        /// For best performance this method should not be used in iterative real-time scenarios. Implement the code directly inside a loop.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="x">The x coordinate (row).</param>
        /// <param name="y">The y coordinate (column).</param>
        /// <param name="a">The alpha value of the color.</param>
        /// <param name="r">The red value of the color.</param>
        /// <param name="g">The green value of the color.</param>
        /// <param name="b">The blue value of the color.</param>
        internal static void SetPixel(this WriteableBitmap bmp, int x, int y, byte a, byte r, byte g, byte b)
        {
            using (var context = bmp.GetBitmapContext())
            {
                context.Pixels[y*bmp.PixelWidth + x] = (a << 24) | (r << 16) | (g << 8) | b;
            }
        }


        
        /// <summary>
        /// Sets the color of the pixel using a precalculated index (faster). 
        /// For best performance this method should not be used in iterative real-time scenarios. Implement the code directly inside a loop.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="index">The coordinate index.</param>
        /// <param name="color">The color.</param>
        internal static void SetPixeli(this WriteableBitmap bmp, int index, Color color)
        {
            using (var context = bmp.GetBitmapContext())
            {
                // Add one to use mul and cheap bit shift for multiplicaltion
                var a = color.A + 1;
                context.Pixels[index] = (color.A << 24)
                                    | ((byte) ((color.R*a) >> 8) << 16)
                                    | ((byte) ((color.G*a) >> 8) << 8)
                                    | ((byte) ((color.B*a) >> 8));
            }
        }

        /// <summary>
        /// Sets the color of the pixel. 
        /// For best performance this method should not be used in iterative real-time scenarios. Implement the code directly inside a loop.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="x">The x coordinate (row).</param>
        /// <param name="y">The y coordinate (column).</param>
        /// <param name="color">The color.</param>
        internal static void SetPixel(this WriteableBitmap bmp, int x, int y, Color color)
        {
            using (var context = bmp.GetBitmapContext())
            {
                // Add one to use mul and cheap bit shift for multiplicaltion
                var a = color.A + 1;
                context.Pixels[y*bmp.PixelWidth + x] = (color.A << 24)
                                                   | ((byte) ((color.R*a) >> 8) << 16)
                                                   | ((byte) ((color.G*a) >> 8) << 8)
                                                   | ((byte) ((color.B*a) >> 8));
            }
        }

        /// <summary>
        /// Sets the color of the pixel using an extra alpha value and a precalculated index (faster). 
        /// For best performance this method should not be used in iterative real-time scenarios. Implement the code directly inside a loop.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="index">The coordinate index.</param>
        /// <param name="a">The alpha value of the color.</param>
        /// <param name="color">The color.</param>
        internal static void SetPixeli(this WriteableBitmap bmp, int index, byte a, Color color)
        {
            using (var context = bmp.GetBitmapContext())
            {
                // Add one to use mul and cheap bit shift for multiplicaltion
                var ai = a + 1;
                context.Pixels[index] = (a << 24)
                                    | ((byte) ((color.R*ai) >> 8) << 16)
                                    | ((byte) ((color.G*ai) >> 8) << 8)
                                    | ((byte) ((color.B*ai) >> 8));
            }
        }

        /// <summary>
        /// Sets the color of the pixel using an extra alpha value. 
        /// For best performance this method should not be used in iterative real-time scenarios. Implement the code directly inside a loop.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="x">The x coordinate (row).</param>
        /// <param name="y">The y coordinate (column).</param>
        /// <param name="a">The alpha value of the color.</param>
        /// <param name="color">The color.</param>
        internal static void SetPixel(this WriteableBitmap bmp, int x, int y, byte a, Color color)
        {
            using (var context = bmp.GetBitmapContext())
            {
                // Add one to use mul and cheap bit shift for multiplicaltion
                var ai = a + 1;
                context.Pixels[y*bmp.PixelWidth + x] = (a << 24)
                                                   | ((byte) ((color.R*ai) >> 8) << 16)
                                                   | ((byte) ((color.G*ai) >> 8) << 8)
                                                   | ((byte) ((color.B*ai) >> 8));
            }
        }

        /// <summary>
        /// Sets the color of the pixel using a precalculated index (faster).  
        /// For best performance this method should not be used in iterative real-time scenarios. Implement the code directly inside a loop.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="index">The coordinate index.</param>
        /// <param name="color">The color.</param>
        internal static void SetPixeli(this WriteableBitmap bmp, int index, int color)
        {
            using (var context = bmp.GetBitmapContext())
            {
                context.Pixels[index] = color;
            }
        }

        /// <summary>
        /// Sets the color of the pixel. 
        /// For best performance this method should not be used in iterative real-time scenarios. Implement the code directly inside a loop.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="x">The x coordinate (row).</param>
        /// <param name="y">The y coordinate (column).</param>
        /// <param name="color">The color.</param>
        internal static void SetPixel(this WriteableBitmap bmp, int x, int y, int color)
        {
            using (var context = bmp.GetBitmapContext())
            {
                context.Pixels[y*bmp.PixelWidth + x] = color;
            }
        }


		internal static void DrawPixelsVertically(this WriteableBitmap bmp, int x, int yStartBottom, int yEndTop, IList<int> pixelColorsArgb, double opacity, bool yAxisIsFlipped)
        {
			var t = Math.Max(yStartBottom, yEndTop);
			yEndTop = Math.Min(yStartBottom, yEndTop);
			yStartBottom = t;

            var w = bmp.PixelWidth;
            var h = bmp.PixelHeight;

            if (yStartBottom == yEndTop) return;
			int opacityA = (int)(opacity * 256);

            using (var context = bmp.GetBitmapContext())
            {
                var pixels = context.Pixels;
                int yStartBottomLimited = Math.Min(yStartBottom, h);
                int destinationPixelIndex = x + yStartBottomLimited * w;

                for (int y = yStartBottomLimited; y >= yEndTop && y >= 0; y--, destinationPixelIndex -= w)
                {
                    if (y < 0 || y >= h) continue;
                    int pixelIndex = (yStartBottom - y) * pixelColorsArgb.Count / (yStartBottom - yEndTop);;
	                if (yAxisIsFlipped)
		                pixelIndex = pixelColorsArgb.Count - 1 - pixelIndex;
					
                    if (pixelIndex >= 0 && pixelIndex < pixelColorsArgb.Count)
                    {
                        int sourcePixel = pixelColorsArgb[pixelIndex];
                        int aA = (int)(((sourcePixel >> 24) & 0xff) * opacity);
                        if (aA == 0xFF)
                        {
                            pixels[destinationPixelIndex] = sourcePixel; // no blending
                        }
                        else if (aA > 0)
                        {
                            int destPixel = context.Pixels[destinationPixelIndex];
                            int aB = ((destPixel >> 24) & 0xff);
                            int rB = ((destPixel >> 16) & 0xff);
                            int gB = ((destPixel >> 8) & 0xff);
                            int bB = ((destPixel) & 0xff);

                            int rA = ((sourcePixel >> 16) & 0xff);
                            int gA = ((sourcePixel >> 8) & 0xff);
                            int bA = ((sourcePixel) & 0xff);

                            int rOut = (rA * aA / 255) + (rB * aB * (255 - aA) / (255 * 255));
                            int gOut = (gA * aA / 255) + (gB * aB * (255 - aA) / (255 * 255));
                            int bOut = (bA * aA / 255) + (bB * aB * (255 - aA) / (255 * 255));
                            int aOut = aA + (aB * (255 - aA) / 255);

                            context.Pixels[destinationPixelIndex] = (aOut << 24) + (rOut << 16) + (gOut << 8) + bOut;
                        }
                    }
                }
            }
        }

    }
}