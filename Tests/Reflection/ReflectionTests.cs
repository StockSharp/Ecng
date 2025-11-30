namespace Ecng.Tests.Reflection;

using System.Collections;
using System.Reflection;

using Ecng.Reflection;
using Ecng.Serialization;

[TestClass]
public class ReflectionTests
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
}