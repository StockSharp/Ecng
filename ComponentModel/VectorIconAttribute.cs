namespace Ecng.ComponentModel;

using System;

using Ecng.Common;

/// <summary>
/// Represents an attribute that specifies a vector icon.
/// </summary>
public class VectorIconAttribute(string icon) : Attribute
{
	/// <summary>
	/// Gets the vector icon identifier.
	/// </summary>
	public string Icon { get; } = icon.ThrowIfEmpty(nameof(icon));
}