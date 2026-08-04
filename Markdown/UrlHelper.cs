namespace Ecng.Markdown;

/// <summary>
/// URL utility methods for markdown/HTML processing.
/// </summary>
static class UrlHelper
{
	/// <summary>
	/// Resolve ASP.NET virtual path prefix (~/) to root-relative path (/).
	/// </summary>
	public static string ResolveVirtualPath(string url)
		=> url is not null && url.StartsWith("~/") ? url[1..] : url;
}
