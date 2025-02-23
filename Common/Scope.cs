namespace Ecng.Common
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Security;
	using System.Threading;

	#endregion

	/// <summary>
	/// Provides a scope for managing a resource with automatic disposal.
	/// </summary>
	/// <typeparam name="T">The type of the resource to be managed.</typeparam>
	public sealed class Scope<T> : Disposable
		//where T : class
	{
		#region Scope.ctor()

		/// <summary>
		/// Initializes a new instance of the <see cref="Scope{T}"/> class with a new instance of T.
		/// </summary>
		public Scope()
			: this(Activator.CreateInstance<T>(), true)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Scope{T}"/> class with the specified value.
		/// </summary>
		/// <param name="value">The instance of T to be managed.</param>
		public Scope(T value)
			: this(value, true)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Scope{T}"/> class with the specified value and ownership.
		/// </summary>
		/// <param name="value">The instance of T to be managed.</param>
		/// <param name="ownInstance">Indicates whether the scope owns the instance and is responsible for disposing it.</param>
		public Scope(T value, bool ownInstance)
		{
			if (value.IsNull())
				throw new ArgumentNullException(nameof(value));

			Value = value;
			OwnInstance = ownInstance;

			Parent = _current.Value;
			_current.Value = this;

			//_all.Add(this);
		}

		#endregion

		#region Parent

		/// <summary>
		/// Gets the parent scope of the current scope.
		/// </summary>
		public Scope<T> Parent { get; }

		#region Current

		// ReSharper disable once InconsistentNaming
		private static readonly AsyncLocal<Scope<T>> _current = new();

		/// <summary>
		/// Gets the current scope.
		/// </summary>
		public static Scope<T> Current => _current.Value;

		/// <summary>
		/// Gets a value indicating whether a current scope is defined.
		/// </summary>
		public static bool IsDefined => Current != null;

		#endregion

		/// <summary>
		/// Gets all scopes in the current hierarchy as a collection.
		/// </summary>
		public static ICollection<Scope<T>> All
		{
			get
			{
				var all = new List<Scope<T>>();

				var current = Current;
				while (current != null)
				{
					all.Add(current);
					current = current.Parent;
				}

				all.Reverse();

				return all;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the scope owns the managed instance.
		/// </summary>
		public bool OwnInstance { get; }

		/// <summary>
		/// Gets the value contained in the scope.
		/// </summary>
		public T Value { get; }

		#endregion

		#region Disposable Members

		/// <summary>
		/// Disposes the managed resources used by the <see cref="Scope{T}"/> class.
		/// </summary>
		/// <remarks>
		/// Throws an <see cref="InvalidOperationException"/> if disposed out of order.
		/// </remarks>
		[SecuritySafeCritical]
		protected override void DisposeManaged()
		{
			if (this != _current.Value)
				throw new InvalidOperationException("Disposed out of order.");

			_current.Value = Parent;

			if (OwnInstance)
			{
				Value.DoDispose();
			}

			//_all.Remove(this);

			base.DisposeManaged();
		}

		#endregion
	}
}
