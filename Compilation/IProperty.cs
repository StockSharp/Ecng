namespace Ecng.Compilation;

using System;
using System.Reflection;

using Ecng.Common;
using Ecng.ComponentModel;
using Ecng.Reflection;

public interface IProperty : IMember
{
	Type Type { get; }

	bool IsBrowsable { get; }
	bool IsReadOnly { get; }

	object GetValue(object instance);
	void SetValue(object instance, object value);
}

class PropertyImpl(PropertyInfo property) : IProperty
{
	private readonly PropertyInfo _property = property ?? throw new ArgumentNullException(nameof(property));

	string IMember.Name => _property.Name;
	string IMember.DisplayName => _property.GetDisplayName();
	string IMember.Description => _property.GetDescription();
	Type IProperty.Type => _property.PropertyType;

	bool IProperty.IsBrowsable => _property.IsBrowsable();
	bool IProperty.IsReadOnly => _property.IsModifiable();
	
	bool IMember.IsPublic => _property.GetGetMethod()?.IsPublic == true;
	bool IMember.IsAbstract => _property.IsAbstract();
	bool IMember.IsGenericDefinition => false;

	object IProperty.GetValue(object instance)
		=> _property.GetValue(instance);

	void IProperty.SetValue(object instance, object value)
		=> _property.SetValue(instance, value);

	T IMember.GetAttribute<T>() => _property.GetAttribute<T>();

	public override string ToString() => _property.ToString();
}