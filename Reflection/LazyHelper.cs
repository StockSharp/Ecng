﻿namespace Ecng.Reflection;

using System;
using System.Reflection;

using Ecng.Common;
using Ecng.Collections;

/// <summary>
/// Lazy helper.
/// </summary>
public static class LazyHelper
{
	private static readonly SynchronizedDictionary<object, Delegate> _factories = [];
	private static readonly SynchronizedDictionary<object, object> _states = [];

	private class Holder<T>
	{
		private static readonly FieldInfo _valueFactory;
		private static readonly FieldInfo _boxed;
		private static readonly FieldInfo _state;
		private static readonly FieldInfo _factory;
		private static readonly FieldInfo _value;

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
				_value = GetField("_value");
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

		public static void Untrack(Lazy<T> lazy)
		{
			if (lazy is null)
				throw new ArgumentNullException(nameof(lazy));

			_factories.Remove(lazy);
			_states.Remove(lazy);
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

		public static void SetValue(Lazy<T> lazy, T value)
		{
			if (lazy is null)
				throw new ArgumentNullException(nameof(lazy));

			if (_value is null)
				throw new PlatformNotSupportedException();

			_state.SetValue(lazy, null);
			_value.SetValue(lazy, value);
		}
	}

	/// <summary>
	/// Tracks the lazy.
	/// </summary>
	/// <typeparam name="T">The type of the lazy.</typeparam>
	/// <param name="lazy">The lazy.</param>
	/// <returns>The lazy.</returns>
	public static Lazy<T> Track<T>(this Lazy<T> lazy)
		=> Holder<T>.Track(lazy);

	/// <summary>
	/// Resets the lazy.
	/// </summary>
	/// <typeparam name="T">The type of the lazy.</typeparam>
	/// <param name="lazy">The lazy.</param>
	public static void Reset<T>(this Lazy<T> lazy)
		=> Holder<T>.Reset(lazy);

	/// <summary>
	/// Sets the value.
	/// </summary>
	/// <typeparam name="T">The type of the lazy.</typeparam>
	/// <param name="lazy">The lazy.</param>
	/// <param name="value">Value.</param>
	public static void SetValue<T>(this Lazy<T> lazy, T value)
		=> Holder<T>.SetValue(lazy, value);

	/// <summary>
	/// Stops tracking the lazy.
	/// </summary>
	/// <typeparam name="T">The type of the lazy.</typeparam>
	/// <param name="lazy">The lazy.</param>
	public static void Untrack<T>(this Lazy<T> lazy)
		=> Holder<T>.Untrack(lazy);
}