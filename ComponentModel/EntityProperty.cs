namespace Ecng.ComponentModel;

using System.Collections.Generic;

/// <summary>
/// Represents an entity property with hierarchical structure.
/// </summary>
public class EntityProperty
{
	/// <summary>
	/// Gets or sets the unique name of the property.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// Gets or sets the display name of the property.
	/// </summary>
	public string DisplayName { get; set; }

	/// <summary>
	/// Gets or sets the description of the property.
	/// </summary>
	public string Description { get; set; }

	/// <summary>
	/// Gets or sets the parent property in the hierarchy.
	/// </summary>
	public EntityProperty Parent { get; set; }

	/// <summary>
	/// Gets or sets the collection of child properties.
	/// </summary>
	public IEnumerable<EntityProperty> Properties { get; set; }

	/// <summary>
	/// Gets the full display name, which includes the names of the parent properties.
	/// </summary>
	public string FullDisplayName => Parent is null ? DisplayName : $"{Parent.FullDisplayName} -> {DisplayName}";

	/// <summary>
	/// Gets the name of the parent property. Returns an empty string if there is no parent.
	/// </summary>
	public string ParentName => Parent is null ? string.Empty : Parent.Name;

	/// <summary>
	/// Returns a string that represents the current entity property.
	/// </summary>
	/// <returns>A string representation of the entity property including its name and full display name.</returns>
	public override string ToString()
	{
		return $"{Name} ({FullDisplayName})";
	}
}
