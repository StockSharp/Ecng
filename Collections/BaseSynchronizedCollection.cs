namespace Ecng.Collections
{
	using System;

	[Serializable]
	public abstract class BaseSynchronizedCollection<T> : ISynchronizedCollection<T>
	{
		private readonly object _syncRoot = new object();

		public object SyncRoot
		{
			get { return _syncRoot; }
		}
	}
}