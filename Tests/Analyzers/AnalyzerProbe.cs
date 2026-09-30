#if NET10_0_OR_GREATER

namespace Ecng.Tests.Analyzers;

using Microsoft.CodeAnalysis;

/// <summary>
/// What an analyzer probe compiles against: the framework the tests run on, and nothing the suite loaded.
/// </summary>
static class AnalyzerProbe
{
	private static readonly Lazy<MetadataReference[]> _framework = new(() =>
	{
		var dir = Path.GetDirectoryName(typeof(object).Assembly.Location);

		return [.. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
			.Split(Path.PathSeparator)
			.Where(path => Path.GetDirectoryName(path).EqualsIgnoreCase(dir))
			.Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))];
	});

	/// <summary>
	/// The framework's assemblies. The same instances every time, so the compilations share what is read out
	/// of them instead of each reading it again.
	/// </summary>
	public static MetadataReference[] Framework => _framework.Value;
}

#endif
