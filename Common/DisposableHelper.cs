namespace Ecng.Common
{
	using System;
	using System.Collections.Generic;

	public static class DisposableHelper
	{
		private sealed class DisposableByAction<T> : Disposable
		{
			private readonly T _unmanagedData;
			private readonly Action<T> _disposeAction;

			public DisposableByAction(T unmanagedData, Action<T> disposeAction)
			{
				if (unmanagedData.IsNull())
					throw new ArgumentNullException(nameof(unmanagedData));

				if (disposeAction == null)
					throw new ArgumentNullException(nameof(disposeAction));

				_unmanagedData = unmanagedData;
				_disposeAction = disposeAction;
			}

			protected override void DisposeManaged()
			{
				_disposeAction(_unmanagedData);
				base.DisposeManaged();
			}
		}

		static class GenericHolder<T>
		{
			public static readonly Dictionary<T, DisposableByAction<T>> Registry = new Dictionary<T, DisposableByAction<T>>();
		}

		public static void DisposeAll(this IEnumerable<IDisposable> disposables)
		{
			if (disposables == null)
				throw new ArgumentNullException(nameof(disposables));

			foreach (var disp in disposables)
				disp.Dispose();
		}

		/// <summary>
		/// Create disposable helper.
		/// </summary>
		public static Disposable MakeDisposable<T>(this T unmanagedData, Action<T> disposeAction)
		{
			if (disposeAction == null)
				throw new ArgumentNullException(nameof(disposeAction));

			lock (GenericHolder<T>.Registry)
			{
				var wrapper = new DisposableByAction<T>(unmanagedData, key =>
				{
					GenericHolder<T>.Registry.Remove(key);
					disposeAction(key);
				});
				GenericHolder<T>.Registry.Add(unmanagedData, wrapper);
				return wrapper;
			}
		}

		public static bool? IsDisposed<T>(this T unmanagedData)
		{
			lock (GenericHolder<T>.Registry)
			{
				return !GenericHolder<T>.Registry.TryGetValue(unmanagedData, out var wrapper) || wrapper.IsDisposed;
			}
		}
	}
}