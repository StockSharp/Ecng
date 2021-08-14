namespace Ecng.Serialization
{
	using System;

	using Ecng.Common;

	public sealed class BatchContext : Disposable, IBatchContext
	{
		private readonly IStorage _storage;

		public BatchContext(IStorage storage)
		{
			_storage = storage ?? throw new ArgumentNullException(nameof(storage));
		}

		public void Commit()
		{
			_storage.CommitBatch();
			Dispose();
		}

		protected override void DisposeManaged()
		{
			_storage.EndBatch();
			base.DisposeManaged();
		}
	}
}