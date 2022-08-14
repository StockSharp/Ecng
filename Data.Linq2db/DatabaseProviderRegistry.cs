namespace Ecng.Data
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Common;

	using LinqToDB.DataProvider;

	[CLSCompliant(false)]
	public static class DatabaseProviderRegistry
	{
		private static readonly CachedSynchronizedDictionary<Type, Func<IDataProvider>> _providers = new();

		public static void AddProvider<TProvider>(Func<TProvider> createProvider)
			where TProvider : IDataProvider
		{
			if (createProvider is null)
				throw new ArgumentNullException(nameof(createProvider));

			AddProvider(typeof(TProvider), () => createProvider());
		}

		public static void AddProvider(Type provider, Func<IDataProvider> createProvider)
		{
			if (provider is null)
				throw new ArgumentNullException(nameof(provider));

			if (!provider.Is<IDataProvider>())
				throw new ArgumentException(nameof(provider));

			_providers.Add(provider, createProvider);
		}

		public static void RemoveProvider<TProvider>()
			where TProvider : IDataProvider
			=> RemoveProvider(typeof(TProvider));

		public static void RemoveProvider(Type provider) => _providers.Remove(provider);

		public static IEnumerable<Type> Providers => _providers.CachedKeys;

		public static IDataProvider CreateProvider(Type provider)
			=> _providers[provider ?? throw new ArgumentNullException(nameof(provider))]();
	}
}