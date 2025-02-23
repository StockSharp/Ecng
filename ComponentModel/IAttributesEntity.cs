namespace Ecng.ComponentModel;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents an entity that contains a collection of attributes.
/// </summary>
public interface IAttributesEntity
{
	/// <summary>
	/// Gets the list of attributes associated with this entity.
	/// </summary>
	public IList<Attribute> Attributes { get; }
}