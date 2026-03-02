namespace Ecng.Serialization;

[AttributeUsage(AttributeTargets.Property)]
public class RelationSingleAttribute : Attribute;

[AttributeUsage(AttributeTargets.Property)]
public class RelationManyAttribute(Type listType = null) : Attribute
{
	public Type ListType { get; } = listType;
	public bool BulkLoad { get; set; }
	public bool CacheCount { get; set; }
	public int BufferSize { get; set; }
}
