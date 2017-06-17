namespace Ecng.Collections
{
	using System;

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