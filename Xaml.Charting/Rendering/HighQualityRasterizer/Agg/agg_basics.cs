//----------------------------------------------------------------------------
// Anti-Grain Geometry - Version 2.4
// Copyright (C) 2002-2005 Maxim Shemanarev (http://www.antigrain.com)
//
// C# Port port by: Lars Brubaker
//                  larsbrubaker@gmail.com
// Copyright (C) 2007
//
// Permission to copy, use, modify, sell and distribute this software 
// is granted provided this copyright notice appears in all copies. 
// This software is provided "as is" without express or implied
// warranty, and with no claim as to its suitability for any purpose.
//
//----------------------------------------------------------------------------
// Contact: mcseem@antigrain.com
//          mcseemagg@yahoo.com
//          http://www.antigrain.com
//----------------------------------------------------------------------------
//#define USE_UNSAFE // no real code for this yet

using System;
using System.IO;
using MatterHackers.VectorMath;

namespace MatterHackers.Agg
{
    static internal class DebugFile
    {
        static bool m_FileOpenedOnce = false;

        public static void Print(String message)
        {
            FileStream file;
            if (m_FileOpenedOnce)
            {
                file = new FileStream("test.txt", FileMode.Append, FileAccess.Write);
            }
            else
            {
                file = new FileStream("test.txt", FileMode.Create, FileAccess.Write);
                m_FileOpenedOnce = true;
            }
            StreamWriter sw = new StreamWriter(file);
            sw.Write(message);
            sw.Close();
            file.Close();
        }
    };
    
    static internal class agg_basics
    {
        //----------------------------------------------------------filling_rule_e
        public enum filling_rule_e
        {
            fill_non_zero,
            fill_even_odd
        };

        public static void memcpy(Byte[] dest, int destIndex, Byte[] source, int sourceIndex, int Count)
        {
#if USE_UNSAFE
#else
            for (int i = 0; i < Count; i++)
            {
                dest[destIndex + i] = source[sourceIndex + i];
            }
#endif
        }

        public static void memcpy(int[] dest, int destIndex, int[] source, int sourceIndex, int Count)
        {
            for (int i = 0; i < Count; i++)
            {
                dest[destIndex + i] = source[sourceIndex + i];
            }
        }

        public static void memcpy(float[] dest, int destIndex, float[] source, int sourceIndex, int count)
        {
            for (int i = 0; i < count; i++)
            {
                dest[destIndex++] = source[sourceIndex++];
            }
        }

        public static void memmove(Byte[] dest, int destIndex, Byte[] source, int sourceIndex, int Count)
        {
            if (source != dest
                || destIndex < sourceIndex)
            {
                memcpy(dest, destIndex, source, sourceIndex, Count);
            }
            else
            {
                throw new Exception("this code needs to be tested");
                /*
                for (int i = Count-1; i > 0; i--)
                {
                    dest[destIndex + i] = source[sourceIndex + i];
                }
                 */
            }

        }

        public static void memmove(int[] dest, int destIndex, int[] source, int sourceIndex, int Count)
        {
            if (source != dest
                || destIndex < sourceIndex)
            {
                memcpy(dest, destIndex, source, sourceIndex, Count);
            }
            else
            {
                throw new Exception("this code needs to be tested");
                /*
                for (int i = Count-1; i > 0; i--)
                {
                    dest[destIndex + i] = source[sourceIndex + i];
                }
                 */
            }

        }

        public static void memmove(float[] dest, int destIndex, float[] source, int sourceIndex, int Count)
        {
            if (source != dest
                || destIndex < sourceIndex)
            {
                memcpy(dest, destIndex, source, sourceIndex, Count);
            }
            else
            {
                throw new Exception("this code needs to be tested");
                /*
                for (int i = Count-1; i > 0; i--)
                {
                    dest[destIndex + i] = source[sourceIndex + i];
                }
                 */
            }

        }

        public static void memset(int[] dest, int destIndex, int Val, int Count)
        {
            for (int i = 0; i < Count; i++)
            {
                dest[destIndex+i] = Val;
            }
        }

        public static void memset(Byte[] dest, int destIndex, byte ByteVal, int Count)
        {
            for (int i = 0; i < Count; i++)
            {
                dest[destIndex + i] = ByteVal;
            }
        }

        public static void MemClear(int[] dest, int destIndex, int Count)
        {
            for (int i = 0; i < Count; i++)
            {
                dest[destIndex + i] = 0;
            }
        }

        public static void MemClear(Byte[] dest, int destIndex, int Count)
        {
            for (int i = 0; i < Count; i++)
            {
                dest[destIndex + i] = 0;
            }
            /*
            // dword align to dest
            while (((int)pDest & 3) != 0
                && Count > 0)
            {
                *pDest++ = 0;
                Count--;
            }

            int NumLongs = Count / 4;

            while (NumLongs-- > 0)
            {
                *((int*)pDest) = 0;

                pDest += 4;
            }

            switch (Count & 3)
            {
                case 3:
                    pDest[2] = 0;
                    goto case 2;
                case 2:
                    pDest[1] = 0;
                    goto case 1;
                case 1:
                    pDest[0] = 0;
                    break;
            }
             */
        }

        public static bool is_equal_eps(double v1, double v2, double epsilon)
        {
            return Math.Abs(v1 - v2) <= (double)(epsilon);
        }

        //------------------------------------------------------------------deg2rad
        public static double deg2rad(double deg)
        {
            return deg * Math.PI / 180.0;
        }

        //------------------------------------------------------------------rad2deg
        public static double rad2deg(double rad)
        {
            return rad * 180.0 / Math.PI;
        }

        public static int iround(double v)
        {
            unchecked
            {
                return (int)((v < 0.0) ? v - 0.5 : v + 0.5);
            }
        }
        
        public static int iround(double v, int saturationLimit)
        {
            if(v < (double)(-saturationLimit)) return -saturationLimit;
            if(v > (double)( saturationLimit)) return  saturationLimit;
            return iround(v);
        }

        public static int uround(double v)
        {
            return (int)(uint)(v + 0.5);
        }

        public static int ufloor(double v)
        {
            return (int)(uint)(v);
        }

        public static int uceil(double v)
        {
            return (int)(uint)(Math.Ceiling(v));
        }

        //----------------------------------------------------poly_subpixel_scale_e
        // These constants determine the subpixel accuracy, to be more precise, 
        // the number of bits of the fractional part of the coordinates. 
        // The possible coordinate capacity in bits can be calculated by formula:
        // sizeof(int) * 8 - poly_subpixel_shift, i.e, for 32-bit integers and
        // 8-bits fractional part the capacity is 24 bits.
        public enum poly_subpixel_scale_e
        {
            poly_subpixel_shift = 8,                      //----poly_subpixel_shift
            poly_subpixel_scale = 1 << poly_subpixel_shift, //----poly_subpixel_scale 
            poly_subpixel_mask = poly_subpixel_scale - 1,  //----poly_subpixel_mask 
        };

    };

    internal struct RectangleInt
    {
        public int Left, Bottom, Right, Top;

        public RectangleInt(int x1_, int y1_, int x2_, int y2_)
        {
            Left = x1_;
            Bottom = y1_;
            Right = x2_;
            Top = y2_;
        }

        public void SetRect(int left, int bottom, int right, int top)
        {
            init(left, bottom, right, top);
        }

        public void init(int x1_, int y1_, int x2_, int y2_) 
        {
            Left = x1_;
            Bottom = y1_;
            Right = x2_;
            Top = y2_;
        }

        // This function assumes the rect is normalized
        public int Width
        {
            get
            {
                return Right - Left;
            }
        }

        // This function assumes the rect is normalized
        public int Height
        {
            get
            {
                return Top - Bottom;
            }
        }

        public RectangleInt normalize()
        {
            int t;
            if (Left > Right) { t = Left; Left = Right; Right = t; }
            if (Bottom > Top) { t = Bottom; Bottom = Top; Top = t; }
            return this;
        }

        public bool clip(RectangleInt r)
        {
            if (Right > r.Right) Right = r.Right;
            if (Top > r.Top) Top = r.Top;
            if (Left < r.Left) Left = r.Left;
            if (Bottom < r.Bottom) Bottom = r.Bottom;
            return Left <= Right && Bottom <= Top;
        }

        public bool is_valid()
        {
            return Left <= Right && Bottom <= Top;
        }

        public bool hit_test(int x, int y)
        {
            return (x >= Left && x <= Right && y >= Bottom && y <= Top);
        }

        public bool IntersectRectangles(RectangleInt rectToCopy, RectangleInt rectToIntersectWith)
        {
            Left = rectToCopy.Left;
            Bottom = rectToCopy.Bottom;
            Right = rectToCopy.Right;
            Top = rectToCopy.Top;

            if (Left < rectToIntersectWith.Left) Left = rectToIntersectWith.Left;
            if (Bottom < rectToIntersectWith.Bottom) Bottom = rectToIntersectWith.Bottom;
            if (Right > rectToIntersectWith.Right) Right = rectToIntersectWith.Right;
            if (Top > rectToIntersectWith.Top) Top = rectToIntersectWith.Top;

            if (Left < Right && Bottom < Top)
            {
                return true;
            }

            return false;
        }

        public bool IntersectWithRectangle(RectangleInt rectToIntersectWith)
        {
            if (Left < rectToIntersectWith.Left) Left = rectToIntersectWith.Left;
            if (Bottom < rectToIntersectWith.Bottom) Bottom = rectToIntersectWith.Bottom;
            if (Right > rectToIntersectWith.Right) Right = rectToIntersectWith.Right;
            if (Top > rectToIntersectWith.Top) Top = rectToIntersectWith.Top;

            if (Left < Right && Bottom < Top)
            {
                return true;
            }

            return false;
        }

        public static bool DoIntersect(RectangleInt rect1, RectangleInt rect2)
        {
            int x1 = rect1.Left;
            int y1 = rect1.Bottom;
            int x2 = rect1.Right;
            int y2 = rect1.Top;

            if (x1 < rect2.Left) x1 = rect2.Left;
            if (y1 < rect2.Bottom) y1 = rect2.Bottom;
            if (x2 > rect2.Right) x2 = rect2.Right;
            if (y2 > rect2.Top) y2 = rect2.Top;

            if (x1 < x2 && y1 < y2)
            {
                return true;
            }

            return false;
        }


        //---------------------------------------------------------unite_rectangles
        public void unite_rectangles(RectangleInt r1, RectangleInt r2)
        {
            Left = r1.Left;
            Bottom = r1.Bottom;
            Right = r1.Right;
            Right = r1.Top;
            if (Right < r2.Right) Right = r2.Right;
            if (Top < r2.Top) Top = r2.Top;
            if (Left > r2.Left) Left = r2.Left;
            if (Bottom > r2.Bottom) Bottom = r2.Bottom;
        }

        public void Inflate(int inflateSize)
        {
            Left = Left - inflateSize;
            Bottom = Bottom - inflateSize;
            Right = Right + inflateSize;
            Top = Top + inflateSize;
        }

        public void Offset(int x, int y)
        {
            Left = Left + x;
            Bottom = Bottom + y;
            Right = Right + x;
            Top = Top + y;
        }

        public static bool ClipRects(RectangleInt pBoundingRect, ref RectangleInt pSourceRect, ref RectangleInt pDestRect)
        {
		    // clip off the top so we don't write into random memory
            if (pDestRect.Top < pBoundingRect.Top)
	        {
		        // This type of clipping only works when we aren't scaling an image...
		        // If we are scaling an image, the source and dest sizes won't match
		        if(pSourceRect.Height != pDestRect.Height)
                {
                    throw new Exception("source and dest rects must have the same height");
                }

                pSourceRect.Top += pBoundingRect.Top - pDestRect.Top;
                pDestRect.Top = pBoundingRect.Top;
                if (pDestRect.Top >= pDestRect.Bottom)
		        {
			        return false;
		        }
	        }
		        // clip off the bottom
            if (pDestRect.Bottom > pBoundingRect.Bottom)
	        {
		        // This type of clipping only works when we arenst scaling an image...
		        // If we are scaling an image, the source and desst sizes won't match
                if (pSourceRect.Height != pDestRect.Height)
                {
                    throw new Exception("source and dest rects must have the same height");
                }

                pSourceRect.Bottom -= pDestRect.Bottom - pBoundingRect.Bottom;
                pDestRect.Bottom = pBoundingRect.Bottom;
                if (pDestRect.Bottom <= pDestRect.Top)
		        {
			        return false;
		        }
	        }

		        // clip off the left
            if (pDestRect.Left < pBoundingRect.Left)
	        {
		        // This type of clipping only works when we aren't scaling an image...
		        // If we are scaling an image, the source and dest sizes won't match
                if (pSourceRect.Width != pDestRect.Width)
                {
                    throw new Exception("source and dest rects must have the same width");
                }

                pSourceRect.Left += pBoundingRect.Left - pDestRect.Left;
                pDestRect.Left = pBoundingRect.Left;
                if (pDestRect.Left >= pDestRect.Right)
		        {
			        return false;
		        }
	        }
		        // clip off the right
            if (pDestRect.Right > pBoundingRect.Right)
	        {
		        // This type of clipping only works when we aren't scaling an image...
		        // If we are scaling an image, the source and dest sizes won't match
                if (pSourceRect.Width != pDestRect.Width)
                {
                    throw new Exception("source and dest rects must have the same width");
                }

                pSourceRect.Right -= pDestRect.Right - pBoundingRect.Right;
                pDestRect.Right = pBoundingRect.Right;
                if (pDestRect.Right <= pDestRect.Left)
		        {
			        return false;
		        }
	        }

	        return true;
        }


        //***************************************************************************************************************************************************
        public static bool ClipRect(RectangleInt pBoundingRect, ref RectangleInt pDestRect)
        {
	        // clip off the top so we don't write into random memory
            if (pDestRect.Top < pBoundingRect.Top)
	        {
                pDestRect.Top = pBoundingRect.Top;
                if (pDestRect.Top >= pDestRect.Bottom)
		        {
			        return false;
		        }
	        }
		        // clip off the bottom
            if (pDestRect.Bottom > pBoundingRect.Bottom)
	        {
                pDestRect.Bottom = pBoundingRect.Bottom;
                if (pDestRect.Bottom <= pDestRect.Top)
		        {
			        return false;
		        }
	        }

	        // clip off the left
            if (pDestRect.Left < pBoundingRect.Left)
	        {
                pDestRect.Left = pBoundingRect.Left;
                if (pDestRect.Left >= pDestRect.Right)
		        {
			        return false;
		        }
	        }

	        // clip off the right
            if (pDestRect.Right > pBoundingRect.Right)
	        {
                pDestRect.Right = pBoundingRect.Right;
                if (pDestRect.Right <= pDestRect.Left)
		        {
			        return false;
		        }
	        }

	        return true;
        }

    }

    /// <summary>
    /// BorderDouble is used to represent the border around (Margin) on inside (Padding) of a rectangular area.
    /// </summary>
    internal struct BorderDouble
    {
        public double Left, Bottom, Right, Top;

        public BorderDouble(double valueForAll)
        {
            Left = valueForAll;
            Right = valueForAll;
            Bottom = valueForAll;
            Top = valueForAll;
        }

        public BorderDouble(double leftRight, double bottomTopValue)
        {
            Left = leftRight;
            Right = leftRight;
            Bottom = bottomTopValue;
            Top = bottomTopValue;
        }

        public BorderDouble(double left, double bottom, double right, double top)
        {
            this.Left = left;
            this.Bottom = bottom;
            this.Right = right;
            this.Top = top;
        }

        public static bool operator ==(BorderDouble a, BorderDouble b)
        {
            if (a.Left == b.Left && a.Bottom == b.Bottom && a.Right == b.Right && a.Top == b.Top)
            {
                return true;
            }

            return false;
        }

        public static bool operator !=(BorderDouble a, BorderDouble b)
        {
            if (a.Left != b.Left || a.Bottom != b.Bottom || a.Right != b.Right || a.Top != b.Top)
            {
                return true;
            }

            return false;
        }

        static public BorderDouble operator *(BorderDouble a, double b)
        {
            return new BorderDouble(a.Left * b, a.Bottom * b, a.Right * b, a.Top * b);
        }

        static public BorderDouble operator *(double b, BorderDouble a)
        {
            return new BorderDouble(a.Left * b, a.Bottom * b, a.Right * b, a.Top * b);
        }


        public override int GetHashCode()
        {
            return new { x1 = Left, x2 = Right, y1 = Bottom, y2 = Top }.GetHashCode();
            //return new { A = x1, B = x2, C = y1, D = y2 }.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() == typeof(BorderDouble))
            {
                return this == (BorderDouble)obj;
            }
            return false;
        }

        public double Width
        {
            get
            {
                return Left + Right;
            }
        }

        // This function assumes the rect is normalized
        public double Height
        {
            get
            {
                return Bottom + Top;
            }
        }
    }

    internal struct RectangleDouble
    {
        public double Left, Bottom, Right, Top;

        public static readonly RectangleDouble ZeroIntersection = new RectangleDouble(double.MaxValue, double.MaxValue, double.MinValue, double.MinValue);

        public RectangleDouble(double left, double bottom, double right, double top)
        {
            this.Left = left;
            this.Bottom = bottom;
            this.Right = right;
            this.Top = top;
        }

        public RectangleDouble(RectangleInt intRect)
        {
            Left = intRect.Left;
            Bottom = intRect.Bottom;
            Right = intRect.Right;
            Top = intRect.Top;
        }

        public void SetRect(double left, double bottom, double right, double top)
        {
            init(left, bottom, right, top);
        }

        public static bool operator ==(RectangleDouble a, RectangleDouble b)
        {
            if (a.Left == b.Left && a.Bottom == b.Bottom && a.Right == b.Right && a.Top == b.Top)
            {
                return true;
            }

            return false;
        }

        public static bool operator !=(RectangleDouble a, RectangleDouble b)
        {
            if (a.Left != b.Left || a.Bottom != b.Bottom || a.Right != b.Right || a.Top != b.Top)
            {
                return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return new { x1 = Left, x2 = Right, y1 = Bottom, y2 = Top }.GetHashCode();
            //return new { A = x1, B = x2, C = y1, D = y2 }.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() == typeof(RectangleDouble))
            {
                return this == (RectangleDouble)obj;
            }
            return false;
        }

        public void init(double left, double bottom, double right, double top)
        {
            Left = left;
            Bottom = bottom;
            Right = right;
            Top = top;
        }

        // This function assumes the rect is normalized
        public double Width
        {
            get
            {
                return Right - Left;
            }
        }

        // This function assumes the rect is normalized
        public double Height
        {
            get
            {
                return Top - Bottom;
            }
        }

        public RectangleDouble normalize()
        {
            double t;
            if (Left > Right) { t = Left; Left = Right; Right = t; }
            if (Bottom > Top) { t = Bottom; Bottom = Top; Top = t; }
            return this;
        }

        public bool clip(RectangleDouble r)
        {
            if (Right > r.Right) Right = r.Right;
            if (Top > r.Top) Top = r.Top;
            if (Left < r.Left) Left = r.Left;
            if (Bottom < r.Bottom) Bottom = r.Bottom;
            return Left <= Right && Bottom <= Top;
        }

        public bool is_valid()
        {
            return Left <= Right && Bottom <= Top;
        }

        public bool Contains(double x, double y)
        {
            return (x >= Left && x <= Right && y >= Bottom && y <= Top);
        }

        public bool Contains(RectangleDouble innerRect)
        {
            if (Contains(innerRect.Left, innerRect.Bottom) && Contains(innerRect.Right, innerRect.Top))
            {
                return true;
            }

            return false;
        }

        public bool Contains(Vector2 position)
        {
            return Contains(position.x, position.y);
        }

        public bool IntersectRectangles(RectangleDouble rectToCopy, RectangleDouble rectToIntersectWith)
        {
            Left = rectToCopy.Left;
            Bottom = rectToCopy.Bottom;
            Right = rectToCopy.Right;
            Top = rectToCopy.Top;

            if (Left < rectToIntersectWith.Left) Left = rectToIntersectWith.Left;
            if (Bottom < rectToIntersectWith.Bottom) Bottom = rectToIntersectWith.Bottom;
            if (Right > rectToIntersectWith.Right) Right = rectToIntersectWith.Right;
            if (Top > rectToIntersectWith.Top) Top = rectToIntersectWith.Top;

            if (Left < Right && Bottom < Top)
            {
                return true;
            }

            return false;
        }

        public bool IntersectWithRectangle(RectangleDouble rectToIntersectWith)
        {
            if (Left < rectToIntersectWith.Left) Left = rectToIntersectWith.Left;
            if (Bottom < rectToIntersectWith.Bottom) Bottom = rectToIntersectWith.Bottom;
            if (Right > rectToIntersectWith.Right) Right = rectToIntersectWith.Right;
            if (Top > rectToIntersectWith.Top) Top = rectToIntersectWith.Top;

            if (Left < Right && Bottom < Top)
            {
                return true;
            }

            return false;
        }

        public void unite_rectangles(RectangleDouble r1, RectangleDouble r2)
        {
            Left = r1.Left;
            Bottom = r1.Bottom;
            Right = r1.Right;
            Right = r1.Top;
            if (Right < r2.Right) Right = r2.Right;
            if (Top < r2.Top) Top = r2.Top;
            if (Left > r2.Left) Left = r2.Left;
            if (Bottom > r2.Bottom) Bottom = r2.Bottom;
        }

        public void ExpandToInclude(RectangleDouble rectToInclude)
        {
            if (Right < rectToInclude.Right) Right = rectToInclude.Right;
            if (Top < rectToInclude.Top) Top = rectToInclude.Top;
            if (Left > rectToInclude.Left) Left = rectToInclude.Left;
            if (Bottom > rectToInclude.Bottom) Bottom = rectToInclude.Bottom;
        }

        public void ExpandToInclude(double x, double y)
        {
            if (Right < x) Right = x;
            if (Top < y) Top = y;
            if (Left > x) Left = x;
            if (Bottom > y) Bottom = y;
        }

        public void Inflate(double inflateSize)
        {
            Left = Left - inflateSize;
            Bottom = Bottom - inflateSize;
            Right = Right + inflateSize;
            Top = Top + inflateSize;
        }

        public void Offset(Vector2 offset)
        {
            Offset(offset.x, offset.y);
        }

        public void Offset(double x, double y)
        {
            Left = Left + x;
            Bottom = Bottom + y;
            Right = Right + x;
            Top = Top + y;
        }

        static public RectangleDouble operator *(RectangleDouble a, double b)
        {
            return new RectangleDouble(a.Left * b, a.Bottom * b, a.Right * b, a.Top * b);
        }

        static public RectangleDouble operator *(double b, RectangleDouble a)
        {
            return new RectangleDouble(a.Left * b, a.Bottom * b, a.Right * b, a.Top * b);
        }

        public double XCenter 
        {
            get { return (Right - Left) / 2; }
        }

        public void Inflate(BorderDouble borderDouble)
        {
            Left -= borderDouble.Left;
            Right += borderDouble.Right;
            Bottom -= borderDouble.Bottom;
            Top += borderDouble.Top;
        }
    }

    internal static class Path
    {
        [Flags]
        public enum FlagsAndCommand
        {
            CommandStop     = 0,
            CommandMoveTo  = 1,
            CommandLineTo  = 2,
            CommandCurve3   = 3,
            CommandCurve4   = 4,
            //CommandCurveN   = 5, // unused [3/10/2009 lbrubaker]
            //CommandCatRom   = 6, // unused [3/10/2009 lbrubaker]
            //CommandUBSpline = 7, // unused [3/10/2009 lbrubaker]
            CommandEndPoly = 0x0F,
            CommandsMask     = 0x0F,

            FlagNone  = 0,
            FlagCCW   = 0x10,
            FlagCW    = 0x20,
            FlagClose = 0x40,
            FlagsMask  = 0xF0
        };

        public static bool is_vertex(FlagsAndCommand c)
        {
            return c >= FlagsAndCommand.CommandMoveTo
                && c < FlagsAndCommand.CommandEndPoly;
        }

        public static bool is_drawing(FlagsAndCommand c)
        {
            return c >= FlagsAndCommand.CommandLineTo && c < FlagsAndCommand.CommandEndPoly;
        }

        public static bool is_stop(FlagsAndCommand c)
        {
            return c == FlagsAndCommand.CommandStop;
        }

        public static bool is_move_to(FlagsAndCommand c)
        {
            return c == FlagsAndCommand.CommandMoveTo;
        }

        public static bool is_line_to(FlagsAndCommand c)
        {
            return c == FlagsAndCommand.CommandLineTo;
        }

        public static bool is_curve(FlagsAndCommand c)
        {
            return c == FlagsAndCommand.CommandCurve3
                || c == FlagsAndCommand.CommandCurve4;
        }

        public static bool is_curve3(FlagsAndCommand c)
        {
            return c == FlagsAndCommand.CommandCurve3;
        }

        public static bool is_curve4(FlagsAndCommand c)
        {
            return c == FlagsAndCommand.CommandCurve4;
        }

        public static bool is_end_poly(FlagsAndCommand c)
        {
            return (c & FlagsAndCommand.CommandsMask) == FlagsAndCommand.CommandEndPoly;
        }

        public static bool is_close(FlagsAndCommand c)
        {
            return (c & ~(FlagsAndCommand.FlagCW | FlagsAndCommand.FlagCCW)) ==
                   (FlagsAndCommand.CommandEndPoly | FlagsAndCommand.FlagClose); 
        }

        public static bool is_next_poly(FlagsAndCommand c)
        {
            return is_stop(c) || is_move_to(c) || is_end_poly(c);
        }

        public static bool is_cw(FlagsAndCommand c)
        {
            return (c & FlagsAndCommand.FlagCW) != 0;
        }

        public static bool is_ccw(FlagsAndCommand c)
        {
            return (c & FlagsAndCommand.FlagCCW) != 0;
        }

        public static bool is_oriented(FlagsAndCommand c)
        {
            return (c & (FlagsAndCommand.FlagCW | FlagsAndCommand.FlagCCW)) != 0; 
        }

        public static bool is_closed(FlagsAndCommand c)
        {
            return (c & FlagsAndCommand.FlagClose) != 0; 
        }

        public static FlagsAndCommand get_close_flag(FlagsAndCommand c)
        {
            return (FlagsAndCommand)(c & FlagsAndCommand.FlagClose); 
        }

        public static FlagsAndCommand clear_orientation(FlagsAndCommand c)
        {
            return c & ~(FlagsAndCommand.FlagCW | FlagsAndCommand.FlagCCW);
        }

        public static FlagsAndCommand get_orientation(FlagsAndCommand c)
        {
            return c & (FlagsAndCommand.FlagCW | FlagsAndCommand.FlagCCW);
        }

        /*
        //---------------------------------------------------------set_orientation
        public static path_flags_e set_orientation(int c, path_flags_e o)
        {
            return clear_orientation(c) | o;
        }
         */

        static public void shorten_path(MatterHackers.Agg.VertexSequence vs, double s)
        {
            shorten_path(vs, s, 0);
        }

        static public void shorten_path(VertexSequence vs, double s, int closed)
        {
            if(s > 0.0 && vs.size() > 1)
            {
                double d;
                int n = (int)(vs.size() - 2);
                while(n != 0)
                {
                    d = vs[n].dist;
                    if(d > s) break;
                    vs.RemoveLast();
                    s -= d;
                    --n;
                }
                if(vs.size() < 2)
                {
                    vs.remove_all();
                }
                else
                {
                    n = (int)vs.size() - 1;
                    VertexDistance prev = vs[n - 1];
                    VertexDistance last = vs[n];
                    d = (prev.dist - s) / prev.dist;
                    double x = prev.x + (last.x - prev.x) * d;
                    double y = prev.y + (last.y - prev.y) * d;
                    last.x = x;
                    last.y = y;
                    if (!prev.IsEqual(last)) vs.RemoveLast();
                    vs.close(closed != 0);
                }
            }
        }
    };
}
