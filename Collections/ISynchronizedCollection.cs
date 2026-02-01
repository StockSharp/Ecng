namespace Ecng.Collections;

using System.Threading;
using System.Collections.Generic;

using Ecng.Common;

/// <summary>
/// Represents a collection that can be synchronized using a <see cref="Lock"/>.
/// </summary>
public interface ISynchronizedCollection : ISynchronizable
{
}

/// <summary>
/// Represents a generic synchronized collection.
/// </summary>
/// <typeparam name="T">The type of elements in the collection.</typeparam>
public interface ISynchronizedCollection<T> : ISynchronizedCollection, ICollection<T>
{
}