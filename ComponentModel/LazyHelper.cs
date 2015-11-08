namespace Ecng.ComponentModel
{
	using System;

	using Ecng.Collections;
	using Ecng.Reflection;

	public static class LazyHelper
	{
		private static readonly SynchronizedDictionary<object, Delegate> _delegates = new SynchronizedDictionary<object, Delegate>(); 

		public static Lazy<T> Track<T>(this Lazy<T> lazy)
		{
			if (lazy == null)
				throw new ArgumentNullException(nameof(lazy));

			if (lazy.IsValueCreated)
				throw new ArgumentException("lazy");

			_delegates.Add(lazy, lazy.GetValue<Lazy<T>, VoidType, Func<T>>("m_valueFactory", null));
			return lazy;
		}

		public static void Reset<T>(this Lazy<T> lazy)
		{
			if (lazy == null)
				throw new ArgumentNullException(nameof(lazy));

			var initFunc = _delegates[lazy];
			lazy.SetValue("m_boxed", (object)null);
			lazy.SetValue("m_valueFactory", initFunc);
		}
	}
}