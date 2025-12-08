namespace Ecng.Nuget;

/// <summary>
/// The factory for the Nuget provider.
/// </summary>
/// <param name="log"><see cref="ILogReceiver"/></param>
/// <param name="privateNugetToken">The private Nuget token.</param>
public class NugetProviderFactory(ILogReceiver log, SecureString privateNugetToken) : Repository.ProviderFactory
{
	private class NugetHttpHandlerProvider(ILogReceiver log, SecureString privateNugetToken) : ResourceProvider(typeof(HttpHandlerResource), nameof(NugetHttpHandlerProvider), NuGetResourceProviderPositions.First)
	{
		private class NugetAuthHandler(HttpClientHandler clientHandler, SecureString privateNugetToken) : DelegatingHandler(clientHandler)
		{
			private readonly SecureString _privateNugetToken = privateNugetToken;

			protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
			{
				request.Headers.Remove(ProtocolConstants.ApiKeyHeader);
				request.Headers.Add(ProtocolConstants.ApiKeyHeader, _privateNugetToken.UnSecure());

				return base.SendAsync(request, cancellationToken);
			}
		}

		private readonly HttpHandlerResourceV3Provider _inner = new();
		private readonly ILogReceiver _log = log ?? throw new ArgumentNullException(nameof(log));
		private readonly SecureString _privateNugetToken = privateNugetToken ?? throw new ArgumentNullException(nameof(privateNugetToken));

		protected ILogReceiver Logs => _log;

		public override async Task<Tuple<bool, INuGetResource>> TryCreate(SourceRepository source, CancellationToken token)
		{
			var res = await _inner.TryCreate(source, token).NoWait();
			if (!res.Item1 || !source.PackageSource.Source.ContainsIgnoreCase(NugetRepoProvider.PrivateRepoKey))
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

	/// <inheritdoc />
	public override IEnumerable<Lazy<INuGetResourceProvider>> GetCoreV3()
	{
		yield return new Lazy<INuGetResourceProvider>(() => new NugetHttpHandlerProvider(_log, _privateNugetToken));

		foreach (var rp in base.GetCoreV3())
			yield return rp;
	}
}