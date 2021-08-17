namespace Ecng.Test.Reflection
{
	using System;

	using Ecng.Common;
	using Ecng.Reflection;
	using Ecng.UnitTesting;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	public class InvokeMethod
	{
		public void VoidMethod()
		{
		}

		public void VoidMethodWithParams(int param)
		{
			param.AreEqual(10);
		}

		public void VoidMethodWithParams2(string param)
		{
			param.AreEqual("John Smith");
		}

		public void VoidMethodWithParams3(string param, int param2)
		{
			param.AreEqual("John Smith");
			param2.AreEqual(10);
		}

		public void VoidMethodWithParams4(string param, ref int param2, out string param3)
		{
			param.AreEqual("John Smith");
			param2.AreEqual(1);
			param2 = 10;
			param3 = "John Smith";
		}

		public void VoidMethodWithParams5(params object[] args)
		{
			(args.Length == 2).AssertTrue();
			args[0].AreEqual("Mark Twain");
			args[1].AreEqual("John Smith");
		}

		public void VoidMethodWithParams6(params int[] args)
		{
			(args.Length == 2).AssertTrue();
			args[0].AreEqual(1);
			args[1].AreEqual(10);
		}

		public void VoidMethodWithParams7(string param, ref int param2, string param3, out string param4, string param5)
		{
			param.AreEqual("John Smith");
			param2.AreEqual(1);
			param3.AreEqual("Mark Twain");
			param5.AreEqual("Billy Bob");

			param2 = 10;
			param4 = "John Smith";
		}

		public void VoidMethodWithParams8(string str, params int[] args)
		{
			str.AreEqual("Mark Twain");
			(args.Length == 2).AssertTrue();
			args[0].AreEqual(1);
			args[1].AreEqual(10);
		}

		public void VoidMethodWithParams9(string param, ref int param2, string param3, out string param4, string param5, params int[] args)
		{
			param.AreEqual("John Smith");
			param2.AreEqual(1);
			param3.AreEqual("Mark Twain");
			param5.AreEqual("Billy Bob");

			param2 = 10;
			param4 = "John Smith";

			(args.Length == 2).AssertTrue();
			args[0].AreEqual(1);
			args[1].AreEqual(10);
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
			param.AreEqual(10);
			return param;
		}

		public object ReturnMethodWithParams2(string param)
		{
			param.AreEqual("John Smith");
			return param;
		}

		public object ReturnMethodWithParams3(string param, int param2)
		{
			param.AreEqual("John Smith ");
			param2.AreEqual(10);
			return param + param2;
		}

		public object ReturnMethodWithParams4(string param, ref int param2, out string param3)
		{
			param.AreEqual("Mark Twain ");
			param2.AreEqual(1);
			param2 = 10;
			param3 = " John Smith";
			return param + param2 + param3;
		}

		public object ReturnMethodWithParams5(params object[] args)
		{
			(args.Length == 2).AssertTrue();
			args[0].AreEqual("John Smith ");
			args[1].AreEqual("10");

			return (string)args[0] + (string)args[1];
		}

		public object ReturnMethodWithParams6(params int[] args)
		{
			(args.Length == 2).AssertTrue();
			args[0].AreEqual(1);
			args[1].AreEqual(10);

			return args[0] + args[1];
		}

		public int ReturnMethodWithParams7(string param, ref int param2, out string param3)
		{
			param.AreEqual("Mark Twain ");
			param2.AreEqual(1);
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
		public int[] Field3;
		public static int Field5;
		public static object Field6;
		public static int[] Field7;

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

		#region Prop3

		private int[] _prop3;

		public int[] Prop3
		{
			get { return _prop3; }
			set { _prop3 = value; }
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

		#region Prop7

		private static int[] _prop7;

		public static int[] Prop7
		{
			get { return _prop7; }
			set { _prop7 = value; }
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

		private int _value3;

		public int this[string index, string index2]
		{
			get { return _value3; }
			set { _value3 = value; }
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
			Event1?.Invoke(this, e);
		}

		public static void RaiseEvent2(EventsClassEventArgs e)
		{
			Event2?.Invoke(null, e);
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
			var args = new object[3];
			args[0] = "John Smith";
			args[1] = 1;
			InvokeVoidMethod(nameof(InvokeMethod.VoidMethodWithParams4), args);
			args[1].AssertEqual(10);
			args[2].AssertEqual("John Smith");
		}

		[TestMethod]
		public void InvokeVoidMethodWithParams5()
		{
			InvokeVoidMethod(nameof(InvokeMethod.VoidMethodWithParams5), new object[] { "Mark Twain", "John Smith" });
			InvokeVoidMethod(nameof(InvokeMethod.VoidMethodWithParams5), new string[] { "Mark Twain", "John Smith" });
		}

		[TestMethod]
		public void InvokeVoidMethodWithParams6()
		{
			InvokeVoidMethod(nameof(InvokeMethod.VoidMethodWithParams6), new int[] { 1, 10 });
		}

		[TestMethod]
		public void InvokeVoidMethodWithParams7()
		{
			var args = new object[] { "John Smith", 1, "Mark Twain", null, "Billy Bob" };
			InvokeVoidMethod(nameof(InvokeMethod.VoidMethodWithParams7), args);
			args[1].AssertEqual(10);
			args[3].AssertEqual("John Smith");
		}

		[TestMethod]
		public void InvokeVoidMethodWithParams8()
		{
			InvokeVoidMethod(nameof(InvokeMethod.VoidMethodWithParams8), new object[] { "Mark Twain", 1, 10 });
		}

		[TestMethod]
		public void InvokeVoidMethodWithParams9()
		{
			var args = new object[] { "John Smith", 1, "Mark Twain", null, "Billy Bob", 1, 10 };
			InvokeVoidMethod(nameof(InvokeMethod.VoidMethodWithParams9), args);
			args[1].AssertEqual(10);
			args[3].AssertEqual("John Smith");
		}

		[TestMethod]
		public void InvokeReturnMethod()
		{
			InvokeReturnMethod<object[], object>(nameof(InvokeMethod.ReturnMethod), null).AreEqual("John Smith");
		}

		[TestMethod]
		public void InvokeReturnMethod2()
		{
			InvokeReturnMethod<object[], int>(nameof(InvokeMethod.ReturnMethod2), null).AreEqual(10);
		}

		[TestMethod]
		public void InvokeReturnMethodWithParams()
		{
			InvokeReturnMethod<int, object>(nameof(InvokeMethod.ReturnMethodWithParams), 10).AreEqual(10);
		}

		[TestMethod]
		public void InvokeReturnMethodWithParams2()
		{
			InvokeReturnMethod<string, object>(nameof(InvokeMethod.ReturnMethodWithParams2), "John Smith").AreEqual("John Smith");
		}

		[TestMethod]
		public void InvokeReturnMethodWithParams3()
		{
			InvokeReturnMethod<object[], object>(nameof(InvokeMethod.ReturnMethodWithParams3), new object[] { "John Smith ", 10 }).AreEqual("John Smith 10");
		}

		[TestMethod]
		public void InvokeReturnMethodWithParams4()
		{
			var args = new object[] { "Mark Twain ", 1, null };
			InvokeReturnMethod<object[], object>(nameof(InvokeMethod.ReturnMethodWithParams4), args).AreEqual("Mark Twain 10 John Smith");
			args[1].AreEqual(10);
			args[2].AreEqual(" John Smith");
		}

		[TestMethod]
		public void InvokeReturnMethodWithParams5()
		{
			InvokeReturnMethod<object[], object>(nameof(InvokeMethod.ReturnMethodWithParams5), new object[] { "John Smith ", "10" }).AreEqual("John Smith 10");
		}

		[TestMethod]
		public void InvokeReturnMethodWithParams6()
		{
			InvokeReturnMethod<int[], object>(nameof(InvokeMethod.ReturnMethodWithParams6), new int[] { 1, 10 }).AreEqual(11);
		}

		[TestMethod]
		public void InvokeReturnMethodWithParams7()
		{
			var args = new object[] { "Mark Twain ", 1, null };
			InvokeReturnMethod<object[], int>(nameof(InvokeMethod.ReturnMethodWithParams7), args).AreEqual(10);
			args[1].AreEqual(10);
			args[2].AreEqual(" John Smith");
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
			var args = new object[3];
			args[0] = "Mark Twain ";
			args[1] = 1;
			typeof(StaticPublicMethodPublicClass).GetValue<object[], object>(nameof(InvokeMethod.ReturnMethodWithParams4), args).AreEqual("Mark Twain 10 John Smith");
			args[1].AreEqual(10);
			args[2].AreEqual(" John Smith");
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
			ReflectionHelper.CreateInstance<string, PublicInstancePublicStruct>("John Smith").StrValue.AreEqual("John Smith");
		}

		[TestMethod]
		public void CreatePublicInstancePublicStruct3()
		{
			ReflectionHelper.CreateInstance<int, PublicInstancePublicStruct>(10).Value.AreEqual(10);
		}

		[TestMethod]
		public void CreatePublicInstancePublicStruct4()
		{
			var args = new object[] { 10, null };
			ReflectionHelper.CreateInstance<object[], PublicInstancePublicStruct>(args).Value.AreEqual(10);
			args[1].AreEqual("John Smith");
		}

		[TestMethod]
		public void CreatePublicInstancePublicClass5()
		{
			var args = new object[] { 10, null, 12 };
			ReflectionHelper.CreateInstance<object[], PublicInstancePublicStruct>(args).Value.AreEqual(10);
			args[1].AreEqual("John Smith");
		}

		[TestMethod]
		public void CreatePublicInstancePublicClass6()
		{
			var args = new object[] { 10, null, 12, null };
			ReflectionHelper.CreateInstance<object[], PublicInstancePublicStruct>(args).Value.AreEqual(10);
			args[1].AreEqual("John Smith");
			args[3].AreEqual("Mark Twain");
		}

		[TestMethod]
		public void CreatePublicInstancePublicClassWithParams()
		{
			var args = new object[] { 10, 20 };
			var obj = ReflectionHelper.CreateInstance<object[], PublicInstancePublicClass>(args);
			obj.Param1.AreEqual(10);
			obj.Param2.AreEqual(20);
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
			var obj = new InvokeFieldsAndPropStruct();
			obj = obj.SetValue(nameof(obj.Prop1), 10);
			obj.GetValue<InvokeFieldsAndPropStruct, VoidType, int>(nameof(obj.Prop1), null).AreEqual(10);
		}

		[TestMethod]
		public void RefPropStruct()
		{
			var obj = new InvokeFieldsAndPropStruct();
			obj = obj.SetValue(nameof(obj.Prop2), "John Smith");
			obj.GetValue<InvokeFieldsAndPropStruct, VoidType, string>(nameof(obj.Prop2), null).AreEqual("John Smith");
		}

		[TestMethod]
		public void GetIntFieldValue()
		{
			var obj = new InvokeFieldsAndPropClass();
			obj.Field1 = 10;
			GetMemberValue(obj, nameof(obj.Field1), 10);
		}

		[TestMethod]
		public void SetIntFieldValue()
		{
			SetMemberValue(nameof(InvokeFieldsAndPropClass.Field1), 10).Field1.AreEqual(10);
		}

		[TestMethod]
		public void GetObjFieldValue()
		{
			var obj = new InvokeFieldsAndPropClass();
			obj.Field2 = "John Smith";
			GetMemberValue<object>(obj, nameof(obj.Field2), "John Smith");
		}

		[TestMethod]
		public void SetObjFieldValue()
		{
			SetMemberValue<object>(nameof(InvokeFieldsAndPropClass.Field2), "John Smith").Field2.AreEqual("John Smith");
		}

		[TestMethod]
		public void GetIntArrayFieldValue()
		{
			var arr = new[] { 10, 20 };
			var obj = new InvokeFieldsAndPropClass();
			obj.Field3 = arr;
			GetMemberValue(obj, nameof(obj.Field3), arr);
		}

		[TestMethod]
		public void SetIntArrayFieldValue()
		{
			var arr = new[] { 10, 20 };
			SetMemberValue(nameof(InvokeFieldsAndPropClass.Field3), arr).Field3.AreEqual(arr);
		}

		[TestMethod]
		public void GetIntPropValue()
		{
			var obj = new InvokeFieldsAndPropClass();
			obj.Prop1 = 10;
			GetMemberValue(obj, nameof(obj.Prop1), 10);
		}

		[TestMethod]
		public void SetIntPropValue()
		{
			SetMemberValue(nameof(InvokeFieldsAndPropClass.Prop1), 10).Prop1.AreEqual(10);
		}

		[TestMethod]
		public void GetObjPropValue()
		{
			var obj = new InvokeFieldsAndPropClass();
			obj.Prop2 = "John Smith";
			GetMemberValue<object>(obj, nameof(obj.Prop2), "John Smith");
		}

		[TestMethod]
		public void SetObjPropValue()
		{
			SetMemberValue<object>(nameof(InvokeFieldsAndPropClass.Prop2), "John Smith").Prop2.AreEqual("John Smith");
		}

		[TestMethod]
		public void GetIntArrayPropValue()
		{
			var arr = new[] { 10, 20 };
			var obj = new InvokeFieldsAndPropClass();
			obj.Prop3 = arr;
			GetMemberValue(obj, nameof(obj.Prop3), arr);
		}

		[TestMethod]
		public void SetIntArrayPropValue()
		{
			var arr = new[] { 10, 20 };
			SetMemberValue(nameof(InvokeFieldsAndPropClass.Prop3), arr).Prop3.AreEqual(arr);
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
			InvokeFieldsAndPropClass.Field5.AreEqual(10);
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
			InvokeFieldsAndPropClass.Field6.AreEqual("John Smith");
		}

		[TestMethod]
		public void GetStaticIntArrayFieldValue()
		{
			var arr = new[] { 10, 20 };
			InvokeFieldsAndPropClass.Field7 = arr;
			GetStaticMemberValue(nameof(InvokeFieldsAndPropClass.Field7), arr);
		}

		[TestMethod]
		public void SetStaticIntArrayFieldValue()
		{
			var arr = new[] { 10, 20 };
			SetStaticMemberValue(nameof(InvokeFieldsAndPropClass.Field7), arr);
			InvokeFieldsAndPropClass.Field7.AreEqual(arr);
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
			InvokeFieldsAndPropClass.Prop5.AreEqual(10);
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
			InvokeFieldsAndPropClass.Prop6.AreEqual("John Smith");
		}

		[TestMethod]
		public void GetStaticIntArrayPropValue()
		{
			var arr = new[] { 10, 20 };
			InvokeFieldsAndPropClass.Prop7 = arr;
			GetStaticMemberValue(nameof(InvokeFieldsAndPropClass.Prop7), arr);
		}

		[TestMethod]
		public void SetStaticIntArrayPropValue()
		{
			var arr = new[] { 10, 20 };
			SetStaticMemberValue(nameof(InvokeFieldsAndPropClass.Prop7), arr);
			InvokeFieldsAndPropClass.Prop7.AreEqual(arr);
		}

		#endregion

		#region Indexers

		[TestMethod]
		public void GetRefIndexerValue()
		{
			var obj = new InvokeFieldsAndPropClass();
			obj[0] = "John Smith";
			obj.GetValue<InvokeFieldsAndPropClass, int, object>(ReflectionHelper.IndexerName, 0).AreEqual("John Smith");
		}

		[TestMethod]
		public void SetRefIndexerValue()
		{
			var obj = new InvokeFieldsAndPropClass();
			obj.SetValue(ReflectionHelper.IndexerName, new object[] { 0, "John Smith" });
			obj[0].AreEqual("John Smith");
		}

		[TestMethod]
		public void GetValueIndexerValue()
		{
			var obj = new InvokeFieldsAndPropClass();
			obj["John Smith"] = 10;
			obj.GetValue<InvokeFieldsAndPropClass, string, int>(ReflectionHelper.IndexerName, "John Smith").AreEqual(10);
		}

		[TestMethod]
		public void SetValueIndexerValue()
		{
			var obj = new InvokeFieldsAndPropClass();
			obj.SetValue(ReflectionHelper.IndexerName, new object[] { "John Smith", 10 });
			obj["John Smith"].AreEqual(10);
		}

		[TestMethod]
		public void GetValueIndexerValue2()
		{
			var obj = new InvokeFieldsAndPropClass();
			obj["John Smith", "1234"] = 10;
			obj.GetValue<InvokeFieldsAndPropClass, object[], int>(ReflectionHelper.IndexerName, new[] { "John Smith", "1234" }).AreEqual(10);
		}

		[TestMethod]
		public void SetValueIndexerValue2()
		{
			var obj = new InvokeFieldsAndPropClass();
			obj.SetValue(ReflectionHelper.IndexerName, new object[] { "John Smith", "1234", 10 });
			obj["John Smith", "1234"].AreEqual(10);
		}

		#endregion

		#region GenericTest

		[TestMethod]
		public void Generic()
		{
			var obj = new DeriveGenericClass();
			obj.Parent = new DeriveGenericClass();
			obj.Parent.Field1 = 100;
			var invoker = FastInvoker<DeriveGenericClass, VoidType, DeriveGenericClass>.Create(typeof(DeriveGenericClass).GetProperty("Parent"), true);
			invoker.GetValue(obj).Field1.AreEqual(100);
		}

		#endregion

		#region Covariants

		[TestMethod]
		public void Covariants()
		{
			var invoker = FastInvoker<BaseCovariantClass, VoidType, BaseCovariantValue>.Create(typeof(DeriveCovariantClass).GetMethod("Method"));
			invoker.ReturnInvoke(new DeriveCovariantClass(), null).GetType().AreEqual(typeof(DeriveCovariantValue));
		}

		#endregion

		private static InvokeFieldsAndPropClass SetMemberValue<T>(string member, T value)
		{
			var obj = new InvokeFieldsAndPropClass();
			obj.SetValue(member, value);
			return obj;
		}

		private static void GetMemberValue<T>(InvokeFieldsAndPropClass obj, string member, T expected)
		{
			obj.GetValue<InvokeFieldsAndPropClass, VoidType, T>(member, null).AreEqual(expected);
		}

		private static void SetStaticMemberValue<T>(string member, T value)
		{
			typeof(InvokeFieldsAndPropClass).SetValue(member, value);
		}

		private static void GetStaticMemberValue<T>(string member, T expected)
		{
			typeof(InvokeFieldsAndPropClass).GetValue<VoidType, T>(member, null).AreEqual(expected);
		}

		#region Events

		[TestMethod]
		public void RaiseEvent()
		{
			var obj = new EventsClass();
			EventHandler<EventsClassEventArgs> handler = (sender, e) => e.Field = 10;
			obj.SetValue(nameof(EventsClass.Event1), handler);
			var arg = new EventsClassEventArgs();
			obj.RaiseEvent1(arg);
			arg.Field.AreEqual(10);
			obj.SetValue(ReflectionHelper.RemovePrefix + nameof(EventsClass.Event1), handler);
			arg.Field = 1;
			obj.RaiseEvent1(arg);
			arg.Field.AreEqual(1);
		}

		[TestMethod]
		public void RaiseEvent2()
		{
			var obj = new EventsClass();
			EventHandler<EventsClassEventArgs> handler = (sender, e) => e.Field = 10;
			obj.SetValue(ReflectionHelper.AddPrefix + nameof(EventsClass.Event1), handler);
			var arg = new EventsClassEventArgs();
			obj.RaiseEvent1(arg);
			arg.Field.AreEqual(10);
			obj.SetValue(ReflectionHelper.RemovePrefix + nameof(EventsClass.Event1), handler);
			arg.Field = 1;
			obj.RaiseEvent1(arg);
			arg.Field.AreEqual(1);
		}

		[TestMethod]
		public void RaiseStaticEvent()
		{
			EventHandler<EventsClassEventArgs> handler = (sender, e) => e.Field = 10;
			typeof(EventsClass).SetValue(nameof(EventsClass.Event2), handler);
			var arg = new EventsClassEventArgs();
			EventsClass.RaiseEvent2(arg);
			arg.Field.AreEqual(10);
			typeof(EventsClass).SetValue(ReflectionHelper.RemovePrefix + nameof(EventsClass.Event2), handler);
			arg.Field = 1;
			EventsClass.RaiseEvent2(arg);
			arg.Field.AreEqual(1);
		}

		[TestMethod]
		public void RaiseStaticEvent2()
		{
			EventHandler<EventsClassEventArgs> handler = (sender, e) => e.Field = 10;
			typeof(EventsClass).SetValue(ReflectionHelper.AddPrefix + nameof(EventsClass.Event2), handler);
			var arg = new EventsClassEventArgs();
			EventsClass.RaiseEvent2(arg);
			arg.Field.AreEqual(10);
			typeof(EventsClass).SetValue(ReflectionHelper.RemovePrefix + nameof(EventsClass.Event2), handler);
			arg.Field = 1;
			EventsClass.RaiseEvent2(arg);
			arg.Field.AreEqual(1);
		}

		#endregion
	}
}