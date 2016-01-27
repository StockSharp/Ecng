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

#if !SILVERLIGHT
			Thread.BeginThreadAffinity();
#endif
			Parent = _current;
			_current = this;

			//_all.Add(this);
		}

		#endregion

		#region Parent

		public Scope<T> Parent { get; }

		#endregion

		#region Current

		[ThreadStatic]
		private static Scope<T> _current;

		public static Scope<T> Current => _current;

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
			if (this != _current)
				throw new InvalidOperationException("Disposed out of order.");

			_current = Parent;
#if !SILVERLIGHT
			Thread.EndThreadAffinity();
#endif

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