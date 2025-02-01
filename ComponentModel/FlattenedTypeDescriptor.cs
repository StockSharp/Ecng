﻿namespace Ecng.ComponentModel;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Ecng.Common;

public class FlattenedTypeDescriptor : ICustomTypeDescriptor
{
	private class FlattenedPropertyDescriptor(object root, PropertyDescriptor originalDescriptor, string parentPath)
#pragma warning disable CS9107 // Parameter is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well.
		: PropertyDescriptor(parentPath.Remove("."), originalDescriptor.Attributes.Cast<Attribute>().ToArray())
#pragma warning restore CS9107 // Parameter is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well.
	{
		public override Type ComponentType => root.GetType();
		public override bool IsReadOnly => originalDescriptor.IsReadOnly;
		public override Type PropertyType => originalDescriptor.PropertyType;
		public override string DisplayName => originalDescriptor.DisplayName;
		public override string Description => originalDescriptor.Description;
		public override string Category => originalDescriptor.Category;

		public override object GetValue(object component)
		{
			var container = GetContainer();
			return container is null ? null : originalDescriptor.GetValue(container);
		}

		public override void SetValue(object component, object value)
		{
			var container = GetContainer();

			if (container is not null)
				originalDescriptor.SetValue(container, value);
		}

		public override bool CanResetValue(object component)
		{
			var container = GetContainer();
			return container is not null && originalDescriptor.CanResetValue(container);
		}

		public override void ResetValue(object component)
		{
			var container = GetContainer();

			if (container is not null)
				originalDescriptor.ResetValue(container);
		}

		public override bool ShouldSerializeValue(object component)
			=> false;

		private object GetContainer()
		{
			if (parentPath.IsEmpty())
				return root;

			var current = root;
			var segments = parentPath.Split('.');

			foreach (var segment in segments.Take(segments.Length - 1))
			{
				if (current == null)
					break;

				var subProp = TypeDescriptor.GetProperties(current).Find(segment, false);
				if (subProp == null)
					return null;

				current = subProp.GetValue(current);
			}

			return current;
		}

		public override string ToString() => Name;
	}

	private readonly object _root;
	private readonly IEnumerable<(PropertyDescriptor prop, string path)> _descriptors;
	private readonly PropertyDescriptorCollection _props;

	public FlattenedTypeDescriptor(object root, IEnumerable<(PropertyDescriptor prop, string path)> descriptors)
	{
		_root = root ?? throw new ArgumentNullException(nameof(root));
		_descriptors = descriptors ?? throw new ArgumentNullException(nameof(descriptors));

		_props = new(_descriptors
			.Select(d => (PropertyDescriptor)new FlattenedPropertyDescriptor(_root, d.prop, d.path))
			.ToArray());
	}

	AttributeCollection ICustomTypeDescriptor.GetAttributes()
		=> TypeDescriptor.GetAttributes(_root, noCustomTypeDesc: true);

	string ICustomTypeDescriptor.GetClassName()
		=> TypeDescriptor.GetClassName(_root, noCustomTypeDesc: true);

	string ICustomTypeDescriptor.GetComponentName()
		=> TypeDescriptor.GetComponentName(_root, noCustomTypeDesc: true);

	TypeConverter ICustomTypeDescriptor.GetConverter()
		=> TypeDescriptor.GetConverter(_root, noCustomTypeDesc: true);

	EventDescriptor ICustomTypeDescriptor.GetDefaultEvent()
		=> TypeDescriptor.GetDefaultEvent(_root, noCustomTypeDesc: true);

	PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty()
		=> _props.TryGetDefault(_root.GetType());

	object ICustomTypeDescriptor.GetEditor(Type editorBaseType)
		=> TypeDescriptor.GetEditor(_root, editorBaseType, noCustomTypeDesc: true);

	EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes)
		=> TypeDescriptor.GetEvents(_root, attributes, noCustomTypeDesc: true);

	EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
		=> TypeDescriptor.GetEvents(_root, noCustomTypeDesc: true);

	PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
		=> this.GetFilteredProperties(attributes);

	PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
		=> _props;

	object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd)
		=> _root;
}