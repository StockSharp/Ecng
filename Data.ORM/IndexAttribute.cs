namespace Ecng.Serialization;

[AttributeUsage(ReflectionHelper.Members | ReflectionHelper.Types)]
public class IndexAttribute : Attribute
{
	public string FieldName { get; set; }
	public bool CacheNull { get; set; }
}

public class UniqueAttribute : IndexAttribute
{
}

public class IdentityAttribute : UniqueAttribute
{
}