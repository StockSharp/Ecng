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
