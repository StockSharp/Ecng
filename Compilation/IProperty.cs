namespace Ecng.Compilation;

using System;
using System.Reflection;

using Ecng.Common;
using Ecng.ComponentModel;
using Ecng.Reflection;

public interface IProperty
{
	string Name { get; }
	string DisplayName { get; }
	Type Type { get; }

	bool IsBrowsable { get; }
	bool IsReadOnly { get; }

	object GetValue(object instance);
	void SetValue(object instance, object value);
}

class PropertyImpl(PropertyInfo property) : IProperty
{
	private readonly PropertyInfo _property = property ?? throw new ArgumentNullException(nameof(property));

	string IProperty.Name => _property.Name;
	string IProperty.DisplayName => _property.GetDisplayName();
	Type IProperty.Type => _property.PropertyType;

	bool IProperty.IsBrowsable => _property.IsBrowsable();
	bool IProperty.IsReadOnly => _property.IsModifiable();

	object IProperty.GetValue(object instance)
		=> _property.GetValue(instance);

	void IProperty.SetValue(object instance, object value)
		=> _property.SetValue(instance, value);

	public override string ToString() => _property.ToString();
}