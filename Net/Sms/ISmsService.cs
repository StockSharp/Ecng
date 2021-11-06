namespace StockSharp.Web.Providers.Interfaces
{
	using System.Threading;
	using System.Threading.Tasks;

	public interface ISmsService
	{
		Task<string> SendAsync(string phone, string message, CancellationToken cancellationToken = default);
	}
}