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
// Arc vertex generator
//
//----------------------------------------------------------------------------

//#ifndef AGG_ARC_INCLUDED
//#define AGG_ARC_INCLUDED

//#include <math.h>
//#include "agg_basics.h"
using System;
namespace MatterHackers.Agg.VertexSource
{

    //=====================================================================arc
    //
    // See Implementation agg_arc.cpp 
    //
    internal class arc
    {
        double   m_OriginX;
        double   m_OriginY;
        
        double   m_RadiusX;
        double   m_RadiusY;

        double   m_StartAngle;
        double   m_EndAngle;
        double   m_Scale;
        EDirection m_Direction;

        double m_CurrentFlatenAngle;
        double m_FlatenDeltaAngle;

        bool m_IsInitialized;
        Path.FlagsAndCommand m_NextPathCommand;

        public enum EDirection
        {
            ClockWise,
            CounterClockWise,
        }

        public arc() 
        {
            m_Scale = 1.0;
            m_IsInitialized = false;
        }

        public arc(double OriginX,  double OriginY, 
             double RadiusX, double RadiusY,
             double Angle1, double Angle2)
            : this(OriginX, OriginY, RadiusX, RadiusY, Angle1, Angle2, EDirection.CounterClockWise)
        {

        }

        public arc(double OriginX,  double OriginY, 
             double RadiusX, double RadiusY, 
             double Angle1, double Angle2,
             EDirection Direction)
        {
            m_OriginX=OriginX;
            m_OriginY=OriginY;
            m_RadiusX=RadiusX;
            m_RadiusY=RadiusY;
            m_Scale=1.0;
            normalize(Angle1, Angle2, Direction);
        }

        public void init(double OriginX,  double OriginY, 
                  double RadiusX, double RadiusY, 
                  double Angle1, double Angle2)
        {
            init(OriginX, OriginY, RadiusX, RadiusY, Angle1, Angle2, EDirection.CounterClockWise);
        }

        public void init(double OriginX,  double OriginY, 
                   double RadiusX, double RadiusY, 
                   double Angle1, double Angle2, 
                   EDirection Direction)
        {
            m_OriginX   = OriginX;  
            m_OriginY  = OriginY;
            m_RadiusX  = RadiusX; 
            m_RadiusY = RadiusY; 
            normalize(Angle1, Angle2, Direction);
        }

        public void approximation_scale(double s)
        {
            m_Scale = s;
            if(m_IsInitialized)
            {
                normalize(m_StartAngle, m_EndAngle, m_Direction);
            }
        }
        
        public double approximation_scale() { return m_Scale;  }

        public void rewind(int unused)
        {
            m_NextPathCommand = Path.FlagsAndCommand.CommandMoveTo; 
            m_CurrentFlatenAngle = m_StartAngle;
        }

        public Path.FlagsAndCommand vertex(out double x, out double y)
        {
            x = 0;
            y = 0;

            if (Path.is_stop(m_NextPathCommand))
            {
                return Path.FlagsAndCommand.CommandStop;
            }

            if ((m_CurrentFlatenAngle < m_EndAngle - m_FlatenDeltaAngle / 4) != ((int)EDirection.CounterClockWise == 1))
            {
                x = m_OriginX + Math.Cos(m_EndAngle) * m_RadiusX;
                y = m_OriginY + Math.Sin(m_EndAngle) * m_RadiusY;
                m_NextPathCommand = Path.FlagsAndCommand.CommandStop;

                return Path.FlagsAndCommand.CommandLineTo;
            }

            x = m_OriginX + Math.Cos(m_CurrentFlatenAngle) * m_RadiusX;
            y = m_OriginY + Math.Sin(m_CurrentFlatenAngle) * m_RadiusY;

            m_CurrentFlatenAngle += m_FlatenDeltaAngle;

            Path.FlagsAndCommand CurrentPathCommand = m_NextPathCommand;
            m_NextPathCommand = Path.FlagsAndCommand.CommandLineTo;
            return CurrentPathCommand;
        }

        private void normalize(double Angle1, double Angle2, EDirection Direction)
        {
            double ra = (Math.Abs(m_RadiusX) + Math.Abs(m_RadiusY)) / 2;
            m_FlatenDeltaAngle = Math.Acos(ra / (ra + 0.125 / m_Scale)) * 2;
            if (Direction == EDirection.CounterClockWise)
            {
                while (Angle2 < Angle1)
                {
                    Angle2 += Math.PI * 2.0;
                }
            }
            else
            {
                while (Angle1 < Angle2)
                {
                    Angle1 += Math.PI * 2.0;
                }
                m_FlatenDeltaAngle = -m_FlatenDeltaAngle;
            }
            m_Direction   = Direction;
            m_StartAngle = Angle1;
            m_EndAngle   = Angle2;
            m_IsInitialized = true;
        }
    };
}
