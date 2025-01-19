namespace Ecng.Compilation.Python;

using System;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;

public class PythonObjectTypeDescriptor(object pythonObject) : ICustomTypeDescriptor
{
	private readonly object _pythonObject = pythonObject ?? throw new ArgumentNullException(nameof(pythonObject));

	public PropertyDescriptorCollection GetProperties()
	{
		var baseType = _pythonObject.GetType();

		while (baseType?.IsPythonType() == true)
			baseType = baseType.BaseType;

		var dotNetProperties = baseType is null
			? []
			: TypeDescriptor
			.GetProperties(baseType)
			.Cast<PropertyDescriptor>()
			.ToDictionary(p => p.Name);

		var pythonProperties = TypeDescriptor
			.GetProperties(_pythonObject)
			.Cast<PropertyDescriptor>()
			.Where(pd => pd.PropertyType.IsPrimitive && !pd.Name.StartsWith("_") && !dotNetProperties.ContainsKey(pd.Name));

		return new PropertyDescriptorCollection(dotNetProperties.Values.Concat(pythonProperties).Where(pd => pd.IsBrowsable).ToArray());
	}

	public AttributeCollection GetAttributes() => TypeDescriptor.GetAttributes(_pythonObject);
	public string GetClassName() => TypeDescriptor.GetClassName(_pythonObject);
	public string GetComponentName() => TypeDescriptor.GetComponentName(_pythonObject);
	public TypeConverter GetConverter() => TypeDescriptor.GetConverter(_pythonObject);
	public EventDescriptor GetDefaultEvent() => TypeDescriptor.GetDefaultEvent(_pythonObject);
	public PropertyDescriptor GetDefaultProperty() => TypeDescriptor.GetDefaultProperty(_pythonObject);
	public object GetEditor(Type editorBaseType) => TypeDescriptor.GetEditor(_pythonObject, editorBaseType);
	public EventDescriptorCollection GetEvents() => TypeDescriptor.GetEvents(_pythonObject);
	public EventDescriptorCollection GetEvents(Attribute[] attributes) => TypeDescriptor.GetEvents(_pythonObject, attributes);
	public PropertyDescriptorCollection GetProperties(Attribute[] attributes) => GetProperties();
	public object GetPropertyOwner(PropertyDescriptor pd) => _pythonObject;
}