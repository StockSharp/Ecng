namespace Ecng.Interop
{
	using System;
	using System.Runtime.InteropServices;

	using Ecng.Collections;

	/// <summary>
	/// Collection of native objects.
	/// </summary>
	/// <typeparam name="TItem"></typeparam>
	/// <typeparam name="THandle"></typeparam>
	public abstract class NativeObjectList<TItem, THandle> : BaseList<TItem>
		where TItem : NativeObject<THandle>
        where THandle : SafeHandle
	{
		private bool _initialized;

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="NativeObjectList{T,H}"/> is initialized.
		/// </summary>
		/// <value><c>true</c> if initialized; otherwise, <c>false</c>.</value>
		protected internal virtual bool Initialized
		{
			get { return _initialized; }
			set
			{
				if (Initialized)
					throw new InvalidOperationException();

				_initialized = value;

				foreach (var item in this)
					OnAdded(item);
			}
		}

		/// <summary>
		/// Creates the handle.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <returns></returns>
		protected abstract THandle CreateHandle(TItem item);

		/// <summary>
		/// Removes the handle.
		/// </summary>
		/// <param name="item">The item.</param>
		protected virtual void RemoveHandle(TItem item)
		{
			item.Dispose();
		}

		/// <summary>
		/// Called when inserting new item.
		/// </summary>
		/// <param name="index">The index.</param>
		/// <param name="item">The item.</param>
		protected override void OnInserted(int index, TItem item)
		{
			OnAdded(item);
			base.OnInserted(index, item);
		}

		/// <summary>
		/// Called when [add].
		/// </summary>
		/// <param name="item">The item.</param>
		protected override void OnAdded(TItem item)
		{
			if (Initialized)
				item.Value = CreateHandle(item);

			base.OnAdded(item);
		}

		/// <summary>
		/// Called when items clearing.
		/// </summary>
		protected override void OnCleared()
		{
			foreach (var item in this)
				OnRemoved(item);

			base.OnCleared();
		}

		/// <summary>
		/// Called when item removing.
		/// </summary>
		/// <param name="item">The item.</param>
		protected override void OnRemoved(TItem item)
		{
			if (Initialized)
			{
				RemoveHandle(item);
				item.Value = null;
			}

			base.OnRemoved(item);
		}
	}
}