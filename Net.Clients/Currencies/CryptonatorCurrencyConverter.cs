namespace Ecng.Net.Currencies;

using Nito.AsyncEx;

[Obsolete("Use CoinvertCurrencyConverter.")]
public class CryptonatorCurrencyConverter : ICurrencyConverter
{
	private readonly Dictionary<DateTime, Dictionary<(CurrencyTypes, CurrencyTypes), decimal>> _rateInfo = new();
	private readonly AsyncLock _mutex = new();
	private readonly HttpClient _client;

	public CryptonatorCurrencyConverter(HttpClient client)
	{
		_client = client ?? throw new ArgumentNullException(nameof(client));
	}

	async Task<decimal> ICurrencyConverter.GetRateAsync(CurrencyTypes from, CurrencyTypes to, DateTime date, CancellationToken cancellationToken)
	{
		if (from == to)
			return 1;

		date = DateTime.Now.Truncate(TimeSpan.FromHours(1));

		using var _ = await _mutex.LockAsync(cancellationToken);

		if (_rateInfo.Count > 0)
		{
			foreach (var key in _rateInfo.Keys.Where(k => k < date).ToArray())
				_rateInfo.Remove(key);
		}

		if (_rateInfo.TryGetValue(date, out var dict))
		{
			if (dict.TryGetValue((from, to), out var rate1))
				return rate1;
		}
		else
			_rateInfo.Add(date, dict = new());

		using var response = await _client.GetAsync($"https://api.cryptonator.com/api/ticker/{from}-{to}", cancellationToken);

		response.EnsureSuccessStatusCode();

		dynamic obj = await response.Content.ReadAsAsync<object>(cancellationToken);

		var rate = (decimal)obj.ticker.price;

		dict[(from, to)] = rate;

		return rate;
	}
}