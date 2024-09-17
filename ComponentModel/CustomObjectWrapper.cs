namespace Ecng.ComponentModel
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;

	using Ecng.Common;

	public abstract class CustomObjectWrapper<T> : Disposable, INotifyPropertyChanged, ICustomTypeDescriptor where T : class
	{
		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

		/// <summary>
		/// Specialization of <see cref="PropertyDescriptor"/>.
		/// </summary>
		protected class ProxyPropDescriptor : PropertyDescriptor
		{
			private readonly PropertyDescriptor _orig;

			/// <summary>
			/// Create instance.
			/// </summary>
			public ProxyPropDescriptor(PropertyDescriptor orig, object owner) : base(orig)
			{
				Owner = owner;
				_orig = orig;
			}

			/// <summary>
			/// Parent object.
			/// </summary>
			public object Owner { get; }

			/// <inheritdoc />
			public override object GetValue(object c)  => _orig.GetValue(c);
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
		/// Specialization of <see cref="EventDescriptor"/>.
		/// </summary>
		protected class ProxyEventDescriptor : EventDescriptor
		{
			private readonly EventDescriptor _orig;

			/// <summary>
			/// Create instance.
			/// </summary>
			public ProxyEventDescriptor(EventDescriptor orig, object owner) : base(orig)
			{
				Owner = owner;
				_orig = orig;
			}

			/// <summary>
			/// Parent object.
			/// </summary>
			public object Owner { get; }

			public override void AddEventHandler(object component, Delegate value) => _orig.AddEventHandler(component, value);
			public override void RemoveEventHandler(object component, Delegate value) => _orig.RemoveEventHandler(component, value);

			public override Type ComponentType => Owner.GetType();
			public override Type EventType => _orig.EventType;
			public override bool IsMulticast => _orig.IsMulticast;
		}

		/// <summary>
		/// Parent object.
		/// </summary>
		public T Obj { get; }

		/// <summary>
		/// Create instance.
		/// </summary>
		/// <param name="obj">Parent chart element or indicator.</param>
		protected CustomObjectWrapper(T obj) => Obj = obj ?? throw new ArgumentNullException(nameof(obj));

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
		/// Get property list from wrapped object.
		/// </summary>
		protected virtual IEnumerable<EventDescriptor> OnGetEvents()
		{
			return Obj is null
				? null
				: TypeDescriptor.GetEvents(Obj, true)
					.OfType<EventDescriptor>()
					.Select(ed => new ProxyEventDescriptor(ed, this));
		}

		/// <summary>
		/// Get property list from wrapped object.
		/// </summary>
		protected virtual IEnumerable<PropertyDescriptor> OnGetProperties()
		{
			return Obj is null
				? null
				: TypeDescriptor.GetProperties(Obj, true)
					.OfType<PropertyDescriptor>()
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
}