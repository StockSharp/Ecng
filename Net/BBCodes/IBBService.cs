namespace Ecng.Net.BBCodes
{
	public interface IBBService
	{
		string ToHtml(string text, object context);
		string Clean(string text);
	}
}