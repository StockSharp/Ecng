namespace Ecng.ComponentModel;

using System;
using System.ComponentModel;

using Ecng.Common;

public abstract class NamedPropertyDescriptor : PropertyDescriptor
{
	protected NamedPropertyDescriptor(MemberDescriptor descr) : base(descr)
	{
	}

	protected NamedPropertyDescriptor(MemberDescriptor descr, Attribute[] attrs) : base(descr, attrs)
	{
	}

	protected NamedPropertyDescriptor(string name, Attribute[] attrs) : base(name, attrs)
	{
	}

	public override string ToString() => DisplayName.IsEmpty(Name);
}