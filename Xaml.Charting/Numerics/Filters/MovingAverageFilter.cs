namespace Ecng.Xaml.Charting.Numerics.Filters
{
    public class MovingAverageFilter : IFilter
    {
        private readonly int _length;
        private int _circIndex;
        private bool _filled;
        private double _current = double.NaN;
        private readonly double _oneOverLength;
        private readonly double[] _circularBuffer;

        public MovingAverageFilter(int length)
        {
            _length = length;
            _oneOverLength = 1.0 / length;
            _circularBuffer = new double[length];
        }

        public IFilter UpdateValue(double value)
        {
            _circularBuffer[_circIndex] = value;

            // If not yet filled, just return. Current value should be double.NaN
            if (!_filled)
            {
                _current = double.NaN;
                return this;
            }

            // Compute the average
            double average = 0.0;
            for (int i = 0; i < _circularBuffer.Length; i++)
            {
                average += _circularBuffer[i];
            }

            _current = average * _oneOverLength;

            return this;
        }

        public IFilter PushValue(double value)
        {
            _circularBuffer[_circIndex++] = value;

            // Apply the circular buffer
            if (_circIndex == _length)
            {
                _circIndex = 0;

                // Set a flag to indicate this is the first time the buffer has been filled
                if (!_filled) { _filled = true; }
            }

            // If not yet filled, just return. Current value should be double.NaN
            if (!_filled)
            {
                _current = double.NaN;
                return this;
            }

            // Compute the average
            double average = 0.0;
            for (int i = 0; i < _circularBuffer.Length; i++)
            {
                average += _circularBuffer[i];
            }

            _current = average * _oneOverLength;

            return this;
        }

        public int Length { get { return _length; } }
        public double Current { get { return _current; } }
    }
}