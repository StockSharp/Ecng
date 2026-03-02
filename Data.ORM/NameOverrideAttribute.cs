namespace Ecng.Serialization;

[AttributeUsage(ReflectionHelper.Members, AllowMultiple = true)]
public sealed class NameOverrideAttribute(string oldName, string newName) : Attribute
{
	public string OldName { get; } = oldName;
	public string NewName { get; } = newName;
}
