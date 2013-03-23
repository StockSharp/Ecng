namespace Ecng.Web
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.Linq;
	using System.Text;

	using Ecng.Common;
	using Ecng.Reflection;
	using Ecng.Collections;

	public class QueryString : Equatable<QueryString>
	{
		private Dictionary<string, string> _queryString;

		private string _compiledString;

		internal QueryString(Url url)
			: this(url, url.Query.ParseUrl())
		{
		}

		internal QueryString(Url url, NameValueCollection queryString)
		{
			Url = url;

			_queryString = new Dictionary<string, string>(queryString.Count, StringComparer.InvariantCultureIgnoreCase);

			foreach (var key in queryString.Cast<string>().Where(k => !k.IsEmpty()))
				_queryString.Add(key, queryString[key]);

			_compiledString = url.Query;
		}

		public Url Url { get; private set; }

		public string Raw
		{
			get { return _queryString.Select(p => p.Key + "=" + p.Value).Join("&"); }
		}

		public bool Contains(string queryField)
		{
			return _queryString.ContainsKey(queryField);
		}

		public int Count
		{
			get { return _queryString.Count; }
		}

		public object this[string queryField]
		{
			get { return _queryString.TryGetValue(queryField); }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				if (Contains(queryField))
				{
					_queryString[queryField] = value.To<string>();
					RefreshUri();
				}
				else
					Append(queryField, value);
			}
		}

		public T GetValue<T>(string queryField)
		{
			return _queryString[queryField].To<T>();
		}

		public T TryGetValue<T>(string queryField)
		{
			return TryGetValue(queryField, default(T));
		}

		public T TryGetValue<T>(string queryField, T defaultValue)
		{
			return Contains(queryField) ? GetValue<T>(queryField) : defaultValue;
		}

		public QueryString Append(string name, object value)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException("name");

			if (value == null)
				throw new ArgumentNullException("value");

			_queryString.Add(name, value.To<string>());
			RefreshUri();
			return this;
		}

		public QueryString Remove(string name)
		{
			if (_queryString.Remove(name))
				RefreshUri();

			return this;
		}

		private void RefreshUri()
		{
			if (_queryString.IsEmpty())
				_compiledString = string.Empty;
			else
			{
				var queryString = new StringBuilder("?");

				foreach (var part in _queryString)
					queryString.AppendFormat("{0}={1}&", part.Key.EncodeUrl(), part.Value.EncodeUrl());

				queryString.Remove(queryString.Length - 1, 1);

				_compiledString = queryString.ToString();
			}

			Url.SetValue("CreateUri", new object[] { Url.Clone(), Url.LocalPath + _compiledString, false });
		}

		public override string ToString()
		{
			return _compiledString;
		}

		/// <summary>
		/// Creates a new object that is a copy of the current instance.
		/// </summary>
		/// <returns>
		/// A new object that is a copy of this instance.
		/// </returns>
		public override QueryString Clone()
		{
			return new QueryString(Url)
			{
				_queryString = new Dictionary<string, string>(_queryString),
				_compiledString = _compiledString,
			};
		}

		protected override bool OnEquals(QueryString other)
		{
			return _compiledString.CompareIgnoreCase(other._compiledString);
		}
	}
}