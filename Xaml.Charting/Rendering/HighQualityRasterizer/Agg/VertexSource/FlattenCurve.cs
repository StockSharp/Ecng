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
//
// classes conv_curve
//
//----------------------------------------------------------------------------
namespace MatterHackers.Agg.VertexSource
{
    //---------------------------------------------------------------conv_curve
    // Curve converter class. Any path storage can have Bezier curves defined 
    // by their control points. There're two types of curves supported: curve3 
    // and curve4. Curve3 is a conic Bezier curve with 2 endpoints and 1 control
    // point. Curve4 has 2 control points (4 points in total) and can be used
    // to interpolate more complicated curves. Curve4, unlike curve3 can be used 
    // to approximate arcs, both circular and elliptical. Curves are approximated 
    // with straight lines and one of the approaches is just to store the whole 
    // sequence of vertices that approximate our curve. It takes additional 
    // memory, and at the same time the consecutive vertices can be calculated 
    // on demand. 
    //
    // Initially, path storages are not suppose to keep all the vertices of the
    // curves (although, nothing prevents us from doing so). Instead, path_storage
    // keeps only vertices, needed to calculate a curve on demand. Those vertices
    // are marked with special commands. So, if the path_storage contains curves 
    // (which are not real curves yet), and we render this storage directly, 
    // all we will see is only 2 or 3 straight line segments (for curve3 and 
    // curve4 respectively). If we need to see real curves drawn we need to 
    // include this class into the conversion pipeline. 
    //
    // Class conv_curve recognizes commands path_cmd_curve3 and path_cmd_curve4 
    // and converts these vertices into a move_to/line_to sequence. 
    //-----------------------------------------------------------------------
    internal class FlattenCurves : IVertexSource
    {
        IVertexSource vertexSource;
        double        lastX;
        double        lastY;
        Curve3 m_curve3;
        Curve4 m_curve4;

        public FlattenCurves(IVertexSource source)
        {
            m_curve3 = new Curve3();
            m_curve4 = new Curve4();
            vertexSource=(source);
            lastX=(0.0);
            lastY=(0.0);
        }

        public double ApproximationScale
        {
            get
            {
                return m_curve4.approximation_scale();
            }

            set
            {
                m_curve3.approximation_scale(value);
                m_curve4.approximation_scale(value);
            }
        }

        public void SetVertexSource(IVertexSource source) { vertexSource = source; }

        public Curves.CurveApproximationMethod ApproximationMethod
        {
            set
            {
                m_curve3.approximation_method(value);
                m_curve4.approximation_method(value);
            }

            get
            {
                return m_curve4.approximation_method();
            }
        }

        public double AngleTolerance 
        {
            set
            {
                m_curve3.angle_tolerance(value);
                m_curve4.angle_tolerance(value);
            }

            get
            {
                return m_curve4.angle_tolerance();
            }
        }

        public double CuspLimit
        {
            set
            {
                m_curve3.cusp_limit(value);
                m_curve4.cusp_limit(value);
            }

            get
            {
                return m_curve4.cusp_limit();
            }
        }

        public void rewind(int path_id)
        {
            vertexSource.rewind(path_id);
            lastX = 0.0;
            lastY = 0.0;
            m_curve3.reset();
            m_curve4.reset();
        }

        public Path.FlagsAndCommand vertex(out double x, out double y)
        {
            if(!Path.is_stop(m_curve3.vertex(out x, out y)))
            {
                lastX = x;
                lastY = y;
                return Path.FlagsAndCommand.CommandLineTo;
            }

            if(!Path.is_stop(m_curve4.vertex(out x, out y)))
            {
                lastX = x;
                lastY = y;
                return Path.FlagsAndCommand.CommandLineTo;
            }

            double ct2_x;
            double ct2_y;
            double end_x;
            double end_y;

            Path.FlagsAndCommand cmd = vertexSource.vertex(out x, out y);
            switch(cmd)
            {
                case Path.FlagsAndCommand.CommandCurve3:
                vertexSource.vertex(out end_x, out end_y);

                m_curve3.init(lastX, lastY, x, y, end_x, end_y);

                m_curve3.vertex(out x, out y);    // First call returns path_cmd_move_to
                m_curve3.vertex(out x, out y);    // This is the first vertex of the curve
                cmd = Path.FlagsAndCommand.CommandLineTo;
                break;

            case Path.FlagsAndCommand.CommandCurve4:
                vertexSource.vertex(out ct2_x, out ct2_y);
                vertexSource.vertex(out end_x, out end_y);

                m_curve4.init(lastX, lastY, x, y, ct2_x, ct2_y, end_x, end_y);

                m_curve4.vertex(out x, out y);    // First call returns path_cmd_move_to
                m_curve4.vertex(out x, out y);    // This is the first vertex of the curve
                cmd = Path.FlagsAndCommand.CommandLineTo;
                break;
            }
            lastX = x;
            lastY = y;
            return cmd;
        }
    };
}