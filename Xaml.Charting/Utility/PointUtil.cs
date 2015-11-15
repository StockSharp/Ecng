// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// PointUtil.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace Ecng.Xaml.Charting.Utility
{
    /// <summary>
    /// Provides helper methods for manipulations with points
    /// </summary>
    public static class PointUtil
    {
        /// <summary>
        /// Represents a straight line between two points
        /// </summary>
        public struct Line
        {
            /// <summary>
            /// The X coord of the first point
            /// </summary>
            public double X1;
            /// <summary>
            /// The Y coord of the first point
            /// </summary>
            public double Y1;
            /// <summary>
            /// The X coord of the second point
            /// </summary>
            public double X2;
            /// <summary>
            /// The Y coord of the second point
            /// </summary>
            public double Y2;

            /// <summary>
            /// Creates a new instance of the <see cref="Line"/> type
            /// </summary>
            public Line(Point pt1, Point pt2)
                : this(pt1.X, pt1.Y, pt2.X, pt2.Y)
            {}

            /// <summary>
            /// Creates a new instance of the <see cref="Line"/> type
            /// </summary>
            public Line(double x1, double y1, double x2, double y2)
                : this()
            {
                X1 = x1;
                Y1 = y1;
                X2 = x2;
                Y2 = y2;
            }
        }

        /// <summary>
        /// Looks for the intersection point for the two lines passed in
        /// </summary>
        /// <param name="firstLine">The first line</param>
        /// <param name="secondLine">The second line</param>
        /// <param name="intersectionPoint">If the lines intersect, holds the intersection point</param>
        /// <returns>The value indicating whether an intersection occurs</returns>
        /// <remarks>See http://paulbourke.net/geometry/lineline2d/Helpers.cs </remarks>
        public static bool LineSegmentsIntersection2D(Line firstLine, Line secondLine, out Point intersectionPoint)
        {
            return LineIntersection2D(firstLine, secondLine, out intersectionPoint, false);
        }

        /// <summary>
        /// Looks for the intersection point for the two lines passed in
        /// </summary>
        /// <param name="firstLine">The first line</param>
        /// <param name="secondLine">The second line</param>
        /// <param name="intersectionPoint">If the lines intersect, holds the intersection point</param>
        /// <returns>The value indicating whether an intersection occurs</returns>
        /// <remarks>See http://paulbourke.net/geometry/lineline2d/Helpers.cs </remarks>
        public static bool LineIntersection2D(Line firstLine, Line secondLine, out Point intersectionPoint)
        {
            return LineIntersection2D(firstLine, secondLine, out intersectionPoint, true);
        }

        private static bool LineIntersection2D(Line firstLine, Line secondLine, out Point intersectionPoint,
            bool intersectAtInfinity)
        {
            var isL1Point = IsPoint(firstLine);
            var isL2Point = IsPoint(secondLine);

            // Handling cases when line is point
            if (isL1Point && isL2Point)
            {
                return PointsIntersection(firstLine, secondLine, out intersectionPoint);
            }

            if (isL1Point)
            {
                return PointAndLineIntersection(firstLine, secondLine, out intersectionPoint);
            }

            if (isL2Point)
            {
                return PointAndLineIntersection(secondLine, firstLine, out intersectionPoint);
            }

            // Denominator for ua and ub are the same, so store this calculation
            double d = (secondLine.Y2 - secondLine.Y1) * (firstLine.X2 - firstLine.X1) - (secondLine.X2 - secondLine.X1) * (firstLine.Y2 - firstLine.Y1);

            //n_a and n_b are calculated as seperate values for readability
            double n_a = (secondLine.X2 - secondLine.X1) * (firstLine.Y1 - secondLine.Y1) - (secondLine.Y2 - secondLine.Y1) * (firstLine.X1 - secondLine.X1);

            double n_b = (firstLine.X2 - firstLine.X1) * (firstLine.Y1 - secondLine.Y1) - (firstLine.Y2 - firstLine.Y1) * (firstLine.X1 - secondLine.X1);

            //Check for coincident lines
            if (n_a == 0 || n_b == 0)
            {
                var inters = GetCoincidentSubset(new Point(firstLine.X1, firstLine.Y1), new Point(firstLine.X2, firstLine.Y2),
                                               new Point(secondLine.X1, secondLine.Y1), new Point(secondLine.X2, secondLine.Y2));

                if (inters.Length > 0)
                {
                    intersectionPoint = inters[0];
                    return true;
                }
            }

            // Make sure there is not a division by zero - this also indicates that
            // the lines are parallel.  
            // If n_a and n_b were both equal to zero the lines would be on top of each 
            // other (coincidental).  This check is not done because it is not 
            // necessary for this implementation (the parallel check accounts for this).
            if (d == 0)
            {
                intersectionPoint = default(Point);
                return false;
            }

            // Calculate the intermediate fractional point that the lines potentially intersect.
            double ua = n_a / d;
            double ub = n_b / d;

            // The fractional point will be between 0 and 1 inclusive if the lines
            // intersect.  If the fractional calculation is larger than 1 or smaller
            // than 0 the lines would need to be longer to intersect.
            if (intersectAtInfinity || (ua >= 0d && ua <= 1d && ub >= 0d && ub <= 1d))
            {
                intersectionPoint = new Point(firstLine.X1 + (ua * (firstLine.X2 - firstLine.X1)), firstLine.Y1 + (ua * (firstLine.Y2 - firstLine.Y1)));
                return true;
            }

            intersectionPoint = default(Point);
            return false;
        }

        private static bool PointsIntersection(Line L1, Line L2, out Point ptIntersection)
        {
            var p1 = new Point(L1.X1, L1.Y1);
            var p2 = new Point(L2.X1, L2.Y1);

            ptIntersection = new Point(p1.X, p1.Y);

            return Distance(p1, p2).CompareTo(0d) == 0;
        }

        public static double Distance(Point point1, Point point2)
        {
            double diffX = point1.X - point2.X;
            double diffY = point1.Y - point2.Y;
            return Math.Sqrt(diffX * diffX + diffY * diffY);
        }

        public static double PolarDistance(Point point1, Point point2)
        {
            var angle = point2.X - point1.X;
            var radians = Math.PI / 180  * angle;

            double coef = 2*point1.Y*point2.Y*Math.Cos(radians);
            return Math.Sqrt(point1.Y * point1.Y + point2.Y * point2.Y - coef);
        }

        private static bool PointAndLineIntersection(Line l1, Line l2, out Point ptIntersection)
        {
            var p1 = new Point(l1.X1, l1.Y1);
            var p2 = new Point(l2.X1, l2.Y1);
            var p3 = new Point(l2.X2, l2.Y2);

            ptIntersection = new Point(p1.X, p1.Y);

            return DistanceFromLine(p1, p2, p3).CompareTo(0d) == 0;
        }

        //Compute the distance from AB to C
        //if isSegment is true, AB is a segment, not a line.
        internal static double DistanceFromLine(Point pt, Point start, Point end, bool isSegment = true)
        {
            double dist = CrossProduct(start, end, pt) / Distance(start, end);
            if (isSegment)
            {
                double dot1 = DotProduct(start, end, pt);
                if (dot1 > 0)
                    return Distance(end, pt);

                double dot2 = DotProduct(end, start, pt);
                if (dot2 > 0)
                    return Distance(start, pt);
            }
            return Math.Abs(dist);
        }

        //Compute the cross product AB x AC
        private static double CrossProduct(Point pointA, Point pointB, Point pointC)
        {
            return (pointB.X - pointA.X) * (pointC.Y - pointA.Y) - (pointB.Y - pointA.Y) * (pointC.X - pointA.X);
        }

        //Compute the dot product AB . AC
        private static double DotProduct(Point pointA, Point pointB, Point pointC)
        {
            Point AB = new Point();
            Point BC = new Point();
            AB.X = pointB.X - pointA.X;
            AB.Y = pointB.Y - pointA.Y;
            BC.X = pointC.X - pointB.X;
            BC.Y = pointC.Y - pointB.Y;
            double dot = AB.X * BC.X + AB.Y * BC.Y;

            return dot;
        }

        //See http://stackoverflow.com/questions/2255842/detecting-coincident-subset-of-two-coincident-line-segments/2255848#2255848
        // IMPORTANT: a1 and a2 cannot be the same, e.g. a1--a2 is a true segment, not a point
        // b1/b2 may be the same (b1--b2 is a point)
        private static Point[] GetCoincidentSubset(Point a1, Point a2, Point b1, Point b2)
        {
            //ua1 = 0.0d; // by definition
            //ua2 = 1.0d; // by definition
            double ub1, ub2;

            double denomx = a2.X - a1.X;
            double denomy = a2.Y - a1.Y;

            if (Math.Abs(denomx) > Math.Abs(denomy))
            {
                ub1 = (b1.X - a1.X) / denomx;
                ub2 = (b2.X - a1.X) / denomx;
            }
            else
            {
                ub1 = (b1.Y - a1.Y) / denomy;
                ub2 = (b2.Y - a1.Y) / denomy;
            }

            List<Point> ret = new List<Point>();

            double[] interval = OverlapIntervals(ub1, ub2);
            foreach (double f in interval)
            {
                double x = a2.X * f + a1.X * (1.0f - f);
                double y = a2.Y * f + a1.Y * (1.0f - f);

                Point p = new Point(x, y);
                ret.Add(p);
            }

            return ret.ToArray();
        }

        private static double[] OverlapIntervals(double ub1, double ub2)
        {
            double l = Math.Min(ub1, ub2);
            double r = Math.Max(ub1, ub2);

            double A = Math.Max(0, l);
            double B = Math.Min(1, r);

            if (A > B) // no intersection
                return new double[] { };

            if (A == B)
                return new[] { A };

            // if (A < B)
            return new[] { A, B };
        }

        internal static bool LiesToTheLeft(Point pointToCheck, Point lineEnd1, Point lineEnd2)
        {
            return CrossProduct(lineEnd1, lineEnd2, pointToCheck) > 0;
        }

        internal static bool IsPointInTriangle(Point checkPt, Point pointA, Point pointB, Point pointC)
        {
            // Handling case if triangle is a point
            if (pointA == pointB && pointB == pointC)
                return checkPt == pointA;

            // Handling cases if triangle is a line
            if (pointA == pointB || pointB == pointC)
                return DistanceFromLine(checkPt, pointA, pointC) < double.Epsilon;
            if (pointA == pointC)
                return DistanceFromLine(checkPt, pointA, pointB) < double.Epsilon;

            bool cp1 = CrossProduct(checkPt, pointA, pointB) < double.Epsilon;
            bool cp2 = CrossProduct(checkPt, pointB, pointC) < double.Epsilon;
            bool cp3 = CrossProduct(checkPt, pointC, pointA) < double.Epsilon;

            return ((cp1 == cp2) && (cp2 == cp3));
        }

        private static bool IsPoint(Line line)
        {
            return (Math.Abs(line.X1 - line.X2) < double.Epsilon && Math.Abs(line.Y1 - line.Y2) < double.Epsilon);
        }

        internal static bool IsInBounds(this Point point, Size viewportSize)
        {
            return point.X >= 0 && point.X <= viewportSize.Width &&
                   point.Y >= 0 && point.Y <= viewportSize.Height;
        }

        // Checking if points lies on the same line
        // http://stackoverflow.com/questions/3813681/checking-to-see-if-3-points-are-on-the-same-line
        internal static bool ArePointsOnSameLine(Point pt1, Point pt2, Point pt3)
        {
            var result = pt1.X * (pt2.Y - pt3.Y) +
                         pt2.X * (pt3.Y - pt1.Y) +
                         pt3.X * (pt1.Y - pt2.Y);

            return result.Equals(0);
        }

        internal static Point ClipPoint(this Point pt, Size viewportSize, int yExtension = 0, int xExtension = 0)
        {
            var w = viewportSize.Width + xExtension;
            var h = viewportSize.Height + yExtension;

            if (pt.X < -xExtension)
                pt.X = -xExtension;
            else if (pt.X > w)
                pt.X = w;

            if (pt.Y < -yExtension)
                pt.Y = -yExtension;
            else if (pt.Y > h)
                pt.Y = h;

            return pt;
        }

        internal static IEnumerable<Point> ClipPolygon(IEnumerable<Point> points, Size viewportSize, int xExtension = 0, int yExtension = 0)
        {
            Point? prev = null;
            var extents = new Rect(-xExtension, -yExtension, viewportSize.Width + xExtension,
                viewportSize.Height + yExtension);

            foreach (var currPoint in points)
            {
                if (prev.HasValue)
                {
                    var prevPoint = prev.Value;
                    if (!prevPoint.IsInBounds(viewportSize) || !currPoint.IsInBounds(viewportSize))
                    {
                        var x1 = prevPoint.X;
                        var y1 = prevPoint.Y;
                        var x2 = currPoint.X;
                        var y2 = currPoint.Y;

                        if (WriteableBitmapExtensions.CohenSutherlandLineClip(
                            extents,
                            ref x1, ref y1, ref x2, ref y2))
                        {
                            // Only one end of the line is outside the viewport 
                            yield return prevPoint.ClipPoint(viewportSize, yExtension, xExtension);
                            yield return new Point(x1, y1);
                            yield return new Point(x2, y2);
                        }
                        else
                        {
                            // Entire line is ouside the viewport,
                            /* There are 4 variants of line placement:
                                                                    
                                   *       *           |                |
                                 *____   ____*     *   |                |   *
                               * |           | *     * |_____      _____| *
                             *   |           |   *     *                *
                                                         *            *                  
                              */

                            var prevClipped = prevPoint.ClipPoint(viewportSize, yExtension, xExtension);
                            var currClipped = currPoint.ClipPoint(viewportSize, yExtension, xExtension);

                            var liesToTheLeft = PointUtil.LiesToTheLeft(new Point(0, 0), prevPoint, currPoint);
                            var nearestCorner = (liesToTheLeft && currPoint.Y < prevPoint.Y) ||
                                                (!liesToTheLeft && currPoint.Y > prevPoint.Y)
                                ? new Point(prevClipped.X, currClipped.Y)
                                : new Point(currClipped.X, prevClipped.Y);

                            yield return prevClipped;
                            yield return nearestCorner;
                            yield return currClipped;
                        }
                    }
                    else
                        yield return prevPoint;

                }

                prev = currPoint;
            }

            if (prev.HasValue)
                yield return prev.Value.ClipPoint(viewportSize, yExtension, xExtension);
        }
    }
}