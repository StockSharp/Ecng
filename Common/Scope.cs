namespace Ecng.Common
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Security;
	using System.Threading;

	#endregion

	public sealed class Scope<T> : Disposable
		//where T : class
	{
		#region Scope.ctor()

		public Scope()
			: this(Activator.CreateInstance<T>(), true)
		{
		}

		public Scope(T value)
			: this(value, true)
		{
		}

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

		public Scope<T> Parent { get; }

		#endregion

		#region Current

		// ReSharper disable once InconsistentNaming
		private static readonly AsyncLocal<Scope<T>> _current = new();

		public static Scope<T> Current => _current.Value;

		public static bool IsDefined => Current != null;

		#endregion

		#region All

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

		#endregion

		public bool OwnInstance { get; }
		public T Value { get; }

		#region Disposable Members

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
