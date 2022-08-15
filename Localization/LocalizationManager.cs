namespace Ecng.Localization
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Collections.Generic;
	using System.Globalization;

	using Ecng.Common;

	using Newtonsoft.Json;

	public class LocalizationManager
	{
		public class Translation
		{
			private readonly Dictionary<string, string> _stringsById = new();
			private readonly Dictionary<string, string> _idsByString = new();

			public string LangCode { get; }

			public IEnumerable<(string id, string text)> Strings => _stringsById.Select(kv => (kv.Key, kv.Value));

			public Translation(string code)
			{
				LangCodes.GetId(code, true);
				LangCode = code;
			}

			public void Add(string id, string text)
			{
				_stringsById.Add(id, text);
				_idsByString[text] = id;
			}

			public string GetTextById(string id) => _stringsById.TryGetValue(id, out var text) ? text : null;
			public string GetIdByText(string text) => _idsByString.TryGetValue(text, out var id) ? id : null;
		}

		private readonly List<Translation> _translations = new();

		public IEnumerable<Translation> Translations => _translations;

		public string ActiveLanguage { get; set; } = LangCodes.En;

		public event Action<string, bool> Missing;

		public LocalizationManager() => _translations.AddRange(LangCodes.Codes.Select(c => (code: c, id: LangCodes.GetId(c))).OrderBy(t => t.id).Select(t => new Translation(t.code)));

		public void Init(TextReader reader)
		{
			if (reader is null)
				throw new ArgumentNullException(nameof(reader));

			using (var jsonReader = new JsonTextReader(reader))
			{
				var resources = new JsonSerializer().Deserialize<IDictionary<string, IDictionary<string, string>>>(jsonReader);

				// ReSharper disable once PossibleNullReferenceException
				foreach (var res in resources)
				{
					var resId = res.Key;
					if (resId.IsEmpty())
						throw new LocalizationException($"{res}: Empty key found.");

					var translations = res.Value;
					foreach (var kv in translations)
						_translations[LangCodes.GetId(kv.Key, true)].Add(resId, kv.Value);
				}
			}

			var currCulture = CultureInfo.CurrentCulture.Name;

			if (!currCulture.IsEmpty() && currCulture.Contains('-'))
			{
				currCulture = currCulture.SplitBySep("-").First().ToLowerInvariant();

				if (LangCodes.GetId(currCulture) >= 0)
					ActiveLanguage = currCulture;
			}
		}

		public string GetString(string resourceId, string language = null)
		{
			if (language.IsEmpty())
				language = ActiveLanguage;

			var langId = LangCodes.GetId(language);
			if (langId < 0)
			{
				Missing?.Invoke(resourceId, false);
				return resourceId;
			}

			var result = _translations[langId].GetTextById(resourceId);
			if (result != null)
				return result;

			Missing?.Invoke(resourceId, false);
			return resourceId;
		}

		public string Translate(string text, string from, string to)
		{
			if (from.IsEmpty())
				throw new ArgumentNullException(nameof(from));

			if (from == to)
				return text;

			var langIdFrom = LangCodes.GetId(from);
			var langIdTo   = LangCodes.GetId(to);

			if (langIdFrom < 0 || langIdTo < 0)
			{
				Missing?.Invoke(text, true);
				return text;
			}

			var id = _translations[langIdFrom].GetIdByText(text);
			if (id.IsEmpty())
			{
				Missing?.Invoke(text, true);
				return text;
			}

			var result = _translations[langIdTo].GetTextById(id);
			if (result.IsEmpty())
			{
				Missing?.Invoke(text, true);
				return text;
			}

			return result;
		}

		public string GetResourceId(string text, string language = LangCodes.En)
		{
			if (language.IsEmpty())
				throw new ArgumentNullException(nameof(language));

			var langid = LangCodes.GetId(language);
			if (langid < 0)
				return null;

			return _translations[langid].GetIdByText(text);
		}
	}
}