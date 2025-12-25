namespace Ecng.ComponentModel;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Ecng.Common;

/// <summary>
/// Abstract base class that wraps an object and exposes its properties and events
/// through <see cref="ICustomTypeDescriptor"/> interface. Wrapped properties are read-only
/// and can be displayed in PropertyGrid or similar controls. The wrapper also implements
/// <see cref="INotifyPropertyChanged"/> to support data binding scenarios.
/// </summary>
/// <typeparam name="T">The type of the wrapped object.</typeparam>
/// <param name="obj">The object to wrap.</param>
public abstract class CustomObjectWrapper<T>(T obj) : Disposable, INotifyPropertyChanged, ICustomTypeDescriptor where T : class
{
	/// <inheritdoc />
	public event PropertyChangedEventHandler PropertyChanged;

	/// <summary>
	/// Call <see cref="PropertyChanged"/> event.
	/// </summary>
	/// <param name="name">Member name.</param>
	protected virtual void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

	/// <summary>
	/// Read-only property descriptor that proxies access to the wrapped object's property.
	/// SetValue and ResetValue operations throw <see cref="NotSupportedException"/>.
	/// </summary>
	/// <param name="orig">The original property descriptor from the wrapped object.</param>
	/// <param name="owner">The wrapper instance that owns this descriptor.</param>
	protected class ProxyPropDescriptor(PropertyDescriptor orig, object owner) : NamedPropertyDescriptor(orig)
	{
		private readonly PropertyDescriptor _orig = orig;

		/// <summary>
		/// Parent object.
		/// </summary>
		public object Owner { get; } = owner;

		/// <inheritdoc />
		public override object GetValue(object c) => _orig.GetValue(c);
		/// <inheritdoc />
		public override void SetValue(object c, object value) => throw new NotSupportedException();

		/// <inheritdoc />
		public override bool CanResetValue(object c) => false;
		/// <inheritdoc />
		public override void ResetValue(object c) => throw new NotSupportedException();

		/// <inheritdoc />
		public override bool ShouldSerializeValue(object c) => false;

		/// <inheritdoc />
		public override Type ComponentType => Owner.GetType();
		/// <inheritdoc />
		public override bool IsReadOnly => true;
		/// <inheritdoc />
		public override Type PropertyType => _orig.PropertyType;
	}

	/// <summary>
	/// Event descriptor that proxies access to the wrapped object's event.
	/// </summary>
	/// <param name="orig">The original event descriptor from the wrapped object.</param>
	/// <param name="owner">The wrapper instance that owns this descriptor.</param>
	protected class ProxyEventDescriptor(EventDescriptor orig, object owner) : EventDescriptor(orig)
	{
		private readonly EventDescriptor _orig = orig;

		/// <summary>
		/// Parent object.
		/// </summary>
		public object Owner { get; } = owner;

		/// <inheritdoc />
		public override void AddEventHandler(object component, Delegate value) => _orig.AddEventHandler(component, value);
		/// <inheritdoc />
		public override void RemoveEventHandler(object component, Delegate value) => _orig.RemoveEventHandler(component, value);

		/// <inheritdoc />
		public override Type ComponentType => Owner.GetType();
		/// <inheritdoc />
		public override Type EventType => _orig.EventType;
		/// <inheritdoc />
		public override bool IsMulticast => _orig.IsMulticast;
	}

	/// <summary>
	/// Parent object.
	/// </summary>
	public T Obj { get; } = obj ?? throw new ArgumentNullException(nameof(obj));

	object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd) => Obj;

	AttributeCollection ICustomTypeDescriptor.GetAttributes() => TypeDescriptor.GetAttributes(Obj, true);

	string ICustomTypeDescriptor.GetClassName() => TypeDescriptor.GetClassName(Obj, true);

	string ICustomTypeDescriptor.GetComponentName() => TypeDescriptor.GetComponentName(Obj, true);

	TypeConverter ICustomTypeDescriptor.GetConverter() => TypeDescriptor.GetConverter(Obj, true);

	EventDescriptor ICustomTypeDescriptor.GetDefaultEvent() => TypeDescriptor.GetDefaultEvent(Obj, true);

	PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty() => TypeDescriptor.GetDefaultProperty(Obj, true);

	object ICustomTypeDescriptor.GetEditor(Type editorBaseType) => TypeDescriptor.GetEditor(Obj, editorBaseType, true);

	EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes) => ((ICustomTypeDescriptor)this).GetEvents();

	PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes) => ((ICustomTypeDescriptor)this).GetProperties();

	/// <summary>
	/// Gets the event descriptors from the wrapped object.
	/// </summary>
	/// <returns>Collection of proxy event descriptors.</returns>
	protected virtual IEnumerable<EventDescriptor> OnGetEvents()
	{
		return Obj is null
			? null
			: TypeDescriptor.GetEvents(Obj, true)
				.OfType<EventDescriptor>()
				.Select(ed => new ProxyEventDescriptor(ed, this));
	}

	/// <summary>
	/// Gets the property descriptors from the wrapped object.
	/// </summary>
	/// <returns>Collection of read-only proxy property descriptors.</returns>
	protected virtual IEnumerable<PropertyDescriptor> OnGetProperties()
	{
		return Obj is null
			? null
			: TypeDescriptor.GetProperties(Obj, true)
				.Typed()
				.Select(pd => new ProxyPropDescriptor(pd, this));
	}

	private EventDescriptorCollection _eventCollection;

	EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
	{
		if(_eventCollection != null)
			return _eventCollection;

		_eventCollection = new EventDescriptorCollection(OnGetEvents()?.ToArray() ?? []);

		return _eventCollection;
	}

	private PropertyDescriptorCollection _propCollection;

	PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
	{
		if(_propCollection != null)
			return _propCollection;

		_propCollection = new PropertyDescriptorCollection(OnGetProperties()?.ToArray() ?? []);

		return _propCollection;
	}

	/// <inheritdoc />
	public override string ToString() => Obj?.ToString();
}