namespace Ecng.Tests.Reflection;

using System.Collections;
using System.Reflection;

using Ecng.Reflection;
using Ecng.Serialization;

[TestClass]
public class ReflectionTests : BaseTestClass
{
	[TestMethod]
	public void ItemType()
	{
		var arr = new[] { 1, 2, 3 };
		var list = new List<int> { 1, 2, 3 };
		var enu = (IEnumerable<int>)arr;
		arr.GetType().GetItemType().AssertSame(typeof(int));
		list.GetType().GetItemType().AssertSame(typeof(int));
		enu.GetType().GetItemType().AssertSame(typeof(int));
		typeof(IAsyncEnumerable<int>).GetItemType().AssertSame(typeof(int));
	}

	[TestMethod]
	public void ToStorage()
	{
		var type = typeof(Disposable);
		var prop = type.GetProperty(nameof(Disposable.IsDisposed));
		var method = type.GetMethod(nameof(Disposable.Dispose));

		void Do(bool isAssemblyQualifiedName)
		{
			type.ToStorage(isAssemblyQualifiedName).ToMember<Type>().AssertEqual(type);
			prop.ToStorage(isAssemblyQualifiedName).ToMember<PropertyInfo>().AssertEqual(prop);
			method.ToStorage(isAssemblyQualifiedName).ToMember<MethodInfo>().AssertEqual(method);

			type.ToStorage(isAssemblyQualifiedName).ToMember().AssertEqual(type);
			prop.ToStorage(isAssemblyQualifiedName).ToMember().AssertEqual(prop);
			method.ToStorage(isAssemblyQualifiedName).ToMember().AssertEqual(method);
		}

		Do(true);
		Do(false);
	}

	class Sample : IList<int>
	{
		public int Value { get; set; }
		public int this[int i] { get => i; set { } }
		public static int StaticValue { get; set; }
#pragma warning disable CS0067
		public event EventHandler TestEvent;
#pragma warning restore CS0067
#pragma warning disable CA1822 // Mark members as static
#pragma warning disable IDE0060 // Remove unused parameter
		public void MethodRef(ref int x) { }
		public void MethodOut(out int x) { x = 0; }
		public void MethodIn(in int x) { }
		public void MethodParams(params int[] items) { }
		public void Method(int x) { }
		public void Target_get_NotAccessor() { }
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore CA1822 // Mark members as static

		// IList implementation
		public int Count => 0;
		public bool IsReadOnly => false;
		public void Add(int item) { }
		public void Clear() { }
		public bool Contains(int item) => false;
		public void CopyTo(int[] array, int arrayIndex) { }
		public IEnumerator<int> GetEnumerator() => Enumerable.Empty<int>().GetEnumerator();
		public int IndexOf(int item) => -1;
		public void Insert(int index, int item) { }
		public bool Remove(int item) => false;
		public void RemoveAt(int index) { }
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	[TestMethod]
	public void GetInvokeMethod()
	{
		typeof(Action).GetInvokeMethod().Name.AssertEqual("Invoke");
	}

	[TestMethod]
	public void IsParams()
	{
		var param = typeof(Sample).GetMethod(nameof(Sample.MethodParams)).GetParameters()[0];
		param.IsParams().AssertTrue();

		var param2 = typeof(Sample).GetMethod(nameof(Sample.Method)).GetParameters()[0];
		param2.IsParams().AssertFalse();
	}

	[TestMethod]
	public void GetParameterTypes()
	{
		var method = typeof(Sample).GetMethod(nameof(Sample.MethodRef));
		var types = method.GetParameterTypes();
		types.Length.AssertEqual(1);
		types[0].info.Name.AssertEqual("x");
		types[0].type.AssertEqual(typeof(int).MakeByRefType());

		var types2 = method.GetParameterTypes(true);
		types2[0].type.AssertEqual(typeof(int));
	}

	[TestMethod]
	public void GetGenericTypeAndArg()
	{
		var listType = typeof(List<string>);
		var genType = listType.GetGenericType(typeof(List<>));
		genType.AssertEqual(typeof(List<string>));
		listType.GetGenericTypeArg(typeof(List<>), 0).AssertEqual(typeof(string));
	}

	[TestMethod]
	public void GetIndexer()
	{
		var prop = typeof(Sample).GetIndexer(typeof(int));
		prop.Name.AssertEqual("Item");
	}

	[TestMethod]
	public void GetIndexers()
	{
		var props = typeof(Sample).GetIndexers(typeof(int));
		props.Length.AssertGreater(0);
		props[0].IsIndexer().AssertTrue();
	}

	[TestMethod]
	public void GetMemberAndGetMembers()
	{
		typeof(Sample).GetMember<ConstructorInfo>().MemberType.AssertEqual(MemberTypes.Constructor);
		typeof(Sample).GetMember<PropertyInfo>(nameof(Sample.Value)).Name.AssertEqual("Value");
		typeof(Sample).GetMembers<PropertyInfo>().Any(p => p.Name == "Value").AssertTrue();
	}

	[TestMethod]
	public void FilterMembers()
	{
		var props = typeof(Sample).GetMembers<PropertyInfo>();
		var filtered = ReflectionHelper.FilterMembers(props, null, typeof(int));
		filtered.Any(p => p.Name == "Value").AssertTrue();
	}

	[TestMethod]
	public void IsAbstractIsVirtualIsOverloadable()
	{
		var prop = typeof(Sample).GetProperty(nameof(Sample.Value));
		prop.IsAbstract().AssertFalse();
		prop.IsVirtual().AssertFalse();
		prop.IsOverloadable().AssertFalse();
	}

	[TestMethod]
	public void IsIndexer()
	{
		var prop = typeof(Sample).GetProperty("Item");
		prop.IsIndexer().AssertTrue();
		ReflectionHelper.IsIndexer(prop).AssertTrue();
	}

	[TestMethod]
	public void GetIndexerTypes()
	{
		var prop = typeof(Sample).GetProperty("Item");
		var idxTypes = prop.GetIndexerTypes();
		idxTypes.First().AssertEqual(typeof(int));
	}

	[TestMethod]
	public void MemberIs()
	{
		var prop = typeof(Sample).GetProperty(nameof(Sample.Value));
		prop.MemberIs(MemberTypes.Property).AssertTrue();
	}

	[TestMethod]
	public void IsOutput()
	{
		var outParam = typeof(Sample).GetMethod(nameof(Sample.MethodOut)).GetParameters()[0];
		outParam.IsOutput().AssertTrue();

		var refParam = typeof(Sample).GetMethod(nameof(Sample.MethodRef)).GetParameters()[0];
		refParam.IsOutput().AssertTrue();

		var inParam = typeof(Sample).GetMethod(nameof(Sample.MethodIn)).GetParameters()[0];
		inParam.IsOutput().AssertFalse();

		var valueParam = typeof(Sample).GetMethod(nameof(Sample.Method)).GetParameters().FirstOrDefault();
		valueParam.IsOutput().AssertFalse();
	}

	[TestMethod]
	public void GetMemberType()
	{
		var prop = typeof(Sample).GetProperty(nameof(Sample.Value));
		prop.GetMemberType().AssertEqual(typeof(int));
	}

	[TestMethod]
	public void IsCollection()
	{
		typeof(List<int>).IsCollection().AssertTrue();
		typeof(int[]).IsCollection().AssertTrue();
		typeof(int).IsCollection().AssertFalse();
	}

	[TestMethod]
	public void IsStatic()
	{
		typeof(Sample).GetProperty(nameof(Sample.StaticValue)).IsStatic().AssertTrue();
		typeof(Sample).GetProperty(nameof(Sample.Value)).IsStatic().AssertFalse();
	}

	[TestMethod]
	public void GetItemType()
	{
		var arr = new[] { 1, 2, 3 };
		arr.GetType().GetItemType().AssertEqual(typeof(int));
	}

	[TestMethod]
	public void MakePropertyName()
	{
		"get_X".MakePropertyName().AssertEqual("X");
		"set_X".MakePropertyName().AssertEqual("X");
		"add_X".MakePropertyName().AssertEqual("X");
		"remove_X".MakePropertyName().AssertEqual("X");
		"get_Target_get_NotAccessor".MakePropertyName().AssertEqual("Target_get_NotAccessor");
	}

	[TestMethod]
	public void GetAccessorOwner()
	{
		var prop = typeof(Sample).GetProperty(nameof(Sample.Value));
		var method = prop.GetGetMethod();
		method.GetAccessorOwner().AssertEqual(prop);

		var fakeAccessor = typeof(Sample).GetMethod(nameof(Sample.Target_get_NotAccessor));
		fakeAccessor.GetAccessorOwner().AssertNull();
	}

	[TestMethod]
	public void Make()
	{
		var method = typeof(List<>).GetMethod("Add");
		method.IsGenericMethodDefinition.AssertFalse();
	}

	[TestMethod]
	public void IsRuntimeType()
	{
		// True only for System.RuntimeType (i.e. typeof(string).GetType())
		var runtimeType = typeof(string).GetType();
		runtimeType.IsRuntimeType().AssertTrue();

		// False for most ordinary types
		typeof(string).IsRuntimeType().AssertFalse();
		typeof(Type).IsRuntimeType().AssertFalse();
	}

	[TestMethod]
	public void IsAssemblyVerifyAssembly()
	{
		"Ecng.Common.dll".IsAssembly().AssertTrue();

		var nativeDll = "runtimes/win-x64/native/WmiLight.Native.dll";
		nativeDll.IsAssembly().AssertFalse();
		nativeDll.VerifyAssembly().AssertNull();
	}

	[TestMethod]
	public void ClearCache()
	{
		ReflectionHelper.ClearCache();
	}

	[TestMethod]
	public void FindImplementationsIsRequiredTypeTryFindType()
	{
		var list = new[] { typeof(List<int>), typeof(string) };
		var found = list.TryFindType(t => t == typeof(List<int>), "List`1");
		found.AssertEqual(typeof(List<int>));

		typeof(List<int>).IsRequiredType<List<int>>().AssertTrue();
	}

	[TestMethod]
	public void OrderByDeclaration()
	{
		var props = typeof(Sample).GetProperties();
		props.OrderByDeclaration().Count().AssertEqual(props.Length);
	}

	[TestMethod]
	public void IsModifiable()
	{
		var prop = typeof(Sample).GetProperty(nameof(Sample.Value));
		prop.IsModifiable().AssertTrue();
	}

	[TestMethod]
	public void IsMatch()
	{
		var prop = typeof(Sample).GetProperty(nameof(Sample.Value));
		prop.IsMatch(BindingFlags.Public | BindingFlags.Instance).AssertTrue();

		var method = typeof(Sample).GetMethod(nameof(Sample.Method));
		method.IsMatch(BindingFlags.Public | BindingFlags.Instance).AssertTrue();

		var field = typeof(DateTime).GetField("MinValue", BindingFlags.Public | BindingFlags.Static);
		field.IsMatch(BindingFlags.Public | BindingFlags.Static).AssertTrue();
	}

	#region Test Classes

	private class TestClass
	{
		public int PublicField;
#pragma warning disable CS0169
		private int _privateField;
#pragma warning restore CS0169
#pragma warning disable CS0649
		public static int StaticField;
#pragma warning restore CS0649

		public int PublicProperty { get; set; }
		private int PrivateProperty { get; set; }
		public static int StaticProperty { get; set; }
		public int ReadOnlyProperty { get; }
		public int WriteOnlyProperty { set { } }

		public string this[int index] => index.ToString();
		public string this[int x, int y] => $"{x},{y}";

#pragma warning disable CS0067
		public event EventHandler PublicEvent;
		public static event EventHandler StaticEvent;
#pragma warning restore CS0067

		public TestClass() { }
		public TestClass(int value) { PublicField = value; }
		private TestClass(string value) { }

		public void PublicMethod() { }
		public void PublicMethod(int x) { }
		public void PublicMethod(int x, int y) { }
		private void PrivateMethod() { }
		public static void StaticMethod() { }

		public virtual void VirtualMethod() { }
		public int MethodWithReturn() => 42;

		public void MethodWithRef(ref int x) { x = 1; }
		public void MethodWithOut(out int x) { x = 1; }
		public void MethodWithIn(in int x) { }
		public void MethodWithParams(params object[] args) { }
	}

	private abstract class AbstractClass
	{
		public abstract void AbstractMethod();
		public abstract int AbstractProperty { get; set; }
	}

	private class DerivedClass : AbstractClass
	{
		public override void AbstractMethod() { }
		public override int AbstractProperty { get; set; }
	}

	private interface ITestInterface
	{
		void InterfaceMethod();
		int InterfaceProperty { get; set; }
	}

	private class InterfaceImplementor : ITestInterface
	{
		public void InterfaceMethod() { }
		public int InterfaceProperty { get; set; }
	}

	private struct TestStruct
	{
		public int Value { get; set; }
	}

	private class GenericClass<T>
	{
		public T Value { get; set; }
		public void GenericMethod<U>(U arg) { }
	}

	private class MultipleIndexers
	{
		public int this[int i] => i;
		public string this[string s] => s;
	}

	#endregion

	#region GetInvokeMethod Tests

	[TestMethod]
	public void GetInvokeMethod_ActionWithParams()
	{
		var invoke = typeof(Action<int, string>).GetInvokeMethod();
		invoke.Name.AssertEqual("Invoke");
		invoke.GetParameters().Length.AssertEqual(2);
	}

	[TestMethod]
	public void GetInvokeMethod_Func()
	{
		var invoke = typeof(Func<int, string>).GetInvokeMethod();
		invoke.ReturnType.AssertEqual(typeof(string));
	}

	#endregion

	#region IsParams Tests

	[TestMethod]
	public void IsParams_ParamsParameter()
	{
		var method = typeof(TestClass).GetMethod(nameof(TestClass.MethodWithParams));
		method.GetParameters()[0].IsParams().AssertTrue();
	}

	[TestMethod]
	public void IsParams_RegularParameter()
	{
		var method = typeof(TestClass).GetMethod(nameof(TestClass.PublicMethod), [typeof(int)]);
		method.GetParameters()[0].IsParams().AssertFalse();
	}

	#endregion

	#region GetParameterTypes Tests

	[TestMethod]
	public void GetParameterTypes_MultipleParams()
	{
		var method = typeof(TestClass).GetMethod(nameof(TestClass.PublicMethod), [typeof(int), typeof(int)]);
		var types = method.GetParameterTypes();
		types.Length.AssertEqual(2);
	}

	[TestMethod]
	public void GetParameterTypes_RefParam_WithRemoveRef()
	{
		var method = typeof(TestClass).GetMethod(nameof(TestClass.MethodWithRef));
		var types = method.GetParameterTypes(true);
		types[0].type.AssertEqual(typeof(int));
	}

	[TestMethod]
	public void GetParameterTypes_OutParam_WithRemoveRef()
	{
		var method = typeof(TestClass).GetMethod(nameof(TestClass.MethodWithOut));
		var types = method.GetParameterTypes(true);
		types[0].type.AssertEqual(typeof(int));
	}

	[TestMethod]
	public void GetParameterTypes_InParam_WithRemoveRef()
	{
		var method = typeof(TestClass).GetMethod(nameof(TestClass.MethodWithIn));
		// in parameters are ByRef but IsOutput is false
		var types = method.GetParameterTypes(true);
		// in params should not be "removed" because IsOutput is false
		types[0].type.IsByRef.AssertTrue();
	}

	[TestMethod]
	public void GetParameterTypes_NullMethod_Throws()
	{
		ThrowsExactly<ArgumentNullException>(() => ((MethodBase)null).GetParameterTypes());
	}

	#endregion

	#region GetGenericType Tests

	[TestMethod]
	public void GetGenericType_DirectMatch()
	{
		var result = typeof(List<int>).GetGenericType(typeof(List<>));
		result.AssertEqual(typeof(List<int>));
	}

	[TestMethod]
	public void GetGenericType_Interface()
	{
		var result = typeof(List<int>).GetGenericType(typeof(IList<>));
		result.AssertEqual(typeof(IList<int>));
	}

	[TestMethod]
	public void GetGenericType_NotFound()
	{
		var result = typeof(string).GetGenericType(typeof(List<>));
		result.AssertNull();
	}

	[TestMethod]
	public void GetGenericType_BaseClass()
	{
		var result = typeof(DerivedClass).GetGenericType(typeof(List<>));
		result.AssertNull();
	}

	[TestMethod]
	public void GetGenericType_NotGenericDefinition_Throws()
	{
		ThrowsExactly<ArgumentException>(() => typeof(List<int>).GetGenericType(typeof(List<int>)));
	}

	#endregion

	#region GetGenericTypeArg Tests

	[TestMethod]
	public void GetGenericTypeArg_ValidIndex()
	{
		var arg = typeof(Dictionary<string, int>).GetGenericTypeArg(typeof(Dictionary<,>), 0);
		arg.AssertEqual(typeof(string));

		var arg2 = typeof(Dictionary<string, int>).GetGenericTypeArg(typeof(Dictionary<,>), 1);
		arg2.AssertEqual(typeof(int));
	}

	[TestMethod]
	public void GetGenericTypeArg_NoMatch_Throws()
	{
		ThrowsExactly<ArgumentException>(() => typeof(string).GetGenericTypeArg(typeof(List<>), 0));
	}

	#endregion

	#region GetIndexer/GetIndexers Tests

	[TestMethod]
	public void GetIndexer_SingleParameter()
	{
		var indexer = typeof(TestClass).GetIndexer(typeof(int));
		indexer.AssertNotNull();
		indexer.GetIndexParameters().Length.AssertEqual(1);
	}

	[TestMethod]
	public void GetIndexer_MultipleParameters()
	{
		var indexer = typeof(TestClass).GetIndexer(typeof(int), typeof(int));
		indexer.AssertNotNull();
		indexer.GetIndexParameters().Length.AssertEqual(2);
	}

	[TestMethod]
	public void GetIndexers_ReturnsAll()
	{
		var indexers = typeof(MultipleIndexers).GetIndexers();
		indexers.Length.AssertEqual(2);
	}

	#endregion

	#region GetMember Tests

	[TestMethod]
	public void GetMember_Constructor_NoParams()
	{
		var ctor = typeof(TestClass).GetMember<ConstructorInfo>();
		ctor.AssertNotNull();
	}

	[TestMethod]
	public void GetMember_Constructor_WithParams()
	{
		var ctor = typeof(TestClass).GetMember<ConstructorInfo>(typeof(int));
		ctor.AssertNotNull();
		ctor.GetParameters().Length.AssertEqual(1);
	}

	[TestMethod]
	public void GetMember_Property()
	{
		var prop = typeof(TestClass).GetMember<PropertyInfo>(nameof(TestClass.PublicProperty));
		prop.AssertNotNull();
	}

	[TestMethod]
	public void GetMember_Method_Overloaded()
	{
		var method = typeof(TestClass).GetMember<MethodInfo>(nameof(TestClass.PublicMethod), typeof(int));
		method.AssertNotNull();
		method.GetParameters().Length.AssertEqual(1);
	}

	[TestMethod]
	public void GetMember_Field()
	{
		var field = typeof(TestClass).GetMember<FieldInfo>(nameof(TestClass.PublicField));
		field.AssertNotNull();
	}

	[TestMethod]
	public void GetMember_Event()
	{
		var evt = typeof(TestClass).GetMember<EventInfo>(nameof(TestClass.PublicEvent));
		evt.AssertNotNull();
	}

	[TestMethod]
	public void GetMember_NotFound_Throws()
	{
		ThrowsExactly<ArgumentException>(() => typeof(TestClass).GetMember<PropertyInfo>("NonExistent"));
	}

	#endregion

	#region GetMembers Tests

	[TestMethod]
	public void GetMembers_AllProperties()
	{
		var props = typeof(TestClass).GetMembers<PropertyInfo>();
		props.Any(p => p.Name == nameof(TestClass.PublicProperty)).AssertTrue();
	}

	[TestMethod]
	public void GetMembers_StaticOnly()
	{
		var members = typeof(TestClass).GetMembers<PropertyInfo>(ReflectionHelper.AllStaticMembers);
		members.Any(p => p.Name == nameof(TestClass.StaticProperty)).AssertTrue();
		members.Any(p => p.Name == nameof(TestClass.PublicProperty)).AssertFalse();
	}

	[TestMethod]
	public void GetMembers_InstanceOnly()
	{
		var members = typeof(TestClass).GetMembers<PropertyInfo>(ReflectionHelper.AllInstanceMembers);
		members.Any(p => p.Name == nameof(TestClass.PublicProperty)).AssertTrue();
	}

	[TestMethod]
	public void GetMembers_WithInheritance()
	{
		var members = typeof(DerivedClass).GetMembers<MethodInfo>(ReflectionHelper.AllInstanceMembers, true);
		members.Any(m => m.Name == nameof(DerivedClass.AbstractMethod)).AssertTrue();
	}

	#endregion

	#region IsAbstract Tests

	[TestMethod]
	public void IsAbstract_AbstractMethod()
	{
		var method = typeof(AbstractClass).GetMethod(nameof(AbstractClass.AbstractMethod));
		method.IsAbstract().AssertTrue();
	}

	[TestMethod]
	public void IsAbstract_AbstractProperty()
	{
		var prop = typeof(AbstractClass).GetProperty(nameof(AbstractClass.AbstractProperty));
		prop.IsAbstract().AssertTrue();
	}

	[TestMethod]
	public void IsAbstract_ConcreteMethod()
	{
		var method = typeof(TestClass).GetMethod(nameof(TestClass.PublicMethod), Type.EmptyTypes);
		method.IsAbstract().AssertFalse();
	}

	[TestMethod]
	public void IsAbstract_AbstractType()
	{
		typeof(AbstractClass).IsAbstract().AssertTrue();
	}

	#endregion

	#region IsVirtual Tests

	[TestMethod]
	public void IsVirtual_VirtualMethod()
	{
		var method = typeof(TestClass).GetMethod(nameof(TestClass.VirtualMethod));
		method.IsVirtual().AssertTrue();
	}

	[TestMethod]
	public void IsVirtual_NonVirtualMethod()
	{
		var method = typeof(TestClass).GetMethod(nameof(TestClass.PublicMethod), Type.EmptyTypes);
		method.IsVirtual().AssertFalse();
	}

	[TestMethod]
	public void IsVirtual_Property()
	{
		var prop = typeof(TestClass).GetProperty(nameof(TestClass.PublicProperty));
		prop.IsVirtual().AssertFalse();
	}

	#endregion

	#region IsOverloadable Tests

	[TestMethod]
	public void IsOverloadable_Constructor()
	{
		var ctor = typeof(TestClass).GetConstructor(Type.EmptyTypes);
		ctor.IsOverloadable().AssertTrue();
	}

	[TestMethod]
	public void IsOverloadable_VirtualMethod()
	{
		var method = typeof(TestClass).GetMethod(nameof(TestClass.VirtualMethod));
		method.IsOverloadable().AssertTrue();
	}

	[TestMethod]
	public void IsOverloadable_NonVirtualMethod()
	{
		var method = typeof(TestClass).GetMethod(nameof(TestClass.PublicMethod), Type.EmptyTypes);
		method.IsOverloadable().AssertFalse();
	}

	#endregion

	#region IsIndexer Tests

	[TestMethod]
	public void IsIndexer_IndexerProperty()
	{
		var prop = typeof(TestClass).GetProperty("Item", [typeof(int)]);
		prop.IsIndexer().AssertTrue();
	}

	[TestMethod]
	public void IsIndexer_RegularProperty()
	{
		var prop = typeof(TestClass).GetProperty(nameof(TestClass.PublicProperty));
		prop.IsIndexer().AssertFalse();
	}

	[TestMethod]
	public void IsIndexer_MemberInfo_NotProperty()
	{
		var field = typeof(TestClass).GetField(nameof(TestClass.PublicField));
		((MemberInfo)field).IsIndexer().AssertFalse();
	}

	#endregion

	#region GetIndexerTypes Tests

	[TestMethod]
	public void GetIndexerTypes_SingleParam()
	{
		var prop = typeof(TestClass).GetProperty("Item", [typeof(int)]);
		var types = prop.GetIndexerTypes().ToArray();
		types.Length.AssertEqual(1);
		types[0].AssertEqual(typeof(int));
	}

	[TestMethod]
	public void GetIndexerTypes_MultipleParams()
	{
		var prop = typeof(TestClass).GetProperty("Item", [typeof(int), typeof(int)]);
		var types = prop.GetIndexerTypes().ToArray();
		types.Length.AssertEqual(2);
	}

	#endregion

	#region MemberIs Tests

	[TestMethod]
	public void MemberIs_Property()
	{
		var prop = typeof(TestClass).GetProperty(nameof(TestClass.PublicProperty));
		prop.MemberIs(MemberTypes.Property).AssertTrue();
		prop.MemberIs(MemberTypes.Field).AssertFalse();
	}

	[TestMethod]
	public void MemberIs_MultipleTypes()
	{
		var prop = typeof(TestClass).GetProperty(nameof(TestClass.PublicProperty));
		prop.MemberIs(MemberTypes.Property, MemberTypes.Field).AssertTrue();
	}

	#endregion

	#region IsOutput Tests

	[TestMethod]
	public void IsOutput_OutParam()
	{
		var param = typeof(TestClass).GetMethod(nameof(TestClass.MethodWithOut)).GetParameters()[0];
		param.IsOutput().AssertTrue();
	}

	[TestMethod]
	public void IsOutput_RefParam()
	{
		var param = typeof(TestClass).GetMethod(nameof(TestClass.MethodWithRef)).GetParameters()[0];
		param.IsOutput().AssertTrue();
	}

	[TestMethod]
	public void IsOutput_InParam()
	{
		var param = typeof(TestClass).GetMethod(nameof(TestClass.MethodWithIn)).GetParameters()[0];
		param.IsOutput().AssertFalse();
	}

	#endregion

	#region GetMemberType Tests

	[TestMethod]
	public void GetMemberType_Property()
	{
		var prop = typeof(TestClass).GetProperty(nameof(TestClass.PublicProperty));
		prop.GetMemberType().AssertEqual(typeof(int));
	}

	[TestMethod]
	public void GetMemberType_Field()
	{
		var field = typeof(TestClass).GetField(nameof(TestClass.PublicField));
		field.GetMemberType().AssertEqual(typeof(int));
	}

	[TestMethod]
	public void GetMemberType_Method()
	{
		var method = typeof(TestClass).GetMethod(nameof(TestClass.MethodWithReturn));
		method.GetMemberType().AssertEqual(typeof(int));
	}

	[TestMethod]
	public void GetMemberType_Event()
	{
		var evt = typeof(TestClass).GetEvent(nameof(TestClass.PublicEvent));
		evt.GetMemberType().AssertEqual(typeof(EventHandler));
	}

	[TestMethod]
	public void GetMemberType_Constructor()
	{
		var ctor = typeof(TestClass).GetConstructor(Type.EmptyTypes);
		ctor.GetMemberType().AssertEqual(typeof(TestClass));
	}

	#endregion

	#region IsCollection Tests

	[TestMethod]
	public void IsCollection_List()
	{
		typeof(List<int>).IsCollection().AssertTrue();
	}

	[TestMethod]
	public void IsCollection_Array()
	{
		typeof(int[]).IsCollection().AssertTrue();
	}

	[TestMethod]
	public void IsCollection_IEnumerable()
	{
		typeof(IEnumerable<int>).IsCollection().AssertTrue();
	}

	[TestMethod]
	public void IsCollection_ICollection()
	{
		typeof(ICollection<int>).IsCollection().AssertTrue();
	}

	[TestMethod]
	public void IsCollection_NonGenericIEnumerable()
	{
		typeof(IEnumerable).IsCollection().AssertTrue();
	}

	[TestMethod]
	public void IsCollection_NonCollection()
	{
		typeof(int).IsCollection().AssertFalse();
		typeof(string).IsCollection().AssertTrue(); // string implements IEnumerable<char>
	}

	#endregion

	#region IsStatic Tests

	[TestMethod]
	public void IsStatic_StaticField()
	{
		var field = typeof(TestClass).GetField(nameof(TestClass.StaticField));
		field.IsStatic().AssertTrue();
	}

	[TestMethod]
	public void IsStatic_InstanceField()
	{
		var field = typeof(TestClass).GetField(nameof(TestClass.PublicField));
		field.IsStatic().AssertFalse();
	}

	[TestMethod]
	public void IsStatic_StaticProperty()
	{
		var prop = typeof(TestClass).GetProperty(nameof(TestClass.StaticProperty));
		prop.IsStatic().AssertTrue();
	}

	[TestMethod]
	public void IsStatic_StaticMethod()
	{
		var method = typeof(TestClass).GetMethod(nameof(TestClass.StaticMethod));
		method.IsStatic().AssertTrue();
	}

	[TestMethod]
	public void IsStatic_StaticEvent()
	{
		var evt = typeof(TestClass).GetEvent(nameof(TestClass.StaticEvent));
		evt.IsStatic().AssertTrue();
	}

	[TestMethod]
	public void IsStatic_StaticClass()
	{
		typeof(ReflectionHelper).IsStatic().AssertTrue();
	}

	#endregion

	#region GetItemType Tests

	[TestMethod]
	public void GetItemType_List()
	{
		typeof(List<string>).GetItemType().AssertEqual(typeof(string));
	}

	[TestMethod]
	public void GetItemType_Array()
	{
		typeof(double[]).GetItemType().AssertEqual(typeof(double));
	}

	[TestMethod]
	public void GetItemType_IEnumerable()
	{
		typeof(IEnumerable<DateTime>).GetItemType().AssertEqual(typeof(DateTime));
	}

	[TestMethod]
	public void GetItemType_NonCollection_Throws()
	{
		ThrowsExactly<InvalidOperationException>(() => typeof(int).GetItemType());
	}

	#endregion

	#region MakePropertyName Tests

	[TestMethod]
	public void MakePropertyName_Getter()
	{
		"get_PropertyName".MakePropertyName().AssertEqual("PropertyName");
	}

	[TestMethod]
	public void MakePropertyName_Setter()
	{
		"set_PropertyName".MakePropertyName().AssertEqual("PropertyName");
	}

	[TestMethod]
	public void MakePropertyName_EventAdd()
	{
		"add_EventName".MakePropertyName().AssertEqual("EventName");
	}

	[TestMethod]
	public void MakePropertyName_EventRemove()
	{
		"remove_EventName".MakePropertyName().AssertEqual("EventName");
	}

	[TestMethod]
	public void MakePropertyName_RegularName()
	{
		"RegularMethod".MakePropertyName().AssertEqual("RegularMethod");
	}

	[TestMethod]
	public void MakePropertyName_Empty_Throws()
	{
		ThrowsExactly<ArgumentNullException>(() => "".MakePropertyName());
	}

	#endregion

	#region GetAccessorOwner Tests

	[TestMethod]
	public void GetAccessorOwner_PropertyGetter()
	{
		var prop = typeof(TestClass).GetProperty(nameof(TestClass.PublicProperty));
		var getter = prop.GetGetMethod();
		getter.GetAccessorOwner().AssertEqual(prop);
	}

	[TestMethod]
	public void GetAccessorOwner_PropertySetter()
	{
		var prop = typeof(TestClass).GetProperty(nameof(TestClass.PublicProperty));
		var setter = prop.GetSetMethod();
		setter.GetAccessorOwner().AssertEqual(prop);
	}

	[TestMethod]
	public void GetAccessorOwner_EventAdd()
	{
		var evt = typeof(TestClass).GetEvent(nameof(TestClass.PublicEvent));
		var add = evt.GetAddMethod();
		add.GetAccessorOwner().AssertEqual(evt);
	}

	[TestMethod]
	public void GetAccessorOwner_RegularMethod()
	{
		var method = typeof(TestClass).GetMethod(nameof(TestClass.PublicMethod), Type.EmptyTypes);
		method.GetAccessorOwner().AssertNull();
	}

	#endregion

	#region Make Tests

	[TestMethod]
	public void Make_GenericMethod()
	{
		var genericMethod = typeof(GenericClass<int>).GetMethod(nameof(GenericClass<int>.GenericMethod));
		var madeMethod = genericMethod.Make(typeof(string));
		madeMethod.IsGenericMethod.AssertTrue();
		madeMethod.GetGenericArguments()[0].AssertEqual(typeof(string));
	}

	#endregion

	#region IsRuntimeType Tests

	[TestMethod]
	public void IsRuntimeType_RuntimeType()
	{
		var runtimeType = typeof(int).GetType();
		runtimeType.IsRuntimeType().AssertTrue();
	}

	[TestMethod]
	public void IsRuntimeType_RegularType()
	{
		typeof(int).IsRuntimeType().AssertFalse();
	}

	#endregion

	#region CacheEnabled and ClearCache Tests

	[TestMethod]
	public void CacheEnabled_CanBeDisabled()
	{
		var originalValue = ReflectionHelper.CacheEnabled;
		try
		{
			ReflectionHelper.CacheEnabled = false;
			ReflectionHelper.CacheEnabled.AssertFalse();

			// Still should work even with cache disabled
			typeof(List<int>).IsCollection().AssertTrue();
		}
		finally
		{
			ReflectionHelper.CacheEnabled = originalValue;
		}
	}

	[TestMethod]
	public void ClearCache_DoesNotThrow()
	{
		// Warm up cache
		typeof(List<int>).IsCollection();
		typeof(TestClass).GetProperty(nameof(TestClass.PublicProperty)).IsStatic();

		// Clear should not throw
		ReflectionHelper.ClearCache();
	}

	#endregion

	#region FindImplementations Tests

	[TestMethod]
	public void FindImplementations_FindsTypes()
	{
		var assembly = typeof(ReflectionTests).Assembly;
		var implementations = assembly.FindImplementations<IDisposable>(showNonPublic: true);
		// Should find some disposable types
		implementations.AssertNotNull();
	}

	#endregion

	#region IsRequiredType Tests

	[TestMethod]
	public void IsRequiredType_ValidType()
	{
		typeof(List<int>).IsRequiredType<IList<int>>().AssertTrue();
	}

	[TestMethod]
	public void IsRequiredType_AbstractType()
	{
		typeof(AbstractClass).IsRequiredType<AbstractClass>().AssertFalse();
	}

	[TestMethod]
	public void IsRequiredType_GenericDefinition()
	{
		typeof(List<>).IsRequiredType<IList<int>>().AssertFalse();
	}

	#endregion

	#region TryFindType Tests

	[TestMethod]
	public void TryFindType_ByName()
	{
		var types = new[] { typeof(string), typeof(int), typeof(List<int>) };
		var found = types.TryFindType(null, "String");
		found.AssertEqual(typeof(string));
	}

	[TestMethod]
	public void TryFindType_ByPredicate()
	{
		var types = new[] { typeof(string), typeof(int), typeof(List<int>) };
		var found = types.TryFindType(t => t == typeof(int), null);
		found.AssertEqual(typeof(int));
	}

	[TestMethod]
	public void TryFindType_NotFound()
	{
		var types = new[] { typeof(string), typeof(int) };
		var found = types.TryFindType(null, "NonExistent");
		found.AssertNull();
	}

	#endregion

	#region OrderByDeclaration Tests

	[TestMethod]
	public void OrderByDeclaration_OrdersByMetadataToken()
	{
		var members = typeof(TestClass).GetMembers();
		var ordered = members.OrderByDeclaration().ToArray();
		ordered.Length.AssertEqual(members.Length);

		// Verify order is consistent
		for (var i = 1; i < ordered.Length; i++)
		{
			(ordered[i].MetadataToken >= ordered[i - 1].MetadataToken).AssertTrue();
		}
	}

	#endregion

	#region IsModifiable Tests

	[TestMethod]
	public void IsModifiable_WritableProperty()
	{
		var prop = typeof(TestClass).GetProperty(nameof(TestClass.PublicProperty));
		prop.IsModifiable().AssertTrue();
	}

	[TestMethod]
	public void IsModifiable_ReadOnlyProperty()
	{
		var prop = typeof(TestClass).GetProperty(nameof(TestClass.ReadOnlyProperty));
		prop.IsModifiable().AssertFalse();
	}

	#endregion

	#region IsMatch Tests

	[TestMethod]
	public void IsMatch_Property_PublicInstance()
	{
		var prop = typeof(TestClass).GetProperty(nameof(TestClass.PublicProperty));
		prop.IsMatch(BindingFlags.Public | BindingFlags.Instance).AssertTrue();
		prop.IsMatch(BindingFlags.NonPublic | BindingFlags.Instance).AssertFalse();
		prop.IsMatch(BindingFlags.Public | BindingFlags.Static).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_Property_StaticPublic()
	{
		var prop = typeof(TestClass).GetProperty(nameof(TestClass.StaticProperty));
		prop.IsMatch(BindingFlags.Public | BindingFlags.Static).AssertTrue();
		prop.IsMatch(BindingFlags.Public | BindingFlags.Instance).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_Method_PublicInstance()
	{
		var method = typeof(TestClass).GetMethod(nameof(TestClass.PublicMethod), Type.EmptyTypes);
		method.IsMatch(BindingFlags.Public | BindingFlags.Instance).AssertTrue();
	}

	[TestMethod]
	public void IsMatch_Method_StaticPublic()
	{
		var method = typeof(TestClass).GetMethod(nameof(TestClass.StaticMethod));
		method.IsMatch(BindingFlags.Public | BindingFlags.Static).AssertTrue();
	}

	[TestMethod]
	public void IsMatch_Field_PublicInstance()
	{
		var field = typeof(TestClass).GetField(nameof(TestClass.PublicField));
		field.IsMatch(BindingFlags.Public | BindingFlags.Instance).AssertTrue();
	}

	[TestMethod]
	public void IsMatch_Field_StaticPublic()
	{
		var field = typeof(TestClass).GetField(nameof(TestClass.StaticField));
		field.IsMatch(BindingFlags.Public | BindingFlags.Static).AssertTrue();
	}

	#endregion

	#region Struct Default Constructor Tests

	[TestMethod]
	public void GetMember_Struct_DefaultConstructor()
	{
		// Structs have implicit default constructor
		var ctor = typeof(TestStruct).GetMember<ConstructorInfo>();
		ctor.AssertNotNull();
	}

	#endregion

	#region ProxyTypes Tests

	[TestMethod]
	public void ProxyTypes_CanAddAndRemove()
	{
		var originalCount = ReflectionHelper.ProxyTypes.Count;

		ReflectionHelper.ProxyTypes[typeof(TestClass)] = typeof(DerivedClass);
		ReflectionHelper.ProxyTypes.ContainsKey(typeof(TestClass)).AssertTrue();

		ReflectionHelper.ProxyTypes.Remove(typeof(TestClass));
		ReflectionHelper.ProxyTypes.Count.AssertEqual(originalCount);
	}

	#endregion

	#region Test Classes for Bug Detection

	private class WriteOnlyIndexer
	{
		public int this[int index] { set { } }
	}

	private class ReadWriteIndexer
	{
		private int _value;
		public int this[int index]
		{
			get => _value;
			set => _value = value;
		}
	}

	#endregion

	#region GetIndexerTypes Bug Test

	[TestMethod]
	public void GetIndexerTypes_WriteOnlyIndexer_ReturnsOnlyIndexParams()
	{
		// For a write-only indexer this[int index] { set; }
		// The setter has parameters (int index, int value)
		// GetIndexerTypes should return only [int] (the index parameter), not [int, int]
		var prop = typeof(WriteOnlyIndexer).GetProperty("Item");
		var types = prop.GetIndexerTypes().ToArray();

		// This test checks if the implementation handles write-only indexers correctly
		// Expected: only the index parameter type (int), not including the value parameter
		types.Length.AssertEqual(1);
		types[0].AssertEqual(typeof(int));
	}

	[TestMethod]
	public void GetIndexerTypes_ReadWriteIndexer_UsesGetter()
	{
		// For read-write indexer, getter should be preferred
		var prop = typeof(ReadWriteIndexer).GetProperty("Item");
		var types = prop.GetIndexerTypes().ToArray();

		types.Length.AssertEqual(1);
		types[0].AssertEqual(typeof(int));
	}

	#endregion
}