namespace Ecng.Net
{
	using System.Threading;
	using System.Threading.Tasks;

	public interface IHtmlFormatter
	{
		Task<string> ToHtmlAsync(string text, object context, CancellationToken cancellationToken = default);
		Task<string> CleanAsync(string text, CancellationToken cancellationToken = default);
	}
}