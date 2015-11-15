using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Ecng.Xaml.Charting.Common
{
    /// <summary>
    /// Wrap a Disposable type to ensure it gets disposed by the Finalizer, if not explictly disposed
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class SmartDisposable<T> : IDisposable
        where T : IDisposable
    {
        private readonly T _inner;

        public SmartDisposable(T inner)
        {
            _inner = inner;
        }

        ~SmartDisposable()
        {
            Debug.WriteLine("~SmartDisposable: {0}", typeof(T));
            Dispose(false);
        }

        internal T Inner { get { return _inner; } }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_inner != null)
                _inner.Dispose();
        }

        public static explicit operator T(SmartDisposable<T> b)  // explicit SmartDisposable<T> to T conversion operator
        {
            return b._inner;
        }
    }
}
