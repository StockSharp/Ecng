namespace Ecng.Collections
{
	using System;
	using System.Collections.Generic;

	[Obsolete("Due to poor performance, this collection will no longer be supported. The applied code should be corrected to avoid unnecessary collection shrinking (for example, use Queue or CircularBuffer).")]
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
			get => _befferSize;
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
