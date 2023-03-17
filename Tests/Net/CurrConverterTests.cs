namespace Ecng.Tests.Net
{
	using System;
	using System.Threading.Tasks;

	using Ecng.Common;
	using Ecng.Net.Currencies;
	using Ecng.UnitTesting;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class CurrConverterTests
	{
		[TestMethod]
		public async Task FloatRates()
		{
			Exception err = null;
			
			ICurrencyConverter converter = new FloatRatesCurrencyConverter(Config.HttpClient, err1 => err = err1);
			var rate = await converter.GetRateAsync(CurrencyTypes.EUR, CurrencyTypes.RUB, DateTime.Today);
			(rate > 50).AssertTrue();
			(rate < 150).AssertTrue();

			rate = await converter.GetRateAsync(CurrencyTypes.EUR, CurrencyTypes.RUB, DateTime.Today);

			err.AssertNull();

			(rate > 50).AssertTrue();
			(rate < 150).AssertTrue();
		}

		// now it is under cloudflare

		//[TestMethod]
		//public async Task Cryptonator()
		//{
		//	ICurrencyConverter converter = new CryptonatorCurrencyConverter(Config.HttpClient);
		//	var rate = await converter.GetRateAsync(CurrencyTypes.BTC, CurrencyTypes.USD, DateTime.Today);
		//	(rate > 1000).AssertTrue();
		//	(rate < 100000).AssertTrue();

		//	rate = await converter.GetRateAsync(CurrencyTypes.BTC, CurrencyTypes.USD, DateTime.Today);

		//	(rate > 1000).AssertTrue();
		//	(rate < 100000).AssertTrue();
		//}

		[TestMethod]
		public async Task Coinvert()
		{
			ICurrencyConverter converter = new CoinvertCurrencyConverter(Config.HttpClient);
			
			var rate = await converter.GetRateAsync(CurrencyTypes.BTC, CurrencyTypes.USD, DateTime.Today);
			(rate > 1000).AssertTrue();
			(rate < 100000).AssertTrue();

			rate = await converter.GetRateAsync(CurrencyTypes.BTC, CurrencyTypes.USD, DateTime.Today);
			(rate > 1000).AssertTrue();
			(rate < 100000).AssertTrue();

			rate = await converter.GetRateAsync(CurrencyTypes.USD, CurrencyTypes.BTC, DateTime.Today);
			(rate > 0.0000001m).AssertTrue();
			(rate < 0.0001m).AssertTrue();

			rate = await converter.GetRateAsync(CurrencyTypes.USD, CurrencyTypes.ETH, DateTime.Today);
			(rate > 0.00001m).AssertTrue();
			(rate < 0.001m).AssertTrue();

			rate = await converter.GetRateAsync(CurrencyTypes.ETH, CurrencyTypes.BTC, DateTime.Today);
			(rate > 0.01m).AssertTrue();
			(rate < 0.1m).AssertTrue();
			
			rate = await converter.GetRateAsync(CurrencyTypes.ETH, CurrencyTypes.USD, DateTime.Today);
			(rate > 100).AssertTrue();
			(rate < 10000).AssertTrue();
		}
	}
}
