namespace Ecng.Net.Sms
{
	using System.Threading;
	using System.Threading.Tasks;

	public interface ISmsService
	{
		Task<string> SendAsync(string phone, string message, CancellationToken cancellationToken = default);
	}
}