namespace Ecng.Tests.ComponentModel;

using System.ComponentModel;

using Ecng.ComponentModel;

[TestClass]
public class CustomObjectWrapperTests : BaseTestClass
{
	#region Test classes

	private class TestObject
	{
		public int Id { get; set; }
		public string Name { get; set; }

		public event EventHandler TestEvent;

		public void RaiseEvent() => TestEvent?.Invoke(this, EventArgs.Empty);
	}

	private class TestWrapper : CustomObjectWrapper<TestObject>
	{
		public TestWrapper(TestObject obj) : base(obj) { }

		public void RaisePropertyChanged(string name) => OnPropertyChanged(name);
	}

	#endregion

	[TestMethod]
	public void Constructor_NullObject_Throws()
	{
		ThrowsExactly<ArgumentNullException>(() => new TestWrapper(null));
	}

	[TestMethod]
	public void Obj_ReturnsWrappedObject()
	{
		var obj = new TestObject { Id = 1, Name = "Test" };
		using var wrapper = new TestWrapper(obj);

		wrapper.Obj.AssertSame(obj);
	}

	[TestMethod]
	public void GetProperties_ReturnsWrappedProperties()
	{
		var obj = new TestObject { Id = 42, Name = "Hello" };
		using var wrapper = new TestWrapper(obj);

		var props = ((ICustomTypeDescriptor)wrapper).GetProperties();

		(props.Count > 0).AssertTrue();
		props["Id"].AssertNotNull();
		props["Name"].AssertNotNull();
	}

	[TestMethod]
	public void GetProperties_CanReadValues()
	{
		var obj = new TestObject { Id = 42, Name = "Hello" };
		using var wrapper = new TestWrapper(obj);

		var props = ((ICustomTypeDescriptor)wrapper).GetProperties();

		props["Id"].GetValue(obj).AssertEqual(42);
		props["Name"].GetValue(obj).AssertEqual("Hello");
	}

	[TestMethod]
	public void GetProperties_IsReadOnly()
	{
		var obj = new TestObject();
		using var wrapper = new TestWrapper(obj);

		var props = ((ICustomTypeDescriptor)wrapper).GetProperties();

		props["Id"].IsReadOnly.AssertTrue();
	}

	[TestMethod]
	public void GetProperties_SetValue_ThrowsNotSupported()
	{
		var obj = new TestObject();
		using var wrapper = new TestWrapper(obj);

		var props = ((ICustomTypeDescriptor)wrapper).GetProperties();

		ThrowsExactly<NotSupportedException>(() => props["Id"].SetValue(obj, 100));
	}

	[TestMethod]
	public void GetProperties_CachesCollection()
	{
		var obj = new TestObject();
		using var wrapper = new TestWrapper(obj);

		var props1 = ((ICustomTypeDescriptor)wrapper).GetProperties();
		var props2 = ((ICustomTypeDescriptor)wrapper).GetProperties();

		props1.AssertSame(props2);
	}

	[TestMethod]
	public void GetEvents_ReturnsWrappedEvents()
	{
		var obj = new TestObject();
		using var wrapper = new TestWrapper(obj);

		var events = ((ICustomTypeDescriptor)wrapper).GetEvents();

		(events.Count > 0).AssertTrue();
		events["TestEvent"].AssertNotNull();
	}

	[TestMethod]
	public void GetEvents_CachesCollection()
	{
		var obj = new TestObject();
		using var wrapper = new TestWrapper(obj);

		var events1 = ((ICustomTypeDescriptor)wrapper).GetEvents();
		var events2 = ((ICustomTypeDescriptor)wrapper).GetEvents();

		events1.AssertSame(events2);
	}

	[TestMethod]
	public void PropertyChanged_RaisesEvent()
	{
		var obj = new TestObject();
		using var wrapper = new TestWrapper(obj);

		var raised = false;
		string changedProp = null;

		wrapper.PropertyChanged += (s, e) =>
		{
			raised = true;
			changedProp = e.PropertyName;
		};

		wrapper.RaisePropertyChanged("TestProp");

		raised.AssertTrue();
		changedProp.AssertEqual("TestProp");
	}

	[TestMethod]
	public void GetClassName_ReturnsWrappedClassName()
	{
		var obj = new TestObject();
		using var wrapper = new TestWrapper(obj);

		((ICustomTypeDescriptor)wrapper).GetClassName().Contains(nameof(TestObject)).AssertTrue();
	}

	[TestMethod]
	public void GetPropertyOwner_ReturnsWrappedObject()
	{
		var obj = new TestObject();
		using var wrapper = new TestWrapper(obj);

		var props = ((ICustomTypeDescriptor)wrapper).GetProperties();

		((ICustomTypeDescriptor)wrapper).GetPropertyOwner(props[0]).AssertSame(obj);
	}

	[TestMethod]
	public void ToString_ReturnsWrappedToString()
	{
		var obj = new TestObject();
		using var wrapper = new TestWrapper(obj);

		wrapper.ToString().AssertEqual(obj.ToString());
	}

	[TestMethod]
	public void GetAttributes_ReturnsWrappedAttributes()
	{
		var obj = new TestObject();
		using var wrapper = new TestWrapper(obj);

		var attrs = ((ICustomTypeDescriptor)wrapper).GetAttributes();

		attrs.AssertNotNull();
	}

	[TestMethod]
	public void GetConverter_ReturnsWrappedConverter()
	{
		var obj = new TestObject();
		using var wrapper = new TestWrapper(obj);

		var converter = ((ICustomTypeDescriptor)wrapper).GetConverter();

		converter.AssertNotNull();
	}

	[TestMethod]
	public void ProxyPropDescriptor_ComponentType_ReturnsWrapperType()
	{
		var obj = new TestObject();
		using var wrapper = new TestWrapper(obj);

		var props = ((ICustomTypeDescriptor)wrapper).GetProperties();

		props["Id"].ComponentType.AssertEqual(typeof(TestWrapper));
	}

	[TestMethod]
	public void ProxyPropDescriptor_CanResetValue_ReturnsFalse()
	{
		var obj = new TestObject();
		using var wrapper = new TestWrapper(obj);

		var props = ((ICustomTypeDescriptor)wrapper).GetProperties();

		props["Id"].CanResetValue(obj).AssertFalse();
	}

	[TestMethod]
	public void ProxyPropDescriptor_ResetValue_ThrowsNotSupported()
	{
		var obj = new TestObject();
		using var wrapper = new TestWrapper(obj);

		var props = ((ICustomTypeDescriptor)wrapper).GetProperties();

		ThrowsExactly<NotSupportedException>(() => props["Id"].ResetValue(obj));
	}

	[TestMethod]
	public void ProxyPropDescriptor_ShouldSerializeValue_ReturnsFalse()
	{
		var obj = new TestObject();
		using var wrapper = new TestWrapper(obj);

		var props = ((ICustomTypeDescriptor)wrapper).GetProperties();

		props["Id"].ShouldSerializeValue(obj).AssertFalse();
	}

	[TestMethod]
	public void ProxyEventDescriptor_ComponentType_ReturnsWrapperType()
	{
		var obj = new TestObject();
		using var wrapper = new TestWrapper(obj);

		var events = ((ICustomTypeDescriptor)wrapper).GetEvents();

		events["TestEvent"].ComponentType.AssertEqual(typeof(TestWrapper));
	}
}
