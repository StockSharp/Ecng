namespace Ecng.Collections
{
	using System;

	[Obsolete("Due to poor performance, this collection will no longer be supported. The applied code should be corrected to avoid unnecessary collection shrinking (for example, use Queue or CircularBuffer).")]
	public class FixedSynchronizedList<T> : SynchronizedList<T>
	{
		public FixedSynchronizedList()
		{
		}

		public FixedSynchronizedList(int capacity)
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

				this.Shrink<SynchronizedList<T>, T>(BufferSize);
			}
		}

		//public override void AddRange(IEnumerable<T> items)
		//{
		//    base.AddRange(items);
		//    this.Shrink<SynchronizedList<T>, T>(BufferSize);
		//}

		public override void Add(T item)
		{
			base.Add(item);
			this.Shrink<SynchronizedList<T>, T>(BufferSize);
		}
	}
}