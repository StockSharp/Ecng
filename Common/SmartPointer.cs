namespace Ecng.Common
{
	using System;
	using System.Threading;

	public class SmartPointer<T> : ISmartPointer
		where T : class
	{
		private readonly Action<T> _release;

		public SmartPointer(IDisposable value)
			: this((T)value, v => ((IDisposable)v).Dispose())
		{
		}

		public SmartPointer(T value, Action<T> release)
		{
			_value = value ?? throw new ArgumentNullException(nameof(value));
			_release = release ?? throw new ArgumentNullException(nameof(release));
		}

		private int _counter;

		public int Counter => _counter;

		private T _value;

		public T Value
		{
			get
			{
				ThrowIfDisposed();

				return _value;
			}
		}

		public void IncRef()
		{
			ThrowIfDisposed();

			Interlocked.Increment(ref _counter);
		}

		public void DecRef()
		{
			ThrowIfDisposed();

			if (Interlocked.Decrement(ref _counter) == 0)
				Dispose();
		}

		public void Dispose()
		{
			if (_value is null)
				return;

			_release(_value);
			_value = null;
			_counter = 0;
		}

		private void ThrowIfDisposed()
		{
			if (_value is null)
				throw new ObjectDisposedException(nameof(_value));
		}
	}
}