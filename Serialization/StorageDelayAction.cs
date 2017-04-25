namespace Ecng.Serialization
{
	using System;

	public class StorageDelayAction : DelayAction
	{
		private readonly IStorage _storage;

		public StorageDelayAction(IStorage storage, Action<Exception> errorHandler)
			: base(errorHandler)
		{
			if (storage == null)
				throw new ArgumentNullException(nameof(storage));

			_storage = storage;
		}

		protected override IBatchContext BeginBatch(Group group)
		{
			return _storage.BeginBatch();
		}
	}
}