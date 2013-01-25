namespace Ecng.Collections
{
	using System.Collections.Generic;

	public interface ISynchronizedCollection
	{
		object SyncRoot { get; }
	}

	public interface ISynchronizedCollection<T> : ISynchronizedCollection, ICollection<T>
	{
	}
}