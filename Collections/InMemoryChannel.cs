namespace Ecng.Collections
{
	using System;

	public class InMemoryChannel<TItem> : BaseInMemoryChannel<TItem>
	{
		public InMemoryChannel(IBlockingQueue<TItem> queue, string name, Action<Exception> errorHandler)
			: base(queue, name, errorHandler)
		{
		}

		public event Action<TItem> NewOut;

		protected override void OnNewOut(TItem item)
		{
			NewOut?.Invoke(item);
		}
	}
}