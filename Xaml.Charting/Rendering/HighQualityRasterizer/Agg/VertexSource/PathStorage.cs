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
using System;
using MatterHackers.VectorMath;

namespace MatterHackers.Agg.VertexSource
{
    //---------------------------------------------------------------path_base
    // A container to store vertices with their flags. 
    // A path consists of a number of contours separated with "move_to" 
    // commands. The path storage can keep and maintain more than one
    // path. 
    // To navigate to the beginning of a particular path, use rewind(path_id);
    // Where path_id is what start_new_path() returns. So, when you call
    // start_new_path() you need to store its return value somewhere else
    // to navigate to the path afterwards.
    //
    // See also: vertex_source concept
    //------------------------------------------------------------------------
    internal class PathStorage : IVertexSource, IVertexDest
    {

                private class VertexStorage
        {
            int m_num_vertices;
            int m_allocated_vertices;
            double[] m_coord_x;
            double[] m_coord_y;
            Path.FlagsAndCommand[] m_CommandAndFlags;
            
            public void free_all()
            {
                m_coord_x = null;
                m_coord_y = null;
                m_CommandAndFlags = null;

                m_num_vertices = 0;
            }

            public int size()
            {
                return m_num_vertices;
            }

            public VertexStorage()
            {
            }

            public void remove_all()
            {
                m_num_vertices = 0;
            }

            public void AddVertex(double x, double y, Path.FlagsAndCommand CommandAndFlags)
            {
                allocate_if_required(m_num_vertices);
                m_coord_x[m_num_vertices] = x;
                m_coord_y[m_num_vertices] = y;
                m_CommandAndFlags[m_num_vertices] = CommandAndFlags;

                m_num_vertices++;
            }

            public void modify_vertex(int index, double x, double y)
            {
                m_coord_x[index] = x;
                m_coord_y[index] = y;
            }

            public void modify_vertex(int index, double x, double y, Path.FlagsAndCommand CommandAndFlags)
            {
                m_coord_x[index] = x;
                m_coord_y[index] = y;
                m_CommandAndFlags[index] = CommandAndFlags;
            }

            public void modify_command(int index, Path.FlagsAndCommand CommandAndFlags)
            {
                m_CommandAndFlags[index] = CommandAndFlags;
            }

            public void swap_vertices(int v1, int v2)
            {
                double val;

                val = m_coord_x[v1]; m_coord_x[v1] = m_coord_x[v2]; m_coord_x[v2] = val;
                val = m_coord_y[v1]; m_coord_y[v1] = m_coord_y[v2]; m_coord_y[v2] = val;
                Path.FlagsAndCommand cmd = m_CommandAndFlags[v1];
                m_CommandAndFlags[v1] = m_CommandAndFlags[v2];
                m_CommandAndFlags[v2] = cmd;
            }

            public Path.FlagsAndCommand last_command()
            {
                if (m_num_vertices != 0)
                {
                    return command(m_num_vertices - 1);
                }

                return Path.FlagsAndCommand.CommandStop;
            }

            public Path.FlagsAndCommand last_vertex(out double x, out double y)
            {
                if (m_num_vertices != 0)
                {
                    return vertex((int)(m_num_vertices - 1), out x, out y);
                }

                x = new double();
                y = new double();
                return Path.FlagsAndCommand.CommandStop;
            }

            public Path.FlagsAndCommand prev_vertex(out double x, out double y)
            {
                if (m_num_vertices > 1)
                {
                    return vertex((int)(m_num_vertices - 2), out x, out y);
                }

                x = new double();
                y = new double();
                return Path.FlagsAndCommand.CommandStop;
            }

            public double last_x()
            {
                if (m_num_vertices > 0)
                {
                    int index = (int)(m_num_vertices - 1);
                    return m_coord_x[index];
                }

                return new double();
            }

            public double last_y()
            {
                if (m_num_vertices > 0)
                {
                    int index = (int)(m_num_vertices - 1);
                    return m_coord_y[index];
                }
                return new double();
            }

            public int total_vertices()
            {
                return m_num_vertices;
            }

            public Path.FlagsAndCommand vertex(int index, out double x, out double y)
            {
                x = m_coord_x[index];
                y = m_coord_y[index];
                return m_CommandAndFlags[index];
            }

            public Path.FlagsAndCommand command(int index)
            {
                return m_CommandAndFlags[index];
            }

            private void allocate_if_required(int indexToAdd)
            {
                if (indexToAdd < m_num_vertices)
                {
                    return;
                }

                while (indexToAdd >= m_allocated_vertices)
                {
                    int newSize = m_allocated_vertices + 256;
                    double[] newX = new double[newSize];
                    double[] newY = new double[newSize];
                    Path.FlagsAndCommand[] newCmd = new Path.FlagsAndCommand[newSize];

                    if (m_coord_x != null)
                    {
                        for (int i = 0; i < m_num_vertices; i++)
                        {
                            newX[i] = m_coord_x[i];
                            newY[i] = m_coord_y[i];
                            newCmd[i] = m_CommandAndFlags[i];
                        }
                    }

                    m_coord_x = newX;
                    m_coord_y = newY;
                    m_CommandAndFlags = newCmd;

                    m_allocated_vertices = newSize;
                }
            }
        };

        private VertexStorage vertices;
        private int iteratorIndex;

        public PathStorage()
        {
            vertices = new VertexStorage();
        }

        public void add(Vector2 vertex)
        {
            throw new System.NotImplementedException();
        }

        public int size()
        {
            return vertices.size();
        }
        
        public Vector2 this[int i]
        {
            get
            {
                throw new NotImplementedException("make this work");
            }
        }

        public void remove_all() { vertices.remove_all(); iteratorIndex = 0; }
        public void free_all()   { vertices.free_all();   iteratorIndex = 0; }

        // Make path functions
        //--------------------------------------------------------------------
        public int start_new_path()
        {
            if(!Path.is_stop(vertices.last_command()))
            {
                vertices.AddVertex(0.0, 0.0, Path.FlagsAndCommand.CommandStop);
            }
            return vertices.total_vertices();
        }


        public void rel_to_abs(ref double x, ref double y)
        {
            if(vertices.total_vertices() != 0)
            {
                double x2;
                double y2;
                if(Path.is_vertex(vertices.last_vertex(out x2, out y2)))
                {
                    x += x2;
                    y += y2;
                }
            }
        }

        public void MoveTo(double x, double y)
        {
            vertices.AddVertex(x, y, Path.FlagsAndCommand.CommandMoveTo);
        }

        public void LineTo(double x, double y)
        {
            vertices.AddVertex(x, y, Path.FlagsAndCommand.CommandLineTo);
        }

        public void HorizontalLineTo(double x)
        {
            vertices.AddVertex(x, GetLastY(), Path.FlagsAndCommand.CommandLineTo);
        }

        public void VerticalLineTo(double y)
        {
            vertices.AddVertex(GetLastX(), y, Path.FlagsAndCommand.CommandLineTo);
        }

        /*
        public void arc_to(double rx, double ry,
                                   double angle,
                                   bool large_arc_flag,
                                   bool sweep_flag,
                                   double x, double y)
        {
            if(m_vertices.total_vertices() && is_vertex(m_vertices.last_command()))
            {
                double epsilon = 1e-30;
                double x0 = 0.0;
                double y0 = 0.0;
                m_vertices.last_vertex(&x0, &y0);

                rx = fabs(rx);
                ry = fabs(ry);

                // Ensure radii are valid
                //-------------------------
                if(rx < epsilon || ry < epsilon) 
                {
                    line_to(x, y);
                    return;
                }

                if(calc_distance(x0, y0, x, y) < epsilon)
                {
                    // If the endpoints (x, y) and (x0, y0) are identical, then this
                    // is equivalent to omitting the elliptical arc segment entirely.
                    return;
                }
                bezier_arc_svg a(x0, y0, rx, ry, angle, large_arc_flag, sweep_flag, x, y);
                if(a.radii_ok())
                {
                    join_path(a);
                }
                else
                {
                    line_to(x, y);
                }
            }
            else
            {
                move_to(x, y);
            }
        }

        public void arc_rel(double rx, double ry,
                                    double angle,
                                    bool large_arc_flag,
                                    bool sweep_flag,
                                    double dx, double dy)
        {
            rel_to_abs(&dx, &dy);
            arc_to(rx, ry, angle, large_arc_flag, sweep_flag, dx, dy);
        }
         */

        /// <summary>
        /// Draws a quadratic B�zier curve from the current point to (x,y) using (xControl,yControl) as the control point.
        /// </summary>
        /// <param name="xControl"></param>
        /// <param name="yControl"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void curve3(double xControl, double yControl, 
                                   double x,   double y)
        {
            vertices.AddVertex(xControl, yControl, Path.FlagsAndCommand.CommandCurve3);
            vertices.AddVertex(x, y, Path.FlagsAndCommand.CommandCurve3);
        }

        /// <summary>
        /// Draws a quadratic B�zier curve from the current point to (x,y) using (xControl,yControl) as the control point.
        /// </summary>
        public void curve3_rel(double dx_ctrl, double dy_ctrl, double dx_to, double dy_to)
        {
            rel_to_abs(ref dx_ctrl, ref dy_ctrl);
            rel_to_abs(ref dx_to,   ref dy_to);
            vertices.AddVertex(dx_ctrl, dy_ctrl, Path.FlagsAndCommand.CommandCurve3);
            vertices.AddVertex(dx_to, dy_to, Path.FlagsAndCommand.CommandCurve3);
        }

        /// <summary>
        /// <para>Draws a quadratic B�zier curve from the current point to (x,y).</para>
        /// <para>The control point is assumed to be the reflection of the control point on the previous command relative to the current point.</para>
        /// <para>(If there is no previous command or if the previous command was not a curve, assume the control point is coincident with the current point.)</para>
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void curve3(double x, double y)
        {
            double x0;
            double y0;
            if(Path.is_vertex(vertices.last_vertex(out x0, out y0)))
            {
                double x_ctrl;
                double y_ctrl;
                Path.FlagsAndCommand cmd = vertices.prev_vertex(out x_ctrl, out y_ctrl);
                if(Path.is_curve(cmd))
                {
                    x_ctrl = x0 + x0 - x_ctrl;
                    y_ctrl = y0 + y0 - y_ctrl;
                }
                else
                {
                    x_ctrl = x0;
                    y_ctrl = y0;
                }
                curve3(x_ctrl, y_ctrl, x, y);
            }
        }

        /// <summary>
        /// <para>Draws a quadratic B�zier curve from the current point to (x,y).</para>
        /// <para>The control point is assumed to be the reflection of the control point on the previous command relative to the current point.</para>
        /// <para>(If there is no previous command or if the previous command was not a curve, assume the control point is coincident with the current point.)</para>
        /// </summary>
        public void curve3_rel(double dx_to, double dy_to)
        {
            rel_to_abs(ref dx_to, ref dy_to);
            curve3(dx_to, dy_to);
        }

        public void curve4(double x_ctrl1, double y_ctrl1, 
                                   double x_ctrl2, double y_ctrl2, 
                                   double x_to,    double y_to)
        {
            vertices.AddVertex(x_ctrl1, y_ctrl1, Path.FlagsAndCommand.CommandCurve4);
            vertices.AddVertex(x_ctrl2, y_ctrl2, Path.FlagsAndCommand.CommandCurve4);
            vertices.AddVertex(x_to, y_to, Path.FlagsAndCommand.CommandCurve4);
        }

        public void curve4_rel(double dx_ctrl1, double dy_ctrl1, 
                                       double dx_ctrl2, double dy_ctrl2, 
                                       double dx_to,    double dy_to)
        {
            rel_to_abs(ref dx_ctrl1, ref dy_ctrl1);
            rel_to_abs(ref dx_ctrl2, ref dy_ctrl2);
            rel_to_abs(ref dx_to, ref dy_to);
            vertices.AddVertex(dx_ctrl1, dy_ctrl1, Path.FlagsAndCommand.CommandCurve4);
            vertices.AddVertex(dx_ctrl2, dy_ctrl2, Path.FlagsAndCommand.CommandCurve4);
            vertices.AddVertex(dx_to, dy_to, Path.FlagsAndCommand.CommandCurve4);
        }

        public void curve4(double x_ctrl2, double y_ctrl2, 
                                   double x_to,    double y_to)
        {
            double x0;
            double y0;
            if(Path.is_vertex(last_vertex(out x0, out y0)))
            {
                double x_ctrl1;
                double y_ctrl1;
                Path.FlagsAndCommand cmd = prev_vertex(out x_ctrl1, out y_ctrl1);
                if(Path.is_curve(cmd))
                {
                    x_ctrl1 = x0 + x0 - x_ctrl1;
                    y_ctrl1 = y0 + y0 - y_ctrl1;
                }
                else
                {
                    x_ctrl1 = x0;
                    y_ctrl1 = y0;
                }
                curve4(x_ctrl1, y_ctrl1, x_ctrl2, y_ctrl2, x_to, y_to);
            }
        }

        public void curve4_rel(double dx_ctrl2, double dy_ctrl2, 
                                       double dx_to,    double dy_to)
        {
            rel_to_abs(ref dx_ctrl2, ref dy_ctrl2);
            rel_to_abs(ref dx_to,    ref dy_to);
            curve4(dx_ctrl2, dy_ctrl2, dx_to, dy_to);
        }

        public int total_vertices()
        {
            return vertices.total_vertices();
        }

        public Path.FlagsAndCommand last_vertex(out double x, out double y)
        {
            return vertices.last_vertex(out x, out y);
        }

        public Path.FlagsAndCommand prev_vertex(out double x, out double y)
        {
            return vertices.prev_vertex(out x, out y);
        }

        public double GetLastX()
        {
            return vertices.last_x();
        }

        public double GetLastY()
        {
            return vertices.last_y();
        }

        public Path.FlagsAndCommand vertex(int index, out double x, out double y)
        {
            return vertices.vertex(index, out x, out y);
        }

        public Path.FlagsAndCommand command(int index)
        {
            return vertices.command(index);
        }

        public void modify_vertex(int index, double x, double y)
        {
            vertices.modify_vertex(index, x, y);
        }

        public void modify_vertex(int index, double x, double y, Path.FlagsAndCommand PathAndFlags)
        {
            vertices.modify_vertex(index, x, y, PathAndFlags);
        }

        public void modify_command(int index, Path.FlagsAndCommand PathAndFlags)
        {
            vertices.modify_command(index, PathAndFlags);
        }

        public virtual void rewind(int path_id)
        {
            iteratorIndex = path_id;
        }

        public Path.FlagsAndCommand vertex(out double x, out double y)
        {
            if (iteratorIndex >= vertices.total_vertices())
            {
                x = 0;
                y = 0;
                return Path.FlagsAndCommand.CommandStop;
            }

            return vertices.vertex(iteratorIndex++, out x, out y);
        }

        // Arrange the orientation of a polygon, all polygons in a path, 
        // or in all paths. After calling arrange_orientations() or 
        // arrange_orientations_all_paths(), all the polygons will have 
        // the same orientation, i.e. path_flags_cw or path_flags_ccw
        //--------------------------------------------------------------------
        public int arrange_polygon_orientation(int start, Path.FlagsAndCommand orientation)
        {
            if(orientation == Path.FlagsAndCommand.FlagNone) return start;
            
            // Skip all non-vertices at the beginning
            while(start < vertices.total_vertices() && 
                  !Path.is_vertex(vertices.command(start))) ++start;

            // Skip all insignificant move_to
            while(start+1 < vertices.total_vertices() && 
                  Path.is_move_to(vertices.command(start)) &&
                  Path.is_move_to(vertices.command(start+1))) ++start;

            // Find the last vertex
            int end = start + 1;
            while(end < vertices.total_vertices() && 
                  !Path.is_next_poly(vertices.command(end))) ++end;

            if(end - start > 2)
            {
                if(perceive_polygon_orientation(start, end) != orientation)
                {
                    // Invert polygon, set orientation flag, and skip all end_poly
                    invert_polygon(start, end);
                    Path.FlagsAndCommand PathAndFlags;
                    while(end < vertices.total_vertices() &&
                          Path.is_end_poly(PathAndFlags = vertices.command(end)))
                    {
                        vertices.modify_command(end++, PathAndFlags | orientation);// Path.set_orientation(cmd, orientation));
                    }
                }
            }
            return end;
        }

        public int arrange_orientations(int start, Path.FlagsAndCommand orientation)
        {
            if(orientation != Path.FlagsAndCommand.FlagNone)
            {
                while(start < vertices.total_vertices())
                {
                    start = arrange_polygon_orientation(start, orientation);
                    if(Path.is_stop(vertices.command(start)))
                    {
                        ++start;
                        break;
                    }
                }
            }
            return start;
        }

        public void arrange_orientations_all_paths(Path.FlagsAndCommand orientation)
        {
            if(orientation != Path.FlagsAndCommand.FlagNone)
            {
                int start = 0;
                while(start < vertices.total_vertices())
                {
                    start = arrange_orientations(start, orientation);
                }
            }
        }

        // Flip all vertices horizontally or vertically, 
        // between x1 and x2, or between y1 and y2 respectively
        //--------------------------------------------------------------------
        public void flip_x(double x1, double x2)
        {
            int i;
            double x, y;
            for(i = 0; i < vertices.total_vertices(); i++)
            {
                Path.FlagsAndCommand PathAndFlags = vertices.vertex(i, out x, out y);
                if (Path.is_vertex(PathAndFlags))
                {
                    vertices.modify_vertex(i, x2 - x + x1, y);
                }
            }
        }

        public void flip_y(double y1, double y2)
        {
            int i;
            double x, y;
            for(i = 0; i < vertices.total_vertices(); i++)
            {
                Path.FlagsAndCommand PathAndFlags = vertices.vertex(i, out x, out y);
                if (Path.is_vertex(PathAndFlags))
                {
                    vertices.modify_vertex(i, x, y2 - y + y1);
                }
            }
        }

        public void end_poly()
        {
            close_polygon(Path.FlagsAndCommand.FlagClose);
        }

        public void end_poly(Path.FlagsAndCommand flags)
        {
            if (Path.is_vertex(vertices.last_command()))
            {
                vertices.AddVertex(0.0, 0.0, Path.FlagsAndCommand.CommandEndPoly | flags);
            }
        }


        public void ClosePolygon()
        {
            close_polygon(Path.FlagsAndCommand.FlagNone);
        }

        public void close_polygon(Path.FlagsAndCommand flags)
        {
            end_poly(Path.FlagsAndCommand.FlagClose | flags);
        }

        // Concatenate path. The path is added as is.
        public void concat_path(IVertexSource vs)
        {
            concat_path(vs, 0);
        }

        public void concat_path(IVertexSource vs, int path_id)
        {
            double x, y;
            Path.FlagsAndCommand PathAndFlags;
            vs.rewind(path_id);
            while (!Path.is_stop(PathAndFlags = vs.vertex(out x, out y)))
            {
                vertices.AddVertex(x, y, PathAndFlags);
            }
        }

        //--------------------------------------------------------------------
        // Join path. The path is joined with the existing one, that is, 
        // it behaves as if the pen of a plotter was always down (drawing)
        //template<class VertexSource> 
        public void join_path(PathStorage vs)
        {
            join_path(vs, 0);

        }

        public void join_path(PathStorage vs, int path_id)
        {
            double x, y;
            vs.rewind(path_id);
            Path.FlagsAndCommand PathAndFlags = vs.vertex(out x, out y);
            if (!Path.is_stop(PathAndFlags))
            {
                if (Path.is_vertex(PathAndFlags))
                {
                    double x0, y0;
                    Path.FlagsAndCommand PathAndFlags0 = last_vertex(out x0, out y0);
                    if (Path.is_vertex(PathAndFlags0))
                    {
                        if(agg_math.calc_distance(x, y, x0, y0) > agg_math.vertex_dist_epsilon)
                        {
                            if (Path.is_move_to(PathAndFlags)) PathAndFlags = Path.FlagsAndCommand.CommandLineTo;
                            vertices.AddVertex(x, y, PathAndFlags);
                        }
                    }
                    else
                    {
                        if (Path.is_stop(PathAndFlags0))
                        {
                            PathAndFlags = Path.FlagsAndCommand.CommandMoveTo;
                        }
                        else
                        {
                            if (Path.is_move_to(PathAndFlags)) PathAndFlags = Path.FlagsAndCommand.CommandLineTo;
                        }
                        vertices.AddVertex(x, y, PathAndFlags);
                    }
                }
                while (!Path.is_stop(PathAndFlags = vs.vertex(out x, out y)))
                {
                    vertices.AddVertex(x, y, Path.is_move_to(PathAndFlags) ?
                                                    Path.FlagsAndCommand.CommandLineTo :
                                                    PathAndFlags);
                }
            }
        }

        /*
        // Concatenate polygon/polyline. 
        //--------------------------------------------------------------------
        void concat_poly(T* data, int num_points, bool closed)
        {
            poly_plain_adaptor<T> poly(data, num_points, closed);
            concat_path(poly);
        }

        // Join polygon/polyline continuously.
        //--------------------------------------------------------------------
        void join_poly(T* data, int num_points, bool closed)
        {
            poly_plain_adaptor<T> poly(data, num_points, closed);
            join_path(poly);
        }
         */

        //--------------------------------------------------------------------
        public void translate(double dx, double dy)
        {
            translate(dx, dy, 0);
        }

        public void translate(double dx, double dy, int path_id)
        {
            int num_ver = vertices.total_vertices();
            for(; path_id < num_ver; path_id++)
            {
                double x, y;
                Path.FlagsAndCommand PathAndFlags = vertices.vertex(path_id, out x, out y);
                if (Path.is_stop(PathAndFlags)) break;
                if (Path.is_vertex(PathAndFlags))
                {
                    x += dx;
                    y += dy;
                    vertices.modify_vertex(path_id, x, y);
                }
            }
        }

        public void translate_all_paths(double dx, double dy)
        {
            int index;
            int num_ver = vertices.total_vertices();
            for (index = 0; index < num_ver; index++)
            {
                double x, y;
                if (Path.is_vertex(vertices.vertex(index, out x, out y)))
                {
                    x += dx;
                    y += dy;
                    vertices.modify_vertex(index, x, y);
                }
            }
        }

        //--------------------------------------------------------------------
        public void transform(Transform.Affine trans)
        {
            transform(trans, 0);
        }

        public void transform(Transform.Affine trans, int path_id)
        {
            int num_ver = vertices.total_vertices();
            for(; path_id < num_ver; path_id++)
            {
                double x, y;
                Path.FlagsAndCommand PathAndFlags = vertices.vertex(path_id, out x, out y);
                if (Path.is_stop(PathAndFlags)) break;
                if (Path.is_vertex(PathAndFlags))
                {
                    trans.transform(ref x, ref y);
                    vertices.modify_vertex(path_id, x, y);
                }
            }
        }

        //--------------------------------------------------------------------
        public void transform_all_paths(Transform.Affine trans)
        {
            int index;
            int num_ver = vertices.total_vertices();
            for (index = 0; index < num_ver; index++)
            {
                double x, y;
                if (Path.is_vertex(vertices.vertex(index, out x, out y)))
                {
                    trans.transform(ref x, ref y);
                    vertices.modify_vertex(index, x, y);
                }
            }
        }

        public void invert_polygon(int start)
        {
            // Skip all non-vertices at the beginning
            while (start < vertices.total_vertices() &&
                  !Path.is_vertex(vertices.command(start))) ++start;

            // Skip all insignificant move_to
            while (start + 1 < vertices.total_vertices() &&
                  Path.is_move_to(vertices.command(start)) &&
                  Path.is_move_to(vertices.command(start + 1))) ++start;

            // Find the last vertex
            int end = start + 1;
            while (end < vertices.total_vertices() &&
                  !Path.is_next_poly(vertices.command(end))) ++end;

            invert_polygon(start, end);
        }

        private Path.FlagsAndCommand perceive_polygon_orientation(int start, int end)
        {
            // Calculate signed area (double area to be exact)
            //---------------------
            int np = end - start;
            double area = 0.0;
            int i;
            for (i = 0; i < np; i++)
            {
                double x1, y1, x2, y2;
                vertices.vertex(start + i, out x1, out y1);
                vertices.vertex(start + (i + 1) % np, out x2, out y2);
                area += x1 * y2 - y1 * x2;
            }
            return (area < 0.0) ? Path.FlagsAndCommand.FlagCW : Path.FlagsAndCommand.FlagCCW;
        }

        private void invert_polygon(int start, int end)
        {
            int i;
            Path.FlagsAndCommand tmp_PathAndFlags = vertices.command(start);

            --end; // Make "end" inclusive

            // Shift all commands to one position
            for (i = start; i < end; i++)
            {
                vertices.modify_command(i, vertices.command(i + 1));
            }

            // Assign starting command to the ending command
            vertices.modify_command(end, tmp_PathAndFlags);

            // Reverse the polygon
            while (end > start)
            {
                vertices.swap_vertices(start++, end--);
            }
        }
    };
}