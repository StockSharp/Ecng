namespace Ecng.Tests;

using System.Net.Http;

using Ecng.Reflection;

[TestClass]
public static class Config
{
	private static string _tempRoot;

	[AssemblyInitialize]
	public static void GlobalInitialize(TestContext _)
	{
		AttributeHelper.CacheEnabled = false;
		ReflectionHelper.CacheEnabled = false;

		_tempRoot = Path.Combine(AppContext.BaseDirectory, "_temp");
		Directory.CreateDirectory(_tempRoot);
	}

	[AssemblyCleanup]
	public static void GlobalCleanup()
	{
		if (_tempRoot.IsEmptyOrWhiteSpace() || !Directory.Exists(_tempRoot))
			return;

		try
		{
			Directory.Delete(_tempRoot, true);
		}
		catch
		{
			// ignore cleanup errors
		}
	}

	public static readonly HttpClient HttpClient = new();

	public static string GetTempPath(string folderName)
	{
		var path = Path.Combine(_tempRoot, Guid.NewGuid().ToString("N"), folderName);
		Directory.CreateDirectory(path);
		return path;
	}
}