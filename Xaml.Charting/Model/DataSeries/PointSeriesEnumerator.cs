// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// PointSeriesEnumerator.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
// For full terms and conditions of the license, see http://www.ultrachart.com/ultrachart-eula/
// 
// This source code is protected by international copyright law. Unauthorized
// reproduction, reverse-engineering, or distribution of all or any portion of
// this source code is strictly prohibited.
// 
// This source code contains confidential and proprietary trade secrets of
// ulc software Services Ltd., and should at no time be copied, transferred, sold,
// distributed or made available without express written permission.
// *************************************************************************************
using System.Collections;
using System.Collections.Generic;

namespace Ecng.Xaml.Charting.Model.DataSeries
{
    /// <summary>
    /// A custom <see cref="IEnumerator{IPoint}"/> implementation to provide enumeration for <see cref="IPointSeries"/> input
    /// </summary>
    public class PointSeriesEnumerator : IEnumerator<IPoint>, IEnumerator<Point2D>
    {
        private readonly IPointSeries _pointSeries;

        private readonly int _count;
        private int _index;

        private bool _hasCurrent;
        private IPoint _current;
        private Point2D _current2d;

        /// <devdoc>
        /// Added so that EnumerableExtensions.SplitByColor doesn't need to check for Current == null, which can cause an unnecessary boxing of Current.
        /// </devdoc>
        public bool IsReset
        {
            get 
            { 
                return this._index < 0; 
            }
        }

        /// <summary>
        /// Gets the <see cref="IPoint"/> in the collection at the current position of the enumerator.
        /// </summary>
        /// <returns>The element in the collection at the current position of the enumerator.</returns>
        public IPoint Current
        {
            get
            {
                //this.CheckIndex();  // An enumerator should throw when current is invalid.
                if (!_hasCurrent)
                {
                    _current = _current2d;
                }

                return _current;
            }
        }

        Point2D IEnumerator<Point2D>.Current
        {
            get
            {
                //this.CheckIndex();  // An enumerator should throw when current is invalid.
                return _current2d;
            }
        }

        public Point2D CurrentPoint2D
        {
            get 
            {
                //this.CheckIndex();  // An enumerator should throw when current is invalid.
                return this._current2d; 
            }
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <returns>The element in the collection at the current position of the enumerator.</returns>
        object IEnumerator.Current
        {
            get 
            {
                //this.CheckIndex();  // An enumerator should throw when current is invalid.
                return Current; 
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PointSeriesEnumerator" /> class.
        /// </summary>
        /// <param name="pointSeries">The point series.</param>
        public PointSeriesEnumerator(IPointSeries pointSeries)
        {
            _pointSeries = pointSeries;
            _count = pointSeries.Count;

            Reset();
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
        /// </returns>
        public virtual bool MoveNext()
        {
            if (++_index >= _count)
                return false;

            {
                _current = _pointSeries[_index];
                _current2d = new Point2D(_current.X, _current.Y);
                _hasCurrent = true;
            }

            return true;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        public virtual void Reset()
        {
            _index = -1;

            _current = null;
            _current2d = new Point2D();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public virtual void Dispose()
        {
        }

        private void CheckIndex()
        {
            if (_index < 0)
            {
                throw new System.InvalidOperationException();
            }
        }
    }
}
