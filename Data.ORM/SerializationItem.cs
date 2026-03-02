namespace Ecng.Serialization;

public class SerializationItem : Equatable<SerializationItem>
{
	private readonly int _hashCode;

	public SerializationItem(string name, Type type, object value)
	{
		Name = name ?? throw new ArgumentNullException(nameof(name));
		Type = type ?? throw new ArgumentNullException(nameof(type));
		Value = value;

		_hashCode = name.GetHashCode() ^ type.GetHashCode() ^ (value?.GetHashCode() ?? 397);
	}

	public string Name { get; }
	public Type Type { get; }

	private object _value;

	public object Value
	{
		get => _value;
		set => _value = value;
	}

	public override string ToString() => $"Name = '{Name}' Type = '{Type.Name}' Value = '{Value}'";

	public override int GetHashCode() => _hashCode;

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

	protected override bool OnEquals(SerializationItem other)
		=> Name == other.Name && Type == other.Type && Equals(Value, other.Value);
}
