using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Ecng.Xaml.Charting.Common.Messaging
{
    public class RenderSurfaceResizedMessage : LoggedMessageBase
    {
        public RenderSurfaceResizedMessage(object sender, Size viewportSize) : base(sender)
        {
            ViewportSize = viewportSize;
        }

        public Size ViewportSize { get; private set; }

    }

    public class CoordinateSystemMessage : LoggedMessageBase
    {
        public CoordinateSystemMessage(object sender, CoordinateSystem coordinateSystem)
            : base(sender)
        {
            CoordinateSystem = coordinateSystem;
        }

        public CoordinateSystem CoordinateSystem { get; private set; }
    }

    public enum CoordinateSystem
    {
        Cartesian,
        Polar,
    }
}
