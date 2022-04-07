namespace Ecng.Serialization
{
	using System;

	using Nito.AsyncEx;

	public class StorageDelayAction : DelayAction
	{
		private readonly IStorage _storage;

		public StorageDelayAction(IStorage storage, Action<Exception> errorHandler)
			: base(errorHandler)
		{
			_storage = storage ?? throw new ArgumentNullException(nameof(storage));
		}

		protected override IBatchContext BeginBatch(IGroup group)
			=> AsyncContext.Run(() => _storage.BeginBatch(default).AsTask());
	}
}