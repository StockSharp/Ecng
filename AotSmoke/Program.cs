namespace Ecng.AotSmoke;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

using Ecng.Common;
using Ecng.Serialization;

internal sealed record QuoteSnapshot(decimal Price, decimal Volume);

[JsonSerializable(typeof(QuoteSnapshot))]
internal partial class AotJsonContext : JsonSerializerContext
{
}

internal static class Program
{
	private static void Main()
	{
		// Static references ensure these generic instantiations exist in the native binary.
		TypeHelper.RegisterConstructedGenericType(typeof(Dictionary<string, QuoteSnapshot>));
		var genericType = typeof(Dictionary<,>).Make(typeof(string), typeof(QuoteSnapshot));
		if (genericType != typeof(Dictionary<string, QuoteSnapshot>))
			throw new InvalidOperationException("Closed generic type lookup failed.");

		TypeHelper.RegisterFactory<QuoteSnapshot>(args => new QuoteSnapshot((decimal)args[0], (decimal)args[1]));
		var quote = typeof(QuoteSnapshot).CreateInstance<QuoteSnapshot>(100m, 2m);
		if (quote.Price != 100m || quote.Volume != 2m)
			throw new InvalidOperationException("Registered factory failed.");

		Converter.RegisterTypeName<QuoteSnapshot>("ecng.aot.quote");
		if ("ECNG.AOT.QUOTE".To<Type>() != typeof(QuoteSnapshot))
			throw new InvalidOperationException("Registered type name failed.");

		var json = quote.ToJson(AotJsonContext.Default.QuoteSnapshot);
		var copy = json.FromJson(AotJsonContext.Default.QuoteSnapshot);
		if (copy != quote)
			throw new InvalidOperationException("Source-generated JSON round trip failed.");

		Console.WriteLine("Ecng Native AOT smoke test passed.");
	}
}
