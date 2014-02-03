namespace Ecng.Localization
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Text;

	using Ecng.Common;
	using Ecng.Collections;

	public class CsvLocalizer
	{
		private readonly CultureInfo _culture;
		private readonly SynchronizedDictionary<string, SynchronizedDictionary<string, string>> _localizationDic = new SynchronizedDictionary<string, SynchronizedDictionary<string, string>>();

		public CsvLocalizer(CultureInfo culture, Assembly asmHolder, string fileName, string splitter = ";")
		{
			if (culture == null)
				throw new ArgumentNullException("culture");

			if (asmHolder == null)
				throw new ArgumentNullException("asmHolder");

			if (fileName.IsEmpty())
				throw new ArgumentNullException("fileName");

			_culture = culture;

			var lines = new List<string>();

			using (var stream = File.Exists(fileName) ? File.OpenRead(fileName) : asmHolder.GetManifestResourceStream(Path.GetFileName(fileName)))
			{
				using (var reader = new StreamReader(stream, Encoding.UTF8))
				{
					while (!reader.EndOfStream)
					{
						var str = reader.ReadLine();
						
						if (str == null)
							break;

						str = str.Trim();

						if (str.IsEmpty())
							break;

						lines.Add(str);
					}
				}
			}

			var langNames = lines[0].Split(splitter, false).ToArray();

			foreach (var row in lines.Skip(1))
			{
				var cells = row.Split(splitter, false);

				var langDic = new SynchronizedDictionary<string, string>();

				for (var i = 1; i < langNames.Length; i++)
				{
					langDic.Add(langNames[i], cells[i]);
				}

				_localizationDic.Add(cells[0], langDic);
			}
		}

		/// <summary>
		/// Возвращает значение указанного ключа.
		/// </summary>
		/// <param name="key">Ключ.</param>
		/// <returns>Значение.</returns>
		public string GetString(string key)
		{
			if (key.IsEmpty())
				throw new ArgumentNullException("key");

			var letter = _culture.TwoLetterISOLanguageName;

			var dic = _localizationDic.TryGetValue(key);

			if (dic.IsNull())
				throw new InvalidOperationException("Key '{0}' not found.".Put(key));

			var value = dic.TryGetValue(letter);
			if (!value.IsEmpty())
				return value;

			const string defaultCulture = "en";

			var defValue = dic.TryGetValue(defaultCulture);
			if (!defValue.IsEmpty())
				return defValue;

			throw new InvalidOperationException("Value for key '{0}' not found.".Put(key));
		}
	}
}