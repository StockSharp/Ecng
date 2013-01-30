namespace Ecng.Collections
{
	using System.Collections.Generic;

	using Ecng.Common;

	public interface ISynchronizedCollection
	{
		SyncObject SyncRoot { get; }
	}

	public interface ISynchronizedCollection<T> : ISynchronizedCollection, ICollection<T>
	{
	}
}