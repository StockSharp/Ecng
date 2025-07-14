namespace Ecng.ComponentModel;

using System;
using System.Collections.Generic;

using Ecng.Common;

/// <summary>
/// Represents an entity property with hierarchical structure.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EntityProperty"/> class with the specified parameters.
/// </remarks>
/// <param name="name"><see cref="Name"/></param>
/// <param name="displayName"><see cref="DisplayName"/></param>
/// <param name="description"><see cref="Description"/></param>
/// <param name="type"><see cref="Type"/></param>
/// <param name="parent"><see cref="Parent"/></param>
public class EntityProperty(string name, string displayName, string description, Type type, EntityProperty parent)
{
	/// <summary>
	/// Gets the unique name of the property.
	/// </summary>
	public string Name { get; } = name.ThrowIfEmpty(nameof(name));

	/// <summary>
	/// Gets the display name of the property.
	/// </summary>
	public string DisplayName { get; } = displayName.ThrowIfEmpty(nameof(displayName));

	/// <summary>
	/// Gets the description of the property.
	/// </summary>
	public string Description { get; } = description;

	/// <summary>
	/// Gets the type of the property.
	/// </summary>
	public Type Type { get; } = type ?? throw new ArgumentNullException(nameof(type));

	/// <summary>
	/// Gets the parent property in the hierarchy.
	/// </summary>
	public EntityProperty Parent { get; } = parent;

	/// <summary>
	/// Gets the collection of child properties.
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

	/// <inheritdoc/>
	public override string ToString() => $"{Name} ({FullDisplayName})";
}
