namespace Ecng.Collections
{
	using System;
	using System.Collections;
	using System.Collections.Generic;

	public class CircularBuffer<T> : IEnumerable<T>
	{
		private sealed class CircularBufferEnumerator : IEnumerator<T>
		{
			private readonly IEnumerator<T> _sourceEnumerator;

			public CircularBufferEnumerator(IEnumerator<T> sourceEnumerator)
			{
				if (sourceEnumerator == null)
					throw new ArgumentNullException(nameof(sourceEnumerator));

				_sourceEnumerator = sourceEnumerator;
			}

			public void Dispose()
			{
				_sourceEnumerator.Dispose();
			}

			public bool MoveNext()
			{
				var retVal = _sourceEnumerator.MoveNext();

				if (!retVal)
				{
					Reset();
					retVal = _sourceEnumerator.MoveNext();
				}

				return retVal;
			}

			public void Reset()
			{
				_sourceEnumerator.Reset();
			}

			public T Current => _sourceEnumerator.Current;

			object IEnumerator.Current => Current;
		}

		private readonly IEnumerable<T> _source;

		public CircularBuffer(IEnumerable<T> source)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			_source = source;
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return new CircularBufferEnumerator(_source.GetEnumerator());
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<T>)this).GetEnumerator();
		}
	}
}