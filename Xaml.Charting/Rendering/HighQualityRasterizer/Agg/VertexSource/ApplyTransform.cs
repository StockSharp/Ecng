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
    //----------------------------------------------------------conv_transform
    internal class VertexSourceApplyTransform : IVertexSource
    {
        private IVertexSource vertexSource;
        private Transform.ITransform transformToApply;

        public VertexSourceApplyTransform(IVertexSource VertexSource, Transform.ITransform newTransformeToApply)
        {
            vertexSource = VertexSource;
            transformToApply = newTransformeToApply;
        }

        public void attach(IVertexSource VertexSource) { vertexSource = VertexSource; }

        public void rewind(int path_id) 
        { 
            vertexSource.rewind(path_id); 
        }

        public Path.FlagsAndCommand vertex(out double x, out double y)
        {
            Path.FlagsAndCommand cmd = vertexSource.vertex(out x, out y);
            if(Path.is_vertex(cmd))
            {
                transformToApply.transform(ref x, ref y);
            }
            return cmd;
        }

        public void SetTransformToApply(Transform.ITransform newTransformeToApply)
        {
            transformToApply = newTransformeToApply;
        }
    };
}