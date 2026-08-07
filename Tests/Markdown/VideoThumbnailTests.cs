namespace Ecng.Tests.Markdown;

using Ecng.Markdown;


[TestClass]
public class VideoThumbnailTests : BaseTestClass
{
	private const string _id = "HQBIB_y5TUA";

	[TestMethod]
	public void Watch_TakesTheReaderToThePageRatherThanToTheBareFrame()
	{
		// What a card carries is an embed address, meant to sit inside a page. Opened on its own it is a
		// player with nothing around it - no title, no channel, no way back - which is what a reader gets
		// when this conversion is skipped.
		foreach (var carried in new[]
		{
			$"https://www.youtube.com/embed/{_id}",
			$"https://youtube-nocookie.com/embed/{_id}",
			$"https://youtu.be/{_id}",
			$"https://www.youtube.com/watch?v={_id}",
		})
		{
			VideoThumbnail.WatchUrl(carried).AssertEqual($"https://www.youtube.com/watch?v={_id}");
		}
	}

	[TestMethod]
	public void Watch_AddressItDoesNotRecognise_IsLeftAlone()
	{
		foreach (var other in new[] { "https://stocksharp.com/video/1/", "not a url", "" })
			VideoThumbnail.WatchUrl(other).AssertEqual(other);
	}

	[TestMethod]
	public void Poster_IsFoundForEveryFormTheTextMayCarry()
	{
		foreach (var carried in new[]
		{
			$"https://www.youtube.com/embed/{_id}",
			$"https://youtu.be/{_id}",
			$"https://www.youtube.com/watch?v={_id}",
		})
		{
			VideoThumbnail.For(carried).AssertEqual($"https://img.youtube.com/vi/{_id}/hqdefault.jpg");
		}

		VideoThumbnail.For("https://stocksharp.com/video/1/").AssertEqual(string.Empty);
	}
}
