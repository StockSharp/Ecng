namespace Ecng.Collections
{
	using System.Collections.Generic;

	/// <summary>
	/// Provides an extended set of methods and events for lists of a specified type.
	/// </summary>
	/// <typeparam name="T">The type of elements in the list.</typeparam>
	public interface IListEx<T> : IList<T>, ICollectionEx<T>
	{
	}
}