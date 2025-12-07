namespace Ecng.Tests.Common;

[TestClass]
public class CurrencyTests : BaseTestClass
{
	[TestMethod]
	public void CheckCrypto()
	{
		CurrencyTypes.ADA.IsCrypto().AssertTrue();
		CurrencyTypes.BTC.IsCrypto().AssertTrue();
		CurrencyTypes.ETH.IsCrypto().AssertTrue();
		CurrencyTypes.USDT.IsCrypto().AssertTrue();

		CurrencyTypes.USD.IsCrypto().AssertFalse();
		CurrencyTypes.RUB.IsCrypto().AssertFalse();
		CurrencyTypes.EUR.IsCrypto().AssertFalse();
		CurrencyTypes.MKD.IsCrypto().AssertFalse();
	}

	[TestMethod]
	public void InconsistMath1()
	{
		var v1 = 10m.ToCurrency(CurrencyTypes.USD);
		var v2 = 1.8m.ToCurrency(CurrencyTypes.EUR);

		ThrowsExactly<InvalidOperationException>(() => (v1 * v2).AssertEqual(null));
	}

	[TestMethod]
	public void InconsistMath2()
	{
		var v1 = 10m.ToCurrency(CurrencyTypes.USD);
		var v2 = 1.8m.ToCurrency(CurrencyTypes.EUR);

		ThrowsExactly<InvalidOperationException>(() => (v1 - v2).AssertEqual(null));
	}

	[TestMethod]
	public void InconsistMath3()
	{
		var v1 = 10m.ToCurrency(CurrencyTypes.USD);
		var v2 = 1.8m.ToCurrency(CurrencyTypes.EUR);

		ThrowsExactly<InvalidOperationException>(() => (v1 + v2).AssertEqual(null));
	}

	[TestMethod]
	public void InconsistMath4()
	{
		var v1 = 10m.ToCurrency(CurrencyTypes.USD);
		var v2 = 1.8m.ToCurrency(CurrencyTypes.EUR);

		ThrowsExactly<InvalidOperationException>(() => (v1 / v2).AssertEqual(null));
	}

	[TestMethod]
	public void Math1()
	{
		var v1 = 10m.ToCurrency(CurrencyTypes.USD);
		var v2 = 1.8m.ToCurrency(CurrencyTypes.USD);

		(v1 * v2).AssertEqual((v1.Value * v2.Value).ToCurrency(v1.Type));
	}

	[TestMethod]
	public void Math2()
	{
		var v1 = 10m.ToCurrency(CurrencyTypes.USD);
		var v2 = 1.8m.ToCurrency(CurrencyTypes.USD);

		(v1 - v2).AssertEqual((v1.Value - v2.Value).ToCurrency(v1.Type));
	}

	[TestMethod]
	public void Math3()
	{
		var v1 = 10m.ToCurrency(CurrencyTypes.USD);
		var v2 = 1.8m.ToCurrency(CurrencyTypes.USD);

		(v1 + v2).AssertEqual((v1.Value + v2.Value).ToCurrency(v1.Type));
	}

	[TestMethod]
	public void Math4()
	{
		var v1 = 10m.ToCurrency(CurrencyTypes.USD);
		var v2 = 1.8m.ToCurrency(CurrencyTypes.USD);

		(v1 / v2).AssertEqual((v1.Value / v2.Value).ToCurrency(v1.Type));
	}
}