namespace Ecng.Serialization;

/// <summary>
/// Overrides column-level metadata for a specific inner property when flattening inner schema types.
/// Applied to the outer property to control individual inner columns.
/// </summary>
/// <param name="propertyName">The inner property name to override.</param>
[AttributeUsage(ReflectionHelper.Members, AllowMultiple = true)]
public sealed class ColumnOverrideAttribute(string propertyName) : Attribute
{
	/// <summary>
	/// Gets the inner property name this override applies to.
	/// </summary>
	public string PropertyName { get; } = propertyName;

	private bool _isNullableValue;
	private bool _isNullableSet;

	/// <summary>
	/// Gets or sets whether the column allows NULL values.
	/// </summary>
	public bool IsNullable
	{
		get => _isNullableValue;
		set { _isNullableValue = value; _isNullableSet = true; }
	}

	/// <summary>
	/// Gets whether <see cref="IsNullable"/> was explicitly set.
	/// </summary>
	public bool IsNullableSet => _isNullableSet;
}
