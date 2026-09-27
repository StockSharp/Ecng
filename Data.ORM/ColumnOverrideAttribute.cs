namespace Ecng.Serialization;

/// <summary>
/// Overrides column-level metadata for a column the declaring site does not own outright.
///
/// <para>On a <b>property</b>, it names an inner property of a flattened inner schema type,
/// so the outer property can shape individual inner columns.</para>
///
/// <para>On an <b>entity type</b>, it names one of that entity's columns — including a column
/// declared on a base class, which no property-level attribute on the derived type could reach.
/// This mirrors the type-level <see cref="IndexAttribute"/> declarations, which name their
/// columns by string for the same reason. An entity-level override wins over whatever the
/// property itself declares.</para>
/// </summary>
/// <param name="propertyName">The property name whose column this override applies to.</param>
[AttributeUsage(ReflectionHelper.Members | AttributeTargets.Class, AllowMultiple = true)]
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

	/// <summary>
	/// Gets or sets the numeric precision for a decimal/numeric column, or the fractional-second digits
	/// for a date/time column. 0 (the default) leaves the column's own <see cref="ColumnAttribute.Precision"/>
	/// and <see cref="ColumnAttribute.Scale"/> in place. A non-zero value replaces both: <see cref="Scale"/>
	/// is then taken literally, so 0 means no digits after the point, as in DECIMAL(p).
	/// </summary>
	public int Precision { get; set; }

	/// <summary>
	/// Gets or sets the numeric scale for a decimal/numeric column. With <see cref="Precision"/> set, it is
	/// taken literally. Without it, 0 (the default) leaves the column's own scale in place, and any other
	/// value replaces that scale while the precision stays the column's own, or the dialect default when the
	/// column declares none.
	/// </summary>
	public int Scale { get; set; }
}
