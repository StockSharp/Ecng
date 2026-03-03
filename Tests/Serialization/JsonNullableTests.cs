namespace Ecng.Tests.Serialization;

using Ecng.Serialization;
using Ecng.ComponentModel;

[TestClass]
public class JsonNullableTests : BaseTestClass
{
	private struct NullableHolder : IEquatable<NullableHolder>, IPersistable
	{
		public int? I { get; set; }
		public DateTime? D { get; set; }
		public TimeSpan? T { get; set; }
		public Guid? G { get; set; }
		public decimal? M { get; set; }
		public Price? P { get; set; }

		public readonly bool Equals(NullableHolder other)
			=> I == other.I && D == other.D && T == other.T && G == other.G && M == other.M && P == other.P;
		public override readonly bool Equals(object obj) => obj is NullableHolder n && Equals(n);
		public override readonly int GetHashCode() => HashCode.Combine(I, D, T, G, M, P);

		void IPersistable.Load(SettingsStorage storage)
		{
			I = storage.GetValue<int?>(nameof(I));
			D = storage.GetValue<DateTime?>(nameof(D));
			T = storage.GetValue<TimeSpan?>(nameof(T));
			G = storage.GetValue<Guid?>(nameof(G));
			M = storage.GetValue<decimal?>(nameof(M));
			P = storage.GetValue<Price?>(nameof(P));
		}

		readonly void IPersistable.Save(SettingsStorage storage)
		{
			storage
				.Set(nameof(I), I)
				.Set(nameof(D), D)
				.Set(nameof(T), T)
				.Set(nameof(G), G)
				.Set(nameof(M), M)
				.Set(nameof(P), P);
		}
	}

	private async Task<T> Roundtrip<T>(T value, Action<string> jsonInspector = null)
	{
		var ser = new JsonSerializer<T>();
		await using var ms = new MemoryStream();
		await ser.SerializeAsync(value, ms, CancellationToken);

		// Verify intermediate format has content
		ms.Length.AssertGreater(0L, "Serialized JSON stream should not be empty");

		// Capture JSON for inspection
		ms.Position = 0;
		var jsonString = ms.ToArray().UTF8();
		jsonInspector?.Invoke(jsonString);

		ms.Position = 0;
		return await ser.DeserializeAsync(ms, CancellationToken);
	}

	[TestMethod]
	public async Task AllSet()
	{
		var obj = new NullableHolder
		{
			I = 7,
			D = new DateTime(2020, 1, 2, 3, 4, 5, 123, DateTimeKind.Utc),
			T = TimeSpan.FromMilliseconds(7654),
			G = Guid.NewGuid(),
			M = 123.45m,
			P = new Price(10m, PriceTypes.Absolute),
		};

		var actual = await Roundtrip(obj);
		actual.Equals(obj).AssertTrue();
	}

	[TestMethod]
	public async Task AllNull()
	{
		var obj = new NullableHolder { I = null, D = null, T = null, G = null, M = null, P = null };
		var actual = await Roundtrip(obj, jsonInspector: json =>
		{
			// Verify JSON contains property names (fields are serialized, not omitted)
			json.Contains("\"I\"").AssertTrue($"JSON should contain 'I' property. JSON: {json}");
			// Verify null values are explicitly null in JSON
			json.Contains("null").AssertTrue($"JSON should contain null values. JSON: {json}");
		});
		actual.Equals(obj).AssertTrue();
	}

	[TestMethod]
	public async Task SettingsStorage()
	{
		var dt = new DateTime(2021, 2, 3, 4, 5, 6, 789, DateTimeKind.Utc);
		var guid = Guid.NewGuid();
		var price = new Price(77.7m, PriceTypes.Absolute);

		var storage = new SettingsStorage();
		storage.Set("I", (int?)7);
		storage.Set("I_null", (int?)null);
		storage.Set("D", (DateTime?)dt);
		storage.Set("D_null", (DateTime?)null);
		storage.Set("T", (TimeSpan?)TimeSpan.FromSeconds(7));
		storage.Set("T_null", (TimeSpan?)null);
		storage.Set("G", (Guid?)guid);
		storage.Set("G_null", (Guid?)null);
		storage.Set("M", (decimal?)123.45m);
		storage.Set("M_null", (decimal?)null);
		storage.Set("P", (Price?)price);
		storage.Set("P_null", (Price?)null);

		var ser = new JsonSerializer<SettingsStorage>();
		await using var ms = new MemoryStream();
		await ser.SerializeAsync(storage, ms, CancellationToken);
		ms.Position = 0;
		var storage2 = await ser.DeserializeAsync(ms, CancellationToken);

		storage2.GetValue<int?>("I").AssertEqual(7);
		storage2.GetValue<int?>("I_null").AssertNull();
		storage2.GetValue<DateTime?>("D").AssertEqual(dt);
		storage2.GetValue<DateTime?>("D_null").AssertNull();
		storage2.GetValue<TimeSpan?>("T").AssertEqual(TimeSpan.FromSeconds(7));
		storage2.GetValue<TimeSpan?>("T_null").AssertNull();
		storage2.GetValue<Guid?>("G").AssertEqual(guid);
		storage2.GetValue<Guid?>("G_null").AssertNull();
		storage2.GetValue<decimal?>("M").AssertEqual(123.45m);
		storage2.GetValue<decimal?>("M_null").AssertNull();
		storage2.GetValue<Price?>("P").AssertEqual((Price?)price);
		storage2.GetValue<Price?>("P_null").AssertNull();

		// TryGet defaults
		storage2.TryGet<int?>("MissingInt", 100).AssertEqual(100);
		((int?)null).AssertEqual(storage2.TryGet<int?>("I_null"));
	}
}
