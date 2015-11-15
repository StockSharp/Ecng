//
//   Project:           WriteableBitmapEx - Silverlight WriteableBitmap extensions
//   Description:       Collection of draw extension methods for the Silverlight WriteableBitmap class.
//
//   Changed by:        $Author: unknown $
//   Changed on:        $Date: 2011-10-28 18:23:21 +0100 (Fri, 28 Oct 2011) $
//   Changed in:        $Revision: 82087 $
//   Project:           $URL: https://writeablebitmapex.svn.codeplex.com/svn/trunk/Source/WriteableBitmapEx/WriteableBitmapFillExtensions.cs $
//   Id:                $Id: WriteableBitmapFillExtensions.cs 82087 2011-10-28 17:23:21Z unknown $
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
        
        
        
        /// <summary>
        /// Draws a filled rectangle.
        /// x2 has to be greater than x1 and y2 has to be greater than y1.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="x1">The x-coordinate of the bounding rectangle's left side.</param>
        /// <param name="y1">The y-coordinate of the bounding rectangle's top side.</param>
        /// <param name="x2">The x-coordinate of the bounding rectangle's right side.</param>
        /// <param name="y2">The y-coordinate of the bounding rectangle's bottom side.</param>
        /// <param name="color">The color.</param>
        internal static void FillRectangle(this WriteableBitmap bmp, int x1, int y1, int x2, int y2, Color color)
        {
            // Add one to use mul and cheap bit shift for multiplicaltion
            var a = color.A + 1;
            var col = (color.A << 24)
                     | ((byte)((color.R * a) >> 8) << 16)
                     | ((byte)((color.G * a) >> 8) << 8)
                     | ((byte)((color.B * a) >> 8));
            bmp.FillRectangle(x1, y1, x2, y2, col);
        }

        /// <summary>
        /// Draws a filled rectangle.
        /// x2 has to be greater than x1 and y2 has to be greater than y1.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="x1">The x-coordinate of the bounding rectangle's left side.</param>
        /// <param name="y1">The y-coordinate of the bounding rectangle's top side.</param>
        /// <param name="x2">The x-coordinate of the bounding rectangle's right side.</param>
        /// <param name="y2">The y-coordinate of the bounding rectangle's bottom side.</param>
        /// <param name="color">The color.</param>
        /// <param name="blendMode">The blend mode.</param>
        internal static void FillRectangle(this WriteableBitmap bmp, int x1, int y1, int x2, int y2, int color, BlendMode blendMode = BlendMode.Alpha)
        {
            // Use refs for faster access (really important!) speeds up a lot!
            var w = bmp.PixelWidth;
            var h = bmp.PixelHeight;

            int sa = ((color >> 24) & 0xff);
            int sr = ((color >> 16) & 0xff);
            int sg = ((color >> 8) & 0xff);
            int sb = ((color) & 0xff);

            bool noBlending = blendMode == BlendMode.None || sa == 255;

            using (var context = bmp.GetBitmapContext())
            {
                var pixels = context.Pixels;

                // Check boundaries
                if ((x1 < 0 && x2 < 0) || (y1 < 0 && y2 < 0)
                    || (x1 >= w && x2 >= w) || (y1 >= h && y2 >= h))
                {
                    return;
                }

                // Clamp boundaries
                if(x1 < 0)  x1 = 0;
                if(x1 >= w) x1 = w - 1;

                if(y1 < 0)  y1 = 0;
                if(y1 >= h) y1 = h - 1;

                if(x2 < 0)  x2 = 0;
                if(x2 >= w) x2 = w - 1;

                if(y2 < 0)  y2 = 0;
                if(y2 >= h) y2 = h - 1;

                //swap values
                if(y1 > y2) {
                    y2 -= y1;
                    y1 += y2;
                    y2 = (y1 - y2);
                }

                // Fill first line
                var startY = y1*w;
                var startYPlusX1 = startY + x1;
                var endOffset = startY + x2;

                for(var idx = startYPlusX1; idx <= endOffset; idx++) {
                    pixels[idx] = noBlending ? color : AlphaBlendColors(pixels[idx], sa, sr, sg, sb);
                }

                // Copy first line
                var len = (x2 - x1 + 1);
                var srcOffsetBytes = startYPlusX1*SizeOfArgb;
                var offset2 = y2*w + x1;
                for(var y = startYPlusX1 + w; y <= offset2; y += w) {

                    if(noBlending) {
                        BitmapContext.BlockCopy(context, srcOffsetBytes, context, y * SizeOfArgb, len * SizeOfArgb);
                        continue;
                    }

                    // Alpha blend line
                    for(int i = 0; i < len; i++) {
                        int idx = y + i;
                        pixels[idx] = AlphaBlendColors(pixels[idx], sa, sr, sg, sb);
                    }
                }
            }
        }

        private static int AlphaBlendColors(int pixel, int sa, int sr, int sg, int sb)
        {
            // Alpha blend
            int destPixel = pixel;
            int da = ((destPixel >> 24) & 0xff);
            int dr = ((destPixel >> 16) & 0xff);
            int dg = ((destPixel >> 8) & 0xff);
            int db = ((destPixel) & 0xff);

            destPixel = ((sa + (((da * (255 - sa)) * 0x8081) >> 23)) << 24) |
                                     ((sr + (((dr * (255 - sa)) * 0x8081) >> 23)) << 16) |
                                     ((sg + (((dg * (255 - sa)) * 0x8081) >> 23)) << 8) |
                                     ((sb + (((db * (255 - sa)) * 0x8081) >> 23)));

            return destPixel;
        }

        /// <summary>
        /// Draws a filled rectangle.
        /// x2 has to be greater than x1 and y2 has to be greater than y1.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="x1">The x-coordinate of the bounding rectangle's left side.</param>
        /// <param name="y1">The y-coordinate of the bounding rectangle's top side.</param>
        /// <param name="x2">The x-coordinate of the bounding rectangle's right side.</param>
        /// <param name="y2">The y-coordinate of the bounding rectangle's bottom side.</param>
        /// <param name="colorCb">The color.</param>
        /// <param name="blendMode">The blend mode.</param>
        internal static void FillRectangle(this WriteableBitmap bmp, int x1, int y1, int x2, int y2, Func<int, int, int> colorCb, BlendMode blendMode = BlendMode.Alpha)
        {
            // Use refs for faster access (really important!) speeds up a lot!
            var w = bmp.PixelWidth;
            var h = bmp.PixelHeight;

            using (var context = bmp.GetBitmapContext())
            {
                var pixels = context.Pixels;

                // Check boundaries
                if ((x1 < 0 && x2 < 0) || (y1 < 0 && y2 < 0)
                    || (x1 >= w && x2 >= w) || (y1 >= h && y2 >= h))
                {
                    return;
                }

                // Clamp boundaries
                if (x1 < 0)
                {
                    x1 = 0;
                }
                if (y1 < 0)
                {
                    y1 = 0;
                }
                if (x2 < 0)
                {
                    x2 = 0;
                }
                if (y2 < 0)
                {
                    y2 = 0;
                }
                if (x1 > w)
                {
                    x1 = w;
                }
                if (y1 > h)
                {
                    y1 = h;
                }
                if (x2 > w)
                {
                    x2 = w;
                }
                if (y2 > h)
                {
                    y2 = h;
                }

                //swap values
                if (y1 > y2)
                {
                    y2 -= y1;
                    y1 += y2;
                    y2 = (y1 - y2);
                }


                var width = x2 - x1 + 1;
                var startYIndex = y1 * w;
                var startYIndexPlusX1 = startYIndex + x1;

                var endYIndex = y2 * w + x1;
                int y = y1;
                for (var yPointer = startYIndexPlusX1; yPointer < endYIndex; yPointer += w, y++)
                {   
                    // Alpha blend line
                    int x = x1;
                    for (int xIndex = 0; xIndex < width; xIndex++, x++)
                    {
                        int pixelIndex = yPointer + xIndex;

                        var color = colorCb(x, y);

                        int sa = ((color >> 24) & 0xff);
                        int sr = ((color >> 16) & 0xff);
                        int sg = ((color >> 8) & 0xff);
                        int sb = ((color) & 0xff);

                        bool noBlending = blendMode == BlendMode.None || sa == 255;

                        pixels[pixelIndex] = noBlending ? color : AlphaBlendColors(pixels[pixelIndex], sa, sr, sg, sb);
                    }

                }
            }
        }

        /// <summary>
        /// A Fast Bresenham Type Algorithm For Drawing filled ellipses http://homepage.smc.edu/kennedy_john/belipse.pdf 
        /// x2 has to be greater than x1 and y2 has to be greater than y1.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="x1">The x-coordinate of the bounding rectangle's left side.</param>
        /// <param name="y1">The y-coordinate of the bounding rectangle's top side.</param>
        /// <param name="x2">The x-coordinate of the bounding rectangle's right side.</param>
        /// <param name="y2">The y-coordinate of the bounding rectangle's bottom side.</param>
        /// <param name="color">The color for the line.</param>
        internal static void FillEllipse(this WriteableBitmap bmp, int x1, int y1, int x2, int y2, Color color)
        {
            // Add one to use mul and cheap bit shift for multiplicaltion
            var a = color.A + 1;
            var col = (color.A << 24)
                     | ((byte)((color.R * a) >> 8) << 16)
                     | ((byte)((color.G * a) >> 8) << 8)
                     | ((byte)((color.B * a) >> 8));
            bmp.FillEllipse(x1, y1, x2, y2, col);
        }

        /// <summary>
        /// A Fast Bresenham Type Algorithm For Drawing filled ellipses http://homepage.smc.edu/kennedy_john/belipse.pdf 
        /// x2 has to be greater than x1 and y2 has to be greater than y1.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="x1">The x-coordinate of the bounding rectangle's left side.</param>
        /// <param name="y1">The y-coordinate of the bounding rectangle's top side.</param>
        /// <param name="x2">The x-coordinate of the bounding rectangle's right side.</param>
        /// <param name="y2">The y-coordinate of the bounding rectangle's bottom side.</param>
        /// <param name="color">The color for the line.</param>
        internal static void FillEllipse(this WriteableBitmap bmp, int x1, int y1, int x2, int y2, int color)
        {
            // Calc center and radius
            int xr = (x2 - x1) >> 1;
            int yr = (y2 - y1) >> 1;
            int xc = x1 + xr;
            int yc = y1 + yr;
            bmp.FillEllipseCentered(xc, yc, xr, yr, color);
        }

        /// <summary>
        /// A Fast Bresenham Type Algorithm For Drawing filled ellipses http://homepage.smc.edu/kennedy_john/belipse.pdf 
        /// Uses a different parameter representation than DrawEllipse().
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="xc">The x-coordinate of the ellipses center.</param>
        /// <param name="yc">The y-coordinate of the ellipses center.</param>
        /// <param name="xr">The radius of the ellipse in x-direction.</param>
        /// <param name="yr">The radius of the ellipse in y-direction.</param>
        /// <param name="color">The color for the line.</param>
        internal static void FillEllipseCentered(this WriteableBitmap bmp, int xc, int yc, int xr, int yr, Color color)
        {
            // Add one to use mul and cheap bit shift for multiplicaltion
            var a = color.A + 1;
            var col = (color.A << 24)
                     | ((byte)((color.R * a) >> 8) << 16)
                     | ((byte)((color.G * a) >> 8) << 8)
                     | ((byte)((color.B * a) >> 8));
            bmp.FillEllipseCentered(xc, yc, xr, yr, col);
        }

        /// <summary>
        /// A Fast Bresenham Type Algorithm For Drawing filled ellipses http://homepage.smc.edu/kennedy_john/belipse.pdf 
        /// Uses a different parameter representation than DrawEllipse().
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="xc">The x-coordinate of the ellipses center.</param>
        /// <param name="yc">The y-coordinate of the ellipses center.</param>
        /// <param name="xr">The radius of the ellipse in x-direction.</param>
        /// <param name="yr">The radius of the ellipse in y-direction.</param>
        /// <param name="color">The color for the line.</param>
        internal static void FillEllipseCentered(this WriteableBitmap bmp, int xc, int yc, int xr, int yr, int color)
        {
            // Use refs for faster access (really important!) speeds up a lot!
            using (var context = bmp.GetBitmapContext())
            {
                FillEllipseCentered(context, xc, yc, xr, yr, color);
            }
        }

        internal static void FillEllipseCentered(BitmapContext context, int xc, int yc, int xr, int yr, int color, BlendMode blendMode = BlendMode.Alpha)
        {
            var pixels = context.Pixels;
            int w = context.PixelWidth;
            int h = context.PixelHeight;            

                // Avoid endless loop
                if (xr < 1 || yr < 1)
                {
                    return;
                }

                // Init vars
                int uh, lh, uy, ly, lx, rx;
                int x = xr;
                int y = 0;
                int xrSqTwo = (xr*xr) << 1;
                int yrSqTwo = (yr*yr) << 1;
                int xChg = yr*yr*(1 - (xr << 1));
                int yChg = xr*xr;
                int err = 0;
                int xStopping = yrSqTwo*xr;
                int yStopping = 0;

                int sa = ((color >> 24) & 0xff);
                int sr = ((color >> 16) & 0xff);
                int sg = ((color >> 8) & 0xff);
                int sb = ((color) & 0xff);

                bool noBlending = blendMode == BlendMode.None || sa == 255;

                // Draw first set of points counter clockwise where tangent line slope > -1.
                while (xStopping >= yStopping)
                {
                    // Draw 4 quadrant points at once
                    uy = yc + y; // Upper half
                    ly = yc - y; // Lower half
                    if (uy < 0) uy = 0; // Clip
                    if (uy >= h) uy = h - 1; // ...
                    if (ly < 0) ly = 0;
                    if (ly >= h) ly = h - 1;
                    uh = uy*w; // Upper half
                    lh = ly*w; // Lower half

                    rx = xc + x;
                    lx = xc - x;
                    if (rx < 0) rx = 0; // Clip
                    if (rx >= w) rx = w - 1; // ...
                    if (lx < 0) lx = 0;
                    if (lx >= w) lx = w - 1;

                    // Draw line
                    for (int i = lx; i <= rx; i++)
                    {
                        // Quadrant II to I (Actually two octants)
                        pixels[i + uh] = noBlending ? color : AlphaBlendColors(pixels[i + uh], sa, sr, sg, sb);

                        // Quadrant III to IV
                        pixels[i + lh] = noBlending ? color : AlphaBlendColors(pixels[i + lh], sa, sr, sg, sb);
                    }

                    y++;
                    yStopping += xrSqTwo;
                    err += yChg;
                    yChg += xrSqTwo;
                    if ((xChg + (err << 1)) > 0)
                    {
                        x--;
                        xStopping -= yrSqTwo;
                        err += xChg;
                        xChg += yrSqTwo;
                    }
                }

                // ReInit vars
                x = 0;
                y = yr;
                uy = yc + y; // Upper half
                ly = yc - y; // Lower half
                if (uy < 0) uy = 0; // Clip
                if (uy >= h) uy = h - 1; // ...
                if (ly < 0) ly = 0;
                if (ly >= h) ly = h - 1;
                uh = uy*w; // Upper half
                lh = ly*w; // Lower half
                xChg = yr*yr;
                yChg = xr*xr*(1 - (yr << 1));
                err = 0;
                xStopping = 0;
                yStopping = xrSqTwo*yr;

                // Draw second set of points clockwise where tangent line slope < -1.
                while (xStopping <= yStopping)
                {
                    // Draw 4 quadrant points at once
                    rx = xc + x;
                    lx = xc - x;
                    if (rx < 0) rx = 0; // Clip
                    if (rx >= w) rx = w - 1; // ...
                    if (lx < 0) lx = 0;
                    if (lx >= w) lx = w - 1;

                    // Draw line
                    for (int i = lx; i <= rx; i++)
                    {
                        // Quadrant II to I (Actually two octants)
                        pixels[i + uh] = noBlending ? color : AlphaBlendColors(pixels[i + uh], sa, sr, sg, sb);

                        // Quadrant III to IV
                        pixels[i + lh] = noBlending ? color : AlphaBlendColors(pixels[i + lh], sa, sr, sg, sb);
                    }

                    x++;
                    xStopping += yrSqTwo;
                    err += xChg;
                    xChg += yrSqTwo;
                    if ((yChg + (err << 1)) > 0)
                    {
                        y--;
                        uy = yc + y; // Upper half
                        ly = yc - y; // Lower half
                        if (uy < 0) uy = 0; // Clip
                        if (uy >= h) uy = h - 1; // ...
                        if (ly < 0) ly = 0;
                        if (ly >= h) ly = h - 1;
                        uh = uy*w; // Upper half
                        lh = ly*w; // Lower half
                        yStopping -= xrSqTwo;
                        err += yChg;
                        yChg += xrSqTwo;
                    }
                }
        }
        
        /// <summary>
        /// Draws a filled polygon. Add the first point also at the end of the array if the line should be closed.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="points">The points of the polygon in x and y pairs, therefore the array is interpreted as (x1, y1, x2, y2, ..., xn, yn).</param>
        /// <param name="color">The color for the line.</param>
        internal static void FillPolygon(this WriteableBitmap bmp, int[] points, Color color)
        {
            // Add one to use mul and cheap bit shift for multiplicaltion
            var a = color.A + 1;
            var col = (color.A << 24)
                     | ((byte)((color.R * a) >> 8) << 16)
                     | ((byte)((color.G * a) >> 8) << 8)
                     | ((byte)((color.B * a) >> 8));
            bmp.FillPolygon(points, col);
        }

        /// <summary>
        /// Draws a filled polygon. Add the first point also at the end of the array if the line should be closed.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="points">The points of the polygon in x and y pairs, therefore the array is interpreted as (x1, y1, x2, y2, ..., xn, yn).</param>
        /// <param name="color">The color for the line.</param>
        /// <param name="blendMode">The blend mode. Default is Alpha</param>
        internal static void FillPolygon(this WriteableBitmap bmp, int[] points, int color, BlendMode blendMode = BlendMode.Alpha)
        {
            // Use refs for faster access (really important!) speeds up a lot!
            int w = bmp.PixelWidth;
            int h = bmp.PixelHeight;

            int sa = ((color >> 24) & 0xff);
            int sr = ((color >> 16) & 0xff);
            int sg = ((color >> 8) & 0xff);
            int sb = ((color) & 0xff);

            bool noBlending = blendMode == BlendMode.None || sa == 255;

            using (var context = bmp.GetBitmapContext())
            {
                var pixels = context.Pixels;
                int pn = points.Length;
                int pnh = points.Length >> 1;
                int[] intersectionsX = new int[pnh];

                // Find y min and max (slightly faster than scanning from 0 to height)
                int yMin = h;
                int yMax = 0;
                for (int i = 1; i < pn; i += 2)
                {
                    int py = points[i];
                    if (py < yMin) yMin = py;
                    if (py > yMax) yMax = py;
                }
                if (yMin < 0) yMin = 0;
                if (yMax >= h) yMax = h - 1;


                // Scan line from min to max
                for (int y = yMin; y <= yMax; y++)
                {
                    // Initial point x, y
                    float vxi = points[0];
                    float vyi = points[1];

                    // Find all intersections
                    // Based on http://alienryderflex.com/polygon_fill/
                    int intersectionCount = 0;
                    for (int i = 2; i < pn; i += 2)
                    {
                        // Next point x, y
                        float vxj = points[i];
                        float vyj = points[i + 1];

                        // Is the scanline between the two points
                        if (vyi < y && vyj >= y
                            || vyj < y && vyi >= y)
                        {
                            // Compute the intersection of the scanline with the edge (line between two points)
                            intersectionsX[intersectionCount++] = (int) (vxi + (y - vyi)/(vyj - vyi)*(vxj - vxi));
                        }
                        vxi = vxj;
                        vyi = vyj;
                    }

                    // Sort the intersections from left to right using Insertion sort 
                    // It's faster than Array.Sort for this small data set
                    int t, j;
                    for (int i = 1; i < intersectionCount; i++)
                    {
                        t = intersectionsX[i];
                        j = i;
                        while (j > 0 && intersectionsX[j - 1] > t)
                        {
                            intersectionsX[j] = intersectionsX[j - 1];
                            j = j - 1;
                        }
                        intersectionsX[j] = t;
                    }

                    // Fill the pixels between the intersections
                    for (int i = 0; i < intersectionCount - 1; i += 2)
                    {
                        int x0 = intersectionsX[i];
                        int x1 = intersectionsX[i + 1];

                        // Check boundary
                        if (x1 > 0 && x0 < w)
                        {
                            if (x0 < 0) x0 = 0;
                            if (x1 >= w) x1 = w - 1;

                            // Fill the pixels
                            for (int x = x0; x <= x1; x++)
                            {
                                int idx = y*w + x;

                                pixels[idx] = noBlending ? color : AlphaBlendColors(pixels[idx], sa, sr, sg, sb);
                            }
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Draws a filled polygon. Add the first point also at the end of the array if the line should be closed.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="points">The points of the polygon in x and y pairs, therefore the array is interpreted as (x1, y1, x2, y2, ..., xn, yn).</param>
        /// <param name="colorCb">The color for the fill.</param>
        /// <param name="blendMode">The blend mode. Default is Alpha</param>
        internal static void FillPolygon(this WriteableBitmap bmp, int[] points, Func<int, int, int> colorCb, BlendMode blendMode = BlendMode.Alpha)
        {
            // Use refs for faster access (really important!) speeds up a lot!
            int w = bmp.PixelWidth;
            int h = bmp.PixelHeight;

            using (var context = bmp.GetBitmapContext())
            {
                var pixels = context.Pixels;
                int pn = points.Length;
                int pnh = points.Length >> 1;
                int[] intersectionsX = new int[pnh];

                // Find y min and max (slightly faster than scanning from 0 to height)
                int yMin = h;
                int yMax = 0;
                for (int i = 1; i < pn; i += 2)
                {
                    int py = points[i];
                    if (py < yMin) yMin = py;
                    if (py > yMax) yMax = py;
                }
                if (yMin < 0) yMin = 0;
                if (yMax >= h) yMax = h - 1;


                // Scan line from min to max
                for (int y = yMin; y <= yMax; y++)
                {
                    // Initial point x, y
                    float vxi = points[0];
                    float vyi = points[1];

                    // Find all intersections
                    // Based on http://alienryderflex.com/polygon_fill/
                    int intersectionCount = 0;
                    for (int i = 2; i < pn; i += 2)
                    {
                        // Next point x, y
                        float vxj = points[i];
                        float vyj = points[i + 1];

                        // Is the scanline between the two points
                        if (vyi < y && vyj >= y
                            || vyj < y && vyi >= y)
                        {
                            // Compute the intersection of the scanline with the edge (line between two points)
                            intersectionsX[intersectionCount++] = (int)(vxi + (y - vyi) / (vyj - vyi) * (vxj - vxi));
                        }
                        vxi = vxj;
                        vyi = vyj;
                    }

                    // Sort the intersections from left to right using Insertion sort 
                    // It's faster than Array.Sort for this small data set
                    int t, j;
                    for (int i = 1; i < intersectionCount; i++)
                    {
                        t = intersectionsX[i];
                        j = i;
                        while (j > 0 && intersectionsX[j - 1] > t)
                        {
                            intersectionsX[j] = intersectionsX[j - 1];
                            j = j - 1;
                        }
                        intersectionsX[j] = t;
                    }

                    // Fill the pixels between the intersections
                    for (int i = 0; i < intersectionCount - 1; i += 2)
                    {
                        int x0 = intersectionsX[i];
                        int x1 = intersectionsX[i + 1];

                        // Check boundary
                        if (x1 > 0 && x0 < w)
                        {
                            if (x0 < 0) x0 = 0;
                            if (x1 >= w) x1 = w - 1;

                            // Fill the pixels
                            for (int x = x0; x <= x1; x++)
                            {
                                int idx = y * w + x;

                                int color = colorCb(x, y);

                                int sa = ((color >> 24) & 0xff);
                                int sr = ((color >> 16) & 0xff);
                                int sg = ((color >> 8) & 0xff);
                                int sb = ((color) & 0xff);

                                var noBlending = blendMode == BlendMode.None || sa == 255;

                                pixels[idx] = noBlending ? color : AlphaBlendColors(pixels[idx], sa, sr, sg, sb);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Draws a filled quad.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="x1">The x-coordinate of the 1st point.</param>
        /// <param name="y1">The y-coordinate of the 1st point.</param>
        /// <param name="x2">The x-coordinate of the 2nd point.</param>
        /// <param name="y2">The y-coordinate of the 2nd point.</param>
        /// <param name="x3">The x-coordinate of the 3rd point.</param>
        /// <param name="y3">The y-coordinate of the 3rd point.</param>
        /// <param name="x4">The x-coordinate of the 4th point.</param>
        /// <param name="y4">The y-coordinate of the 4th point.</param>
        /// <param name="color">The color.</param>
        internal static void FillQuad(this WriteableBitmap bmp, int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4, Color color)
        {
            // Add one to use mul and cheap bit shift for multiplicaltion
            var a = color.A + 1;
            var col = (color.A << 24)
                     | ((byte)((color.R * a) >> 8) << 16)
                     | ((byte)((color.G * a) >> 8) << 8)
                     | ((byte)((color.B * a) >> 8));
            bmp.FillQuad(x1, y1, x2, y2, x3, y3, x4, y4, col);
        }

        /// <summary>
        /// Draws a filled quad.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="x1">The x-coordinate of the 1st point.</param>
        /// <param name="y1">The y-coordinate of the 1st point.</param>
        /// <param name="x2">The x-coordinate of the 2nd point.</param>
        /// <param name="y2">The y-coordinate of the 2nd point.</param>
        /// <param name="x3">The x-coordinate of the 3rd point.</param>
        /// <param name="y3">The y-coordinate of the 3rd point.</param>
        /// <param name="x4">The x-coordinate of the 4th point.</param>
        /// <param name="y4">The y-coordinate of the 4th point.</param>
        /// <param name="color">The color.</param>
        internal static void FillQuad(this WriteableBitmap bmp, int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4, int color)
        {
            bmp.FillPolygon(new int[] { x1, y1, x2, y2, x3, y3, x4, y4, x1, y1 }, color);
        }

        /// <summary>
        /// Draws a filled triangle.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="x1">The x-coordinate of the 1st point.</param>
        /// <param name="y1">The y-coordinate of the 1st point.</param>
        /// <param name="x2">The x-coordinate of the 2nd point.</param>
        /// <param name="y2">The y-coordinate of the 2nd point.</param>
        /// <param name="x3">The x-coordinate of the 3rd point.</param>
        /// <param name="y3">The y-coordinate of the 3rd point.</param>
        /// <param name="color">The color.</param>
        internal static void FillTriangle(this WriteableBitmap bmp, int x1, int y1, int x2, int y2, int x3, int y3, Color color)
        {
            // Add one to use mul and cheap bit shift for multiplicaltion
            var a = color.A + 1;
            var col = (color.A << 24)
                     | ((byte)((color.R * a) >> 8) << 16)
                     | ((byte)((color.G * a) >> 8) << 8)
                     | ((byte)((color.B * a) >> 8));
            bmp.FillTriangle(x1, y1, x2, y2, x3, y3, col);
        }

        /// <summary>
        /// Draws a filled triangle.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="x1">The x-coordinate of the 1st point.</param>
        /// <param name="y1">The y-coordinate of the 1st point.</param>
        /// <param name="x2">The x-coordinate of the 2nd point.</param>
        /// <param name="y2">The y-coordinate of the 2nd point.</param>
        /// <param name="x3">The x-coordinate of the 3rd point.</param>
        /// <param name="y3">The y-coordinate of the 3rd point.</param>
        /// <param name="color">The color.</param>
        internal static void FillTriangle(this WriteableBitmap bmp, int x1, int y1, int x2, int y2, int x3, int y3, int color)
        {
            bmp.FillPolygon(new int[] { x1, y1, x2, y2, x3, y3, x1, y1 }, color);
        }


        
        /// <summary>
        /// Draws a filled, cubic Beziér spline defined by start, end and two control points.
        /// </summary>
        /// <param name="x1">The x-coordinate of the start point.</param>
        /// <param name="y1">The y-coordinate of the start point.</param>
        /// <param name="cx1">The x-coordinate of the 1st control point.</param>
        /// <param name="cy1">The y-coordinate of the 1st control point.</param>
        /// <param name="cx2">The x-coordinate of the 2nd control point.</param>
        /// <param name="cy2">The y-coordinate of the 2nd control point.</param>
        /// <param name="x2">The x-coordinate of the end point.</param>
        /// <param name="y2">The y-coordinate of the end point.</param>
        /// <param name="color">The color.</param>
        /// <param name="context">The bitmap context.</param>
        /// <param name="w">The width of the bitmap.</param>
        /// <param name="h">The height of the bitmap.</param> 
        private static System.Collections.Generic.List<int> ComputeBezierPoints(int x1, int y1, int cx1, int cy1, int cx2, int cy2, int x2, int y2, int color, BitmapContext context, int w, int h)
        {
            var pixels = context.Pixels;

            // Determine distances between controls points (bounding rect) to find the optimal stepsize
            var minX = Math.Min(x1, Math.Min(cx1, Math.Min(cx2, x2)));
            var minY = Math.Min(y1, Math.Min(cy1, Math.Min(cy2, y2)));
            var maxX = Math.Max(x1, Math.Max(cx1, Math.Max(cx2, x2)));
            var maxY = Math.Max(y1, Math.Max(cy1, Math.Max(cy2, y2)));

            // Get slope
            var lenx = maxX - minX;
            var len = maxY - minY;
            if (lenx > len)
            {
                len = lenx;
            }

            // Prevent divison by zero
            var list = new System.Collections.Generic.List<int>();
            if (len != 0)
            {
                // Init vars
                var step = StepFactor / len;
                int tx = x1;
                int ty = y1;

                // Interpolate
                for (var t = 0f; t <= 1; t += step)
                {
                    var tSq = t * t;
                    var t1 = 1 - t;
                    var t1Sq = t1 * t1;

                    tx = (int)(t1 * t1Sq * x1 + 3 * t * t1Sq * cx1 + 3 * t1 * tSq * cx2 + t * tSq * x2);
                    ty = (int)(t1 * t1Sq * y1 + 3 * t * t1Sq * cy1 + 3 * t1 * tSq * cy2 + t * tSq * y2);

                    list.Add(tx);
                    list.Add(ty);
                }

                // Prevent rounding gap
                list.Add(x2);
                list.Add(y2);
            }
            return list;
        }

        /// <summary>
        /// Draws a series of filled, cubic Beziér splines each defined by start, end and two control points. 
        /// The ending point of the previous curve is used as starting point for the next. 
        /// Therfore the inital curve needs four points and the subsequent 3 (2 control and 1 end point).
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="points">The points for the curve in x and y pairs, therefore the array is interpreted as (x1, y1, cx1, cy1, cx2, cy2, x2, y2, cx3, cx4 ..., xn, yn).</param>
        /// <param name="color">The color for the spline.</param>
        internal static void FillBeziers(this WriteableBitmap bmp, int[] points, Color color)
        {
            // Add one to use mul and cheap bit shift for multiplicaltion
            var a = color.A + 1;
            var col = (color.A << 24)
                     | ((byte)((color.R * a) >> 8) << 16)
                     | ((byte)((color.G * a) >> 8) << 8)
                     | ((byte)((color.B * a) >> 8));
            bmp.FillBeziers(points, col);
        }

        /// <summary>
        /// Draws a series of filled, cubic Beziér splines each defined by start, end and two control points. 
        /// The ending point of the previous curve is used as starting point for the next. 
        /// Therfore the inital curve needs four points and the subsequent 3 (2 control and 1 end point).
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="points">The points for the curve in x and y pairs, therefore the array is interpreted as (x1, y1, cx1, cy1, cx2, cy2, x2, y2, cx3, cx4 ..., xn, yn).</param>
        /// <param name="color">The color for the spline.</param>
        internal static void FillBeziers(this WriteableBitmap bmp, int[] points, int color)
        {
            // Use refs for faster access (really important!) speeds up a lot!
            int w = bmp.PixelWidth;
            int h = bmp.PixelHeight;

            using (var context = bmp.GetBitmapContext())
            {
                // Compute Beziér curve
                int x1 = points[0];
                int y1 = points[1];
                int x2, y2;
                var list = new System.Collections.Generic.List<int>();
                for (int i = 2; i + 5 < points.Length; i += 6)
                {
                    x2 = points[i + 4];
                    y2 = points[i + 5];
                    list.AddRange(ComputeBezierPoints(x1, y1, points[i], points[i + 1], points[i + 2], points[i + 3], x2,
                                                      y2, color, context, w, h));
                    x1 = x2;
                    y1 = y2;
                }

                // Fill
                bmp.FillPolygon(list.ToArray(), color);
            }
        }


        
        /// <summary>
        /// Computes the discrete segment points of a Cardinal spline (cubic) defined by four control points.
        /// </summary>
        /// <param name="x1">The x-coordinate of the 1st control point.</param>
        /// <param name="y1">The y-coordinate of the 1st control point.</param>
        /// <param name="x2">The x-coordinate of the 2nd control point.</param>
        /// <param name="y2">The y-coordinate of the 2nd control point.</param>
        /// <param name="x3">The x-coordinate of the 3rd control point.</param>
        /// <param name="y3">The y-coordinate of the 3rd control point.</param>
        /// <param name="x4">The x-coordinate of the 4th control point.</param>
        /// <param name="y4">The y-coordinate of the 4th control point.</param>
        /// <param name="tension">The tension of the curve defines the shape. Usually between 0 and 1. 0 would be a straight line.</param>
        /// <param name="color">The color.</param>
        /// <param name="context">The context containing the pixels array.</param>
        /// <param name="w">The width of the bitmap.</param>
        /// <param name="h">The height of the bitmap.</param> 
        private static System.Collections.Generic.List<int> ComputeSegmentPoints(int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4, float tension, int color, BitmapContext context, int w, int h)
        {
            var pixels = context.Pixels;

            // Determine distances between controls points (bounding rect) to find the optimal stepsize
            var minX = Math.Min(x1, Math.Min(x2, Math.Min(x3, x4)));
            var minY = Math.Min(y1, Math.Min(y2, Math.Min(y3, y4)));
            var maxX = Math.Max(x1, Math.Max(x2, Math.Max(x3, x4)));
            var maxY = Math.Max(y1, Math.Max(y2, Math.Max(y3, y4)));

            // Get slope
            var lenx = maxX - minX;
            var len = maxY - minY;
            if (lenx > len)
            {
                len = lenx;
            }

            // Prevent divison by zero
            var list = new System.Collections.Generic.List<int>();
            if (len != 0)
            {
                // Init vars
                var step = StepFactor / len;

                // Calculate factors
                var sx1 = tension * (x3 - x1);
                var sy1 = tension * (y3 - y1);
                var sx2 = tension * (x4 - x2);
                var sy2 = tension * (y4 - y2);
                var ax = sx1 + sx2 + 2 * x2 - 2 * x3;
                var ay = sy1 + sy2 + 2 * y2 - 2 * y3;
                var bx = -2 * sx1 - sx2 - 3 * x2 + 3 * x3;
                var by = -2 * sy1 - sy2 - 3 * y2 + 3 * y3;

                // Interpolate
                for (var t = 0f; t <= 1; t += step)
                {
                    var tSq = t * t;

                    int tx = (int)(ax * tSq * t + bx * tSq + sx1 * t + x2);
                    int ty = (int)(ay * tSq * t + by * tSq + sy1 * t + y2);

                    list.Add(tx);
                    list.Add(ty);
                }

                // Prevent rounding gap
                list.Add(x3);
                list.Add(y3);
            }
            return list;
        }

        /// <summary>
        /// Draws a filled Cardinal spline (cubic) defined by a point collection. 
        /// The cardinal spline passes through each point in the collection.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="points">The points for the curve in x and y pairs, therefore the array is interpreted as (x1, y1, x2, y2, x3, y3, x4, y4, x1, x2 ..., xn, yn).</param>
        /// <param name="tension">The tension of the curve defines the shape. Usually between 0 and 1. 0 would be a straight line.</param>
        /// <param name="color">The color for the spline.</param>
        internal static void FillCurve(this WriteableBitmap bmp, int[] points, float tension, Color color)
        {
            // Add one to use mul and cheap bit shift for multiplicaltion
            var a = color.A + 1;
            var col = (color.A << 24)
                     | ((byte)((color.R * a) >> 8) << 16)
                     | ((byte)((color.G * a) >> 8) << 8)
                     | ((byte)((color.B * a) >> 8));
            bmp.FillCurve(points, tension, col);
        }

        /// <summary>
        /// Draws a filled Cardinal spline (cubic) defined by a point collection. 
        /// The cardinal spline passes through each point in the collection.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="points">The points for the curve in x and y pairs, therefore the array is interpreted as (x1, y1, x2, y2, x3, y3, x4, y4, x1, x2 ..., xn, yn).</param>
        /// <param name="tension">The tension of the curve defines the shape. Usually between 0 and 1. 0 would be a straight line.</param>
        /// <param name="color">The color for the spline.</param>
        internal static void FillCurve(this WriteableBitmap bmp, int[] points, float tension, int color)
        {
            // Use refs for faster access (really important!) speeds up a lot!
            int w = bmp.PixelWidth;
            int h = bmp.PixelHeight;

            using (var context = bmp.GetBitmapContext())
            {
                // First segment
                var list = ComputeSegmentPoints(points[0], points[1], points[0], points[1], points[2], points[3], points[4], points[5], tension, color, context, w, h);

                // Middle segments
                int i;
                for (i = 2; i < points.Length - 4; i += 2)
                {
                    list.AddRange(ComputeSegmentPoints(points[i - 2], points[i - 1], points[i], points[i + 1], points[i + 2], points[i + 3], points[i + 4], points[i + 5], tension, color, context, w, h));
                }

                // Last segment
                list.AddRange(ComputeSegmentPoints(points[i - 2], points[i - 1], points[i], points[i + 1], points[i + 2], points[i + 3], points[i + 2], points[i + 3], tension, color, context, w, h));

                // Fill
                bmp.FillPolygon(list.ToArray(), color);
            }
        }

        /// <summary>
        /// Draws a filled, closed Cardinal spline (cubic) defined by a point collection. 
        /// The cardinal spline passes through each point in the collection.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="points">The points for the curve in x and y pairs, therefore the array is interpreted as (x1, y1, x2, y2, x3, y3, x4, y4, x1, x2 ..., xn, yn).</param>
        /// <param name="tension">The tension of the curve defines the shape. Usually between 0 and 1. 0 would be a straight line.</param>
        /// <param name="color">The color for the spline.</param>
        internal static void FillCurveClosed(this WriteableBitmap bmp, int[] points, float tension, Color color)
        {
            // Add one to use mul and cheap bit shift for multiplicaltion
            var a = color.A + 1;
            var col = (color.A << 24)
                     | ((byte)((color.R * a) >> 8) << 16)
                     | ((byte)((color.G * a) >> 8) << 8)
                     | ((byte)((color.B * a) >> 8));
            bmp.FillCurveClosed(points, tension, col);
        }

        /// <summary>
        /// Draws a filled, closed Cardinal spline (cubic) defined by a point collection. 
        /// The cardinal spline passes through each point in the collection.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="points">The points for the curve in x and y pairs, therefore the array is interpreted as (x1, y1, x2, y2, x3, y3, x4, y4, x1, x2 ..., xn, yn).</param>
        /// <param name="tension">The tension of the curve defines the shape. Usually between 0 and 1. 0 would be a straight line.</param>
        /// <param name="color">The color for the spline.</param>
        internal static void FillCurveClosed(this WriteableBitmap bmp, int[] points, float tension, int color)
        {
            // Use refs for faster access (really important!) speeds up a lot!
            int w = bmp.PixelWidth;
            int h = bmp.PixelHeight;

            using (var context = bmp.GetBitmapContext())
            {
                int pn = points.Length;

                // First segment
                var list = ComputeSegmentPoints(points[pn - 2], points[pn - 1], points[0], points[1], points[2], points[3], points[4], points[5], tension, color, context, w, h);

                // Middle segments
                int i;
                for (i = 2; i < pn - 4; i += 2)
                {
                    list.AddRange(ComputeSegmentPoints(points[i - 2], points[i - 1], points[i], points[i + 1], points[i + 2], points[i + 3], points[i + 4], points[i + 5], tension, color, context, w, h));
                }

                // Last segment
                list.AddRange(ComputeSegmentPoints(points[i - 2], points[i - 1], points[i], points[i + 1], points[i + 2], points[i + 3], points[0], points[1], tension, color, context, w, h));

                // Last-to-First segment
                list.AddRange(ComputeSegmentPoints(points[i], points[i + 1], points[i + 2], points[i + 3], points[0], points[1], points[2], points[3], tension, color, context, w, h));

                // Fill
                bmp.FillPolygon(list.ToArray(), color);
            }
        }




    }
}