//----------------------------------------------------------------------------
// Anti-Grain Geometry - Version 2.4
// Copyright (C) 2002-2005 Maxim Shemanarev (http://www.antigrain.com)
// Copyright (C) 2005 Tony Juricic (tonygeek@yahoo.com)
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
using System;

using MatterHackers.VectorMath;

namespace MatterHackers.Agg.VertexSource
{
    internal interface ICurve : IVertexSource
    {

    };

    internal static class Curves
    {
        //--------------------------------------------curve_approximation_method_e
        public enum CurveApproximationMethod
        {
            curve_inc,
            curve_div
        };

        public static double curve_distance_epsilon                  = 1e-30;
        public static double curve_collinearity_epsilon = 1e-30;
        public static double curve_angle_tolerance_epsilon = 0.01;
        public enum curve_recursion_limit_e { curve_recursion_limit = 32 };

        //-------------------------------------------------------catrom_to_bezier
        public static curve4_points catrom_to_bezier(double x1, double y1, 
                                              double x2, double y2, 
                                              double x3, double y3,
                                              double x4, double y4)
        {
            // Trans. matrix Catmull-Rom to Bezier
            //
            //  0       1       0       0
            //  -1/6    1       1/6     0
            //  0       1/6     1       -1/6
            //  0       0       1       0
            //
            return new curve4_points(
                x2,
                y2,
                (-x1 + 6*x2 + x3) / 6,
                (-y1 + 6*y2 + y3) / 6,
                ( x2 + 6*x3 - x4) / 6,
                ( y2 + 6*y3 - y4) / 6,
                x3,
                y3);
        }

        //-----------------------------------------------------------------------
        public static curve4_points
        catrom_to_bezier(curve4_points cp)
        {
            return catrom_to_bezier(cp[0], cp[1], cp[2], cp[3], 
                                    cp[4], cp[5], cp[6], cp[7]);
        }


        //-----------------------------------------------------ubspline_to_bezier
        public static curve4_points ubspline_to_bezier(double x1, double y1, 
                                                double x2, double y2, 
                                                double x3, double y3,
                                                double x4, double y4)
        {
            // Trans. matrix Uniform BSpline to Bezier
            //
            //  1/6     4/6     1/6     0
            //  0       4/6     2/6     0
            //  0       2/6     4/6     0
            //  0       1/6     4/6     1/6
            //
            return new curve4_points(
                (x1 + 4*x2 + x3) / 6,
                (y1 + 4*y2 + y3) / 6,
                (4*x2 + 2*x3) / 6,
                (4*y2 + 2*y3) / 6,
                (2*x2 + 4*x3) / 6,
                (2*y2 + 4*y3) / 6,
                (x2 + 4*x3 + x4) / 6,
                (y2 + 4*y3 + y4) / 6);
        }


        //-----------------------------------------------------------------------
        public static curve4_points 
        ubspline_to_bezier(curve4_points cp)
        {
            return ubspline_to_bezier(cp[0], cp[1], cp[2], cp[3], 
                                      cp[4], cp[5], cp[6], cp[7]);
        }



        //------------------------------------------------------hermite_to_bezier
        public static curve4_points hermite_to_bezier(double x1, double y1, 
                                               double x2, double y2, 
                                               double x3, double y3,
                                               double x4, double y4)
        {
            // Trans. matrix Hermite to Bezier
            //
            //  1       0       0       0
            //  1       0       1/3     0
            //  0       1       0       -1/3
            //  0       1       0       0
            //
            return new curve4_points(
                x1,
                y1,
                (3*x1 + x3) / 3,
                (3*y1 + y3) / 3,
                (3*x2 - x4) / 3,
                (3*y2 - y4) / 3,
                x2,
                y2);
        }



        //-----------------------------------------------------------------------
        public static curve4_points 
        hermite_to_bezier(curve4_points cp)
        {
            return hermite_to_bezier(cp[0], cp[1], cp[2], cp[3], 
                                     cp[4], cp[5], cp[6], cp[7]);
        }


    };

    //--------------------------------------------------------------curve3_inc
    internal sealed class curve3_inc
    {
        int      m_num_steps;
        int      m_step;
        double   m_scale;
        double   m_start_x; 
        double   m_start_y;
        double   m_end_x; 
        double   m_end_y;
        double   m_fx; 
        double   m_fy;
        double   m_dfx; 
        double   m_dfy;
        double   m_ddfx; 
        double   m_ddfy;
        double   m_saved_fx; 
        double   m_saved_fy;
        double   m_saved_dfx; 
        double   m_saved_dfy;

        public curve3_inc()
        {
            m_num_steps=(0);
            m_step=(0);
            m_scale=(1.0);
        }

        public curve3_inc(double x1, double y1, 
                   double x2, double y2, 
                   double x3, double y3)
        { 
            m_num_steps=(0);
            m_step=(0);
            m_scale=(1.0);
            init(x1, y1, x2, y2, x3, y3);
        }

        public void reset() { m_num_steps = 0; m_step = -1; }

        public void init(double x1, double y1, 
                  double x2, double y2, 
                  double x3, double y3)
        {
            m_start_x = x1;
            m_start_y = y1;
            m_end_x = x3;
            m_end_y = y3;

            double dx1 = x2 - x1;
            double dy1 = y2 - y1;
            double dx2 = x3 - x2;
            double dy2 = y3 - y2;

            double len = Math.Sqrt(dx1 * dx1 + dy1 * dy1) + Math.Sqrt(dx2 * dx2 + dy2 * dy2);

            m_num_steps = (int)agg_basics.uround(len * 0.25 * m_scale);

            if (m_num_steps < 4)
            {
                m_num_steps = 4;
            }

            double subdivide_step = 1.0 / m_num_steps;
            double subdivide_step2 = subdivide_step * subdivide_step;

            double tmpx = (x1 - x2 * 2.0 + x3) * subdivide_step2;
            double tmpy = (y1 - y2 * 2.0 + y3) * subdivide_step2;

            m_saved_fx = m_fx = x1;
            m_saved_fy = m_fy = y1;

            m_saved_dfx = m_dfx = tmpx + (x2 - x1) * (2.0 * subdivide_step);
            m_saved_dfy = m_dfy = tmpy + (y2 - y1) * (2.0 * subdivide_step);

            m_ddfx = tmpx * 2.0;
            m_ddfy = tmpy * 2.0;

            m_step = m_num_steps;
        }


        public void approximation_method(Curves.CurveApproximationMethod method) {}
        public Curves.CurveApproximationMethod approximation_method() { return Curves.CurveApproximationMethod.curve_inc; }

        public void approximation_scale(double s)
        { 
            m_scale = s;
        }

        public double approximation_scale()
        { 
            return m_scale;
        }

        public void angle_tolerance(double angle) {}
        public double angle_tolerance(){ return 0.0; }

        public void cusp_limit(double limit) {}
        public double cusp_limit(){ return 0.0; }

        public void rewind(int path_id)
        {
            if (m_num_steps == 0)
            {
                m_step = -1;
                return;
            }
            m_step = m_num_steps;
            m_fx = m_saved_fx;
            m_fy = m_saved_fy;
            m_dfx = m_saved_dfx;
            m_dfy = m_saved_dfy;
        }

        public Path.FlagsAndCommand vertex(out double x, out double y)
        {
            if (m_step < 0)
            {
                x = 0;
                y = 0;
                return Path.FlagsAndCommand.CommandStop;
            }
            if (m_step == m_num_steps)
            {
                x = m_start_x;
                y = m_start_y;
                --m_step;
                return Path.FlagsAndCommand.CommandMoveTo;
            }
            if (m_step == 0)
            {
                x = m_end_x;
                y = m_end_y;
                --m_step;
                return Path.FlagsAndCommand.CommandLineTo;
            }
            m_fx += m_dfx;
            m_fy += m_dfy;
            m_dfx += m_ddfx;
            m_dfy += m_ddfy;
            x = m_fx;
            y = m_fy;
            --m_step;
            return Path.FlagsAndCommand.CommandLineTo;
        }

    };

    //-------------------------------------------------------------curve3_div
    internal sealed class curve3_div
    {
        double               m_approximation_scale;
        double               m_distance_tolerance_square;
        double               m_angle_tolerance;
        int m_count;
        VectorPOD<Vector2> m_points;

        public curve3_div()
        {
            m_points = new VectorPOD<Vector2>();
            m_approximation_scale=(1.0);
            m_angle_tolerance=(0.0);
            m_count=(0);
        }

        public curve3_div(double x1, double y1, 
                   double x2, double y2, 
                   double x3, double y3)
        { 
            m_approximation_scale=(1.0);
            m_angle_tolerance=(0.0);
            m_count=(0);
            init(x1, y1, x2, y2, x3, y3);
        }

        public void reset() { m_points.remove_all(); m_count = 0; }
        public void init(double x1, double y1, 
                  double x2, double y2, 
                  double x3, double y3)
        {
            m_points.remove_all();
            m_distance_tolerance_square = 0.5 / m_approximation_scale;
            m_distance_tolerance_square *= m_distance_tolerance_square;
            bezier(x1, y1, x2, y2, x3, y3);
            m_count = 0;
        }


        public void approximation_method(Curves.CurveApproximationMethod method) { }
        public Curves.CurveApproximationMethod approximation_method() { return Curves.CurveApproximationMethod.curve_div; }

        public void approximation_scale(double s) { m_approximation_scale = s; }
        public double approximation_scale(){ return m_approximation_scale;  }

        public void angle_tolerance(double a) { m_angle_tolerance = a; }
        public double angle_tolerance(){ return m_angle_tolerance;  }

        public void cusp_limit(double limit) {}
        public double cusp_limit(){ return 0.0; }

        public void rewind(int idx)
        {
            m_count = 0;
        }

        public Path.FlagsAndCommand vertex(out double x, out double y)
        {
            if (m_count >= m_points.size())
            {
                x = 0;
                y = 0;
                return Path.FlagsAndCommand.CommandStop;
            }

            Vector2 p = m_points[m_count++];
            x = p.x;
            y = p.y;
            return (m_count == 1) ? Path.FlagsAndCommand.CommandMoveTo : Path.FlagsAndCommand.CommandLineTo;
        }

        private void bezier(double x1, double y1, 
                    double x2, double y2, 
                    double x3, double y3)
        {
            m_points.add(new Vector2(x1, y1));
            recursive_bezier(x1, y1, x2, y2, x3, y3, 0);
            m_points.add(new Vector2(x3, y3));
        }

        private void recursive_bezier(double x1, double y1, 
                              double x2, double y2, 
                              double x3, double y3,
                              int level)
        {
            if (level > (int)Curves.curve_recursion_limit_e.curve_recursion_limit)
            {
                return;
            }

            // Calculate all the mid-points of the line segments
            //----------------------
            double x12 = (x1 + x2) / 2;
            double y12 = (y1 + y2) / 2;
            double x23 = (x2 + x3) / 2;
            double y23 = (y2 + y3) / 2;
            double x123 = (x12 + x23) / 2;
            double y123 = (y12 + y23) / 2;

            double dx = x3 - x1;
            double dy = y3 - y1;
            double d = Math.Abs(((x2 - x3) * dy - (y2 - y3) * dx));
            double da;

            if (d > Curves.curve_collinearity_epsilon)
            {
                // Regular case
                //-----------------
                if (d * d <= m_distance_tolerance_square * (dx * dx + dy * dy))
                {
                    // If the curvature doesn't exceed the distance_tolerance value
                    // we tend to finish subdivisions.
                    //----------------------
                    if (m_angle_tolerance < Curves.curve_angle_tolerance_epsilon)
                    {
                        m_points.add(new Vector2(x123, y123));
                        return;
                    }

                    // Angle & Cusp Condition
                    //----------------------
                    da = Math.Abs(Math.Atan2(y3 - y2, x3 - x2) - Math.Atan2(y2 - y1, x2 - x1));
                    if (da >= Math.PI) da = 2 * Math.PI - da;

                    if (da < m_angle_tolerance)
                    {
                        // Finally we can stop the recursion
                        //----------------------
                        m_points.add(new Vector2(x123, y123));
                        return;
                    }
                }
            }
            else
            {
                // Collinear case
                //------------------
                da = dx * dx + dy * dy;
                if (da == 0)
                {
                    d = agg_math.calc_sq_distance(x1, y1, x2, y2);
                }
                else
                {
                    d = ((x2 - x1) * dx + (y2 - y1) * dy) / da;
                    if (d > 0 && d < 1)
                    {
                        // Simple collinear case, 1---2---3
                        // We can leave just two endpoints
                        return;
                    }
                    if (d <= 0) d = agg_math.calc_sq_distance(x2, y2, x1, y1);
                    else if (d >= 1) d = agg_math.calc_sq_distance(x2, y2, x3, y3);
                    else d = agg_math.calc_sq_distance(x2, y2, x1 + d * dx, y1 + d * dy);
                }
                if (d < m_distance_tolerance_square)
                {
                    m_points.add(new Vector2(x2, y2));
                    return;
                }
            }

            // Continue subdivision
            //----------------------
            recursive_bezier(x1, y1, x12, y12, x123, y123, level + 1);
            recursive_bezier(x123, y123, x23, y23, x3, y3, level + 1);
        }
    };

    //-------------------------------------------------------------curve4_points
    internal sealed class curve4_points
    {
        double[] cp = new double[8];
        public curve4_points() {}
        public curve4_points(double x1, double y1,
                      double x2, double y2,
                      double x3, double y3,
                      double x4, double y4)
        {
            cp[0] = x1; cp[1] = y1; cp[2] = x2; cp[3] = y2;
            cp[4] = x3; cp[5] = y3; cp[6] = x4; cp[7] = y4;
        }
        public void init(double x1, double y1,
                  double x2, double y2,
                  double x3, double y3,
                  double x4, double y4)
        {
            cp[0] = x1; cp[1] = y1; cp[2] = x2; cp[3] = y2;
            cp[4] = x3; cp[5] = y3; cp[6] = x4; cp[7] = y4;
        }

        public double this[int i]
        {
            get
            {
                return cp[i];
            }
        }

        //double  operator [] (int i){ return cp[i]; }
        //double& operator [] (int i)       { return cp[i]; }
    };


    //-------------------------------------------------------------curve4_inc
    internal sealed class curve4_inc
    {
        int      m_num_steps;
        int      m_step;
        double   m_scale;
        double   m_start_x; 
        double   m_start_y;
        double   m_end_x; 
        double   m_end_y;
        double   m_fx; 
        double   m_fy;
        double   m_dfx; 
        double   m_dfy;
        double   m_ddfx; 
        double   m_ddfy;
        double   m_dddfx; 
        double   m_dddfy;
        double   m_saved_fx; 
        double   m_saved_fy;
        double   m_saved_dfx; 
        double   m_saved_dfy;
        double   m_saved_ddfx; 
        double   m_saved_ddfy;

        public curve4_inc()
        {
            m_num_steps=(0);
            m_step=(0);
            m_scale=(1.0);
        }

        public curve4_inc(double x1, double y1, 
                   double x2, double y2, 
                   double x3, double y3,
                   double x4, double y4)
        { 
            m_num_steps=(0);
            m_step=(0);
            m_scale=(1.0);
            init(x1, y1, x2, y2, x3, y3, x4, y4);
        }

        public curve4_inc(curve4_points cp)
        { 
            m_num_steps=(0);
            m_step=(0);
            m_scale=(1.0);
            init(cp[0], cp[1], cp[2], cp[3], cp[4], cp[5], cp[6], cp[7]);
        }

        public void reset() { m_num_steps = 0; m_step = -1; }
        public void init(double x1, double y1, 
                  double x2, double y2, 
                  double x3, double y3,
                  double x4, double y4)
    {
        m_start_x = x1;
        m_start_y = y1;
        m_end_x   = x4;
        m_end_y   = y4;

        double dx1 = x2 - x1;
        double dy1 = y2 - y1;
        double dx2 = x3 - x2;
        double dy2 = y3 - y2;
        double dx3 = x4 - x3;
        double dy3 = y4 - y3;

        double len = (Math.Sqrt(dx1 * dx1 + dy1 * dy1) +
                      Math.Sqrt(dx2 * dx2 + dy2 * dy2) +
                      Math.Sqrt(dx3 * dx3 + dy3 * dy3)) * 0.25 * m_scale;

        m_num_steps = (int)agg_basics.uround(len);

        if(m_num_steps < 4)
        {
            m_num_steps = 4;   
        }

        double subdivide_step  = 1.0 / m_num_steps;
        double subdivide_step2 = subdivide_step * subdivide_step;
        double subdivide_step3 = subdivide_step * subdivide_step * subdivide_step;

        double pre1 = 3.0 * subdivide_step;
        double pre2 = 3.0 * subdivide_step2;
        double pre4 = 6.0 * subdivide_step2;
        double pre5 = 6.0 * subdivide_step3;
	
        double tmp1x = x1 - x2 * 2.0 + x3;
        double tmp1y = y1 - y2 * 2.0 + y3;

        double tmp2x = (x2 - x3) * 3.0 - x1 + x4;
        double tmp2y = (y2 - y3) * 3.0 - y1 + y4;

        m_saved_fx = m_fx = x1;
        m_saved_fy = m_fy = y1;

        m_saved_dfx = m_dfx = (x2 - x1) * pre1 + tmp1x * pre2 + tmp2x * subdivide_step3;
        m_saved_dfy = m_dfy = (y2 - y1) * pre1 + tmp1y * pre2 + tmp2y * subdivide_step3;

        m_saved_ddfx = m_ddfx = tmp1x * pre4 + tmp2x * pre5;
        m_saved_ddfy = m_ddfy = tmp1y * pre4 + tmp2y * pre5;

        m_dddfx = tmp2x * pre5;
        m_dddfy = tmp2y * pre5;

        m_step = m_num_steps;
    }


        public void init(curve4_points cp)
        {
            init(cp[0], cp[1], cp[2], cp[3], cp[4], cp[5], cp[6], cp[7]);
        }

        public void approximation_method(Curves.CurveApproximationMethod method) {}
        public Curves.CurveApproximationMethod approximation_method() { return Curves.CurveApproximationMethod.curve_inc; }

        public void approximation_scale(double s)
        {
            m_scale = s;
        }

        public double approximation_scale()
        {
            return m_scale;
        }

        public void angle_tolerance(double angle) {}
        public double angle_tolerance(){ return 0.0; }

        public void cusp_limit(double limit) {}
        public double cusp_limit(){ return 0.0; }

        public void rewind(int path_id)
        {
            if (m_num_steps == 0)
            {
                m_step = -1;
                return;
            }
            m_step = m_num_steps;
            m_fx = m_saved_fx;
            m_fy = m_saved_fy;
            m_dfx = m_saved_dfx;
            m_dfy = m_saved_dfy;
            m_ddfx = m_saved_ddfx;
            m_ddfy = m_saved_ddfy;
        }

        public Path.FlagsAndCommand vertex(out double x, out double y)
        {
            if (m_step < 0)
            {
                x = 0;
                y = 0;
                return Path.FlagsAndCommand.CommandStop;
            }

            if (m_step == m_num_steps)
            {
                x = m_start_x;
                y = m_start_y;
                --m_step;
                return Path.FlagsAndCommand.CommandMoveTo;
            }

            if (m_step == 0)
            {
                x = m_end_x;
                y = m_end_y;
                --m_step;
                return Path.FlagsAndCommand.CommandLineTo;
            }

            m_fx += m_dfx;
            m_fy += m_dfy;
            m_dfx += m_ddfx;
            m_dfy += m_ddfy;
            m_ddfx += m_dddfx;
            m_ddfy += m_dddfy;

            x = m_fx;
            y = m_fy;
            --m_step;
            return Path.FlagsAndCommand.CommandLineTo;
        }

    };


    //-------------------------------------------------------------curve4_div
    internal sealed class curve4_div
    {
        double               m_approximation_scale;
        double               m_distance_tolerance_square;
        double               m_angle_tolerance;
        double               m_cusp_limit;
        int m_count;
        VectorPOD<Vector2> m_points;

        public curve4_div()
        {
            m_points = new VectorPOD<Vector2>();
            m_approximation_scale=(1.0);
            m_angle_tolerance=(0.0);
            m_cusp_limit=(0.0);
            m_count=(0);
        }

        public curve4_div(double x1, double y1, 
                   double x2, double y2, 
                   double x3, double y3,
                   double x4, double y4)
        { 
            m_approximation_scale=(1.0);
            m_angle_tolerance=(0.0);
            m_cusp_limit=(0.0);
            m_count=(0);
            init(x1, y1, x2, y2, x3, y3, x4, y4);
        }

        public curve4_div(curve4_points cp)
        { 
            m_approximation_scale=(1.0);
            m_angle_tolerance=(0.0);
            m_count=(0);
            init(cp[0], cp[1], cp[2], cp[3], cp[4], cp[5], cp[6], cp[7]);
        }

        public void reset() { m_points.remove_all(); m_count = 0; }
        public void init(double x1, double y1, 
                  double x2, double y2, 
                  double x3, double y3,
                  double x4, double y4)
        {
            m_points.remove_all();
            m_distance_tolerance_square = 0.5 / m_approximation_scale;
            m_distance_tolerance_square *= m_distance_tolerance_square;
            bezier(x1, y1, x2, y2, x3, y3, x4, y4);
            m_count = 0;
        }


        public void init(curve4_points cp)
        {
            init(cp[0], cp[1], cp[2], cp[3], cp[4], cp[5], cp[6], cp[7]);
        }

        public void approximation_method(Curves.CurveApproximationMethod method) { }

        public Curves.CurveApproximationMethod approximation_method()
        {
            return Curves.CurveApproximationMethod.curve_div; 
        }

        public void approximation_scale(double s) { m_approximation_scale = s; }
        public double approximation_scale(){ return m_approximation_scale;  }

        public void angle_tolerance(double a) { m_angle_tolerance = a; }
        public double angle_tolerance(){ return m_angle_tolerance;  }

        public void cusp_limit(double v) 
        {
            m_cusp_limit = (v == 0.0) ? 0.0 : Math.PI - v; 
        }

        public double cusp_limit()
        {
            return (m_cusp_limit == 0.0) ? 0.0 : Math.PI - m_cusp_limit; 
        }

        public void rewind(int idx)
        {
            m_count = 0;
        }

        public Path.FlagsAndCommand vertex(out double x, out double y)
        {
            if (m_count >= m_points.size())
            {
                x = 0;
                y = 0;
                return Path.FlagsAndCommand.CommandStop;
            }
            Vector2 p = m_points[m_count++];
            x = p.x;
            y = p.y;
            return (m_count == 1) ? Path.FlagsAndCommand.CommandMoveTo : Path.FlagsAndCommand.CommandLineTo;
        }

        private void bezier(double x1, double y1, 
                    double x2, double y2, 
                    double x3, double y3, 
                    double x4, double y4)
        {
            m_points.add(new Vector2(x1, y1));
            recursive_bezier(x1, y1, x2, y2, x3, y3, x4, y4, 0);
            m_points.add(new Vector2(x4, y4));
        }


        private void recursive_bezier(double x1, double y1, 
                              double x2, double y2, 
                              double x3, double y3, 
                              double x4, double y4,
                              int level)
    {
        if (level > (int)Curves.curve_recursion_limit_e.curve_recursion_limit) 
        {
            return;
        }

        // Calculate all the mid-points of the line segments
        //----------------------
        double x12   = (x1 + x2) / 2;
        double y12   = (y1 + y2) / 2;
        double x23   = (x2 + x3) / 2;
        double y23   = (y2 + y3) / 2;
        double x34   = (x3 + x4) / 2;
        double y34   = (y3 + y4) / 2;
        double x123  = (x12 + x23) / 2;
        double y123  = (y12 + y23) / 2;
        double x234  = (x23 + x34) / 2;
        double y234  = (y23 + y34) / 2;
        double x1234 = (x123 + x234) / 2;
        double y1234 = (y123 + y234) / 2;


        // Try to approximate the full cubic curve by a single straight line
        //------------------
        double dx = x4-x1;
        double dy = y4-y1;

        double d2 = Math.Abs(((x2 - x4) * dy - (y2 - y4) * dx));
        double d3 = Math.Abs(((x3 - x4) * dy - (y3 - y4) * dx));
        double da1, da2, k;

        int SwitchCase = 0;
        if(d2 > Curves.curve_collinearity_epsilon)
        {
            SwitchCase = 2;
        }
        if(d3 > Curves.curve_collinearity_epsilon)
        {
            SwitchCase++;
        }

        switch(SwitchCase)
        {
        case 0:
            // All collinear OR p1==p4
            //----------------------
            k = dx*dx + dy*dy;
            if(k == 0)
            {
                d2 = agg_math.calc_sq_distance(x1, y1, x2, y2);
                d3 = agg_math.calc_sq_distance(x4, y4, x3, y3);
            }
            else
            {
                k   = 1 / k;
                da1 = x2 - x1;
                da2 = y2 - y1;
                d2  = k * (da1*dx + da2*dy);
                da1 = x3 - x1;
                da2 = y3 - y1;
                d3  = k * (da1*dx + da2*dy);
                if(d2 > 0 && d2 < 1 && d3 > 0 && d3 < 1)
                {
                    // Simple collinear case, 1---2---3---4
                    // We can leave just two endpoints
                    return;
                }
                     if(d2 <= 0) d2 = agg_math.calc_sq_distance(x2, y2, x1, y1);
                else if(d2 >= 1) d2 = agg_math.calc_sq_distance(x2, y2, x4, y4);
                else             d2 = agg_math.calc_sq_distance(x2, y2, x1 + d2*dx, y1 + d2*dy);

                     if(d3 <= 0) d3 = agg_math.calc_sq_distance(x3, y3, x1, y1);
                else if(d3 >= 1) d3 = agg_math.calc_sq_distance(x3, y3, x4, y4);
                else             d3 = agg_math.calc_sq_distance(x3, y3, x1 + d3*dx, y1 + d3*dy);
            }
            if(d2 > d3)
            {
                if(d2 < m_distance_tolerance_square)
                {
                    m_points.add(new Vector2(x2, y2));
                    return;
                }
            }
            else
            {
                if(d3 < m_distance_tolerance_square)
                {
                    m_points.add(new Vector2(x3, y3));
                    return;
                }
            }
            break;

        case 1:
            // p1,p2,p4 are collinear, p3 is significant
            //----------------------
            if(d3 * d3 <= m_distance_tolerance_square * (dx*dx + dy*dy))
            {
                if(m_angle_tolerance < Curves.curve_angle_tolerance_epsilon)
                {
                    m_points.add(new Vector2(x23, y23));
                    return;
                }

                // Angle Condition
                //----------------------
                da1 = Math.Abs(Math.Atan2(y4 - y3, x4 - x3) - Math.Atan2(y3 - y2, x3 - x2));
                if(da1 >= Math.PI) da1 = 2*Math.PI - da1;

                if(da1 < m_angle_tolerance)
                {
                    m_points.add(new Vector2(x2, y2));
                    m_points.add(new Vector2(x3, y3));
                    return;
                }

                if(m_cusp_limit != 0.0)
                {
                    if(da1 > m_cusp_limit)
                    {
                        m_points.add(new Vector2(x3, y3));
                        return;
                    }
                }
            }
            break;

        case 2:
            // p1,p3,p4 are collinear, p2 is significant
            //----------------------
            if(d2 * d2 <= m_distance_tolerance_square * (dx*dx + dy*dy))
            {
                if(m_angle_tolerance < Curves.curve_angle_tolerance_epsilon)
                {
                    m_points.add(new Vector2(x23, y23));
                    return;
                }

                // Angle Condition
                //----------------------
                da1 = Math.Abs(Math.Atan2(y3 - y2, x3 - x2) - Math.Atan2(y2 - y1, x2 - x1));
                if(da1 >= Math.PI) da1 = 2*Math.PI - da1;

                if(da1 < m_angle_tolerance)
                {
                    m_points.add(new Vector2(x2, y2));
                    m_points.add(new Vector2(x3, y3));
                    return;
                }

                if(m_cusp_limit != 0.0)
                {
                    if(da1 > m_cusp_limit)
                    {
                        m_points.add(new Vector2(x2, y2));
                        return;
                    }
                }
            }
            break;

        case 3: 
            // Regular case
            //-----------------
            if((d2 + d3)*(d2 + d3) <= m_distance_tolerance_square * (dx*dx + dy*dy))
            {
                // If the curvature doesn't exceed the distance_tolerance value
                // we tend to finish subdivisions.
                //----------------------
                if(m_angle_tolerance < Curves.curve_angle_tolerance_epsilon)
                {
                    m_points.add(new Vector2(x23, y23));
                    return;
                }

                // Angle & Cusp Condition
                //----------------------
                k   = Math.Atan2(y3 - y2, x3 - x2);
                da1 = Math.Abs(k - Math.Atan2(y2 - y1, x2 - x1));
                da2 = Math.Abs(Math.Atan2(y4 - y3, x4 - x3) - k);
                if(da1 >= Math.PI) da1 = 2*Math.PI - da1;
                if(da2 >= Math.PI) da2 = 2*Math.PI - da2;

                if(da1 + da2 < m_angle_tolerance)
                {
                    // Finally we can stop the recursion
                    //----------------------
                    m_points.add(new Vector2(x23, y23));
                    return;
                }

                if(m_cusp_limit != 0.0)
                {
                    if(da1 > m_cusp_limit)
                    {
                        m_points.add(new Vector2(x2, y2));
                        return;
                    }

                    if(da2 > m_cusp_limit)
                    {
                        m_points.add(new Vector2(x3, y3));
                        return;
                    }
                }
            }
            break;
        }

        // Continue subdivision
        //----------------------
        recursive_bezier(x1, y1, x12, y12, x123, y123, x1234, y1234, level + 1); 
        recursive_bezier(x1234, y1234, x234, y234, x34, y34, x4, y4, level + 1); 
    }

    };

    //-----------------------------------------------------------------curve3
    internal sealed class Curve3 : ICurve
    {
        curve3_inc m_curve_inc;
        curve3_div m_curve_div;
        Curves.CurveApproximationMethod m_approximation_method;

        public Curve3() 
        {
            m_curve_inc = new curve3_inc();
            m_curve_div = new curve3_div();
            m_approximation_method = Curves.CurveApproximationMethod.curve_div;
        }
        public Curve3(double x1, double y1, 
               double x2, double y2, 
               double x3, double y3) : base()
        { 
            m_approximation_method = Curves.CurveApproximationMethod.curve_div;
            init(x1, y1, x2, y2, x3, y3);
        }

        public void reset() 
        { 
            m_curve_inc.reset();
            m_curve_div.reset();
        }

        public void init(double x1, double y1, 
                  double x2, double y2, 
                  double x3, double y3)
        {
            if (m_approximation_method == Curves.CurveApproximationMethod.curve_inc) 
            {
                m_curve_inc.init(x1, y1, x2, y2, x3, y3);
            }
            else
            {
                m_curve_div.init(x1, y1, x2, y2, x3, y3);
            }
        }

        public void approximation_method(Curves.CurveApproximationMethod v) 
        { 
            m_approximation_method = v; 
        }

        public Curves.CurveApproximationMethod approximation_method()
        { 
            return m_approximation_method; 
        }

        public void approximation_scale(double s) 
        { 
            m_curve_inc.approximation_scale(s);
            m_curve_div.approximation_scale(s);
        }

        public double approximation_scale()
        { 
            return m_curve_inc.approximation_scale(); 
        }

        public void angle_tolerance(double a) 
        { 
            m_curve_div.angle_tolerance(a); 
        }

        public double angle_tolerance()
        { 
            return m_curve_div.angle_tolerance(); 
        }

        public void cusp_limit(double v) 
        { 
            m_curve_div.cusp_limit(v); 
        }

        public double cusp_limit()
        { 
            return m_curve_div.cusp_limit();  
        }

        public void rewind(int path_id)
        {
            if (m_approximation_method == Curves.CurveApproximationMethod.curve_inc) 
            {
                m_curve_inc.rewind(path_id);
            }
            else
            {
                m_curve_div.rewind(path_id);
            }
        }

        public Path.FlagsAndCommand vertex(out double x, out double y)
        {
            if (m_approximation_method == Curves.CurveApproximationMethod.curve_inc) 
            {
                return m_curve_inc.vertex(out x, out y);
            }
            return m_curve_div.vertex(out x, out y);
        }
    };



    //-----------------------------------------------------------------curve4
    internal sealed class Curve4 : ICurve
    {
        curve4_inc m_curve_inc;
        curve4_div m_curve_div;
        Curves.CurveApproximationMethod m_approximation_method;

        public Curve4()
        {
            m_curve_inc = new curve4_inc();
            m_curve_div = new curve4_div();
            m_approximation_method = Curves.CurveApproximationMethod.curve_div;
        }
        public Curve4(double x1, double y1, 
               double x2, double y2, 
               double x3, double y3,
               double x4, double y4) : base()
        { 
            m_approximation_method = Curves.CurveApproximationMethod.curve_div;
            init(x1, y1, x2, y2, x3, y3, x4, y4);
        }

        public Curve4(curve4_points cp)
        { 
            m_approximation_method = Curves.CurveApproximationMethod.curve_div;
            init(cp[0], cp[1], cp[2], cp[3], cp[4], cp[5], cp[6], cp[7]);
        }

        public void reset() 
        { 
            m_curve_inc.reset();
            m_curve_div.reset();
        }

        public void init(double x1, double y1, 
                  double x2, double y2, 
                  double x3, double y3,
                  double x4, double y4)
        {
            if (m_approximation_method == Curves.CurveApproximationMethod.curve_inc) 
            {
                m_curve_inc.init(x1, y1, x2, y2, x3, y3, x4, y4);
            }
            else
            {
                m_curve_div.init(x1, y1, x2, y2, x3, y3, x4, y4);
            }
        }

        public void init(curve4_points cp)
        {
            init(cp[0], cp[1], cp[2], cp[3], cp[4], cp[5], cp[6], cp[7]);
        }

        public void approximation_method(Curves.CurveApproximationMethod v) 
        { 
            m_approximation_method = v; 
        }

        public Curves.CurveApproximationMethod approximation_method()
        { 
            return m_approximation_method; 
        }

        public void approximation_scale(double s) 
        { 
            m_curve_inc.approximation_scale(s);
            m_curve_div.approximation_scale(s);
        }
        public double approximation_scale(){ return m_curve_inc.approximation_scale(); }

        public void angle_tolerance(double v) 
        { 
            m_curve_div.angle_tolerance(v); 
        }

        public double angle_tolerance()
        { 
            return m_curve_div.angle_tolerance();  
        }

        public void cusp_limit(double v) 
        { 
            m_curve_div.cusp_limit(v); 
        }

        public double cusp_limit()
        { 
            return m_curve_div.cusp_limit();  
        }

        public void rewind(int path_id)
        {
            if (m_approximation_method == Curves.CurveApproximationMethod.curve_inc) 
            {
                m_curve_inc.rewind(path_id);
            }
            else
            {
                m_curve_div.rewind(path_id);
            }
        }

        public Path.FlagsAndCommand vertex(out double x, out double y)
        {
            if (m_approximation_method == Curves.CurveApproximationMethod.curve_inc) 
            {
                return m_curve_inc.vertex(out x, out y);
            }
            return m_curve_div.vertex(out x, out y);
        }
    };
}