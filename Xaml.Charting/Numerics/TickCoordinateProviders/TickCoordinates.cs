namespace Ecng.Xaml.Charting.Numerics.TickCoordinateProviders
{
    /// <summary>
    /// A structure contaning tick coordinates. Used internally when drawing tick marks and grid lines
    /// </summary>
    public struct TickCoordinates
    {
        private readonly double[] _minorTicks;
        private readonly double[] _majorTicks;

        private readonly float[] _minorTickCoordinates;
        private readonly float[] _majorTickCoordinates;

        internal TickCoordinates(double[] minorTicks, double[] majorTicks, float[] minorCoords, float[] majorCoords)
        {
            _minorTicks = minorTicks;
            _majorTicks = majorTicks;

            _minorTickCoordinates = minorCoords;
            _majorTickCoordinates = majorCoords;
        }

        internal bool IsEmpty
        {
            get { return _majorTickCoordinates == null || _minorTickCoordinates == null; }
        }

        internal double[] MinorTicks
        {
            get { return _minorTicks; }
        }

        internal double[] MajorTicks
        {
            get { return _majorTicks; }
        }

        internal float[] MinorTickCoordinates
        {
            get { return _minorTickCoordinates; }
        }

        internal float[] MajorTickCoordinates
        {
            get { return _majorTickCoordinates; }
        }
    }
}
