namespace Ecng.Serialization
{
	using System;

	using Ecng.Common;

	public class StorageDelayAction : DelayAction
	{
		private readonly IStorage _storage;

		public StorageDelayAction(IStorage storage, Action<Exception> errorHandler)
			: base(errorHandler)
		{
			_storage = storage ?? throw new ArgumentNullException(nameof(storage));
		}

		protected override IBatchContext BeginBatch(IGroup group)
			=> ThreadingHelper.Run(() => _storage.BeginBatch(default));
	}
}