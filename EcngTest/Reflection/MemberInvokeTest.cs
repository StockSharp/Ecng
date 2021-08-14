namespace Ecng.Test.Reflection
{
	using System;

	using Ecng.Reflection;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	public class InvokeMethod
	{
		public void VoidMethod()
		{
		}

		public void VoidMethodWithParams(int param)
		{
			Assert.AreEqual(10, param);
		}

		public void VoidMethodWithParams2(string param)
		{
			Assert.AreEqual("John Smith", param);
		}

		public void VoidMethodWithParams3(string param, int param2)
		{
			Assert.AreEqual("John Smith", param);
			Assert.AreEqual(10, param2);
		}

		public void VoidMethodWithParams4(string param, ref int param2, out string param3)
		{
			Assert.AreEqual("John Smith", param);
			Assert.AreEqual(1, param2);
			param2 = 10;
			param3 = "John Smith";
		}

		public void VoidMethodWithParams5(params object[] args)
		{
			Assert.IsTrue(args.Length == 2);
			Assert.AreEqual("Mark Twain", args[0]);
			Assert.AreEqual("John Smith", args[1]);
		}

		public void VoidMethodWithParams6(params int[] args)
		{
			Assert.IsTrue(args.Length == 2);
			Assert.AreEqual(1, args[0]);
			Assert.AreEqual(10, args[1]);
		}

		public void VoidMethodWithParams7(string param, ref int param2, string param3, out string param4, string param5)
		{
			Assert.AreEqual("John Smith", param);
			Assert.AreEqual(1, param2);
			Assert.AreEqual("Mark Twain", param3);
			Assert.AreEqual("Billy Bob", param5);

			param2 = 10;
			param4 = "John Smith";
		}

		public object ReturnMethod()
		{
			return "John Smith";
		}

		public int ReturnMethod2()
		{
			return 10;
		}

		public object ReturnMethodWithParams(int param)
		{
			Assert.AreEqual(10, param);
			return param;
		}

		public object ReturnMethodWithParams2(string param)
		{
			Assert.AreEqual("John Smith", param);
			return param;
		}

		public object ReturnMethodWithParams3(string param, int param2)
		{
			Assert.AreEqual("John Smith ", param);
			Assert.AreEqual(10, param2);
			return param + param2;
		}

		public object ReturnMethodWithParams4(string param, ref int param2, out string param3)
		{
			Assert.AreEqual("Mark Twain ", param);
			Assert.AreEqual(1, param2);
			param2 = 10;
			param3 = " John Smith";
			return param + param2 + param3;
		}

		public object ReturnMethodWithParams5(params object[] args)
		{
			Assert.IsTrue(args.Length == 2);
			Assert.AreEqual("John Smith ", args[0]);
			Assert.AreEqual("10", args[1]);

			return (string)args[0] + (string)args[1];
		}

		public object ReturnMethodWithParams6(params int[] args)
		{
			Assert.IsTrue(args.Length == 2);
			Assert.AreEqual(1, args[0]);
			Assert.AreEqual(10, args[1]);

			return args[0] + args[1];
		}

		public int ReturnMethodWithParams7(string param, ref int param2, out string param3)
		{
			Assert.AreEqual("Mark Twain ", param);
			Assert.AreEqual(1, param2);
			param2 = 10;
			param3 = " John Smith";
			return param2;
		}
	}

	public struct PublicMethodPublicStruct
	{
		public void VoidMethod()
		{
		}

		private void PrivateMethod()
		{
		}
	}

	public struct PublicInstancePublicStruct
	{
		#region PublicInstancePublicStruct.ctor()

		public PublicInstancePublicStruct(string value)
		{
			_myVar = 0;
			_strValue = value;
		}

		public PublicInstancePublicStruct(int value)
		{
			_myVar = value;
			_strValue = null;
		}

		public PublicInstancePublicStruct(int value, out string s)
		{
			_myVar = value;
			s = "John Smith";
			_strValue = null;
		}

		public PublicInstancePublicStruct(int value, out string s, ref int i)
		{
			_myVar = value;
			s = "John Smith";
			_strValue = null;
		}

		public PublicInstancePublicStruct(int value, out string s, ref int i, out string s2)
		{
			_myVar = value;
			s = "John Smith";
			s2 = "Mark Twain";
			_strValue = null;
		}

		#endregion

		#region Value

		private int _myVar;

		public int Value
		{
			get { return _myVar; }
			set { _myVar = value; }
		}

		#endregion

		#region StrValue

		private string _strValue;

		public string StrValue
		{
			get { return _strValue; }
			set { _strValue = value; }
		}

		#endregion
	}

	class PublicMethodPrivateClass
	{
		public void VoidMethod()
		{
		}
	}

	public class PrivateMethodPublicClass
	{
		private void VoidMethod()
		{
		}
	}

	public static class StaticPublicMethodPublicClass
	{
		public static void VoidMethod()
		{
		}

		public static object ReturnMethodWithParams4(string param, ref int param2, out string param3)
		{
			param2 = 10;
			param3 = " John Smith";
			return param + param2 + param3;
		}
	}

	public class PublicInstancePublicClass
	{
		public PublicInstancePublicClass()
		{
		}

		public PublicInstancePublicClass(int param1, int param2)
		{
			Param1 = param1;
			Param2 = param2;
		}

		public int Param1;
		public int Param2;
	}

	class PublicInstancePrivateClass
	{
	}

	class PrivateInstancePrivateClass
	{
		private PrivateInstancePrivateClass()
		{
		}
	}

	public class PrivateInstancePublicClass
	{
		private PrivateInstancePublicClass()
		{
		}
	}

	class PrivateInstancePrivateStruct
	{
		private PrivateInstancePrivateStruct()
		{
		}
	}

	public struct InvokeFieldsAndPropStruct
	{
		#region Prop1

		private int _prop1;

		public int Prop1
		{
			get { return _prop1; }
			set { _prop1 = value; }
		}

		#endregion

		#region Prop2

		private string _prop2;

		public string Prop2
		{
			get { return _prop2; }
			set { _prop2 = value; }
		}

		#endregion
	}

	public class InvokeFieldsAndPropClass
	{
		public int Field1;
		public object Field2;
		public static int Field5;
		public static object Field6;

		#region Prop1

		private int _prop1;

		public int Prop1
		{
			get { return _prop1; }
			set { _prop1 = value; }
		}

		#endregion

		#region Prop2

		private object _prop2;

		public object Prop2
		{
			get { return _prop2; }
			set { _prop2 = value; }
		}

		#endregion

		#region Prop5

		private static int _prop5;

		public static int Prop5
		{
			get { return _prop5; }
			set { _prop5 = value; }
		}

		#endregion

		#region Prop6

		private static object _prop6;

		public static object Prop6
		{
			get { return _prop6; }
			set { _prop6 = value; }
		}

		#endregion

		private object _value;

		public object this[int index]
		{
			get { return _value; }
			set { _value = value; }
		}

		private int _value2;

		public int this[string index]
		{
			get { return _value2; }
			set { _value2 = value; }
		}
	}

	public class EventsClassEventArgs : EventArgs
	{
		public int Field;
	}

	public class EventsClass
	{
		public event EventHandler<EventsClassEventArgs> Event1;
		public static event EventHandler<EventsClassEventArgs> Event2;

		public void RaiseEvent1(EventsClassEventArgs e)
		{
			if (Event1 != null)
				Event1(this, e);
		}

		public static void RaiseEvent2(EventsClassEventArgs e)
		{
			if (Event2 != null)
				Event2(null, e);
		}
	}

	class BaseGenericClass<T>
	{
		#region Parent

		private T _myVar;

		public T Parent
		{
			get { return _myVar; }
			set { _myVar = value; }
		}

		#endregion
	}

	class DeriveGenericClass : BaseGenericClass<DeriveGenericClass>
	{
		public int Field1;
	}

	class BaseCovariantValue
	{
	}

	class DeriveCovariantValue : BaseCovariantValue
	{
	}

	class BaseCovariantClass
	{
		public virtual DeriveCovariantValue Method()
		{
			return new DeriveCovariantValue();
		}
	}

	class DeriveCovariantClass : BaseCovariantClass
	{
		public override DeriveCovariantValue Method()
		{
			return base.Method();
		}
	}

	[TestClass]
	public class MemberInvokeTest
	{
		#region Invoke Method

		[TestMethod]
		public void InvokeVoidMethod()
		{
			InvokeVoidMethod<object[]>(nameof(InvokeMethod.VoidMethod), null);
		}

		[TestMethod]
		public void InvokeVoidMethodWithParams()
		{
			InvokeVoidMethod(nameof(InvokeMethod.VoidMethodWithParams), 10);
		}

		[TestMethod]
		public void InvokeVoidMethodWithParams2()
		{
			InvokeVoidMethod(nameof(InvokeMethod.VoidMethodWithParams2), "John Smith");
		}

		[TestMethod]
		public void InvokeVoidMethodWithParams3()
		{
			InvokeVoidMethod(nameof(InvokeMethod.VoidMethodWithParams3), new object[] { "John Smith", 10 });
		}

		[TestMethod]
		public void InvokeVoidMethodWithParams4()
		{
			object[] args = new object[3];
			args[0] = "John Smith";
			args[1] = 1;
			InvokeVoidMethod(nameof(InvokeMethod.VoidMethodWithParams4), args);
			Assert.AreEqual(10, args[1]);
			Assert.AreEqual("John Smith", args[2]);
		}

		[TestMethod]
		public void InvokeVoidMethodWithParams5()
		{
			InvokeVoidMethod(nameof(InvokeMethod.VoidMethodWithParams5), new object[] { "Mark Twain", "John Smith" });
		}

		[TestMethod]
		public void InvokeVoidMethodWithParams6()
		{
			InvokeVoidMethod(nameof(InvokeMethod.VoidMethodWithParams6), new int[] { 1, 10 });
		}

		[TestMethod]
		public void InvokeVoidMethodWithParams7()
		{
			object[] args = new object[5] { "John Smith", 1, "Mark Twain", null, "Billy Bob" };
			InvokeVoidMethod(nameof(InvokeMethod.VoidMethodWithParams7), args);
			Assert.AreEqual(10, args[1]);
			Assert.AreEqual("John Smith", args[3]);
		}

		[TestMethod]
		public void InvokeReturnMethod()
		{
			Assert.AreEqual("John Smith", InvokeReturnMethod<object[], object>(nameof(InvokeMethod.ReturnMethod), null));
		}

		[TestMethod]
		public void InvokeReturnMethod2()
		{
			Assert.AreEqual(10, InvokeReturnMethod<object[], int>(nameof(InvokeMethod.ReturnMethod2), null));
		}

		[TestMethod]
		public void InvokeReturnMethodWithParams()
		{
			Assert.AreEqual(10, InvokeReturnMethod<int, object>(nameof(InvokeMethod.ReturnMethodWithParams), 10));
		}

		[TestMethod]
		public void InvokeReturnMethodWithParams2()
		{
			Assert.AreEqual("John Smith", InvokeReturnMethod<string, object>(nameof(InvokeMethod.ReturnMethodWithParams2), "John Smith"));
		}

		[TestMethod]
		public void InvokeReturnMethodWithParams3()
		{
			Assert.AreEqual("John Smith 10", InvokeReturnMethod<object[], object>(nameof(InvokeMethod.ReturnMethodWithParams3), new object[] { "John Smith ", 10 }));
		}

		[TestMethod]
		public void InvokeReturnMethodWithParams4()
		{
			object[] args = new object[] { "Mark Twain ", 1, null };
			Assert.AreEqual("Mark Twain 10 John Smith", InvokeReturnMethod<object[], object>(nameof(InvokeMethod.ReturnMethodWithParams4), args));
			Assert.AreEqual(10, args[1]);
			Assert.AreEqual(" John Smith", args[2]);
		}

		[TestMethod]
		public void InvokeReturnMethodWithParams5()
		{
			Assert.AreEqual("John Smith 10", InvokeReturnMethod<object[], object>(nameof(InvokeMethod.ReturnMethodWithParams5), new object[] { "John Smith ", "10" }));
		}

		[TestMethod]
		public void InvokeReturnMethodWithParams6()
		{
			Assert.AreEqual(11, InvokeReturnMethod<int[], object>(nameof(InvokeMethod.ReturnMethodWithParams6), new int[] { 1, 10 }));
		}

		[TestMethod]
		public void InvokeReturnMethodWithParams7()
		{
			object[] args = new object[] { "Mark Twain ", 1, null };
			Assert.AreEqual(10, InvokeReturnMethod<object[], int>(nameof(InvokeMethod.ReturnMethodWithParams7), args));
			Assert.AreEqual(10, args[1]);
			Assert.AreEqual(" John Smith", args[2]);
		}

		private static void InvokeVoidMethod<V>(string method, V arg)
		{
			new InvokeMethod().SetValue(method, arg);
		}

		private static V InvokeReturnMethod<A, V>(string method, A arg)
		{
			return new InvokeMethod().GetValue<InvokeMethod, A, V>(method, arg);
		}

		#endregion

		#region Accessor Methods

		[TestMethod]
		public void InvokePublicMethodPublicStruct()
		{
			new PublicMethodPublicStruct().SetValue(nameof(PublicMethodPublicStruct.VoidMethod), (object[])null);
		}

		[TestMethod]
		public void InvokePrivateMethodPublicStruct()
		{
			new PublicMethodPublicStruct().SetValue("PrivateMethod", (object[])null);
		}

		[TestMethod]
		public void InvokePublicMethodPrivateClass()
		{
			new PublicMethodPrivateClass().SetValue(nameof(PublicMethodPublicStruct.VoidMethod), (object[])null);
		}

		[TestMethod]
		public void InvokePrivateMethodPublicClass()
		{
			new PrivateMethodPublicClass().SetValue(nameof(PublicMethodPublicStruct.VoidMethod), (object[])null);
		}

		[TestMethod]
		public void InvokeStaticPublicMethodPublicClass()
		{
			typeof(StaticPublicMethodPublicClass).SetValue(nameof(PublicMethodPublicStruct.VoidMethod), (object[])null);
		}

		[TestMethod]
		public void InvokeStaticPublicOutRefMethodPublicClass()
		{
			object[] args = new object[3];
			args[0] = "Mark Twain ";
			args[1] = 1;
			Assert.AreEqual("Mark Twain 10 John Smith", typeof(StaticPublicMethodPublicClass).GetValue<object[], object>(nameof(InvokeMethod.ReturnMethodWithParams4), args));
			Assert.AreEqual(10, args[1]);
			Assert.AreEqual(" John Smith", args[2]);
		}

		#endregion

		#region Create Instance

		[TestMethod]
		public void CreatePublicInstancePublicStruct()
		{
			ReflectionHelper.CreateInstance<PublicInstancePublicStruct>();
		}

		[TestMethod]
		public void CreatePublicInstancePublicStruct2()
		{
			Assert.AreEqual("John Smith", ReflectionHelper.CreateInstance<string, PublicInstancePublicStruct>("John Smith").StrValue);
		}

		[TestMethod]
		public void CreatePublicInstancePublicStruct3()
		{
			Assert.AreEqual(10, ReflectionHelper.CreateInstance<int, PublicInstancePublicStruct>(10).Value);
		}

		[TestMethod]
		public void CreatePublicInstancePublicStruct4()
		{
			object[] args = new object[] { 10, null };
			Assert.AreEqual(10, ReflectionHelper.CreateInstance<object[], PublicInstancePublicStruct>(args).Value);
			Assert.AreEqual("John Smith", args[1]);
		}

		[TestMethod]
		public void CreatePublicInstancePublicClass5()
		{
			object[] args = new object[] { 10, null, 12 };
			Assert.AreEqual(10, ReflectionHelper.CreateInstance<object[], PublicInstancePublicStruct>(args).Value);
			Assert.AreEqual("John Smith", args[1]);
		}

		[TestMethod]
		public void CreatePublicInstancePublicClass6()
		{
			object[] args = new object[] { 10, null, 12, null };
			Assert.AreEqual(10, ReflectionHelper.CreateInstance<object[], PublicInstancePublicStruct>(args).Value);
			Assert.AreEqual("John Smith", args[1]);
			Assert.AreEqual("Mark Twain", args[3]);
		}

		[TestMethod]
		public void CreatePublicInstancePublicClassWithParams()
		{
			object[] args = new object[] { 10, 20 };
			PublicInstancePublicClass obj = ReflectionHelper.CreateInstance<object[], PublicInstancePublicClass>(args);
			Assert.AreEqual(10, obj.Param1);
			Assert.AreEqual(20, obj.Param2);
		}

		[TestMethod]
		public void CreatePublicInstancePrivateClass()
		{
			ReflectionHelper.CreateInstance<PublicInstancePrivateClass>();
		}

		[TestMethod]
		public void CreatePrivateInstancePublicClass()
		{
			ReflectionHelper.CreateInstance<PrivateInstancePublicClass>();
		}

		[TestMethod]
		public void CreatePrivateInstancePrivateClass()
		{
			ReflectionHelper.CreateInstance<PrivateInstancePrivateClass>();
		}

		[TestMethod]
		public void CreatePrivateInstancePrivateStruct()
		{
			ReflectionHelper.CreateInstance<PrivateInstancePrivateStruct>();
		}

		#endregion

		#region Props And Fields

		[TestMethod]
		public void ValuePropStruct()
		{
			InvokeFieldsAndPropStruct obj = new InvokeFieldsAndPropStruct();
			obj = obj.SetValue(nameof(obj.Prop1), 10);
			Assert.AreEqual(10, obj.GetValue<InvokeFieldsAndPropStruct, VoidType, int>(nameof(obj.Prop1), null));
		}

		[TestMethod]
		public void RefPropStruct()
		{
			InvokeFieldsAndPropStruct obj = new InvokeFieldsAndPropStruct();
			obj = obj.SetValue(nameof(obj.Prop2), "John Smith");
			Assert.AreEqual("John Smith", obj.GetValue<InvokeFieldsAndPropStruct, VoidType, string>(nameof(obj.Prop2), null));
		}

		[TestMethod]
		public void GetIntFieldValue()
		{
			InvokeFieldsAndPropClass obj = new InvokeFieldsAndPropClass();
			obj.Field1 = 10;
			GetMemberValue(obj, nameof(obj.Field1), 10);
		}

		[TestMethod]
		public void SetIntFieldValue()
		{
			Assert.AreEqual(10, SetMemberValue(nameof(InvokeFieldsAndPropClass.Field1), 10).Field1);
		}

		[TestMethod]
		public void GetObjFieldValue()
		{
			InvokeFieldsAndPropClass obj = new InvokeFieldsAndPropClass();
			obj.Field2 = "John Smith";
			GetMemberValue<object>(obj, nameof(obj.Field2), "John Smith");
		}

		[TestMethod]
		public void SetObjFieldValue()
		{
			Assert.AreEqual("John Smith", SetMemberValue<object>(nameof(InvokeFieldsAndPropClass.Field2), "John Smith").Field2);
		}

		[TestMethod]
		public void GetIntPropValue()
		{
			InvokeFieldsAndPropClass obj = new InvokeFieldsAndPropClass();
			obj.Prop1 = 10;
			GetMemberValue(obj, nameof(obj.Prop1), 10);
		}

		[TestMethod]
		public void SetIntPropValue()
		{
			Assert.AreEqual(10, SetMemberValue(nameof(InvokeFieldsAndPropClass.Prop1), 10).Prop1);
		}

		[TestMethod]
		public void GetObjPropValue()
		{
			InvokeFieldsAndPropClass obj = new InvokeFieldsAndPropClass();
			obj.Prop2 = "John Smith";
			GetMemberValue<object>(obj, nameof(obj.Prop2), "John Smith");
		}

		[TestMethod]
		public void SetObjPropValue()
		{
			Assert.AreEqual("John Smith", SetMemberValue<object>(nameof(InvokeFieldsAndPropClass.Prop2), "John Smith").Prop2);
		}

		[TestMethod]
		public void GetStaticIntFieldValue()
		{
			InvokeFieldsAndPropClass.Field5 = 10;
			GetStaticMemberValue(nameof(InvokeFieldsAndPropClass.Field5), 10);
		}

		[TestMethod]
		public void SetStaticIntFieldValue()
		{
			SetStaticMemberValue(nameof(InvokeFieldsAndPropClass.Field5), 10);
			Assert.AreEqual(10, InvokeFieldsAndPropClass.Field5);
		}

		[TestMethod]
		public void GetStaticObjFieldValue()
		{
			InvokeFieldsAndPropClass.Field6 = "John Smith";
			GetStaticMemberValue<object>(nameof(InvokeFieldsAndPropClass.Field6), "John Smith");
		}

		[TestMethod]
		public void SetStaticObjFieldValue()
		{
			SetStaticMemberValue<object>(nameof(InvokeFieldsAndPropClass.Field6), "John Smith");
			Assert.AreEqual("John Smith", InvokeFieldsAndPropClass.Field6);
		}

		[TestMethod]
		public void GetStaticIntPropValue()
		{
			InvokeFieldsAndPropClass.Prop5 = 10;
			GetStaticMemberValue(nameof(InvokeFieldsAndPropClass.Prop5), 10);
		}

		[TestMethod]
		public void SetStaticIntPropValue()
		{
			SetStaticMemberValue(nameof(InvokeFieldsAndPropClass.Prop5), 10);
			Assert.AreEqual(10, InvokeFieldsAndPropClass.Prop5);
		}

		[TestMethod]
		public void GetStaticObjPropValue()
		{
			InvokeFieldsAndPropClass.Prop6 = "John Smith";
			GetStaticMemberValue<object>(nameof(InvokeFieldsAndPropClass.Prop6), "John Smith");
		}

		[TestMethod]
		public void SetStaticObjPropValue()
		{
			SetStaticMemberValue<object>(nameof(InvokeFieldsAndPropClass.Prop6), "John Smith");
			Assert.AreEqual("John Smith", InvokeFieldsAndPropClass.Prop6);
		}

		#endregion

		#region Indexers

		[TestMethod]
		public void GetRefIndexerValue()
		{
			InvokeFieldsAndPropClass obj = new InvokeFieldsAndPropClass();
			obj[0] = "John Smith";
			Assert.AreEqual("John Smith", obj.GetValue<InvokeFieldsAndPropClass, int, object>(ReflectionHelper.IndexerName, 0));
		}

		[TestMethod]
		public void SetRefIndexerValue()
		{
			InvokeFieldsAndPropClass obj = new InvokeFieldsAndPropClass();
			obj.SetValue(ReflectionHelper.IndexerName, new object[] { 0, "John Smith" });
			Assert.AreEqual("John Smith", obj[0]);
		}

		[TestMethod]
		public void GetValueIndexerValue()
		{
			InvokeFieldsAndPropClass obj = new InvokeFieldsAndPropClass();
			obj["John Smith"] = 10;
			Assert.AreEqual(10, obj.GetValue<InvokeFieldsAndPropClass, string, int>(ReflectionHelper.IndexerName, "John Smith"));
		}

		[TestMethod]
		public void SetValueIndexerValue()
		{
			InvokeFieldsAndPropClass obj = new InvokeFieldsAndPropClass();
			obj.SetValue(ReflectionHelper.IndexerName, new object[] { "John Smith", 10 });
			Assert.AreEqual(10, obj["John Smith"]);
		}

		#endregion

		#region GenericTest

		[TestMethod]
		public void Generic()
		{
			DeriveGenericClass obj = new DeriveGenericClass();
			obj.Parent = new DeriveGenericClass();
			obj.Parent.Field1 = 100;
			FastInvoker<DeriveGenericClass, VoidType, DeriveGenericClass> invoker = FastInvoker<DeriveGenericClass, VoidType, DeriveGenericClass>.Create(typeof(DeriveGenericClass).GetProperty("Parent"), true);
			Assert.AreEqual(100, invoker.GetValue(obj).Field1);
		}

		#endregion

		#region Covariants

		[TestMethod]
		public void Covariants()
		{
			FastInvoker<BaseCovariantClass, VoidType, BaseCovariantValue> invoker = FastInvoker<BaseCovariantClass, VoidType, BaseCovariantValue>.Create(typeof(DeriveCovariantClass).GetMethod("Method"));
			Assert.AreEqual(typeof(DeriveCovariantValue), invoker.ReturnInvoke(new DeriveCovariantClass(), null).GetType());
		}

		#endregion

		private static InvokeFieldsAndPropClass SetMemberValue<T>(string member, T value)
		{
			InvokeFieldsAndPropClass obj = new InvokeFieldsAndPropClass();
			obj.SetValue(member, value);
			return obj;
		}

		private static void GetMemberValue<T>(InvokeFieldsAndPropClass obj, string member, T expected)
		{
			Assert.AreEqual(expected, obj.GetValue<InvokeFieldsAndPropClass, VoidType, T>(member, null));
		}

		private static void SetStaticMemberValue<T>(string member, T value)
		{
			typeof(InvokeFieldsAndPropClass).SetValue(member, value);
		}

		private static void GetStaticMemberValue<T>(string member, T expected)
		{
			Assert.AreEqual(expected, typeof(InvokeFieldsAndPropClass).GetValue<VoidType, T>(member, null));
		}

		[TestMethod]
		public void RaiseEvent()
		{
			EventsClass obj = new EventsClass();
			EventHandler<EventsClassEventArgs> handler = (sender, e) => e.Field = 10;
			obj.SetValue(nameof(EventsClass.Event1), handler);
			EventsClassEventArgs arg = new EventsClassEventArgs();
			obj.RaiseEvent1(arg);
			Assert.AreEqual(10, arg.Field);
			obj.SetValue(ReflectionHelper.RemovePrefix + nameof(EventsClass.Event1), handler);
			arg.Field = 1;
			obj.RaiseEvent1(arg);
			Assert.AreEqual(1, arg.Field);
		}

		[TestMethod]
		public void RaiseEvent2()
		{
			EventsClass obj = new EventsClass();
			EventHandler<EventsClassEventArgs> handler = (sender, e) => e.Field = 10;
			obj.SetValue(ReflectionHelper.AddPrefix + nameof(EventsClass.Event1), handler);
			EventsClassEventArgs arg = new EventsClassEventArgs();
			obj.RaiseEvent1(arg);
			Assert.AreEqual(10, arg.Field);
			obj.SetValue(ReflectionHelper.RemovePrefix + nameof(EventsClass.Event1), handler);
			arg.Field = 1;
			obj.RaiseEvent1(arg);
			Assert.AreEqual(1, arg.Field);
		}

		[TestMethod]
		public void RaiseStaticEvent()
		{
			EventHandler<EventsClassEventArgs> handler = (sender, e) => e.Field = 10;
			typeof(EventsClass).SetValue(nameof(EventsClass.Event2), handler);
			EventsClassEventArgs arg = new EventsClassEventArgs();
			EventsClass.RaiseEvent2(arg);
			Assert.AreEqual(10, arg.Field);
			typeof(EventsClass).SetValue(ReflectionHelper.RemovePrefix + nameof(EventsClass.Event2), handler);
			arg.Field = 1;
			EventsClass.RaiseEvent2(arg);
			Assert.AreEqual(1, arg.Field);
		}

		[TestMethod]
		public void RaiseStaticEvent2()
		{
			EventHandler<EventsClassEventArgs> handler = (sender, e) => e.Field = 10;
			typeof(EventsClass).SetValue(ReflectionHelper.AddPrefix + nameof(EventsClass.Event2), handler);
			EventsClassEventArgs arg = new EventsClassEventArgs();
			EventsClass.RaiseEvent2(arg);
			Assert.AreEqual(10, arg.Field);
			typeof(EventsClass).SetValue(ReflectionHelper.RemovePrefix + nameof(EventsClass.Event2), handler);
			arg.Field = 1;
			EventsClass.RaiseEvent2(arg);
			Assert.AreEqual(1, arg.Field);
		}
	}
}