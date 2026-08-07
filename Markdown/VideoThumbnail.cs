namespace Ecng.Markdown;

using System;

using Ecng.Common;

/// <summary>
/// Works out the still image that stands for a video before it is played.
/// </summary>
/// <remarks>
/// Shared rather than written in each host because it is a reading of an address, not a drawing decision:
/// two hosts deriving it separately would show a poster in one and a bare link in the other for the same
/// video. Nothing here plays anything - a host that can embed a player passes one in.
/// </remarks>
public static class VideoThumbnail
{
	/// <summary>
	/// Returns the address of a still for the video, or an empty string when none can be worked out.
	/// </summary>
	/// <param name="url">Address the video plays from.</param>
	/// <returns>Address of the still.</returns>
	public static string For(string url)
	{
		if (url.IsEmpty() || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
			return string.Empty;

		var id = GetYouTubeId(uri);

		return id.IsEmpty() ? string.Empty : $"https://img.youtube.com/vi/{id}/hqdefault.jpg";
	}

	/// <summary>
	/// The address a reader should be sent to when they ask to play the video.
	/// </summary>
	/// <param name="url">Address the markdown carries.</param>
	/// <returns>Address to open.</returns>
	/// <remarks>
	/// What the text carries is an embed address - a bare player meant to sit inside a page. Opened on its
	/// own it is a video with nothing around it: no title, no channel, no way back. The watch address is the
	/// page a reader expects to land on.
	/// </remarks>
	public static string WatchUrl(string url)
	{
		if (url.IsEmpty() || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
			return url;

		var id = GetYouTubeId(uri);

		return id.IsEmpty() ? url : $"https://www.youtube.com/watch?v={id}";
	}

	private static string GetYouTubeId(Uri uri)
	{
		var host = uri.Host.Remove("www.", true);

		if (host.EqualsIgnoreCase("youtu.be"))
			return uri.AbsolutePath.Trim('/');

		if (!host.EqualsIgnoreCase("youtube.com") && !host.EqualsIgnoreCase("youtube-nocookie.com"))
			return string.Empty;

		var path = uri.AbsolutePath.Trim('/');

		// Both forms the site produces: the embed the markdown carries, and the watch address a reader pastes.
		if (path.StartsWithIgnoreCase("embed/"))
			return path["embed/".Length..];

		if (path.EqualsIgnoreCase("watch"))
		{
			foreach (var pair in uri.Query.TrimStart('?').Split('&'))
			{
				var parts = pair.Split('=');

				if (parts.Length == 2 && parts[0].EqualsIgnoreCase("v"))
					return parts[1];
			}
		}

		return string.Empty;
	}
}
