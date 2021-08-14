namespace Ecng.Interop
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Runtime.InteropServices;

	using Ecng.Common;

	#endregion

	/// <summary>
	/// Provide base and extended functionality for all native objects.
	/// </summary>
	/// <typeparam name="T">SafeHandle derived type.</typeparam>
	public abstract class NativeObject<T> : Wrapper<T>
		where T : SafeHandle
	{
		#region Private Fields

		private readonly List<Action> _actions = new List<Action>();

		#endregion

		#region Value

		/// <summary>
		/// Gets or sets the value.
		/// </summary>
		/// <value>The value.</value>
		public override T Value
		{
			set
			{
				if (value != base.Value)
				{
					base.Value = value;
					value.RegisterObject(this);
					InitState();
				}
			}
		}

		#endregion

		/// <summary>
		/// Inits the state. Process all cached actions.
		/// </summary>
		protected internal virtual void InitState()
		{
			lock (_actions)
			{
				foreach (var action in _actions)
					action();

				_actions.Clear();
			}
		}

		/// <summary>
		/// Gets the value.
		/// </summary>
		/// <remarks>If object isn't initialized, return default</remarks>
		/// <param name="create">The create.</param>
		/// <returns></returns>
		protected TValue GetValue<TValue>(Func<TValue> create)
		{
			return Value is null ? default : create();
		}

		/// <summary>
		/// Gets the value.
		/// </summary>
		/// <remarks>If object isn't initialized, return default</remarks>
		/// <param name="action">The action.</param>
		/// <returns></returns>
		protected TValue GetValue<TValue>(Func<TValue, TValue> action)
			where TValue : struct
		{
			return GetValue(action, default);
		}

		/// <summary>
		/// Gets the value.
		/// </summary>
		/// <remarks>If object isn't initialized, return default</remarks>
		/// <param name="action">The action.</param>
		/// <param name="defaultValue">The default value.</param>
		/// <returns></returns>
		protected TValue GetValue<TValue>(Func<TValue, TValue> action, TValue defaultValue)
			where TValue : struct
		{
			return Value != null ? action(new TValue()) : defaultValue;
		}

		/// <summary>
		/// Adds the action. Actions will be processed when object will be initialized.
		/// </summary>
		/// <param name="action">The action.</param>
		protected void AddAction(Action action)
		{
			lock (_actions)
				_actions.Add(action);
		}

		#region Disposable Members

		/// <summary>
		/// Disposes the native values.
		/// </summary>
		protected override void DisposeNative()
		{
			if (Value != null)
				Value.Dispose();
		}

		#endregion
	}
}