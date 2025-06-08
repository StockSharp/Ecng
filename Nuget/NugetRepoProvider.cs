namespace Ecng.Nuget;

using Ecng.Net;

using Nito.AsyncEx;

/// <summary>
/// Nuget repository provider.
/// </summary>
public class NugetRepoProvider : CachingSourceProvider
{
	private class PrivatePackageSource : PackageSource
	{
		private static TaskCompletionSource<PrivatePackageSource> _tcs;
		private static readonly LogReceiver _log = new(nameof(PrivatePackageSource));

		static PrivatePackageSource() => _log.Parent = LogManager.Instance.Application;

		public static PrivatePackageSource Instance => _tcs.Task.Result;

		private static async Task<PrivatePackageSource> GetImplAsync(string src, CancellationToken token)
		{
			var sourceRepository = Repository.Factory.GetCoreV3(src);
			_log.AddInfoLog("trying to resolve nuget v3 service index at {0}", src);

			try
			{
				await sourceRepository.GetResourceAsync<ServiceIndexResourceV3>(token).NoWait();
				_log.AddInfoLog("nuget v3 success!");
				return new PrivatePackageSource(src);
			}
			catch (Exception e)
			{
				_log.AddWarningLog("nuget v3 service index is not available: {0}", e);
			}

			return null;
		}

		public static async Task GetAsync(string privateUrl, CancellationToken token)
		{
			if (privateUrl.IsEmpty())
				throw new ArgumentNullException(nameof(privateUrl));

			var tcs = new TaskCompletionSource<PrivatePackageSource>();

			lock (_log)
				_tcs ??= tcs;

			if (tcs != _tcs)
			{
				await _tcs.Task;
				return;
			}

			_tcs.SetResult(await GetImplAsync(privateUrl, token));
		}

		private PrivatePackageSource(string addr) : base(addr, PrivateRepoKey) {}
	}

	private static readonly AsyncLock _instanceLock = new();
	private static NugetRepoProvider _instance;

	/// <summary>
	/// Get instance.
	/// </summary>
	/// <param name="authToken">Auth token.</param>
	/// <param name="packagesFolder"><see cref="Directory"/></param>
	/// <param name="retryPolicy"><see cref="RetryPolicyInfo"/></param>
	/// <param name="token"><see cref="CancellationToken"/></param>
	/// <returns>Task.</returns>
	public static Task<NugetRepoProvider> GetInstanceAsync(SecureString authToken, string packagesFolder, RetryPolicyInfo retryPolicy, CancellationToken token)
		=> GetInstanceAsync("https://nuget.stocksharp.com/x/v3/index.json", authToken, packagesFolder, retryPolicy, token);

	/// <summary>
	/// Get instance.
	/// </summary>
	/// <param name="privateUrl">Private url.</param>
	/// <param name="authToken">Auth token.</param>
	/// <param name="packagesFolder"><see cref="Directory"/></param>
	/// <param name="retryPolicy"><see cref="RetryPolicyInfo"/></param>
	/// <param name="token"><see cref="CancellationToken"/></param>
	/// <returns>Task.</returns>
	public static async Task<NugetRepoProvider> GetInstanceAsync(string privateUrl, SecureString authToken, string packagesFolder, RetryPolicyInfo retryPolicy, CancellationToken token)
	{
		await PrivatePackageSource.GetAsync(privateUrl, token).NoWait();

		using (await _instanceLock.LockAsync(token))
		{
			if (_instance is null)
			{
				_instance = new(authToken, packagesFolder, retryPolicy);
				await _instance.InitBaseUrls(token);
			}
		}

		return _instance;
	}

	/// <summary>
	/// Private nuget repository.
	/// </summary>
	public const string PrivateRepoKey = "stocksharp";
	/// <summary>
	/// nuget.org
	/// </summary>
	public const string NugetFeedRepoKey = "nugetorg";

	private static readonly ISettings _settings = NullSettings.Instance;

	/// <summary>
	/// Settings.
	/// </summary>
	public ISettings Settings => ((PackageSourceProvider)PackageSourceProvider).Settings;

	/// <summary>
	/// <see cref="RetryPolicyInfo"/>
	/// </summary>
	public RetryPolicyInfo RetryPolicy { get; }

	private readonly SourceRepository _privateRepo;
	private readonly SourceRepository _nugetRepo;
	private readonly FrameworkReducer _fwkReducer = new();
	private readonly Dictionary<PackageIdentity, (DateTime till, (PackageIdentity id, SourceRepository repo)[] deps)> _depsCache = [];
	private readonly TimeSpan _cacheLen = TimeSpan.FromMinutes(10);
	private readonly List<(SourceRepository repo, Uri baseUrl)> _repoUrls = [];

	private readonly SecureString _authToken;
	private readonly HttpClient _publicHttp = new();
	private readonly HttpClient _privateHttp;

	private NugetRepoProvider(SecureString authToken, string packagesFolder, RetryPolicyInfo retryPolicy)
		: base(new PackageSourceProvider(_settings, GetPackageSources(packagesFolder)))
	{
		_authToken = authToken.ThrowIfEmpty(nameof(authToken));
		
		if (!packagesFolder.IsEmpty())
			Directory.CreateDirectory(packagesFolder);

		var repos = GetRepositories().ToArray();

		_privateRepo = repos.First(r => r.PackageSource.Name.EqualsIgnoreCase(PrivateRepoKey));
		_nugetRepo = repos.First(r => r.PackageSource.Name.EqualsIgnoreCase(NugetFeedRepoKey));
		RetryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));

		_privateHttp = NugetExtensions.CreatePrivateHttp(_authToken.UnSecure());
	}

	private async Task InitBaseUrls(CancellationToken cancellationToken)
	{
		async Task initBaseUrl(SourceRepository repo)
			=> _repoUrls.Add((repo, await repo.GetBaseUrl(cancellationToken).NoWait()));

		await initBaseUrl(_nugetRepo);
		await initBaseUrl(_privateRepo);
	}

	/// <summary>
	/// Try to find version.
	/// </summary>
	/// <param name="packageId">The package id.</param>
	/// <param name="versionRange">The version range.</param>
	/// <param name="cache"><see cref="SourceCacheContext"/></param>
	/// <param name="logger"><see cref="ILogger"/></param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Found version, repository and base url.</returns>
	public async Task<(NuGetVersion version, SourceRepository repo, Uri baseUrl)> TryFindVersion(string packageId, VersionRange versionRange, SourceCacheContext cache, ILogger logger, CancellationToken cancellationToken)
	{
		foreach (var (repo, baseUrl) in _repoUrls)
		{
			var resource = await repo.GetResourceAsync<FindPackageByIdResource>(cancellationToken).NoWait();

			var versions = await RetryPolicy.TryRepeat(t =>
				resource.GetAllVersionsAsync(
					packageId,
					cache,
					logger,
					t),
				RetryPolicy.ReadMaxCount, cancellationToken);

			var foundVer = versionRange.FindBestMatch(versions);

			if (foundVer is null)
				continue;

			return (foundVer, repo, baseUrl);
		}

		throw new InvalidOperationException($"Package {packageId} for version range {versionRange} not found.");
	}

	/// <summary>
	/// Get dependencies.
	/// </summary>
	/// <param name="identities">The package identities.</param>
	/// <param name="framework">The framework.</param>
	/// <param name="localFiles">The local files.</param>
	/// <param name="cache"><see cref="SourceCacheContext"/></param>
	/// <param name="logger"><see cref="ILogger"/></param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Dependencies.</returns>
	public async Task<IDictionary<PackageIdentity, IEnumerable<(PackageIdentity identity, SourceRepository repo)>>> GetDependenciesAsync(IEnumerable<PackageIdentity> identities, NuGetFramework framework, IDictionary<string, NuGetVersion> localFiles, SourceCacheContext cache, ILogger logger, CancellationToken cancellationToken)
	{
		if (identities is null)			throw new ArgumentNullException(nameof(identities));
		if (framework is null)			throw new ArgumentNullException(nameof(framework));
		if (localFiles is null)			throw new ArgumentNullException(nameof(localFiles));

		var fwkComparer = NuGetFrameworkFullComparer.Instance;

		var processedPackageIds = new Dictionary<string, NuGetVersion>(StringComparer.InvariantCultureIgnoreCase);
		var cycledIds = new HashSet<PackageIdentity>();

		async Task<IEnumerable<(PackageIdentity identity, SourceRepository repo)>> get(string packageId, VersionRange versionRange)
		{
			if (localFiles.TryGetValue(packageId, out var localVersion))
			{
				if (localVersion is null || versionRange.FindBestMatch([localVersion]) == localVersion)
					return [];
			}

			var (foundVersion, repo, baseUrl) = await TryFindVersion(packageId, versionRange, cache, logger, cancellationToken);

			if (processedPackageIds.TryGetValue(packageId, out var existVer))
			{
				if (existVer >= foundVersion)
					return [];
			}

			processedPackageIds[packageId] = foundVersion;

			var foundId = new PackageIdentity(packageId, foundVersion);

			if (_depsCache.TryGetValue(foundId, out var depsCache))
			{
				if (depsCache.till > DateTime.UtcNow)
					return depsCache.deps;

				_depsCache.Remove(foundId);
			}

			if (!cycledIds.Add(foundId))
			{
				logger.LogError($"Cycled {foundId} dependency.");
				return [];
			}

			var http = repo == _privateRepo ? _privateHttp : _publicHttp;
			using var nuspec = await RetryPolicy.TryRepeat(t => http.GetNuspecAsync(baseUrl, packageId, foundVersion, t), RetryPolicy.ReadMaxCount, cancellationToken);
			var reader = new NuspecReader(nuspec);
			var groups = reader.GetDependencyGroups().ToDictionary(g => g.TargetFramework);

			var dependencies = new List<(PackageIdentity identity, SourceRepository repo)>
			{
				(foundId, repo)
			};

			if (groups.Count > 0)
			{
				var frameworks = new HashSet<NuGetFramework>(fwkComparer);
				frameworks.UnionWith(groups.Keys);

				var compatibleFwk = _fwkReducer.GetNearest(framework, frameworks);
				//?? throw new InvalidOperationException($"Package {packageId} no compatible dependencies for framework {framework}.");

				if (compatibleFwk is not null)
				{
					var group = groups[compatibleFwk];

					foreach (var dep in group.Packages)
						dependencies.AddRange(await get(dep.Id, dep.VersionRange));
				}
			}

			var arr = dependencies.OrderBy(d => d.identity.Id, StringComparer.InvariantCultureIgnoreCase).ToArray();

			_depsCache[foundId] = (DateTime.UtcNow + _cacheLen, arr);

			return arr;
		}

		var retVal = new Dictionary<PackageIdentity, IEnumerable<(PackageIdentity identity, SourceRepository repo)>>();

		foreach (var identity in identities)
			retVal.Add(identity, await get(identity.Id, new(identity.Version)));

		var allDependencies = retVal.Values
			.SelectMany(x => x)
			.GroupBy(x => x.identity.Id, StringComparer.InvariantCultureIgnoreCase)
			.ToDictionary(
				g => g.Key,
				g => g.OrderByDescending(x => x.identity.Version).First(),
				StringComparer.InvariantCultureIgnoreCase
			);

		return retVal.ToDictionary(
			kvp => kvp.Key,
			kvp => kvp.Value
				.Select(dep => allDependencies[dep.identity.Id])
				.Distinct()
		);
	}

	private static PackageSource[] GetPackageSources(string packagesFolder)
	{
		if (packagesFolder.IsEmpty())
		{
			return
			[
				new PackageSource(NuGetConstants.V3FeedUrl, NugetFeedRepoKey),
				PrivatePackageSource.Instance,
			];
		}
		else
		{
			return
			[
				new FeedTypePackageSource(packagesFolder, FeedType.FileSystemV2),
				new PackageSource(NuGetConstants.V3FeedUrl, NugetFeedRepoKey),
				PrivatePackageSource.Instance,
			];
		}
	}
}
