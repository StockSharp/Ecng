namespace Ecng.Tests.ComponentModel;

using System.ComponentModel;

using Ecng.ComponentModel;

[TestClass]
public class FlattenedTypeDescriptorTests : BaseTestClass
{
	#region Test classes

	private class SimpleObject
	{
		public int Id { get; set; }
		public string Name { get; set; }
	}

	private class NotifyObject : INotifyPropertyChanged, INotifyPropertyChanging
	{
		private int _value;

		public int Value
		{
			get => _value;
			set
			{
				PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(nameof(Value)));
				_value = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		public event PropertyChangingEventHandler PropertyChanging;
	}

	private class NestedObject
	{
		public SimpleObject Child { get; set; }
	}

	#endregion

	[TestMethod]
	public void Constructor_NullRoot_Throws()
	{
		ThrowsExactly<ArgumentNullException>(() => new FlattenedTypeDescriptor(null, []));
	}

	[TestMethod]
	public void Constructor_NullDescriptors_Throws()
	{
		ThrowsExactly<ArgumentNullException>(() => new FlattenedTypeDescriptor(new SimpleObject(), null));
	}

	[TestMethod]
	public void GetProperties_ReturnsFlattened()
	{
		var obj = new SimpleObject { Id = 1, Name = "Test" };
		var props = TypeDescriptor.GetProperties(obj).Typed().Select(p => (p, p.Name)).ToList();

		using var descriptor = new FlattenedTypeDescriptor(obj, props);
		var result = ((ICustomTypeDescriptor)descriptor).GetProperties();

		result.Count.AssertEqual(2);
	}

	[TestMethod]
	public void GetProperties_CanReadValues()
	{
		var obj = new SimpleObject { Id = 42, Name = "Hello" };
		var props = TypeDescriptor.GetProperties(obj).Typed().Select(p => (p, p.Name)).ToList();

		using var descriptor = new FlattenedTypeDescriptor(obj, props);
		var result = ((ICustomTypeDescriptor)descriptor).GetProperties();

		result["Id"].GetValue(obj).AssertEqual(42);
		result["Name"].GetValue(obj).AssertEqual("Hello");
	}

	[TestMethod]
	public void GetProperties_CanWriteValues()
	{
		var obj = new SimpleObject { Id = 1, Name = "Old" };
		var props = TypeDescriptor.GetProperties(obj).Typed().Select(p => (p, p.Name)).ToList();

		using var descriptor = new FlattenedTypeDescriptor(obj, props);
		var result = ((ICustomTypeDescriptor)descriptor).GetProperties();

		result["Id"].SetValue(obj, 100);
		result["Name"].SetValue(obj, "New");

		obj.Id.AssertEqual(100);
		obj.Name.AssertEqual("New");
	}

	[TestMethod]
	public void PropertyChanged_ForwardsEvent()
	{
		var obj = new NotifyObject();
		var props = TypeDescriptor.GetProperties(obj).Typed().Select(p => (p, p.Name)).ToList();

		using var descriptor = new FlattenedTypeDescriptor(obj, props);

		var raised = false;
		descriptor.PropertyChanged += (s, e) =>
		{
			raised = true;
			e.PropertyName.AssertEqual("Value");
		};

		obj.Value = 10;

		raised.AssertTrue();
	}

	[TestMethod]
	public void PropertyChanging_ForwardsEvent()
	{
		var obj = new NotifyObject();
		var props = TypeDescriptor.GetProperties(obj).Typed().Select(p => (p, p.Name)).ToList();

		using var descriptor = new FlattenedTypeDescriptor(obj, props);

		var raised = false;
		descriptor.PropertyChanging += (s, e) =>
		{
			raised = true;
			e.PropertyName.AssertEqual("Value");
		};

		obj.Value = 10;

		raised.AssertTrue();
	}

	[TestMethod]
	public void Dispose_UnsubscribesEvents()
	{
		var obj = new NotifyObject();
		var props = TypeDescriptor.GetProperties(obj).Typed().Select(p => (p, p.Name)).ToList();

		var descriptor = new FlattenedTypeDescriptor(obj, props);

		var raised = false;
		descriptor.PropertyChanged += (s, e) => raised = true;

		descriptor.Dispose();

		obj.Value = 10;

		raised.AssertFalse();
	}

	[TestMethod]
	public void GetClassName_ReturnsRootClassName()
	{
		var obj = new SimpleObject();
		var props = TypeDescriptor.GetProperties(obj).Typed().Select(p => (p, p.Name)).ToList();

		using var descriptor = new FlattenedTypeDescriptor(obj, props);

		((ICustomTypeDescriptor)descriptor).GetClassName().Contains(nameof(SimpleObject)).AssertTrue();
	}

	[TestMethod]
	public void GetPropertyOwner_ReturnsRoot()
	{
		var obj = new SimpleObject();
		var props = TypeDescriptor.GetProperties(obj).Typed().Select(p => (p, p.Name)).ToList();

		using var descriptor = new FlattenedTypeDescriptor(obj, props);
		var result = ((ICustomTypeDescriptor)descriptor).GetProperties();

		((ICustomTypeDescriptor)descriptor).GetPropertyOwner(result[0]).AssertSame(obj);
	}

	[TestMethod]
	public void NestedPath_GetValue_NavigatesCorrectly()
	{
		var obj = new NestedObject { Child = new SimpleObject { Id = 99 } };

		var childProp = TypeDescriptor.GetProperties(typeof(SimpleObject))["Id"];
		var props = new List<(PropertyDescriptor, string)> { (childProp, "Child.Id") };

		using var descriptor = new FlattenedTypeDescriptor(obj, props);
		var result = ((ICustomTypeDescriptor)descriptor).GetProperties();

		result["ChildId"].GetValue(obj).AssertEqual(99);
	}

	[TestMethod]
	public void NestedPath_NullChild_ReturnsNull()
	{
		var obj = new NestedObject { Child = null };

		var childProp = TypeDescriptor.GetProperties(typeof(SimpleObject))["Id"];
		var props = new List<(PropertyDescriptor, string)> { (childProp, "Child.Id") };

		using var descriptor = new FlattenedTypeDescriptor(obj, props);
		var result = ((ICustomTypeDescriptor)descriptor).GetProperties();

		result["ChildId"].GetValue(obj).AssertNull();
	}
}
