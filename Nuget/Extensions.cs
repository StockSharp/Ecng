using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using NuGet.Common;
using NuGet.Versioning;
using NuGet.Packaging;
using NuGet.Protocol.Core.Types;

namespace Ecng.Nuget;

public static class Extensions
{
	public static string[] GetTargetFrameworks(this PackageArchiveReader reader)
	{
		var targetFrameworks = reader
			.GetSupportedFrameworks()
			.Select(f => f.GetShortFolderName())
			.ToList();

		// Default to the "any" framework if no frameworks were found.
		if (targetFrameworks.Count == 0)
		{
			targetFrameworks.Add("any");
		}

		return targetFrameworks.ToArray();
	}

	public static async Task<NuGetVersion> GetLastVersionAsync(this SourceRepository repo, string packageId, bool allowPreview, CancellationToken token = default, ILogger log = null, SourceCacheContext cacheCtx = null)
	{
		var cache = cacheCtx ?? new SourceCacheContext();
		var resource = await repo.GetResourceAsync<FindPackageByIdResource>(token);
		var versions = (await resource.GetAllVersionsAsync(packageId, cache, log ?? NullLogger.Instance, token)).OrderBy(v => v).ToArray();

		Func<NuGetVersion, bool> cond = allowPreview ? _ => true : v => !v.IsPrerelease;

		return versions.LastOrDefault(cond);
	}
}
