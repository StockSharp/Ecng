namespace Ecng.Net.Currencies
{
	using System;
	using System.Linq;
	using System.Net.Http;
	using System.Collections.Generic;
	using System.Threading;
	using System.Threading.Tasks;

	using Ecng.Common;

	using Nito.AsyncEx;

	using Newtonsoft.Json;

	public class FloatRatesCurrencyConverter : ICurrencyConverter
	{
		private class CurrInfo
		{
			[JsonProperty("code")]
			public string Code { get; set; }

			[JsonProperty("alphaCode")]
			public string AlphaCode { get; set; }

			[JsonProperty("numericCode")]
			public int NumericCode { get; set; }

			[JsonProperty("name")]
			public string Name { get; set; }

			[JsonProperty("rate")]
			public double Rate { get; set; }

			[JsonProperty("date")]
			public string Date { get; set; }

			[JsonProperty("inverseRate")]
			public double InverseRate { get; set; }
		}

		private readonly Dictionary<DateTime, Dictionary<(CurrencyTypes, CurrencyTypes), decimal>> _rateInfo = new();
		private readonly AsyncLock _mutex = new();
		private readonly Action<Exception> _currParseError;

		public FloatRatesCurrencyConverter(Action<Exception> currParseError)
		{
			_currParseError = currParseError ?? throw new ArgumentNullException(nameof(currParseError));
		}

		async Task<decimal> ICurrencyConverter.ConvertAsync(CurrencyTypes from, CurrencyTypes to, DateTime date, CancellationToken cancellationToken)
		{
			if (from == to)
				return 1;

			date = DateTime.Today;

			using var _ = await _mutex.LockAsync(cancellationToken);

			if (_rateInfo.Count > 0)
			{
				foreach (var key in _rateInfo.Keys.Where(k => k < date).ToArray())
					_rateInfo.Remove(key);
			}

			if (_rateInfo.TryGetValue(date, out var dict))
			{
				if (dict.TryGetValue((from, to), out var rate))
					return rate;
			}

			_rateInfo.Add(date, dict = new());

			using var client = new HttpClient();
			using var response = await client.GetAsync($"https://floatrates.com/daily/{from}.json".ToLowerInvariant(), cancellationToken);

			response.EnsureSuccessStatusCode();

			var respDict = await response.Content.ReadAsAsync<IDictionary<string, CurrInfo>>(cancellationToken);

			foreach (var pair in respDict)
			{
				CurrencyTypes curr;

				try
				{
					curr = pair.Key.To<CurrencyTypes>();
				}
				catch (Exception ex)
				{
					_currParseError(ex);
					continue;
				}

				dict[(from, curr)] = (decimal)pair.Value.Rate;
			}

			return dict[(from, to)];
		}
	}
}