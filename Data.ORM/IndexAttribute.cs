namespace Ecng.Serialization;

/// <summary>
/// Indicates that a member or type should be indexed in the database.
/// </summary>
[AttributeUsage(ReflectionHelper.Members | ReflectionHelper.Types)]
public class IndexAttribute : Attribute
{
	/// <summary>
	/// Gets or sets the database field name for the index.
	/// </summary>
	public string FieldName { get; set; }

	/// <summary>
	/// Gets or sets whether null values should be cached for this index.
	/// </summary>
	public bool CacheNull { get; set; }
}

/// <summary>
/// Indicates that a member or type has a unique index constraint.
/// </summary>
public class UniqueAttribute : IndexAttribute
{
}

/// <summary>
/// Indicates that a member or type serves as the identity (primary key) field.
/// </summary>
public class IdentityAttribute : UniqueAttribute
{
}