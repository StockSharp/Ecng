namespace Ecng.Serialization;

/// <summary>
/// A collection of <see cref="SerializationItem"/> instances with name-based lookup.
/// </summary>
public class SerializationItemCollection : BaseList<SerializationItem>, ICloneable<SerializationItemCollection>
{
	private readonly Dictionary<string, SerializationItem> _innerDictionary = new(StringComparer.InvariantCultureIgnoreCase);

	/// <summary>
	/// Initializes an empty collection.
	/// </summary>
	public SerializationItemCollection()
	{
	}

	/// <summary>
	/// Initializes a new collection populated from the specified source.
	/// </summary>
	public SerializationItemCollection(IEnumerable<SerializationItem> source)
	{
		this.AddRange(source);
	}

	#region Item

	/// <summary>
	/// Gets the item with the specified name.
	/// </summary>
	public SerializationItem this[string name]
	{
		get
		{
			if (!_innerDictionary.TryGetValue(name, out var retVal))
				throw new ArgumentException($"Item with name '{name}' doesn't exists.", nameof(name));

			return retVal;
		}
	}

	#endregion

	/// <summary>
	/// Tries to get an item by name, returning false if not found.
	/// </summary>
	public bool TryGetItem(string name, out SerializationItem item)
		=> _innerDictionary.TryGetValue(name, out item);

	/// <summary>
	/// Removes the item with the specified name.
	/// </summary>
	public bool Remove(string name)
		=> TryGetItem(name, out var item) && base.Remove(item);

	#region BaseCollection<SerializationItem> Members

	/// <inheritdoc />
	protected override bool OnAdding(SerializationItem item)
	{
		if (_innerDictionary.ContainsKey(item.Name))
			throw new ArgumentException($"Item with name '{item.Name}' already added.", nameof(item));

		_innerDictionary.Add(item.Name, item);

		return base.OnAdding(item);
	}

	/// <inheritdoc />
	protected override bool OnClearing()
	{
		_innerDictionary.Clear();
		return base.OnClearing();
	}

	/// <inheritdoc />
	protected override bool OnRemoving(SerializationItem item)
	{
		_innerDictionary.Remove(item.Name);
		return base.OnRemoving(item);
	}

	/// <inheritdoc />
	protected override bool OnInserting(int index, SerializationItem item)
	{
		_innerDictionary.Add(item.Name, item);
		return base.OnInserting(index, item);
	}

	#endregion

	#region Implementation of ICloneable

	/// <summary>
	/// Creates a deep copy of this collection.
	/// </summary>
	public SerializationItemCollection Clone()
	{
		var clone = new SerializationItemCollection();

		foreach (var item in this)
			clone.Add(item.Clone());

		return clone;
	}

	object ICloneable.Clone() => Clone();

	#endregion

	/// <inheritdoc />
	public override bool Equals(object obj) => obj is SerializationItemCollection list && this.SequenceEqual(list);

	/// <inheritdoc />
	public override int GetHashCode() => this.GetHashCodeEx();
}
