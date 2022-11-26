namespace Ecng.Net.Currencies;

public interface ICurrencyConverter
{
	Task<decimal> GetRateAsync(CurrencyTypes from, CurrencyTypes to, DateTime date, CancellationToken cancellationToken = default);
}