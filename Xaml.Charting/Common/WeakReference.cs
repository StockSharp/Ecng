using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ecng.Xaml.Charting.Common
{
    /// <summary>
    /// WeakReference{T} generic implementation. 
    /// </summary>
    /// <remarks>
    ///     Wraps a WeakReference but does not inherit. You cannot inherit a WeakReference for Silverlight/WinRT
    ///     See http://stackoverflow.com/questions/3231945/inherited-weakreference-throwing-reflectiontypeloadexception-in-silverlight
    /// </remarks>
    /// <typeparam name="T">The type of object to wrap</typeparam>
    internal class WeakReference<T> where T : class
    {
        private readonly WeakReference inner;

        internal WeakReference(T target)
            : this(target, false)
        { }

        internal WeakReference(T target, bool trackResurrection)
        {
            if (target == null) throw new ArgumentNullException("target");
            this.inner = new WeakReference(target, trackResurrection);
        }

        internal T Target
        {
            get
            {
                return (T)this.inner.Target;
            }
            set
            {
                this.inner.Target = value;
            }
        }

        internal bool IsAlive
        {
            get
            {
                return this.inner.IsAlive;
            }
        }
    }   
}
