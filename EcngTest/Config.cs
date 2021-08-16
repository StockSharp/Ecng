namespace Ecng.Test
{
	using Ecng.Common;
	using Ecng.Reflection;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	class Config
	{
		[AssemblyInitialize]
		public static void GlobalInitialize(TestContext context)
		{
			AttributeHelper.CacheEnabled = false;
			ReflectionHelper.CacheEnabled = false;
			FastInvoker.CacheEnabled = false;
		}
	}
}