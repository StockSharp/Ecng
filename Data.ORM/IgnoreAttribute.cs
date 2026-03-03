namespace Ecng.Serialization;

/// <summary>
/// Marks a type or member to be ignored during serialization or mapping.
/// </summary>
[AttributeUsage(ReflectionHelper.Types | ReflectionHelper.Members, AllowMultiple = true)]
public class IgnoreAttribute : Attribute
{
	/// <summary>
	/// Gets or sets the specific field name to ignore.
	/// </summary>
	public string FieldName { get; set; }
}