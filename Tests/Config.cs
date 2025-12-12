namespace Ecng.Tests;

using System.Net.Http;
using System.Text.Json;

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

	public static string GetTempPath(string folderName)
		=> Path.Combine(IOHelper.CreateTempDir(), folderName);

	private static readonly JsonSerializerOptions _opts = new()
	{
		PropertyNameCaseInsensitive = true
	};

	public static T DeserializeSecrets<T>(this string path)
		=> JsonSerializer.Deserialize<T>(File.ReadAllText(path), _opts);
}