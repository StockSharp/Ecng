﻿namespace Ecng.Localization
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Collections.Generic;

	using Ecng.Common;

	abstract public class LocalizedStringsBase
	{
		public enum Language : byte
		{
			English,
			Russian
		}

		private static byte _activeLanguage;

		public static Language ActiveLanguage
		{
			get
			{
				return (Language)_activeLanguage;
			}

			set
			{
				var newLang = (byte)value;

				if(newLang >= _numLanguages)
					throw new InvalidOperationException("Invalid language");

				_activeLanguage = newLang;
			}
		}

		private static readonly Dictionary<string, string[]> _strings = new Dictionary<string, string[]>();

		private static bool _initialized;

		static readonly int _numLanguages = Enum.GetValues(typeof(Language)).Length;

		protected LocalizedStringsBase(Assembly asmHolder, string fileName)
		{
			lock (typeof(LocalizedStringsBase))
			{
				if (asmHolder == null)
					throw new ArgumentNullException("asmHolder");

				if (fileName.IsEmpty())
					throw new ArgumentNullException("fileName");

				var ex1 = ProcessCsvStream("embedded", () => asmHolder.GetManifestResourceStream(ConvertPathToResourceName(asmHolder, fileName)));
				var ex2 = File.Exists(fileName) ? ProcessCsvStream("file", () => File.OpenRead(fileName)) : new Exception("file not found");

				if(ex1 != null && ex2 != null)
					throw new AggregateException("Unable to load string resources", ex1, ex2);
			}

			_initialized = true;
		}

		private static Exception ProcessCsvStream(string name, Func<Stream> getCsvStream)
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
						while(reader.ReadRow(list))
						{
							if (list.Count < 2 || list.Count > _numLanguages + 1)
								throw new LocalizedStringsException("{0}: Unexpected number of columns in CSV ({1}): {2}".Put(name, list.Count, list.Join("|")));

							var key = list[0];
							if (key.IsEmpty())
								throw new LocalizedStringsException("{0}: Empty key found".Put(name));

							if (!names.Add(key))
								throw new LocalizedStringsException("{0}: Duplicated key({1}) found".Put(name, key));

							string[] arr;
							_strings.TryGetValue(key, out arr);

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
			catch (LocalizedStringsException)
			{
				throw;
			}
			catch (Exception e)
			{
				return e;
			}

			return null;
		}

		private static string ConvertPathToResourceName(Assembly assembly, string fileName)
		{
			return "{0}.{1}".Put(assembly.GetName().Name, Path.GetFileName(fileName));
		}

		protected static string GetString(string resourceId)
		{
			if(!_initialized)
				throw new InvalidOperationException("Localized strings were not initialized.");

			string[] arr;
			_strings.TryGetValue(resourceId, out arr);

			return arr == null ? resourceId : arr[_activeLanguage];
		}
	}

	public class LocalizedStringsException : ApplicationException
	{
		public LocalizedStringsException(string message) : base(message) {}
	}
}
