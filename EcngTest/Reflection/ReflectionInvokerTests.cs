namespace Ecng.Test.Reflection
{
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	using Ecng.Common;
	using Ecng.Reflection;

	[TestClass]
	public class ReflectionInvokerTests
	{
		[ClassInitialize]
		public static void Initialize(TestContext _)
		{
			AttributeHelper.CacheEnabled = false;
			ReflectionHelper.CacheEnabled = false;
			FastInvoker.CacheEnabled = false;
			FastInvoker.NotSupported = true;
		}

		[ClassCleanup]
		public static void Cleanup()
			=> FastInvoker.NotSupported = false;

		#region Reflection Invoke Method

		[TestMethod]
		public void ReflectionInvokeVoidMethod()
		{
			new MemberInvokeTest().InvokeVoidMethod();
		}

		[TestMethod]
		public void ReflectionInvokeVoidMethodWithParams()
		{
			new MemberInvokeTest().InvokeVoidMethodWithParams();
		}

		[TestMethod]
		public void ReflectionInvokeVoidMethodWithParams2()
		{
			new MemberInvokeTest().InvokeVoidMethodWithParams2();
		}

		[TestMethod]
		public void ReflectionInvokeVoidMethodWithParams3()
		{
			new MemberInvokeTest().InvokeVoidMethodWithParams3();
		}

		[TestMethod]
		public void ReflectionInvokeVoidMethodWithParams4()
		{
			new MemberInvokeTest().InvokeVoidMethodWithParams4();
		}

		[TestMethod]
		public void ReflectionInvokeVoidMethodWithParams5()
		{
			new MemberInvokeTest().InvokeVoidMethodWithParams5();
		}

		[TestMethod]
		public void ReflectionInvokeVoidMethodWithParams6()
		{
			new MemberInvokeTest().InvokeVoidMethodWithParams6();
		}

		[TestMethod]
		public void ReflectionInvokeVoidMethodWithParams7()
		{
			new MemberInvokeTest().InvokeVoidMethodWithParams7();
		}

		[TestMethod]
		public void ReflectionInvokeReturnMethod()
		{
			new MemberInvokeTest().InvokeReturnMethod();
		}

		[TestMethod]
		public void ReflectionInvokeReturnMethod2()
		{
			new MemberInvokeTest().InvokeReturnMethod2();
		}

		[TestMethod]
		public void ReflectionInvokeReturnMethodWithParams()
		{
			new MemberInvokeTest().InvokeReturnMethodWithParams();
		}

		[TestMethod]
		public void ReflectionInvokeReturnMethodWithParams2()
		{
			new MemberInvokeTest().InvokeReturnMethodWithParams2();
		}

		[TestMethod]
		public void ReflectionInvokeReturnMethodWithParams3()
		{
			new MemberInvokeTest().InvokeReturnMethodWithParams3();
		}

		[TestMethod]
		public void ReflectionInvokeReturnMethodWithParams4()
		{
			new MemberInvokeTest().InvokeReturnMethodWithParams4();
		}

		[TestMethod]
		public void ReflectionInvokeReturnMethodWithParams5()
		{
			new MemberInvokeTest().InvokeReturnMethodWithParams5();
		}

		[TestMethod]
		public void ReflectionInvokeReturnMethodWithParams6()
		{
			new MemberInvokeTest().InvokeReturnMethodWithParams6();
		}

		[TestMethod]
		public void ReflectionInvokeReturnMethodWithParams7()
		{
			new MemberInvokeTest().InvokeReturnMethodWithParams7();
		}

		#endregion

		#region Reflection Accessor Methods

		[TestMethod]
		public void ReflectionInvokePublicMethodPublicStruct()
		{
			new MemberInvokeTest().InvokePublicMethodPublicStruct();
		}

		[TestMethod]
		public void ReflectionInvokePrivateMethodPublicStruct()
		{
			new MemberInvokeTest().InvokePrivateMethodPublicStruct();
		}

		[TestMethod]
		public void ReflectionInvokePublicMethodPrivateClass()
		{
			new MemberInvokeTest().InvokePublicMethodPrivateClass();
		}

		[TestMethod]
		public void ReflectionInvokePrivateMethodPublicClass()
		{
			new MemberInvokeTest().InvokePrivateMethodPublicClass();
		}

		[TestMethod]
		public void ReflectionInvokeStaticPublicMethodPublicClass()
		{
			new MemberInvokeTest().InvokeStaticPublicMethodPublicClass();
		}

		[TestMethod]
		public void ReflectionInvokeStaticPublicOutRefMethodPublicClass()
		{
			new MemberInvokeTest().InvokeStaticPublicOutRefMethodPublicClass();
		}

		#endregion

		#region Reflection Create Instance

		[TestMethod]
		public void ReflectionCreatePublicInstancePublicStruct()
		{
			new MemberInvokeTest().CreatePublicInstancePublicStruct();
		}

		[TestMethod]
		public void ReflectionCreatePublicInstancePublicStruct2()
		{
			new MemberInvokeTest().CreatePublicInstancePublicStruct2();
		}

		[TestMethod]
		public void ReflectionCreatePublicInstancePublicStruct3()
		{
			new MemberInvokeTest().CreatePublicInstancePublicStruct3();
		}

		[TestMethod]
		public void ReflectionCreatePublicInstancePublicStruct4()
		{
			new MemberInvokeTest().CreatePublicInstancePublicStruct4();
		}

		[TestMethod]
		public void ReflectionCreatePublicInstancePublicClass5()
		{
			new MemberInvokeTest().CreatePublicInstancePublicClass5();
		}

		[TestMethod]
		public void ReflectionCreatePublicInstancePublicClass6()
		{
			new MemberInvokeTest().CreatePublicInstancePublicClass6();
		}

		[TestMethod]
		public void ReflectionCreatePublicInstancePublicClassWithParams()
		{
			new MemberInvokeTest().CreatePublicInstancePublicClassWithParams();
		}

		[TestMethod]
		public void ReflectionCreatePublicInstancePrivateClass()
		{
			new MemberInvokeTest().CreatePublicInstancePrivateClass();
		}

		[TestMethod]
		public void ReflectionCreatePrivateInstancePublicClass()
		{
			new MemberInvokeTest().CreatePrivateInstancePublicClass();
		}

		[TestMethod]
		public void ReflectionCreatePrivateInstancePrivateClass()
		{
			new MemberInvokeTest().CreatePrivateInstancePrivateClass();
		}

		[TestMethod]
		public void ReflectionCreatePrivateInstancePrivateStruct()
		{
			new MemberInvokeTest().CreatePrivateInstancePrivateStruct();
		}

		#endregion

		#region Reflection Props And Fields

		[TestMethod]
		public void ReflectionValuePropStruct()
		{
			new MemberInvokeTest().ValuePropStruct();
		}

		[TestMethod]
		public void ReflectionRefPropStruct()
		{
			new MemberInvokeTest().RefPropStruct();
		}

		[TestMethod]
		public void ReflectionGetIntFieldValue()
		{
			new MemberInvokeTest().GetIntFieldValue();
		}

		[TestMethod]
		public void ReflectionSetIntFieldValue()
		{
			new MemberInvokeTest().SetIntFieldValue();
		}

		[TestMethod]
		public void ReflectionGetObjFieldValue()
		{
			new MemberInvokeTest().GetObjFieldValue();
		}

		[TestMethod]
		public void ReflectionSetObjFieldValue()
		{
			new MemberInvokeTest().SetObjFieldValue();
		}

		[TestMethod]
		public void ReflectionGetIntPropValue()
		{
			new MemberInvokeTest().GetIntPropValue();
		}

		[TestMethod]
		public void ReflectionSetIntPropValue()
		{
			new MemberInvokeTest().SetIntPropValue();
		}

		[TestMethod]
		public void ReflectionGetObjPropValue()
		{
			new MemberInvokeTest().GetObjPropValue();
		}

		[TestMethod]
		public void ReflectionSetObjPropValue()
		{
			new MemberInvokeTest().SetObjPropValue();
		}

		[TestMethod]
		public void ReflectionGetStaticIntFieldValue()
		{
			new MemberInvokeTest().GetStaticIntFieldValue();
		}

		[TestMethod]
		public void ReflectionSetStaticIntFieldValue()
		{
			new MemberInvokeTest().SetStaticIntFieldValue();
		}

		[TestMethod]
		public void ReflectionGetStaticObjFieldValue()
		{
			new MemberInvokeTest().GetStaticObjFieldValue();
		}

		[TestMethod]
		public void ReflectionSetStaticObjFieldValue()
		{
			new MemberInvokeTest().SetStaticObjFieldValue();
		}

		[TestMethod]
		public void ReflectionGetStaticIntPropValue()
		{
			new MemberInvokeTest().GetStaticIntPropValue();
		}

		[TestMethod]
		public void ReflectionSetStaticIntPropValue()
		{
			new MemberInvokeTest().SetStaticIntPropValue();
		}

		[TestMethod]
		public void ReflectionGetStaticObjPropValue()
		{
			new MemberInvokeTest().GetStaticObjPropValue();
		}

		[TestMethod]
		public void ReflectionSetStaticObjPropValue()
		{
			new MemberInvokeTest().SetStaticObjPropValue();
		}

		#endregion

		#region Reflection Indexers

		[TestMethod]
		public void ReflectionGetRefIndexerValue()
		{
			new MemberInvokeTest().GetRefIndexerValue();
		}

		[TestMethod]
		public void ReflectionSetRefIndexerValue()
		{
			new MemberInvokeTest().SetRefIndexerValue();
		}

		[TestMethod]
		public void ReflectionGetValueIndexerValue()
		{
			new MemberInvokeTest().GetValueIndexerValue();
		}

		[TestMethod]
		public void ReflectionSetValueIndexerValue()
		{
			new MemberInvokeTest().SetValueIndexerValue();
		}

		#endregion

		#region Reflection GenericTest

		[TestMethod]
		public void ReflectionGeneric()
		{
			new MemberInvokeTest().Generic();
		}

		#endregion

		#region Reflection Covariants

		[TestMethod]
		public void ReflectionCovariants()
		{
			new MemberInvokeTest().Covariants();
		}

		#endregion

		#region Reflection Events

		[TestMethod]
		public void ReflectionRaiseEvent()
		{
			new MemberInvokeTest().RaiseEvent();
		}

		[TestMethod]
		public void ReflectionRaiseEvent2()
		{
			new MemberInvokeTest().RaiseEvent2();
		}

		[TestMethod]
		public void ReflectionRaiseStaticEvent()
		{
			new MemberInvokeTest().RaiseStaticEvent();
		}

		[TestMethod]
		public void ReflectionRaiseStaticEvent2()
		{
			new MemberInvokeTest().RaiseStaticEvent2();
		}

		#endregion
	}
}