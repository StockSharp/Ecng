namespace Ecng.Serialization;

/// <summary>
/// Marks a property as a single-entity (one-to-one or many-to-one) relation.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class RelationSingleAttribute : Attribute;

/// <summary>
/// Declares that a plain scalar id property is a foreign key to <see cref="ReferencedType"/>,
/// without exposing a navigation property. Use on a raw id column (for example
/// <c>long</c> / <c>long?</c>) that the model deliberately keeps as an id but that has — or
/// should have — a database foreign key. Schema comparison then treats the column as a known
/// foreign key: an existing constraint is not reported as an "extra" FK, and a missing one
/// surfaces as <see cref="SchemaDiffKind.MissingForeignKey"/>. The value still loads and
/// persists as an ordinary scalar; this attribute only affects schema comparison.
/// </summary>
/// <param name="referencedType">The entity type the column references.</param>
[AttributeUsage(AttributeTargets.Property)]
public class ForeignKeyAttribute(Type referencedType) : Attribute
{
	/// <summary>
	/// The entity type this foreign-key column references.
	/// </summary>
	public Type ReferencedType { get; } = referencedType ?? throw new ArgumentNullException(nameof(referencedType));
}

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
