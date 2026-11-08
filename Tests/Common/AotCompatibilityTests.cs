namespace Ecng.Tests.Common;

using System.Text.Json.Serialization;

using Ecng.Serialization;

[TestClass]
public class AotCompatibilityTests : BaseTestClass
{
	public sealed record SampleOrder(int Quantity, decimal Price);

	private sealed class FactoryProduct(int value)
	{
		public int Value { get; } = value;
	}

	[TestMethod]
	public void ExplicitTypeNameCanResolveWithoutReflection()
	{
		const string name = "ecng.aot.sample-order";

		Converter.RegisterTypeName<SampleOrder>(name);

		name.To<Type>().AssertEqual(typeof(SampleOrder));
		name.ToUpperInvariant().To<Type>().AssertEqual(typeof(SampleOrder));
		// Registering the same mapping twice is idempotent.
		Converter.RegisterTypeName(name, typeof(SampleOrder));
		ThrowsExactly<InvalidOperationException>(() => Converter.RegisterTypeName(name, typeof(int)));
	}

	[TestMethod]
	public void ExplicitFactoryIsUsedBeforeActivator()
	{
		TypeHelper.RegisterFactory<FactoryProduct>(args => new FactoryProduct((int)args[0]));

		var result = typeof(FactoryProduct).CreateInstance<FactoryProduct>(17);
		result.Value.AssertEqual(17);

		ThrowsExactly<InvalidOperationException>(() =>
			TypeHelper.RegisterFactory<FactoryProduct>(_ => new FactoryProduct(0)));
	}

	[TestMethod]
	public void RegisteredClosedGenericTypeCanBeResolved()
	{
		TypeHelper.RegisterConstructedGenericType(typeof(Dictionary<string, SampleOrder>));

		var result = typeof(Dictionary<,>).Make(typeof(string), typeof(SampleOrder));
		result.AssertEqual(typeof(Dictionary<string, SampleOrder>));

		// Duplicate registration is harmless.
		TypeHelper.RegisterConstructedGenericType(typeof(Dictionary<string, SampleOrder>));
		ThrowsExactly<ArgumentException>(() => TypeHelper.RegisterConstructedGenericType(typeof(Dictionary<,>)));
	}

	private sealed class ArrayPayload
	{
	}

	private sealed class PrivateConstructor
	{
		private PrivateConstructor() { }
		public int Value { get; private set; }
		public static PrivateConstructor Create(int value) => new() { Value = value };
	}

#pragma warning disable CS0618 // Verify the existing legacy persistence path.
	private sealed class PersistedPayload : IPersistable
	{
		public int Value { get; private set; }
		public void Load(SettingsStorage storage) => Value = storage.GetValue<int>("value");
		public void Save(SettingsStorage storage) => storage.Set("value", Value);
	}
#pragma warning restore CS0618

	[TestMethod]
	public void RegisteredArrayFactoryIsUsedByExistingCreateArray()
	{
		var callCount = 0;
		ArrayHelper.RegisterArrayFactory<ArrayPayload>(length =>
		{
			callCount++;
			return new ArrayPayload[length];
		});

		var result = typeof(ArrayPayload).CreateArray(3);
		(result is ArrayPayload[]).AssertTrue();
		result.Length.AssertEqual(3);
		callCount.AssertEqual(1);
	}

	[TestMethod]
	public void ExistingFastActivatorSupportsExplicitFactories()
	{
		FastActivator<PrivateConstructor>.RegisterFactory(() => PrivateConstructor.Create(42));
		FastActivator<PrivateConstructor>.CreateObject().Value.AssertEqual(42);
	}

	[TestMethod]
	public void ExistingSettingsStorageUsesRegisteredFactory()
	{
		TypeHelper.RegisterFactory<PersistedPayload>(_ => new PersistedPayload());

		var nested = new SettingsStorage().Set("value", 42);
		var storage = new SettingsStorage().Set("payload", nested);

		storage.GetValue<PersistedPayload>("payload").Value.AssertEqual(42);
	}

	[TestMethod]
	public void SourceGeneratedNonGenericJsonRoundTrip()
	{
		var original = new SampleOrder(4, 112.50m);
		System.Text.Json.Serialization.Metadata.JsonTypeInfo metadata = TestAotJsonContext.Default.SampleOrder;
		var json = ((object)original).ToJson(metadata);
		var restored = (SampleOrder)json.FromJson(metadata);
		restored.AssertEqual(original);
	}

	[TestMethod]
	public void SourceGeneratedJsonRoundTrip()
	{
		var order = new SampleOrder(5, 101.25m);
		var json = order.ToJson(TestAotJsonContext.Default.SampleOrder);
		var restored = json.FromJson(TestAotJsonContext.Default.SampleOrder);

		restored.AssertEqual(order);
	}
}

[JsonSerializable(typeof(AotCompatibilityTests.SampleOrder))]
internal partial class TestAotJsonContext : JsonSerializerContext
{
}
