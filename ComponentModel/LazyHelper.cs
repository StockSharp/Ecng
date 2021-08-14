namespace Ecng.ComponentModel
{
	using System;

	using Ecng.Collections;
	using Ecng.Reflection;

	public static class LazyHelper
	{
		private static readonly SynchronizedDictionary<object, Delegate> _factories = new SynchronizedDictionary<object, Delegate>(); 
#if NETCOREAPP || NETSTANDARD
		private static readonly SynchronizedDictionary<object, object> _states = new SynchronizedDictionary<object, object>(); 
#endif

		public static Lazy<T> Track<T>(this Lazy<T> lazy)
		{
			if (lazy is null)
				throw new ArgumentNullException(nameof(lazy));

			if (lazy.IsValueCreated)
				throw new ArgumentException(nameof(lazy));

#if !NETCOREAPP && !NETSTANDARD
			_factories.Add(lazy, lazy.GetValue<Lazy<T>, VoidType, Func<T>>("m_valueFactory", null));
#else
			_states.Add(lazy, lazy.GetValue<Lazy<T>, VoidType, object>("_state", null));
			_factories.Add(lazy, lazy.GetValue<Lazy<T>, VoidType, Func<T>>("_factory", null));
#endif
			return lazy;
		}

		public static void Reset<T>(this Lazy<T> lazy)
		{
			if (lazy is null)
				throw new ArgumentNullException(nameof(lazy));

			var factory = _factories[lazy];

#if !NETCOREAPP && !NETSTANDARD
			lazy.SetValue("m_boxed", (object)null);
			lazy.SetValue("m_valueFactory", factory);
#else
			lazy.SetValue("_state", _states[lazy]);
			lazy.SetValue("_factory", factory);
#endif
		}
	}
}