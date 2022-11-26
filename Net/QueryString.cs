namespace Ecng.Net;

using System.Collections;
using System.Collections.Specialized;

using Ecng.Reflection;

public class QueryString : Equatable<QueryString>, IEnumerable<KeyValuePair<string, string>>
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

		_queryString = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

		foreach (var key in queryString.Cast<string>().Where(k => !k.IsEmpty()))
			_queryString.Add(key, queryString[key]);

		_compiledString = url.Query;
	}

	public Url Url { get; }

	public string Raw
	{
		get { return _queryString.Select(p => p.Key + "=" + p.Value).JoinAnd(); }
	}

	public bool Contains(string queryField)
	{
		return _queryString.ContainsKey(queryField);
	}

	public int Count => _queryString.Count;

	public object this[string queryField]
	{
		get
		{
			if (queryField.IsEmpty())
				throw new ArgumentNullException(nameof(queryField));

			return _queryString.TryGetValue(queryField);
		}
		set
		{
			if (queryField.IsEmpty())
				throw new ArgumentNullException(nameof(queryField));

			if (value is null)
				throw new ArgumentNullException(nameof(value), $"Value for key '{queryField}' is null.");

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
		return TryGetValue<T>(queryField, default);
	}

	public T TryGetValue<T>(string queryField, T defaultValue)
	{
		return Contains(queryField) ? GetValue<T>(queryField) : defaultValue;
	}

	public bool TryGetValue<T>(string queryField, out T value)
	{
		if (_queryString.TryGetValue(queryField, out var str))
		{
			value = str.To<T>();
			return true;
		}

		value = default;
		return false;
	}

	public QueryString Append(string name, object value)
	{
		if (name.IsEmpty())
			throw new ArgumentNullException(nameof(name));

		if (value is null)
			throw new ArgumentNullException(nameof(value), $"Value for key '{name}' is null.");

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

	public QueryString Clear()
	{
		_queryString.Clear();
		RefreshUri();
		return this;
	}

	private void RefreshUri()
	{
		if (_queryString.IsEmpty())
			_compiledString = string.Empty;
		else
		{
			_compiledString = "?" + _queryString.Select(p =>
			{
				var key = Url.PreventEncodeUrl ? p.Key : p.Key.EncodeUrl();
				var value = Url.PreventEncodeUrl ? p.Value : p.Value.EncodeUrl();
				return $"{key}={value}";
			}).Join("&");
		}

		Url.SetValue("CreateUri", new object[] { Url.Clone(), Url.LocalPath + _compiledString, false });
	}

	public override string ToString()
	{
		return _compiledString;
	}

	public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
	{
		return _queryString.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
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
		return _compiledString.EqualsIgnoreCase(other._compiledString);
	}
}