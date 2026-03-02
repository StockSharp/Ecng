namespace Ecng.Serialization;

[AttributeUsage(ReflectionHelper.Types | ReflectionHelper.Members, AllowMultiple = true)]
public class IgnoreAttribute : Attribute
{
	public string FieldName { get; set; }
}