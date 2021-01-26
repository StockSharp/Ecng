namespace Ecng.Localization
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Collections.Generic;
	using System.Globalization;

	using Ecng.Common;
	using Ecng.Collections;

	using Newtonsoft.Json;
	using Newtonsoft.Json.Linq;

	public class LocalizationManager
	{
		private readonly Dictionary<string, string[]> _stringByResourceId = new Dictionary<string, string[]>();
		private readonly Dictionary<string, int> _langIdx = new Dictionary<string, int>();
		private readonly Dictionary<(string lang, string text), Dictionary<string, string>> _stringsByLang = new Dictionary<(string lang, string text), Dictionary<string, string>>();
		private readonly Dictionary<(string lang, string text), string> _keysByLang = new Dictionary<(string lang, string text), string>();

		public string ActiveLanguage { get; set; } = LangCodes.En;

		public void Init(TextReader reader)
		{
			if (reader is null)
				throw new ArgumentNullException(nameof(reader));

			var currCulture = CultureInfo.CurrentCulture.Name;
			var currLang = currCulture.SplitBySep("-").First().ToLowerInvariant();

			using (var jsonReader = new JsonTextReader(reader))
			{
				var rows = new JsonSerializer().Deserialize<IDictionary<string, object>>(jsonReader);

				foreach (var row in rows)
				{
					var key = row.Key;
					if (key.IsEmpty())
						throw new LocalizationException($"{row}: Empty key found.");

					var props = ((JObject)row.Value).Properties().ToArray();

					if (!_stringByResourceId.TryGetValue(key, out var stringByResourceId))
						_stringByResourceId.Add(key, stringByResourceId = new string[props.Length]);

					var tuples = new List<(string, string)>();

					foreach (var prop in props)
					{
						var translation = (string)prop.Value;

						var lang = prop.Name;

						if (!_langIdx.TryGetValue(lang, out var i))
							_langIdx.Add(lang, i = _langIdx.Count);

						stringByResourceId[i] = translation;

						tuples.Add((lang, translation));
					}

					for (var j = 0; j < tuples.Count; j++)
					{
						var dict = tuples
							        .Where((t, k) => k != j)
							        .ToDictionary(tuple => tuple.Item1, tuple => tuple.Item2);

						var t1 = tuples[j];

						_stringsByLang[t1] = dict;

						if (!key.IsEmpty())
							_keysByLang[t1] = key;
					}
				}
			}

			if (_langIdx.ContainsKey(currLang))
				ActiveLanguage = currLang;
		}

		public event Action<string, bool> Missing;

		public string GetString(string resourceId, string language = null)
		{
			if (_stringByResourceId.TryGetValue(resourceId, out var arr))
			{
				if (language.IsEmpty())
					language = ActiveLanguage;

				if (_langIdx.TryGetValue(language, out var index) && index < arr.Length)
					return arr[index];
			}

			Missing?.Invoke(resourceId, false);
			return resourceId;
		}

		public string Translate(string text, string from, string to)
		{
			if (from.IsEmpty())
				throw new ArgumentNullException(nameof(from));

			if (_stringsByLang.TryGetValue((from, text), out var dict))
			{
				if (dict.TryGetValue(to, out var translate))
					return translate;
			}

			Missing?.Invoke(text, true);
			return text;
		}

		public string GetResourceId(string text, string language = LangCodes.En)
		{
			if (language.IsEmpty())
				throw new ArgumentNullException(nameof(language));

			return _keysByLang.TryGetValue((language, text));
		}
	}
}