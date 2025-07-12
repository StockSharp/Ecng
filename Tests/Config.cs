namespace Ecng.Tests;

using System.Net.Http;

using Ecng.Reflection;

[TestClass]
public static class Config
{
	[AssemblyInitialize]
	public static void GlobalInitialize(TestContext _)
	{
		AttributeHelper.CacheEnabled = false;
		ReflectionHelper.CacheEnabled = false;
	}

	public static readonly HttpClient HttpClient = new();
}