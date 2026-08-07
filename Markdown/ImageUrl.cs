namespace Ecng.Markdown;

using System;
using System.Linq;

using Ecng.Common;

/// <summary>
/// Turns the address of a picture as a page uses it into the address of the picture itself.
/// </summary>
/// <remarks>
/// A page asks for a thumbnail, because a thumbnail is what fits in the column. A reader who clicks it wants
/// the picture, and handing them the same shrunken copy blown up is worse than not opening anything. The two
/// ways an address says "smaller" - a size in the query and a size baked into the path - are undone here so
/// both hosts open the same thing.
/// </remarks>
public static class ImageUrl
{
	private static readonly string[] _sizeKeys =
		["width", "height", "w", "h", "size", "maxwidth", "maxheight", "maxsize", "scale", "thumb"];

	/// <summary>
	/// The address of the picture at its own size.
	/// </summary>
	/// <param name="url">Address as the page uses it.</param>
	/// <returns>Address to open.</returns>
	public static string FullSize(string url)
	{
		if (url.IsEmpty())
			return url;

		// A video still names its size in the path rather than the query, and the largest one is a different
		// word in the same place.
		url = url
			.Replace("/hqdefault.jpg", "/maxresdefault.jpg")
			.Replace("/mqdefault.jpg", "/maxresdefault.jpg")
			.Replace("/default.jpg", "/maxresdefault.jpg");

		var mark = url.IndexOf('?');

		if (mark < 0)
			return url;

		var query = url[(mark + 1)..]
			.Split('&', StringSplitOptions.RemoveEmptyEntries)
			.Where(pair => !_sizeKeys.Contains(pair.Split('=')[0], StringComparer.OrdinalIgnoreCase))
			.ToArray();

		return query.Length == 0 ? url[..mark] : $"{url[..mark]}?{query.Join("&")}";
	}
}
