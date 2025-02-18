namespace Ecng.Net;

using System.Collections;
using System.Collections.Specialized;
using System.Reflection;

using Ecng.Reflection;

/// <summary>
/// Represents a query string associated with a URL and provides methods to manage its parameters.
/// </summary>
public class QueryString : Equatable<QueryString>, IEnumerable<KeyValuePair<string, string>>
{
	private static readonly MethodInfo _createUri;

	static QueryString()
	{
		_createUri = typeof(Uri).GetMethod("CreateUri", ReflectionHelper.AllInstanceMembers);
	}

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

	/// <summary>
	/// Gets the URL associated with this query string.
	/// </summary>
	public Url Url { get; }

	/// <summary>
	/// Gets the raw query string composed from the key-value pairs.
	/// </summary>
	public string Raw => _queryString.ToQueryString();

	/// <summary>
	/// Checks whether the specified query field exists.
	/// </summary>
	/// <param name="queryField">The query string field name.</param>
	/// <returns>True if the query field exists; otherwise, false.</returns>
	public bool Contains(string queryField)
	{
		return _queryString.ContainsKey(queryField);
	}

	/// <summary>
	/// Gets the number of query parameters.
	/// </summary>
	public int Count => _queryString.Count;

	/// <summary>
	/// Gets or sets the value of the specified query field.
	/// </summary>
	/// <param name="queryField">The query field name.</param>
	/// <returns>The value of the query field.</returns>
	/// <exception cref="ArgumentNullException">Thrown if the queryField is null or empty.</exception>
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

	/// <summary>
	/// Retrieves the value of a query parameter converted to the specified type.
	/// </summary>
	/// <typeparam name="T">The type to convert the query parameter value to.</typeparam>
	/// <param name="queryField">The query field name.</param>
	/// <returns>The converted value of the query parameter.</returns>
	public T GetValue<T>(string queryField)
	{
		return _queryString[queryField].To<T>();
	}

	/// <summary>
	/// Attempts to retrieve the value of a query parameter converted to the specified type.
	/// Returns the default value of T if the query parameter is not found.
	/// </summary>
	/// <typeparam name="T">The type to convert the query parameter value to.</typeparam>
	/// <param name="queryField">The query field name.</param>
	/// <returns>The converted value if found; otherwise, the default value of T.</returns>
	public T TryGetValue<T>(string queryField)
	{
		return TryGetValue<T>(queryField, default);
	}

	/// <summary>
	/// Attempts to retrieve the value of a query parameter converted to the specified type.
	/// Returns a specified default value if the query parameter is not found.
	/// </summary>
	/// <typeparam name="T">The type to convert the query parameter value to.</typeparam>
	/// <param name="queryField">The query field name.</param>
	/// <param name="defaultValue">The default value to return if query parameter is not found.</param>
	/// <returns>The converted value if found; otherwise, the specified default value.</returns>
	public T TryGetValue<T>(string queryField, T defaultValue)
	{
		return Contains(queryField) ? GetValue<T>(queryField) : defaultValue;
	}

	/// <summary>
	/// Attempts to retrieve the value of a query parameter converted to the specified type.
	/// </summary>
	/// <typeparam name="T">The type to convert the query parameter value to.</typeparam>
	/// <param name="queryField">The query field name.</param>
	/// <param name="value">When this method returns, contains the converted value if the query parameter exists; otherwise, the default value of T.</param>
	/// <returns>True if the query parameter exists and conversion was successful; otherwise, false.</returns>
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

	/// <summary>
	/// Appends a new query parameter to the query string.
	/// </summary>
	/// <param name="name">The name of the query parameter.</param>
	/// <param name="value">The value of the query parameter.</param>
	/// <returns>The current instance with the new parameter added.</returns>
	/// <exception cref="ArgumentNullException">Thrown if the name is null or empty, or if the value is null.</exception>
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

	/// <summary>
	/// Removes a query parameter from the query string.
	/// </summary>
	/// <param name="name">The name of the query parameter to remove.</param>
	/// <returns>The current instance with the parameter removed if it existed.</returns>
	public QueryString Remove(string name)
	{
		if (_queryString.Remove(name))
			RefreshUri();

		return this;
	}

	/// <summary>
	/// Clears all the query parameters.
	/// </summary>
	/// <returns>The current instance with all query parameters removed.</returns>
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
			_compiledString = "?" + _queryString
				.Select(p => (Encode(p.Key), Encode(p.Value)))
				.ToQueryString();
		}

		_createUri.Invoke(Url, [Url.Clone(), Url.LocalPath + _compiledString, false]);
	}

	private string Encode(string str)
		=> Url.Encode switch
		{
			UrlEncodes.None => str,
			UrlEncodes.Lower => str.EncodeUrl(),
			UrlEncodes.Upper => str.EncodeUrlUpper(),
			_ => throw new ArgumentOutOfRangeException(Url.Encode.ToString()),
		};

	/// <summary>
	/// Returns the compiled query string.
	/// </summary>
	/// <returns>The query string as a string.</returns>
	public override string ToString()
	{
		return _compiledString;
	}

	/// <summary>
	/// Returns an enumerator that iterates through the key-value pairs of the query string.
	/// </summary>
	/// <returns>An enumerator for the query parameters.</returns>
	public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
	{
		return _queryString.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	/// <summary>
	/// Creates a new instance of <see cref="QueryString"/> that is a copy of the current instance.
	/// </summary>
	/// <returns>A new instance of <see cref="QueryString"/> with the same query parameters and URL.</returns>
	public override QueryString Clone()
	{
		return new QueryString(Url)
		{
			_queryString = new Dictionary<string, string>(_queryString),
			_compiledString = _compiledString,
		};
	}

	/// <summary>
	/// Determines whether the specified <see cref="QueryString"/> is equal to the current instance.
	/// </summary>
	/// <param name="other">The <see cref="QueryString"/> to compare with the current instance.</param>
	/// <returns>True if the query strings are equal (ignoring case); otherwise, false.</returns>
	protected override bool OnEquals(QueryString other)
	{
		return _compiledString.EqualsIgnoreCase(other._compiledString);
	}
}