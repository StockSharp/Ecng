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
// class ellipse
//
//----------------------------------------------------------------------------
using System;
using path_flags_e = MatterHackers.Agg.Path.FlagsAndCommand;

using MatterHackers.VectorMath;

namespace MatterHackers.Agg.VertexSource
{
    //----------------------------------------------------------------ellipse
    internal class Ellipse : IVertexSource
    {
        public double originX;
        public double originY;
        public double radiusX;
        public double radiusY;
        private double m_scale;
        private int numSteps;
        private int m_step;
        private bool m_cw;

        public Ellipse()
        {
            originX = 0.0;
            originY = 0.0;
            radiusX = 1.0;
            radiusY = 1.0;
            m_scale = 1.0;
            numSteps = 4;
            m_step = 0;
            m_cw = false;
        }

        public Ellipse(Vector2 origin, double Radius)
            : this(origin.x, origin.y, Radius, Radius, 0, false)
        {
        }

        public Ellipse(Vector2 origin, double RadiusX, double RadiusY, int num_steps = 0, bool cw = false)
            : this(origin.x, origin.y, RadiusX, RadiusY, num_steps, cw)
        {
        }
        
        public Ellipse(double OriginX, double OriginY, double RadiusX, double RadiusY, int num_steps = 0, bool cw = false)
        {
            this.originX = OriginX;
            this.originY = OriginY;
            this.radiusX = RadiusX;
            this.radiusY = RadiusY;
            m_scale = 1;
            numSteps = num_steps;
            m_step = 0;
            m_cw = cw;
            if (numSteps == 0)
            {
                calc_num_steps();
            }
        }

        public void init(double OriginX, double OriginY, double RadiusX, double RadiusY)
        {
            init(OriginX, OriginY, RadiusX, RadiusY, 0, false);
        }

        public void init(double OriginX, double OriginY, double RadiusX, double RadiusY, int num_steps)
        {
            init(OriginX, OriginY, RadiusX, RadiusY, num_steps, false);
        }

        public void init(double OriginX, double OriginY, double RadiusX, double RadiusY,
                  int num_steps, bool cw)
        {
            originX = OriginX;
            originY = OriginY;
            radiusX = RadiusX;
            radiusY = RadiusY;
            numSteps = num_steps;
            m_step = 0;
            m_cw = cw;
            if (numSteps == 0)
            {
                calc_num_steps();
            }
        }

        public void approximation_scale(double scale)
        {   
            m_scale = scale;
            calc_num_steps();
        }

        public void rewind(int path_id)
        {
            m_step = 0;
        }

        public Path.FlagsAndCommand vertex(out double x, out double y)
        {
            x = 0;
            y = 0;
            if (m_step == numSteps) 
            {
                ++m_step;
                return path_flags_e.CommandEndPoly | path_flags_e.FlagClose | path_flags_e.FlagCCW;
            }
            if (m_step > numSteps) return path_flags_e.CommandStop;
            double angle = (double)(m_step) / (double)(numSteps) * 2.0 * Math.PI;
            if(m_cw) angle = 2.0 * Math.PI - angle;
            x = originX + Math.Cos(angle) * radiusX;
            y = originY + Math.Sin(angle) * radiusY;
            m_step++;
            return ((m_step == 1) ? path_flags_e.CommandMoveTo : path_flags_e.CommandLineTo);
        }

        private void calc_num_steps()
        {
            double ra = (Math.Abs(radiusX) + Math.Abs(radiusY)) / 2;
            double da = Math.Acos(ra / (ra + 0.125 / m_scale)) * 2;
            numSteps = (int)Math.Round(2 * Math.PI / da);
        }
    };
}