namespace Ecng.ComponentModel;

using System;
using System.ComponentModel;

using Ecng.Common;

/// <summary>
/// Represents a property descriptor with a defined name used for display purposes.
/// </summary>
public abstract class NamedPropertyDescriptor : PropertyDescriptor
{
	/// <summary>
	/// Initializes a new instance of the <see cref="NamedPropertyDescriptor"/> class.
	/// </summary>
	/// <param name="descr"><see cref="MemberDescriptor"/></param>
	protected NamedPropertyDescriptor(MemberDescriptor descr) : base(descr)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="NamedPropertyDescriptor"/> class.
	/// </summary>
	/// <param name="descr"><see cref="MemberDescriptor"/></param>
	/// <param name="attrs"><see cref="MemberDescriptor.Attributes"/></param>
	protected NamedPropertyDescriptor(MemberDescriptor descr, Attribute[] attrs) : base(descr, attrs)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="NamedPropertyDescriptor"/> class.
	/// </summary>
	/// <param name="name"><see cref="MemberDescriptor.Name"/></param>
	/// <param name="attrs"><see cref="MemberDescriptor.Attributes"/></param>
	protected NamedPropertyDescriptor(string name, Attribute[] attrs) : base(name, attrs)
	{
	}

	/// <inheritdoc/>
	public override string ToString() => DisplayName.IsEmpty(Name);
}