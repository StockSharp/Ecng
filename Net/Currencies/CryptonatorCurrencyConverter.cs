namespace Ecng.Net.Currencies
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net.Http;
	using System.Threading;
	using System.Threading.Tasks;

	using Ecng.Common;

	using Nito.AsyncEx;

	public class CryptonatorCurrencyConverter : ICurrencyConverter
	{
		private readonly Dictionary<DateTime, Dictionary<(CurrencyTypes, CurrencyTypes), decimal>> _rateInfo = new();
		private readonly AsyncLock _mutex = new();

		async Task<decimal> ICurrencyConverter.ConvertAsync(CurrencyTypes from, CurrencyTypes to, DateTime date, CancellationToken cancellationToken)
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

			if (_rateInfo.TryGetValue(date, out var dict) && dict.TryGetValue((from, to), out var rate))
				return rate;

			_rateInfo.Add(date, dict = new());

			var client = new HttpClient();
			var response = await client.GetAsync($"https://api.cryptonator.com/api/ticker/{from}-{to}", cancellationToken);

			response.EnsureSuccessStatusCode();

			dynamic obj = await response.Content.ReadAsAsync<object>(cancellationToken);

			rate = (decimal)obj.ticker.price;

			dict[(from, to)] = rate;

			return rate;
		}
	}
}