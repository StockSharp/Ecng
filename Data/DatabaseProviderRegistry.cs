namespace Ecng.Data
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;

	using LinqToDB.DataProvider;

	[CLSCompliant(false)]
	public static class DatabaseProviderRegistry
	{
		private static readonly CachedSynchronizedSet<Type> _providers = new();

		public static void AddProvider<TProvider>()
			where TProvider : IDataProvider
			=> AddProvider(typeof(TProvider));

		public static void AddProvider(Type provider)
		{
			if (provider is null)
				throw new ArgumentNullException(nameof(provider));

			if (!typeof(IDataProvider).IsAssignableFrom(provider))
				throw new ArgumentException(nameof(provider));

			_providers.Add(provider);
		}

		public static void RemoveProvider<TProvider>()
			where TProvider : IDataProvider
			=> RemoveProvider(typeof(TProvider));

		public static void RemoveProvider(Type provider) => _providers.Remove(provider);

		public static IEnumerable<Type> Providers => _providers.Cache;
	}
}