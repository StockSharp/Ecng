namespace Ecng.Data
{
	using System;
	using System.Collections.Generic;
	using System.Data;

	using Ecng.Collections;
	using Ecng.Common;

	using SqlKata.Compilers;

	[CLSCompliant(false)]
	public static class DatabaseProviderRegistry
	{
		private static readonly CachedSynchronizedDictionary<Type, Type> _providerTypes = new();

		public static void AddProvider<TProvider, TCompiler>()
			where TProvider : IDbConnection
			where TCompiler : Compiler
			=> AddProvider(typeof(TProvider), typeof(TCompiler));

		public static void AddProvider(Type provider, Type compiler)
		{
			if (!typeof(IDbConnection).IsAssignableFrom(provider))
				throw new ArgumentException(nameof(provider));

			if (!typeof(Compiler).IsAssignableFrom(compiler))
				throw new ArgumentException(nameof(compiler));

			_providerTypes.Add(provider, compiler);
		}

		public static Compiler CreateCompiler(Type providerType)
			=> _providerTypes[providerType].CreateInstance<Compiler>();

		public static IEnumerable<Type> Providers { get; } = _providerTypes.CachedKeys;
		public static IEnumerable<Type> Compilers { get; } = _providerTypes.CachedValues;
	}
}