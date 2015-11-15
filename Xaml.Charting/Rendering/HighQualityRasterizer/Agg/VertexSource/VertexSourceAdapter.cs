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
namespace MatterHackers.Agg.VertexSource
{
    //------------------------------------------------------------null_markers
    internal struct null_markers : IMarkers
    {
        public void remove_all() { }
        public void add_vertex(double x, double y, Path.FlagsAndCommand unknown) { }
        public void prepare_src() { }

        public void rewind(int unknown) { }
        public Path.FlagsAndCommand vertex(ref double x, ref double y) { return Path.FlagsAndCommand.CommandStop; }
    };

    //------------------------------------------------------conv_adaptor_vcgen
    internal class VertexSourceAdapter
    {
        private enum status
        {
            initial,
            accumulate,
            generate
        };

        public VertexSourceAdapter(IVertexSource source, IGenerator generator)
        {
            markers = new null_markers();
            this.source = source;
            this.generator = generator;
            m_status = status.initial;
        }

        public VertexSourceAdapter(IVertexSource source, IGenerator generator, IMarkers markers)
            : this(source, generator)
        {
            this.markers = markers;
        }
        void Attach(IVertexSource source) { this.source = source; }

        protected IGenerator GetGenerator() { return generator; }

        IMarkers GetMarkers() { return markers; }

        public void rewind(int path_id) 
        { 
            source.rewind(path_id);
            m_status = status.initial;
        }

        public Path.FlagsAndCommand vertex(out double x, out double y)
        {
            x = 0;
            y = 0;
            Path.FlagsAndCommand cmd = Path.FlagsAndCommand.CommandStop;
            bool done = false;
            while(!done)
            {
                switch(m_status)
                {
                    case status.initial:
                    markers.remove_all();
                    m_last_cmd = source.vertex(out m_start_x, out m_start_y);
                    m_status = status.accumulate;
                    goto case status.accumulate;

                case status.accumulate:
                    if (Path.is_stop(m_last_cmd)) return Path.FlagsAndCommand.CommandStop;

                    generator.RemoveAll();
                    generator.AddVertex(m_start_x, m_start_y, Path.FlagsAndCommand.CommandMoveTo);
                    markers.add_vertex(m_start_x, m_start_y, Path.FlagsAndCommand.CommandMoveTo);

                    for(;;)
                    {
                        cmd = source.vertex(out x, out y);
                        //DebugFile.Print("x=" + x.ToString() + " y=" + y.ToString() + "\n");
                        if (Path.is_vertex(cmd))
                        {
                            m_last_cmd = cmd;
                            if(Path.is_move_to(cmd))
                            {
                                m_start_x = x;
                                m_start_y = y;
                                break;
                            }
                            generator.AddVertex(x, y, cmd);
                            markers.add_vertex(x, y, Path.FlagsAndCommand.CommandLineTo);
                        }
                        else
                        {
                            if(Path.is_stop(cmd))
                            {
                                m_last_cmd = Path.FlagsAndCommand.CommandStop;
                                break;
                            }
                            if(Path.is_end_poly(cmd))
                            {
                                generator.AddVertex(x, y, cmd);
                                break;
                            }
                        }
                    }
                    generator.Rewind(0);
                    m_status = status.generate;
                    goto case status.generate;

                case status.generate:
                    cmd = generator.Vertex(ref x, ref y);
                    //DebugFile.Print("x=" + x.ToString() + " y=" + y.ToString() + "\n");
                    if (Path.is_stop(cmd))
                    {
                        m_status = status.accumulate;
                        break;
                    }
                    done = true;
                    break;
                }
            }
            return cmd;
        }

        private IVertexSource  source;
        private IGenerator     generator;
        private IMarkers       markers;
        private status        m_status;
        private Path.FlagsAndCommand m_last_cmd;
        private double        m_start_x;
        private double        m_start_y;
    };
}