namespace Ecng.Common
{
	using System;
	using System.Threading;

	public class SmartPointer<T> : ISmartPointer
	{
		private readonly T _value;
		private readonly Action<T> _release;

		public SmartPointer(T value, Action<T> release)
		{
			if (value.IsNull())
				throw new ArgumentNullException("value");

			if (release == null)
				throw new ArgumentNullException("release");

			_value = value;
			_release = release;
		}

		private int _counter;

		void ISmartPointer.IncRef()
		{
			Interlocked.Increment(ref _counter);
		}

		void ISmartPointer.DecRef()
		{
			if (Interlocked.Decrement(ref _counter) == 0)
				_release(_value);
		}
	}
}