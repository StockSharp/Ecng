namespace Ecng.Serialization;

/// <summary>
/// Represents a named, typed serialization value.
/// </summary>
public class SerializationItem : Equatable<SerializationItem>
{
	private readonly int _hashCode;

	/// <summary>
	/// Initializes a new instance with the specified name, type, and value.
	/// </summary>
	public SerializationItem(string name, Type type, object value)
	{
		Name = name ?? throw new ArgumentNullException(nameof(name));
		Type = type ?? throw new ArgumentNullException(nameof(type));
		Value = value;

		_hashCode = name.GetHashCode() ^ type.GetHashCode() ^ (value?.GetHashCode() ?? 397);
	}

	/// <summary>
	/// Gets the item name.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Gets the CLR type of the item.
	/// </summary>
	public Type Type { get; }

	private object _value;

	/// <summary>
	/// Gets or sets the item value.
	/// </summary>
	public object Value
	{
		get => _value;
		set => _value = value;
	}

	/// <inheritdoc />
	public override string ToString() => $"Name = '{Name}' Type = '{Type.Name}' Value = '{Value}'";

	/// <inheritdoc />
	public override int GetHashCode() => _hashCode;

	/// <summary>
	/// Creates a deep copy of this item.
	/// </summary>
	public override SerializationItem Clone()
	{
		var clone = new SerializationItem(Name, Type, Value);

		if (Value != null)
		{
			if (Value is SerializationItemCollection collection)
				clone.Value = collection.Clone();
			else
				clone.Value = Value;
		}

		return clone;
	}

	/// <inheritdoc />
	protected override bool OnEquals(SerializationItem other)
		=> Name == other.Name && Type == other.Type && Equals(Value, other.Value);
}
