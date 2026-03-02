namespace Ecng.Data;

public abstract class SqlRenderer(string name)
{
	#region Private Fields

	//private static readonly Dictionary<Type, string> _tables = new Dictionary<Type, string>();
	//private static Dictionary<string, string> _procedures = new Dictionary<string, string>();

	private readonly Dictionary<DbType, Converter<Range<int>, string>> _typeNames = [];

	#endregion

	#region SqlRenderer.ctor()

	#endregion

	public string Name { get; } = name;

	private ISet<string> _reservedWords;

	public string FormatReserver(string name)
	{
		_reservedWords ??= ReservedWords.ToIgnoreCaseSet();

		if (_reservedWords.Contains(name))
			name = $"[{name}]";

		return name;
	}

	#region AddTypeName

	protected void AddTypeName(DbType type, string name)
	{
		AddTypeName(type, delegate { return name; });
	}

	protected void AddTypeName(DbType type, Converter<Range<int>, string> converter)
	{
		_typeNames.Add(type, converter);
	}

	#endregion

	public abstract string GetIdentitySelect(string idCol);

	public string FormatParameter(string value)
	{
		if (value.IsEmpty())
			throw new ArgumentNullException(nameof(value));

		return ParameterPrefix + value + ParameterSuffix;
	}

	public string UnformatParameter(string value)
	{
		if (value.IsEmpty())
			throw new ArgumentNullException(nameof(value));

		if (value.Length < ParameterPrefix.Length + ParameterSuffix.Length + 1)
			throw new ArgumentOutOfRangeException(nameof(value));

		value = value[ParameterPrefix.Length..];
		return value[..^ParameterSuffix.Length];
	}

	public string GetTypeName(DbType type, Range<int> length)
	{
		if (!_typeNames.TryGetValue(type, out var value))
			throw new ArgumentException($"Type '{type}' is not supported.", nameof(type));

		return value(length);
	}

	protected abstract string ParameterPrefix { get; }
	protected virtual string ParameterSuffix => string.Empty;

	protected abstract string[] ReservedWords { get; }

	public abstract string Skip(string skip);
	public abstract string Take(string skip);

	public abstract string Now();
	public abstract string UtcNow();
	public abstract string SysNow();
	public abstract string SysUtcNow();
	public abstract string NewId();
}