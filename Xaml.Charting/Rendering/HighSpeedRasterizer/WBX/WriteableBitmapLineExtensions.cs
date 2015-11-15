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
using Ecng.Xaml.Charting.Common.Extensions;

namespace System.Windows.Media.Imaging
{
    internal
#if !SAFECODE
#if !SILVERLIGHT 
        unsafe 
#endif
#endif
 static partial class WriteableBitmapExtensions
    {
        internal static void DrawPennedLine(BitmapContext context, int w, int h, int x1, int y1, int x2, int y2, BitmapContext pen)
        {
            // Edge case where lines that went out of vertical bounds clipped instead of dissapear
            if ((y1 < 0 && y2 < 0) || (y1 > h && y2 > h))
                return;

            if (x1 == x2 && y1 == y2)
                return;

            int size = pen.WriteableBitmap.PixelWidth;
            int offset = size/2;

            var srcRect = new Rect(0, 0, size, size);

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

            var destRect = new Rect(x - offset, y - offset, size, size);

            if (y < h && y >= 0 && x < w && x >= 0)
            {
                Blit(context, w, h, destRect, pen, srcRect, size);
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
                    destRect.X = x - offset;
                    destRect.Y = y - offset;

                    Blit(context, w, h, destRect, pen, srcRect, size);
                }
            }
        }

        /// <summary>
        /// Bitfields used to partition the space into 9 regiond
        /// </summary>
        private const byte INSIDE = 0; // 0000
        private const byte LEFT = 1;   // 0001
        private const byte RIGHT = 2;  // 0010
        private const byte BOTTOM = 4; // 0100
        private const byte TOP = 8;    // 1000

        /// <summary>
        /// Compute the bit code for a point (x, y) using the clip rectangle
        /// bounded diagonally by (xmin, ymin), and (xmax, ymax)
        /// ASSUME THAT xmax , xmin , ymax and ymin are global constants.
        /// </summary>
        /// <param name="extents">The extents.</param>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns></returns>
        private static byte ComputeOutCode(Rect extents, double x, double y)
        {
            // initialised as being inside of clip window
            byte code = INSIDE;

            if (x < extents.Left)           // to the left of clip window
                code |= LEFT;
            else if (x > extents.Right)     // to the right of clip window
                code |= RIGHT;
            if (y > extents.Bottom)         // below the clip window
                code |= BOTTOM;
            else if (y < extents.Top)       // above the clip window
                code |= TOP;

            return code;
        }

        internal static bool CohenSutherlandLineClipWithViewPortOffset(Rect viewPort, ref float xi0, ref float yi0, ref float xi1, ref float yi1, int offset)
        {
            Rect viewPortWithOffset = new Rect(viewPort.X - offset, viewPort.Y - offset, viewPort.Width + 2*offset, viewPort.Height + 2*offset);

            return CohenSutherlandLineClip(viewPortWithOffset, ref xi0, ref yi0, ref xi1, ref yi1);
        }

        internal static bool CohenSutherlandLineClip(Rect extents, ref float xi0, ref float yi0, ref float xi1, ref float yi1)
        {
            // Fix #SC-1555: Log(0) issue
            // CohenSuzerland line clipping algorithm returns NaN when point has infinity value
            double x0 = xi0.ClipToInt();
            double y0 = yi0.ClipToInt();
            double x1 = xi1.ClipToInt();
            double y1 = yi1.ClipToInt();

            var isValid = CohenSutherlandLineClip(extents, ref x0, ref y0, ref x1, ref y1);

            // Update the clipped line
            xi0 = (float)x0;
            yi0 = (float)y0;
            xi1 = (float)x1;
            yi1 = (float)y1;

            return isValid;
        }

        internal static bool CohenSutherlandLineClip(Rect extents, ref int xi0, ref int yi0, ref int xi1, ref int yi1)
        {
            double x0 = xi0;
            double y0 = yi0;
            double x1 = xi1;
            double y1 = yi1;

            var isValid = CohenSutherlandLineClip(extents, ref x0, ref y0, ref x1, ref y1);

            // Update the clipped line
            xi0 = (int)x0;
            yi0 = (int)y0;
            xi1 = (int)x1;
            yi1 = (int)y1;

            return isValid;
        }

        /// <summary>
        /// Cohen–Sutherland clipping algorithm clips a line from
        /// P0 = (x0, y0) to P1 = (x1, y1) against a rectangle with 
        /// diagonal from (xmin, ymin) to (xmax, ymax).
        /// </summary>
        /// <remarks>See http://en.wikipedia.org/wiki/Cohen%E2%80%93Sutherland_algorithm for details</remarks>
        /// <returns>a list of two points in the resulting clipped line, or zero</returns>
        internal static bool CohenSutherlandLineClip(Rect extents, ref double x0, ref double y0, ref double x1, ref double y1)
        {
            // compute outcodes for P0, P1, and whatever point lies outside the clip rectangle
            byte outcode0 = ComputeOutCode(extents, x0, y0);
            byte outcode1 = ComputeOutCode(extents, x1, y1);

            // No clipping if both points lie inside viewport
            if (outcode0 == INSIDE && outcode1 == INSIDE)
                return true;

            bool isValid = false;

            while (true)
            {
                // Bitwise OR is 0. Trivially accept and get out of loop
                if ((outcode0 | outcode1) == 0)
                {
                    isValid = true;
                    break;
                }
                // Bitwise AND is not 0. Trivially reject and get out of loop
                else if ((outcode0 & outcode1) != 0)
                {
                    break;
                }
                else
                {
                    // failed both tests, so calculate the line segment to clip
                    // from an outside point to an intersection with clip edge
                    double x = x1, y = y1;
                    
                    // At least one endpoint is outside the clip rectangle; pick it.
                    byte outcodeOut = (outcode0 != 0) ? outcode0 : outcode1;

                    // Now find the intersection point;
                    // use formulas y = y0 + slope * (x - x0), x = x0 + (1 / slope) * (y - y0)
                    if ((outcodeOut & TOP) != 0)
                    {  
                        // point is above the clip rectangle
                        if (!double.IsInfinity(y0))
                        {
                            x = x0 + (x1 - x0)*(extents.Top - y0)/(y1 - y0);
                        }
                        y = extents.Top;
                    }
                    else if ((outcodeOut & BOTTOM) != 0)
                    {
                        // point is below the clip rectangle
                        if (!double.IsInfinity(y0))
                        {
                            x = x0 + (x1 - x0) * (extents.Bottom - y0) / (y1 - y0);
                        }
                        y = extents.Bottom;
                    }
                    else if ((outcodeOut & RIGHT) != 0)
                    {  
                        // point is to the right of clip rectangle
                        if (!double.IsInfinity(x0))
                        {
                            y = y0 + (y1 - y0) * (extents.Right - x0) / (x1 - x0);
                        }
                        x = extents.Right;
                    }
                    else if ((outcodeOut & LEFT) != 0)
                    {  
                        // point is to the left of clip rectangle
                        if (!double.IsInfinity(x0))
                        {
                            y = y0 + (y1 - y0) * (extents.Left - x0) / (x1 - x0);
                        }
                        x = extents.Left;
                    }
                    else
                    {
                        x = double.NaN;
                        y = double.NaN;
                    }

                    // Now we move outside point to intersection point to clip
                    // and get ready for next pass.
                    if (outcodeOut == outcode0)
                    {
                        x0 = x;
                        y0 = y;
                        outcode0 = ComputeOutCode(extents, x0, y0);
                    }
                    else
                    {
                        x1 = x;
                        y1 = y;
                        outcode1 = ComputeOutCode(extents, x1, y1);
                    }
                }
            }

            return isValid;
        }


        
        /// <summary>
        /// Alpha blends 2 premultiplied colors with each other
        /// </summary>
        /// <param name="sa">Source alpha color component</param>
        /// <param name="sr">Premultiplied source red color component</param>
        /// <param name="sg">Premultiplied source green color component</param>
        /// <param name="sb">Premultiplied source blue color component</param>
        /// <param name="destPixel">Premultiplied destination color</param>
        /// <returns>Premultiplied blended color value</returns>
        public static int AlphaBlend(int sa, int sr, int sg, int sb, int destPixel)
        {
            int dr, dg, db;
            int da;
            da = ((destPixel >> 24) & 0xff);
            dr = ((destPixel >> 16) & 0xff);
            dg = ((destPixel >> 8) & 0xff);
            db = ((destPixel) & 0xff);
            
            destPixel = ((sa + (((da * (255 - sa)) * 0x8081) >> 23)) << 24) |
               ((sr + (((dr * (255 - sa)) * 0x8081) >> 23)) << 16) |
               ((sg + (((dg * (255 - sa)) * 0x8081) >> 23)) << 8) |
               ((sb + (((db * (255 - sa)) * 0x8081) >> 23)));

            return destPixel;
        }

        /// <summary>
        /// Draws an anti-aliased, alpha blended, colored line by connecting two points using Wu's antialiasing algorithm
        /// Uses the pixels array and the width directly for best performance.
        /// </summary>
        /// <param name="context">An array containing the pixels as int RGBA value.</param>
        /// <param name="pixelWidth">The width of one scanline in the pixels array.</param>
        /// <param name="pixelHeight">The height of the bitmap.</param>
        /// <param name="X0">The x0.</param>
        /// <param name="Y0">The y0.</param>
        /// <param name="X1">The x1.</param>
        /// <param name="Y1">The y1.</param>
        /// <param name="sa">Alpha color component</param>
        /// <param name="sr">Premultiplied red color component</param>
        /// <param name="sg">Premultiplied green color component</param>
        /// <param name="sb">Premultiplied blue color component</param>
        public static void DrawWuLine(BitmapContext context, int pixelWidth, int pixelHeight, short X0, short Y0, short X1, short Y1, int sa, int sr, int sg, int sb)
        {
            var pixels = context.Pixels;

            const ushort INTENSITY_BITS = 8;
            const short NUM_LEVELS = 1 << INTENSITY_BITS; // 256
            // mask used to compute 1-value by doing (value XOR mask)
            const ushort WEIGHT_COMPLEMENT_MASK = NUM_LEVELS - 1; // 255
            // # of bits by which to shift ErrorAcc to get intensity level 
            const ushort INTENSITY_SHIFT = (ushort)(16 - INTENSITY_BITS); // 8

            ushort ErrorAdj, ErrorAcc;
            ushort ErrorAccTemp, Weighting;
            short DeltaX, DeltaY, Temp, XDir;

            // ensure line runs from top to bottom
            if (Y0 > Y1)
            {
                Temp = Y0; Y0 = Y1; Y1 = Temp;
                Temp = X0; X0 = X1; X1 = Temp;
            }

            // draw initial pixle, which is always intersected by line to it's at 100% intensity
            pixels[Y0 * pixelWidth + X0] = AlphaBlend(sa, sr, sg, sb, pixels[Y0 * pixelWidth + X0]);
            //bitmap.SetPixel(X0, Y0, BaseColor);

            DeltaX = (short)(X1 - X0);
            if (DeltaX >= 0)
            {
                XDir = 1;
            }
            else
            {
                XDir = -1;
                DeltaX = (short)-DeltaX; /* make DeltaX positive */
            }

            // Special-case horizontal, vertical, and diagonal lines, which
            // require no weighting because they go right through the center of
            // every pixel; also avoids division by zero later
            DeltaY = (short)(Y1 - Y0);
            if (DeltaY == 0) // if horizontal line
            {
                while (DeltaX-- != 0)
                {
                    X0 += XDir;
                    pixels[Y0 * pixelWidth + X0] = AlphaBlend(sa, sr, sg, sb, pixels[Y0 * pixelWidth + X0]);
                }
                return;
            }

            if (DeltaX == 0) // if vertical line 
            {
                do
                {
                    Y0++;
                    pixels[Y0 * pixelWidth + X0] = AlphaBlend(sa, sr, sg, sb, pixels[Y0 * pixelWidth + X0]);
                } while (--DeltaY != 0);
                return;
            }

            if (DeltaX == DeltaY) // diagonal line
            {
                do
                {
                    X0 += XDir;
                    Y0++;
                    pixels[Y0 * pixelWidth + X0] = AlphaBlend(sa, sr, sg, sb, pixels[Y0 * pixelWidth + X0]);
                } while (--DeltaY != 0);
                return;
            }

            // Line is not horizontal, diagonal, or vertical
            ErrorAcc = 0;  // initialize the line error accumulator to 0

            // Is this an X-major or Y-major line? 
            if (DeltaY > DeltaX)
            {
                // Y-major line; calculate 16-bit fixed-point fractional part of a
                // pixel that X advances each time Y advances 1 pixel, truncating the
                // result so that we won't overrun the endpoint along the X axis 
                ErrorAdj = (ushort)(((ulong)DeltaX << 16) / (ulong)DeltaY);

                // Draw all pixels other than the first and last 
                while (--DeltaY != 0)
                {
                    ErrorAccTemp = ErrorAcc;   // remember currrent accumulated error 
                    ErrorAcc += ErrorAdj;      // calculate error for next pixel 
                    if (ErrorAcc <= ErrorAccTemp)
                    {
                        // The error accumulator turned over, so advance the X coord */
                        X0 += XDir;
                    }
                    Y0++; /* Y-major, so always advance Y */
                    // The IntensityBits most significant bits of ErrorAcc give us the
                    // intensity weighting for this pixel, and the complement of the
                    // weighting for the paired pixel 
                    Weighting = (ushort)(ErrorAcc >> INTENSITY_SHIFT);

                    int weight = Weighting ^ WEIGHT_COMPLEMENT_MASK;
                    pixels[Y0 * pixelWidth + X0] = AlphaBlend(sa, (sr * weight) >> 8, (sg * weight) >> 8, (sb * weight) >> 8, pixels[Y0 * pixelWidth + X0]);

                    weight = Weighting;
                    pixels[Y0 * pixelWidth + X0 + XDir] = AlphaBlend(sa, (sr * weight) >> 8, (sg * weight) >> 8, (sb * weight) >> 8, pixels[Y0 * pixelWidth + X0 + XDir]);

                    //bitmap.SetPixel(X0, Y0, 255 - (BaseColor + Weighting));
                    //bitmap.SetPixel(X0 + XDir, Y0, 255 - (BaseColor + (Weighting ^ WeightingComplementMask)));
                }

                // Draw the final pixel, which is always exactly intersected by the line and so needs no weighting
                pixels[Y1 * pixelWidth + X1] = AlphaBlend(sa, sr, sg, sb, pixels[Y1 * pixelWidth + X1]);
                //bitmap.SetPixel(X1, Y1, BaseColor);
                return;
            }
            // It's an X-major line; calculate 16-bit fixed-point fractional part of a
            // pixel that Y advances each time X advances 1 pixel, truncating the
            // result to avoid overrunning the endpoint along the X axis */
            ErrorAdj = (ushort)(((ulong)DeltaY << 16) / (ulong)DeltaX);

            // Draw all pixels other than the first and last 
            while (--DeltaX != 0)
            {
                ErrorAccTemp = ErrorAcc;   // remember currrent accumulated error 
                ErrorAcc += ErrorAdj;      // calculate error for next pixel 
                if (ErrorAcc <= ErrorAccTemp) // if error accumulator turned over
                {
                    // advance the Y coord
                    Y0++;
                }
                X0 += XDir; // X-major, so always advance X 
                // The IntensityBits most significant bits of ErrorAcc give us the
                // intensity weighting for this pixel, and the complement of the
                // weighting for the paired pixel 
                Weighting = (ushort)(ErrorAcc >> INTENSITY_SHIFT);

                int weight = Weighting ^ WEIGHT_COMPLEMENT_MASK;
                pixels[Y0 * pixelWidth + X0] = AlphaBlend(sa, (sr * weight) >> 8, (sg * weight) >> 8, (sb * weight) >> 8, pixels[Y0 * pixelWidth + X0]);

                weight = Weighting;
                pixels[(Y0 + 1) * pixelWidth + X0] = AlphaBlend(sa, (sr * weight) >> 8, (sg * weight) >> 8, (sb * weight) >> 8, pixels[(Y0 + 1) * pixelWidth + X0]);

                //bitmap.SetPixel(X0, Y0, 255 - (BaseColor + Weighting));
                //bitmap.SetPixel(X0, Y0 + 1,
                //      255 - (BaseColor + (Weighting ^ WeightingComplementMask)));
            }
            // Draw the final pixel, which is always exactly intersected by the line and thus needs no weighting 
            pixels[Y1 * pixelWidth + X1] = AlphaBlend(sa, sr, sg, sb, pixels[Y1 * pixelWidth + X1]);
            //bitmap.SetPixel(X1, Y1, BaseColor);
        }

    }
}
