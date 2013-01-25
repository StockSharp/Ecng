namespace Ecng.Collections
{
	using System;
	using System.Collections.Generic;

	public class FixedSynchronizedDictionary<TKey, TValue> : SynchronizedDictionary<TKey, TValue>
	{
		public FixedSynchronizedDictionary()
		{
		}

		public FixedSynchronizedDictionary(int capacity)
			: base(capacity)
		{
		}

		private int _befferSize = int.MaxValue;

		public int BufferSize
		{
			get { return _befferSize; }
			set
			{
				if (value <= 1)
					throw new ArgumentOutOfRangeException();

				_befferSize = value;

				this.Shrink<SynchronizedDictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>(BufferSize);
			}
		}

		public override void Add(TKey key, TValue value)
		{
			base.Add(key, value);
			this.Shrink<SynchronizedDictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>(BufferSize);
		}
	}
}
