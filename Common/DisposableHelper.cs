namespace Ecng.Common;

using System;
using System.Collections.Generic;

/// <summary>
/// Provides helper methods for creating and managing disposable objects.
/// </summary>
public static class DisposableHelper
{
	private sealed class DisposableByAction<T> : Disposable
	{
		private readonly T _unmanagedData;
		private readonly Action<T> _disposeAction;

		/// <summary>
		/// Initializes a new instance of the <see cref="DisposableByAction{T}"/> class with the specified unmanaged data and disposal action.
		/// </summary>
		/// <param name="unmanagedData">The unmanaged data to dispose.</param>
		/// <param name="disposeAction">The action that disposes the unmanaged data.</param>
		/// <exception cref="ArgumentNullException">Thrown when unmanagedData or disposeAction is null.</exception>
		public DisposableByAction(T unmanagedData, Action<T> disposeAction)
		{
			if (unmanagedData.IsNull())
				throw new ArgumentNullException(nameof(unmanagedData));

			_unmanagedData = unmanagedData;
			_disposeAction = disposeAction ?? throw new ArgumentNullException(nameof(disposeAction));
		}

		/// <summary>
		/// Disposes the managed resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			_disposeAction(_unmanagedData);
			base.DisposeManaged();
		}
	}

	/// <summary>
	/// Disposes all IDisposable objects in the enumerable.
	/// </summary>
	/// <param name="disposables">The enumerable of disposable objects.</param>
	/// <exception cref="ArgumentNullException">Thrown when disposables is null.</exception>
	public static void DisposeAll(this IEnumerable<IDisposable> disposables)
	{
		if (disposables is null)
			throw new ArgumentNullException(nameof(disposables));

		foreach (var disp in disposables)
			disp.Dispose();
	}

	/// <summary>
	/// Creates a disposable object that performs the specified disposal action on the provided unmanaged data.
	/// </summary>
	/// <typeparam name="T">The type of the unmanaged data.</typeparam>
	/// <param name="unmanagedData">The unmanaged data to be disposed.</param>
	/// <param name="disposeAction">The action to execute during disposal.</param>
	/// <returns>A disposable object that calls the disposal action.</returns>
	public static Disposable MakeDisposable<T>(this T unmanagedData, Action<T> disposeAction)
	{
		return new DisposableByAction<T>(unmanagedData, disposeAction);
	}
}