namespace Ecng.Serialization;

public class SerializationItemCollection : BaseList<SerializationItem>, ICloneable<SerializationItemCollection>
{
	private readonly Dictionary<string, SerializationItem> _innerDictionary = new(StringComparer.InvariantCultureIgnoreCase);

	public SerializationItemCollection()
	{
	}

	public SerializationItemCollection(IEnumerable<SerializationItem> source)
	{
		this.AddRange(source);
	}

	#region Item

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

	public bool TryGetItem(string name, out SerializationItem item)
		=> _innerDictionary.TryGetValue(name, out item);

	public bool Remove(string name)
		=> TryGetItem(name, out var item) && base.Remove(item);

	#region BaseCollection<SerializationItem> Members

	protected override bool OnAdding(SerializationItem item)
	{
		if (_innerDictionary.ContainsKey(item.Name))
			throw new ArgumentException($"Item with name '{item.Name}' already added.", nameof(item));

		_innerDictionary.Add(item.Name, item);

		return base.OnAdding(item);
	}

	protected override bool OnClearing()
	{
		_innerDictionary.Clear();
		return base.OnClearing();
	}

	protected override bool OnRemoving(SerializationItem item)
	{
		_innerDictionary.Remove(item.Name);
		return base.OnRemoving(item);
	}

	protected override bool OnInserting(int index, SerializationItem item)
	{
		_innerDictionary.Add(item.Name, item);
		return base.OnInserting(index, item);
	}

	#endregion

	#region Implementation of ICloneable

	public SerializationItemCollection Clone()
	{
		var clone = new SerializationItemCollection();

		foreach (var item in this)
			clone.Add(item.Clone());

		return clone;
	}

	object ICloneable.Clone() => Clone();

	#endregion

	public override bool Equals(object obj) => obj is SerializationItemCollection list && this.SequenceEqual(list);

	public override int GetHashCode() => this.GetHashCodeEx();
}
