using System;
using System.Collections.Generic;

namespace Ecng.Xaml.Charting.Model.DataSeries {
    class DataSeriesAppendBuffer<TPoint> {
        readonly Action<IList<TPoint>> _flushAction;
        List<TPoint> _points = new List<TPoint>();

        public object SyncRoot {get;} = new object();

        public DataSeriesAppendBuffer(Action<IList<TPoint>> flushAction) {
            _flushAction = flushAction;
        }

        public void Clear() {
            lock(SyncRoot)
                _points.Clear();
        }

        public void Flush() {
            if(_points.Count == 0)
                return;

            List<TPoint> points;

            lock(SyncRoot) {
                points = _points;
                _points = new List<TPoint>();
            }

            _flushAction(points);
        }

        public void Append(TPoint newPoint) {
            lock(SyncRoot)
                _points.Add(newPoint);
        }
    }
}
