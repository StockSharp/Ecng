namespace Ecng.Net;

public enum UrlEncodes
{
	None,
	Lower,
	Upper,
}

public class Url : Uri, ICloneable<Url>
{
	public Url(Uri url)
		: this(url.ToString())
	{
	}

	public Url(string url)
		: base(url)
	{
	}

	public Url(string basePart, string relativePart)
		: this(new Uri(basePart), relativePart)
	{
	}

	public Url(Uri basePart, string relativePart)
		: base(basePart, relativePart)
	{
	}

	public bool KeepDefaultPage { get; set; }
	public UrlEncodes Encode { get; set; } = UrlEncodes.Lower;

	private QueryString _queryString;

	public QueryString QueryString => _queryString ??= new QueryString(this);

	object ICloneable.Clone()
	{
		return Clone();
	}

	public Url Clone()
	{
		return new Url(this);
	}
}