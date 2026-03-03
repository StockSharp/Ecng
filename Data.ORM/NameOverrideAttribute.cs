namespace Ecng.Serialization;

/// <summary>
/// Specifies a name mapping override for a member during serialization or migration.
/// </summary>
/// <param name="oldName">The original field name.</param>
/// <param name="newName">The new field name.</param>
[AttributeUsage(ReflectionHelper.Members, AllowMultiple = true)]
public sealed class NameOverrideAttribute(string oldName, string newName) : Attribute
{
	/// <summary>
	/// Gets the original field name.
	/// </summary>
	public string OldName { get; } = oldName;

	/// <summary>
	/// Gets the new field name.
	/// </summary>
	public string NewName { get; } = newName;
}
