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
		Russian
	}

	public class LocalizationManager
	{
		private readonly Dictionary<string, string[]> _strings = new Dictionary<string, string[]>();
		private readonly int _numLanguages = Enumerator.GetValues<Languages>().Count();

		public LocalizationManager(Assembly asmHolder, string fileName)
		{
			if (asmHolder == null)
				throw new ArgumentNullException("asmHolder");

			if (fileName.IsEmpty())
				throw new ArgumentNullException("fileName");

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

					var list = new List<string>();

					using (var reader = new CsvFileReader(stream, EmptyLineBehavior.Ignore))
					{
						while (reader.ReadRow(list))
						{
							if (list.Count < 2 || list.Count > _numLanguages + 1)
								throw new LocalizationException("{0}: Unexpected number of columns in CSV ({1}): {2}".Put(name, list.Count, list.Join("|")));

							var key = list[0];
							if (key.IsEmpty())
								throw new LocalizationException("{0}: Empty key found.".Put(name));

							if (!names.Add(key))
								throw new LocalizationException("{0}: Duplicated key({1}) found.".Put(name, key));

							var arr = _strings.TryGetValue(key);

							if (arr == null)
								_strings.Add(key, arr = new string[_numLanguages]);

							var fallback = list.Skip(1).FirstOrDefault(s => !s.IsEmpty()) ?? key;

							for (var i = 0; i < _numLanguages; ++i)
							{
								if (i + 1 < list.Count)
									arr[i] = list[i + 1].Trim() != string.Empty ? list[i + 1] : (!arr[i].IsEmpty() ? arr[i] : fallback);
								else
									arr[i] = !arr[i].IsEmpty() ? arr[i] : fallback;
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

		public string GetString(string resourceId)
		{
			var arr = _strings.TryGetValue(resourceId);
			return arr == null ? resourceId : arr[(int)ActiveLanguage];
		}
	}
}