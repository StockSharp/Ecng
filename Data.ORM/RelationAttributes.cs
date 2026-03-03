namespace Ecng.Serialization;

/// <summary>
/// Marks a property as a single-entity (one-to-one or many-to-one) relation.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class RelationSingleAttribute : Attribute;

/// <summary>
/// Marks a property as a collection (one-to-many or many-to-many) relation.
/// </summary>
/// <param name="listType">Optional custom list type for the collection.</param>
[AttributeUsage(AttributeTargets.Property)]
public class RelationManyAttribute(Type listType = null) : Attribute
{
	/// <summary>
	/// Gets the custom list type for the relation collection.
	/// </summary>
	public Type ListType { get; } = listType;

	/// <summary>
	/// Gets or sets whether to use bulk loading for this relation.
	/// </summary>
	public bool BulkLoad { get; set; }

	/// <summary>
	/// Gets or sets whether to cache the element count for this relation.
	/// </summary>
	public bool CacheCount { get; set; }

	/// <summary>
	/// Gets or sets the buffer size for loading relation elements.
	/// </summary>
	public int BufferSize { get; set; }
}
