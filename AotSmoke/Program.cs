namespace Ecng.AotSmoke;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

using Ecng.Common;
using Ecng.Serialization;

using Newtonsoft.Json;

internal sealed record QuoteSnapshot(decimal Price, decimal Volume);

[JsonSerializable(typeof(QuoteSnapshot))]
internal partial class AotJsonContext : JsonSerializerContext
{
}

#pragma warning disable CS0618 // Test compatibility with existing persistable settings storage.
internal sealed class StoredQuote : IPersistable
{
	public int Quantity { get; private set; }
	public void Load(SettingsStorage storage) => Quantity = storage.GetValue<int>("quantity");
	public void Save(SettingsStorage storage) => storage.Set("quantity", Quantity);
}
#pragma warning restore CS0618

internal sealed class WireQuoteRow
{
	public decimal Price;
	public decimal Volume;
}

internal sealed class PrivateAotCtor
{
	private PrivateAotCtor() { }
	public int Value { get; private set; }
	public static PrivateAotCtor Create(int value) => new() { Value = value };
}

internal static class Program
{
	private static async Task Main()
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

		// Existing array allocation and generic construction paths, not only new APIs.
		ArrayHelper.RegisterArrayType<QuoteSnapshot>();
		var typedArray = typeof(QuoteSnapshot).CreateArray(2);
		if (typedArray is not QuoteSnapshot[] || typedArray.Length != 2)
			throw new InvalidOperationException("Registered array allocation failed.");

		ArrayHelper.RegisterArrayType<int>();
		TypeHelper.RegisterConstructedGenericType(typeof(List<int>));
		TypeHelper.RegisterFactory<List<int>>(_ => new List<int>());
		var listType = typeof(List<>).Make(typeof(int));
		if (listType.CreateInstance() is not List<int>)
			throw new InvalidOperationException("Registered list factory failed.");

		FastActivator<PrivateAotCtor>.RegisterFactory(() => PrivateAotCtor.Create(9));
		if (FastActivator<PrivateAotCtor>.CreateObject().Value != 9)
			throw new InvalidOperationException("FastActivator override failed.");

		TypeHelper.RegisterFactory<StoredQuote>(_ => new StoredQuote());
		var storage = new SettingsStorage()
			.Set("quote", new SettingsStorage().Set("quantity", 7));
		if (storage.GetValue<StoredQuote>("quote").Quantity != 7)
			throw new InvalidOperationException("Existing SettingsStorage factory path failed.");

		// Exercise the existing Ecng JSON collection deserializer with the registered
		// List<T> and array factory. This uses the legacy wire format on purpose.
		var legacySerializer = new Ecng.Serialization.JsonSerializer<List<int>>();
		using var stream = new MemoryStream();
		await legacySerializer.SerializeAsync(new List<int> { 1, 2, 3 }, stream, default);
		stream.Position = 0;
		var restoredList = await legacySerializer.DeserializeAsync(stream, default);
		if (restoredList.Count != 3 || restoredList[0] != 1 || restoredList[2] != 3)
			throw new InvalidOperationException("Existing collection serializer path failed.");

		// The legacy Newtonsoft field setter must not attempt Reflection.Emit on AOT.
		using var reader = new JsonTextReader(new StringReader("[101.25,2.5]"));
		reader.Read();
		var row = (WireQuoteRow)new JArrayToObjectConverter<WireQuoteRow>()
			.ReadJson(reader, typeof(WireQuoteRow), null, Newtonsoft.Json.JsonSerializer.CreateDefault());
		if (row.Price != 101.25m || row.Volume != 2.5m)
			throw new InvalidOperationException("Legacy JSON-array field setter path failed.");

		var dynamicTypeInfo = (System.Text.Json.Serialization.Metadata.JsonTypeInfo)AotJsonContext.Default.QuoteSnapshot;
		var metadataJson = ((object)quote).ToJson(dynamicTypeInfo);
		if ((QuoteSnapshot)metadataJson.FromJson(dynamicTypeInfo) != quote)
			throw new InvalidOperationException("Non-generic source-generated JSON failed.");

		Console.WriteLine("Ecng Native AOT smoke test passed.");
	}
}
