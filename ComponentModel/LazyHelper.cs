namespace Ecng.ComponentModel
{
	using System;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Reflection;

	public static class LazyHelper
	{
		private static readonly SynchronizedDictionary<object, Delegate> _factories = new(); 
		private static readonly SynchronizedDictionary<object, object> _states = new(); 

		public static Lazy<T> Track<T>(this Lazy<T> lazy)
		{
			if (lazy is null)
				throw new ArgumentNullException(nameof(lazy));

			if (lazy.IsValueCreated)
				throw new ArgumentException(nameof(lazy));

			if (OperatingSystemEx.IsFramework)
				_factories.Add(lazy, lazy.GetValue<Lazy<T>, VoidType, Func<T>>("m_valueFactory", null));
			else
			{
				_states.Add(lazy, lazy.GetValue<Lazy<T>, VoidType, object>("_state", null));
				_factories.Add(lazy, lazy.GetValue<Lazy<T>, VoidType, Func<T>>("_factory", null));
			}

			return lazy;
		}

		public static void Reset<T>(this Lazy<T> lazy)
		{
			if (lazy is null)
				throw new ArgumentNullException(nameof(lazy));

			var factory = _factories[lazy];

			if (OperatingSystemEx.IsFramework)
			{
				lazy.SetValue("m_boxed", (object)null);
				lazy.SetValue("m_valueFactory", factory);
			}
			else
			{
				lazy.SetValue("_state", _states[lazy]);
				lazy.SetValue("_factory", factory);
			}
		}
	}
}