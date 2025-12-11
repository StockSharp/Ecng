namespace Ecng.Net;

/// <summary>
/// Specifies the type of URL encoding to apply.
/// </summary>
public enum UrlEncodes
{
	/// <summary>
	/// No encoding is applied.
	/// </summary>
	None,
	/// <summary>
	/// Applies lower case encoding.
	/// </summary>
	Lower,
	/// <summary>
	/// Applies upper case encoding.
	/// </summary>
	Upper,
}

/// <summary>
/// Represents a URL and provides functionality for URL manipulation and cloning.
/// </summary>
public class Url : Uri, ICloneable<Url>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="Url"/> class using the specified <see cref="Uri"/>.
	/// </summary>
	/// <param name="url">The <see cref="Uri"/> to initialize the URL from.</param>
	public Url(Uri url)
		: this(url.ToString())
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Url"/> class using the specified URL string.
	/// </summary>
	/// <param name="url">The URL string.</param>
	public Url(string url)
		: base(url)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Url"/> class by combining a base URL and a relative URL.
	/// </summary>
	/// <param name="basePart">The base URL as a string.</param>
	/// <param name="relativePart">The relative URL string.</param>
	public Url(string basePart, string relativePart)
		: this(new Uri(basePart), relativePart)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Url"/> class by combining a base <see cref="Uri"/> and a relative URL string.
	/// </summary>
	/// <param name="basePart">The base <see cref="Uri"/>.</param>
	/// <param name="relativePart">The relative URL string.</param>
	public Url(Uri basePart, string relativePart)
		: base(basePart, relativePart)
	{
	}

	/// <summary>
	/// Gets or sets a value indicating whether the default page should be kept in the URL.
	/// </summary>
	public bool KeepDefaultPage { get; set; }

	/// <summary>
	/// Gets or sets the type of encoding to apply on the URL.
	/// </summary>
	public UrlEncodes Encode { get; set; } = UrlEncodes.Lower;

	private QueryString _queryString;

	/// <summary>
	/// Gets the query string associated with the URL.
	/// </summary>
	public QueryString QueryString => _queryString ??= new QueryString(this);

	/// <summary>
	/// Creates a new object that is a copy of the current instance.
	/// </summary>
	/// <returns>A new <see cref="Url"/> that is a copy of this instance.</returns>
	object ICloneable.Clone()
	{
		return Clone();
	}

	/// <summary>
	/// Creates a new instance of the <see cref="Url"/> class that is a copy of the current instance.
	/// </summary>
	/// <returns>A new <see cref="Url"/> that is a copy of this instance.</returns>
	public Url Clone()
	{
		return new Url(this)
		{
			KeepDefaultPage = KeepDefaultPage,
			Encode = Encode,
		};
	}
}