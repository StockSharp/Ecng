namespace Ecng.ComponentModel;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Ecng.Common;

public class FlattenedTypeDescriptor : Disposable, ICustomTypeDescriptor, INotifyPropertiesChanged, INotifyPropertyChanged, INotifyPropertyChanging
{
	private class FlattenedPropertyDescriptor(object root, PropertyDescriptor originalDescriptor, string parentPath)
		: NamedPropertyDescriptor(parentPath.Remove("."), [.. originalDescriptor.Attributes.Cast<Attribute>()])
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
	}

	private readonly object _root;
	private readonly IEnumerable<(PropertyDescriptor prop, string path)> _descriptors;
	private readonly PropertyDescriptorCollection _props;

	public FlattenedTypeDescriptor(object root, IEnumerable<(PropertyDescriptor prop, string path)> descriptors)
	{
		_root = root ?? throw new ArgumentNullException(nameof(root));
		_descriptors = descriptors ?? throw new ArgumentNullException(nameof(descriptors));

		_props = new([.. _descriptors.Select(d => (PropertyDescriptor)new FlattenedPropertyDescriptor(_root, d.prop, d.path))]);

		if (_root is INotifyPropertiesChanged npc)
			npc.PropertiesChanged += OnPropertiesChanged;

		if (_root is INotifyPropertyChanged npc1)
			npc1.PropertyChanged += OnPropertyChanged;

		if (_root is INotifyPropertyChanging npc2)
			npc2.PropertyChanging += OnPropertyChanging;
	}

	protected override void DisposeManaged()
	{
		base.DisposeManaged();

		if (_root is INotifyPropertiesChanged npc)
			npc.PropertiesChanged -= OnPropertiesChanged;

		if (_root is INotifyPropertyChanged npc1)
			npc1.PropertyChanged -= OnPropertyChanged;

		if (_root is INotifyPropertyChanging npc2)
			npc2.PropertyChanging -= OnPropertyChanging;
	}

	public event Action PropertiesChanged;
	public event PropertyChangedEventHandler PropertyChanged;
	public event PropertyChangingEventHandler PropertyChanging;

	private void OnPropertiesChanged()
		=> PropertiesChanged?.Invoke();

	private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
		=> PropertyChanged?.Invoke(this, e);

	private void OnPropertyChanging(object sender, PropertyChangingEventArgs e)
		=> PropertyChanging?.Invoke(this, e);

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