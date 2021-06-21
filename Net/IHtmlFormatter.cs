namespace Ecng.Net
{
	public interface IHtmlFormatter
	{
		string ToHtml(string text, object context);
		string Clean(string text);
	}
}