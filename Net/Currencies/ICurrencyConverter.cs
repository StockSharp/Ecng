namespace Ecng.Net.Currencies
{
	using System;
	using System.Threading;
	using System.Threading.Tasks;

	using Ecng.Common;

	public interface ICurrencyConverter
	{
		Task<decimal> GetRateAsync(CurrencyTypes from, CurrencyTypes to, DateTime date, CancellationToken cancellationToken = default);
	}
}