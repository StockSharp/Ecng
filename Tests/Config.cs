namespace Ecng.Tests
{
	using System.Net.Http;

	using Ecng.Common;
	using Ecng.Reflection;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class Config
	{
		[AssemblyInitialize]
		public static void GlobalInitialize(TestContext context)
		{
			AttributeHelper.CacheEnabled = false;
			ReflectionHelper.CacheEnabled = false;
			FastInvoker.CacheEnabled = false;
		}

		public static readonly HttpClient HttpClient = new();
	}
}