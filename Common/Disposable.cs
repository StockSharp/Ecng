namespace Ecng.Common
{
	#region Using Directives

	using System;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.Xml.Serialization;

	#endregion

	[Serializable]
	public abstract class Disposable : IDisposable
	{
		private readonly object _lock = new object();

		#region IsDisposed

		/// <summary>
		/// Gets a value indicating whether this instance is disposed.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is disposed; otherwise, <c>false</c>.
		/// </value>
		[XmlIgnore]
		[Browsable(false)]
		public bool IsDisposed { get; private set; }

        #endregion

		#region IDisposable Members

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public virtual void Dispose()
		{
			lock (_lock)
			{
				if (!IsDisposed)
				{
					DisposeManaged();
					DisposeNative();
					IsDisposed = true;
					GC.SuppressFinalize(this);
				}
			}
		}

		#endregion

		/// <summary>
		/// Disposes the managed resources.
		/// </summary>
        protected virtual void DisposeManaged()
        {
        }

		/// <summary>
		/// Disposes the native resources.
		/// </summary>
        protected virtual void DisposeNative()
        {
        }

		/// <summary>
		/// Throws if object is already disposed.
		/// </summary>
		protected void ThrowIfDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(GetType().Name);
		}

		#region Finalize

		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// <see cref="Disposable"/> is reclaimed by garbage collection.
		/// </summary>
		~Disposable()
		{
			// http://stackoverflow.com/a/9903121
			try
			{
				DisposeNative();
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex);
			}
		}

		#endregion
	}
}