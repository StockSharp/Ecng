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

				_unmanagedData = unmanagedData;
				_disposeAction = disposeAction ?? throw new ArgumentNullException(nameof(disposeAction));
			}

			protected override void DisposeManaged()
			{
				_disposeAction(_unmanagedData);
				base.DisposeManaged();
			}
		}

		public static void DisposeAll(this IEnumerable<IDisposable> disposables)
		{
			if (disposables is null)
				throw new ArgumentNullException(nameof(disposables));

			foreach (var disp in disposables)
				disp.Dispose();
		}

		/// <summary>
		/// Create disposable helper.
		/// </summary>
		public static Disposable MakeDisposable<T>(this T unmanagedData, Action<T> disposeAction)
		{
			return new DisposableByAction<T>(unmanagedData, disposeAction);
		}
	}
}