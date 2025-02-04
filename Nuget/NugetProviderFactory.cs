namespace Ecng.Nuget;

using Newtonsoft.Json.Linq;

public class NugetProviderFactory(ILogReceiver log, SecureString privateNugetToken) : Repository.ProviderFactory
{
	private class NugetRegistrationResourceProvider : ResourceProvider
	{
		private class NugetRegistrationResourceV3 : RegistrationResourceV3
		{
			private readonly LogReceiver _log = new(nameof(NugetRegistrationResourceV3));

			public NugetRegistrationResourceV3(ILogSource parentLog, HttpSource client, Uri baseUrl) : base(client, baseUrl)
			{
				_log.Parent = parentLog;
				_log.LogDebug("created");
			}

			public override async Task<JObject> GetPackageMetadata(PackageIdentity identity, SourceCacheContext cacheContext, NuGet.Common.ILogger log, CancellationToken token)
			{
				var results = (await GetPackageMetadata(identity.Id, new VersionRange(identity.Version, true, identity.Version, true), true, true, cacheContext, log, token)).ToArray();

				if (results.Length < 2)
					return results.SingleOrDefault();

				_log.AddWarningLog("ERROR! GetPackageMetadata returned multiple results for '{0}':\n{1}", identity, results.Select(r => r.ToString()).Join("\n=======================\n"));

				var groups = results.GroupBy(r => r["packageContent"].Value<string>()).ToArray();
				if (groups.Length != 1)
				{
					_log.AddErrorLog("Multiple addresses for the same package version '{0}':\n{1}", identity, groups.Select(g => g.Key).JoinN());
					return results.SingleOrDefault(); // will throw
				}

				var contentAddr = groups[0].Key;
				if (!contentAddr.ContainsIgnoreCase(NugetRepoProvider.PrivateRepoKey))
					_log.AddWarningLog("unexpected package source: {0}", groups[0].Key);

				return results.First();
			}
		}

		private readonly LogReceiver _log = new(nameof(NugetRegistrationResourceProvider));

		public NugetRegistrationResourceProvider(ILogSource parentLog)
			: base(typeof(RegistrationResourceV3), nameof(RegistrationResourceV3), NuGetResourceProviderPositions.First)
		{
			_log.Parent = parentLog;
			_log.LogDebug("created");
		}

		public override async Task<Tuple<bool, INuGetResource>> TryCreate(SourceRepository source, CancellationToken token)
		{
			RegistrationResourceV3 regResource = null;
			var serviceIndex = await source.GetResourceAsync<ServiceIndexResourceV3>(token);

			if (serviceIndex != null)
			{
				var baseUrl = serviceIndex.GetServiceEntryUri(ServiceTypes.RegistrationsBaseUrl);

				var httpSourceResource = await source.GetResourceAsync<HttpSourceResource>(token);

				regResource = new NugetRegistrationResourceV3(_log, httpSourceResource.HttpSource, baseUrl);
			}

			return new Tuple<bool, INuGetResource>(regResource != null, regResource);
		}
	}

	private class NugetHttpHandlerProvider(ILogReceiver log, SecureString privateNugetToken) : ResourceProvider(typeof(HttpHandlerResource), nameof(NugetHttpHandlerProvider), NuGetResourceProviderPositions.First)
	{
		private class NugetAuthHandler(HttpClientHandler clientHandler, SecureString privateNugetToken) : DelegatingHandler(clientHandler)
		{
			private readonly SecureString _privateNugetToken = privateNugetToken;

			protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
			{
				request.Headers.Remove(ProtocolConstants.ApiKeyHeader);
				request.Headers.Add(ProtocolConstants.ApiKeyHeader, _privateNugetToken.UnSecure());

				return await base.SendAsync(request, cancellationToken);
			}
		}

		private readonly HttpHandlerResourceV3Provider _inner = new();
		private readonly ILogReceiver _log = log ?? throw new ArgumentNullException(nameof(log));
		private readonly SecureString _privateNugetToken = privateNugetToken ?? throw new ArgumentNullException(nameof(privateNugetToken));

		protected ILogReceiver Logs => _log;

		public override async Task<Tuple<bool, INuGetResource>> TryCreate(SourceRepository source, CancellationToken token)
		{
			var res = await _inner.TryCreate(source, token);
			if (!res.Item1 || !source.PackageSource.Source.ToLowerInvariant().Contains(NugetRepoProvider.PrivateRepoKey))
				return res;

			if (res.Item2 is not HttpHandlerResourceV3 oldHandler)
			{
				_log.AddErrorLog("unexpected resource type: {0}", res.Item2?.GetType().FullName);
				return res;
			}

			var newHandler = new NugetAuthHandler(oldHandler.ClientHandler, _privateNugetToken) { InnerHandler = oldHandler.MessageHandler };

			var wrappedResource = new HttpHandlerResourceV3(oldHandler.ClientHandler, newHandler);

			return new Tuple<bool, INuGetResource>(true, wrappedResource);
		}
	}

	private readonly SecureString _privateNugetToken = privateNugetToken ?? throw new ArgumentNullException(nameof(privateNugetToken));
	private readonly ILogReceiver _log = log ?? throw new ArgumentNullException(nameof(log));

	public override IEnumerable<Lazy<INuGetResourceProvider>> GetCoreV3()
	{
		yield return new Lazy<INuGetResourceProvider>(() => new NugetHttpHandlerProvider(_log, _privateNugetToken));

		foreach (var rp in base.GetCoreV3())
			yield return rp;
	}
}