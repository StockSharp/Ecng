namespace Ecng.ComponentModel
{
	using System;
	using System.Reflection;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Reflection;

	public static class LazyHelper
	{
		private static readonly SynchronizedDictionary<object, Delegate> _factories = new();
		private static readonly SynchronizedDictionary<object, object> _states = new();

		private class Holder<T>
		{
			private static readonly FieldInfo _valueFactory;
			private static readonly FieldInfo _boxed;
			private static readonly FieldInfo _state;
			private static readonly FieldInfo _factory;

			static Holder()
			{
				static FieldInfo GetField(string name)
					=> typeof(Lazy<T>).GetField(name, ReflectionHelper.AllInstanceMembers);

				if (OperatingSystemEx.IsFramework)
				{
					_valueFactory = GetField("m_valueFactory");
					_boxed = GetField("m_boxed");
				}
				else
				{
					_state = GetField("_state");
					_factory = GetField("_factory");
				}
			}

			public static Lazy<T> Track(Lazy<T> lazy)
			{
				if (lazy is null)
					throw new ArgumentNullException(nameof(lazy));

				if (lazy.IsValueCreated)
					throw new ArgumentException(nameof(lazy));

				if (_valueFactory is not null)
					_factories.Add(lazy, (Delegate)_valueFactory.GetValue(lazy));
				else
				{
					_states.Add(lazy, _state.GetValue(lazy));
					_factories.Add(lazy, (Delegate)_factory.GetValue(lazy));
				}

				return lazy;
			}

			public static void Reset(Lazy<T> lazy)
			{
				if (lazy is null)
					throw new ArgumentNullException(nameof(lazy));

				var factory = _factories[lazy];

				if (_valueFactory is not null)
				{
					_boxed.SetValue(lazy, null);
					_valueFactory.SetValue(lazy, factory);
				}
				else
				{
					_state.SetValue(lazy, _states[lazy]);
					_factory.SetValue(lazy, factory);
				}
			}
		}

		public static Lazy<T> Track<T>(this Lazy<T> lazy)
			=> Holder<T>.Track(lazy);

		public static void Reset<T>(this Lazy<T> lazy)
			=> Holder<T>.Reset(lazy);
	}
}