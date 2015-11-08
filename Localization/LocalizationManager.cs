namespace Ecng.Localization
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.Collections;

	public enum Languages
	{
		English,
		Russian,
		Chinese,
		Indian,
	}

	public class LocalizationManager
	{
		private readonly Dictionary<string, string[]> _stringByResourceId = new Dictionary<string, string[]>();
		private readonly Dictionary<Tuple<Languages, string>, Dictionary<Languages, string>> _stringsByLang = new Dictionary<Tuple<Languages, string>, Dictionary<Languages, string>>();

		public LocalizationManager(Assembly asmHolder, string fileName)
		{
			if (asmHolder == null)
				throw new ArgumentNullException(nameof(asmHolder));

			if (fileName.IsEmpty())
				throw new ArgumentNullException(nameof(fileName));

			var ex1 = ProcessCsvStream("embedded", () => asmHolder.GetManifestResourceStream("{0}.{1}".Put(asmHolder.GetName().Name, Path.GetFileName(fileName))));

			var ex2 = File.Exists(fileName)
				? ProcessCsvStream("file", () => File.OpenRead(fileName))
				: new InvalidOperationException("File {0} not found.".Put(fileName));

			if (ex1 != null && ex2 != null)
				throw new AggregateException("Unable to load string resources.", ex1, ex2);
		}

		public Languages ActiveLanguage { get; set; }

		private Exception ProcessCsvStream(string name, Func<Stream> getCsvStream)
		{
			try
			{
				var names = new HashSet<string>();

				using (var stream = getCsvStream())
				{
					if (stream == null)
						return new Exception("stream is null");

					var row = new List<string>();

					//var langCount = Enumerator.GetValues<Languages>().Count();

					using (var reader = new CsvFileReader(stream, EmptyLineBehavior.Ignore))
					{
						while (reader.ReadRow(row))
						{
							if (row.Count < 2/* || list.Count > (langCount + 1)*/)
								throw new LocalizationException("{0}: Unexpected number of columns in CSV ({1}): {2}".Put(name, row.Count, row.Join("|")));

							var key = row[0];
							//if (key.IsEmpty())
							//	throw new LocalizationException("{0}: Empty key found.".Put(name));

							string[] stringByResourceId = null;

							if (!key.IsEmpty())
							{
								if (!names.Add(key))
									throw new LocalizationException("{0}: Duplicated key({1}) found.".Put(name, key));

								stringByResourceId = _stringByResourceId.TryGetValue(key);

								if (stringByResourceId == null)
									_stringByResourceId.Add(key, stringByResourceId = new string[row.Count - 1]);

								//var fallback = row.Skip(1).FirstOrDefault(s => !s.IsEmpty()) ?? key;	
							}
							

							var i = 0;

							var tuples = new List<Tuple<Languages, string>>();

							foreach (var cell in row.Skip(1))
							{
								//if (i + 1 < row.Count)
								//	arr[i] = row[i + 1].Trim() != string.Empty ? row[i + 1] : (!arr[i].IsEmpty() ? arr[i] : fallback);
								//else
								//	arr[i] = !arr[i].IsEmpty() ? arr[i] : fallback;

								if (stringByResourceId != null)
									stringByResourceId[i] = cell;

								tuples.Add(Tuple.Create((Languages)i, cell));

								i++;
							}

							for (var j = 0; j < tuples.Count; j++)
							{
								var dict = tuples
									.Where((t, k) => k != j)
									.ToDictionary(tuple => tuple.Item1, tuple => tuple.Item2);

								_stringsByLang[tuples[j]] = dict;
							}
						}
					}
				}
			}
			catch (LocalizationException)
			{
				throw;
			}
			catch (Exception e)
			{
				return e;
			}

			return null;
		}

		public event Action<string, bool> Missing;

		public string GetString(string resourceId, Languages? language = null)
		{
			var arr = _stringByResourceId.TryGetValue(resourceId);

			if (arr == null)
			{
				Missing.SafeInvoke(resourceId, false);
				return resourceId;
			}

			var index = (int)(language ?? ActiveLanguage);

			if (index < arr.Length)
				return arr[index];

			Missing.SafeInvoke(resourceId, false);
			return resourceId;
		}

		public string Translate(string text, Languages from, Languages to)
		{
			var dict = _stringsByLang.TryGetValue(Tuple.Create(from, text));

			if (dict == null)
			{
				Missing.SafeInvoke(text, true);
				return text;
			}

			var translate = dict.TryGetValue(to);

			if (translate != null)
				return translate;

			Missing.SafeInvoke(text, true);
			return text;
		}
	}
}