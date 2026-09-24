namespace Ecng.Markdown;

/// <summary>
/// URL utility methods for markdown/HTML processing.
/// </summary>
static class UrlHelper
{
	private static readonly Regex _entityReference = new(@"^@(user|product_name|product|topic|message|page|file)\((\d+)\)$", RegexOptions.Compiled);

	/// <summary>
	/// Where an "@type(id)" reference written as a link's address points: null when the address is no such
	/// reference, empty when it is one that did not resolve.
	/// </summary>
	public static string ResolveEntityReference(string target, ResolvedMarkdownData data)
	{
		var match = _entityReference.Match(target ?? string.Empty);

		if (!match.Success)
			return null;

		var type = match.Groups[1].Value;
		var id = match.Groups[2].Value.To<long>();

		MarkdownLink link = null;

		if (type == "file")
			data.Files.TryGetValue(id, out link);
		else if (data.Entities.TryGetValue(type, out var byId))
			byId.TryGetValue(id, out link);

		return ResolveVirtualPath(link?.Url) ?? string.Empty;
	}

	/// <summary>
	/// Resolve ASP.NET virtual path prefix (~/) to root-relative path (/).
	/// </summary>
	public static string ResolveVirtualPath(string url)
		=> url is not null && url.StartsWith("~/") ? url[1..] : url;
}
