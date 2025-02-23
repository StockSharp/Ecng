namespace Ecng.Net.Currencies;

/// <summary>
/// Provides methods to convert one currency to another.
/// </summary>
public interface ICurrencyConverter
{
	/// <summary>
	/// Retrieves the conversion rate from one currency to another for a specified date.
	/// </summary>
	/// <param name="from">The source currency type.</param>
	/// <param name="to">The target currency type.</param>
	/// <param name="date">The date for which the conversion rate is applicable.</param>
	/// <param name="cancellationToken">A cancellation token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the conversion rate as a decimal.</returns>
	Task<decimal> GetRateAsync(CurrencyTypes from, CurrencyTypes to, DateTime date, CancellationToken cancellationToken = default);
}