namespace Ecng.Test.Reflection
{
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	using Ecng.Common;

	[TestClass]
	public class ReflectionInvokerTests
	{
		#region Reflection Invoke Method

		[TestMethod]
		public void ReflectionInvokeVoidMethod()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().InvokeVoidMethod();
		}

		[TestMethod]
		public void ReflectionInvokeVoidMethodWithParams()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().InvokeVoidMethodWithParams();
		}

		[TestMethod]
		public void ReflectionInvokeVoidMethodWithParams2()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().InvokeVoidMethodWithParams2();
		}

		[TestMethod]
		public void ReflectionInvokeVoidMethodWithParams3()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().InvokeVoidMethodWithParams3();
		}

		[TestMethod]
		public void ReflectionInvokeVoidMethodWithParams4()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().InvokeVoidMethodWithParams4();
		}

		[TestMethod]
		public void ReflectionInvokeVoidMethodWithParams5()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().InvokeVoidMethodWithParams5();
		}

		[TestMethod]
		public void ReflectionInvokeVoidMethodWithParams6()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().InvokeVoidMethodWithParams6();
		}

		[TestMethod]
		public void ReflectionInvokeVoidMethodWithParams7()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().InvokeVoidMethodWithParams7();
		}

		[TestMethod]
		public void ReflectionInvokeVoidMethodWithParams8()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().InvokeVoidMethodWithParams8();
		}

		[TestMethod]
		public void ReflectionInvokeVoidMethodWithParams9()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().InvokeVoidMethodWithParams9();
		}

		[TestMethod]
		public void ReflectionInvokeReturnMethod()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().InvokeReturnMethod();
		}

		[TestMethod]
		public void ReflectionInvokeReturnMethod2()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().InvokeReturnMethod2();
		}

		[TestMethod]
		public void ReflectionInvokeReturnMethodWithParams()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().InvokeReturnMethodWithParams();
		}

		[TestMethod]
		public void ReflectionInvokeReturnMethodWithParams2()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().InvokeReturnMethodWithParams2();
		}

		[TestMethod]
		public void ReflectionInvokeReturnMethodWithParams3()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().InvokeReturnMethodWithParams3();
		}

		[TestMethod]
		public void ReflectionInvokeReturnMethodWithParams4()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().InvokeReturnMethodWithParams4();
		}

		[TestMethod]
		public void ReflectionInvokeReturnMethodWithParams5()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().InvokeReturnMethodWithParams5();
		}

		[TestMethod]
		public void ReflectionInvokeReturnMethodWithParams6()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().InvokeReturnMethodWithParams6();
		}

		[TestMethod]
		public void ReflectionInvokeReturnMethodWithParams7()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().InvokeReturnMethodWithParams7();
		}

		#endregion

		#region Reflection Accessor Methods

		[TestMethod]
		public void ReflectionInvokePublicMethodPublicStruct()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().InvokePublicMethodPublicStruct();
		}

		[TestMethod]
		public void ReflectionInvokePrivateMethodPublicStruct()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().InvokePrivateMethodPublicStruct();
		}

		[TestMethod]
		public void ReflectionInvokePublicMethodPrivateClass()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().InvokePublicMethodPrivateClass();
		}

		[TestMethod]
		public void ReflectionInvokePrivateMethodPublicClass()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().InvokePrivateMethodPublicClass();
		}

		[TestMethod]
		public void ReflectionInvokeStaticPublicMethodPublicClass()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().InvokeStaticPublicMethodPublicClass();
		}

		[TestMethod]
		public void ReflectionInvokeStaticPublicOutRefMethodPublicClass()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().InvokeStaticPublicOutRefMethodPublicClass();
		}

		#endregion

		#region Reflection Create Instance

		[TestMethod]
		public void ReflectionCreatePublicInstancePublicStruct()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().CreatePublicInstancePublicStruct();
		}

		[TestMethod]
		public void ReflectionCreatePublicInstancePublicStruct2()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().CreatePublicInstancePublicStruct2();
		}

		[TestMethod]
		public void ReflectionCreatePublicInstancePublicStruct3()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().CreatePublicInstancePublicStruct3();
		}

		[TestMethod]
		public void ReflectionCreatePublicInstancePublicStruct4()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().CreatePublicInstancePublicStruct4();
		}

		[TestMethod]
		public void ReflectionCreatePublicInstancePublicClass5()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().CreatePublicInstancePublicClass5();
		}

		[TestMethod]
		public void ReflectionCreatePublicInstancePublicClass6()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().CreatePublicInstancePublicClass6();
		}

		[TestMethod]
		public void ReflectionCreatePublicInstancePublicClassWithParams()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().CreatePublicInstancePublicClassWithParams();
		}

		[TestMethod]
		public void ReflectionCreatePublicInstancePrivateClass()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().CreatePublicInstancePrivateClass();
		}

		[TestMethod]
		public void ReflectionCreatePrivateInstancePublicClass()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().CreatePrivateInstancePublicClass();
		}

		[TestMethod]
		public void ReflectionCreatePrivateInstancePrivateClass()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().CreatePrivateInstancePrivateClass();
		}

		[TestMethod]
		public void ReflectionCreatePrivateInstancePrivateStruct()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().CreatePrivateInstancePrivateStruct();
		}

		#endregion

		#region Reflection Props And Fields

		[TestMethod]
		public void ReflectionValuePropStruct()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().ValuePropStruct();
		}

		[TestMethod]
		public void ReflectionRefPropStruct()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().RefPropStruct();
		}

		[TestMethod]
		public void ReflectionGetIntFieldValue()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().GetIntFieldValue();
		}

		[TestMethod]
		public void ReflectionSetIntFieldValue()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().SetIntFieldValue();
		}

		[TestMethod]
		public void ReflectionGetObjFieldValue()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().GetObjFieldValue();
		}

		[TestMethod]
		public void ReflectionSetObjFieldValue()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().SetObjFieldValue();
		}

		[TestMethod]
		public void ReflectionGetIntPropValue()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().GetIntPropValue();
		}

		[TestMethod]
		public void ReflectionSetIntPropValue()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().SetIntPropValue();
		}

		[TestMethod]
		public void ReflectionGetObjPropValue()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().GetObjPropValue();
		}

		[TestMethod]
		public void ReflectionSetObjPropValue()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().SetObjPropValue();
		}

		[TestMethod]
		public void ReflectionGetStaticIntFieldValue()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().GetStaticIntFieldValue();
		}

		[TestMethod]
		public void ReflectionSetStaticIntFieldValue()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().SetStaticIntFieldValue();
		}

		[TestMethod]
		public void ReflectionGetStaticObjFieldValue()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().GetStaticObjFieldValue();
		}

		[TestMethod]
		public void ReflectionSetStaticObjFieldValue()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().SetStaticObjFieldValue();
		}

		[TestMethod]
		public void ReflectionGetStaticIntPropValue()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().GetStaticIntPropValue();
		}

		[TestMethod]
		public void ReflectionSetStaticIntPropValue()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().SetStaticIntPropValue();
		}

		[TestMethod]
		public void ReflectionGetStaticObjPropValue()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().GetStaticObjPropValue();
		}

		[TestMethod]
		public void ReflectionSetStaticObjPropValue()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().SetStaticObjPropValue();
		}

		#endregion

		#region Reflection Indexers

		[TestMethod]
		public void ReflectionGetRefIndexerValue()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().GetRefIndexerValue();
		}

		[TestMethod]
		public void ReflectionSetRefIndexerValue()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().SetRefIndexerValue();
		}

		[TestMethod]
		public void ReflectionGetValueIndexerValue()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().GetValueIndexerValue();
		}

		[TestMethod]
		public void ReflectionSetValueIndexerValue()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().SetValueIndexerValue();
		}

		[TestMethod]
		public void ReflectionSetValueIndexerValue2()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().SetValueIndexerValue2();
		}

		[TestMethod]
		public void ReflectionGetValueIndexerValue2()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().GetValueIndexerValue2();
		}

		#endregion

		#region Reflection GenericTest

		[TestMethod]
		public void ReflectionGeneric()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().Generic();
		}

		#endregion

		#region Reflection Covariants

		[TestMethod]
		public void ReflectionCovariants()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().Covariants();
		}

		#endregion

		#region Reflection Events

		[TestMethod]
		public void ReflectionRaiseEvent()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().RaiseEvent();
		}

		[TestMethod]
		public void ReflectionRaiseEvent2()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().RaiseEvent2();
		}

		[TestMethod]
		public void ReflectionRaiseStaticEvent()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().RaiseStaticEvent();
		}

		[TestMethod]
		public void ReflectionRaiseStaticEvent2()
		{
			using var _ = new Scope<FastEmitNotSupported>(new FastEmitNotSupported());
			new MemberInvokeTest().RaiseStaticEvent2();
		}

		#endregion
	}
}