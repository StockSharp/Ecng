namespace Ecng.Net.Sms;

public interface ISmsService
{
	Task<string> SendAsync(string phone, string message, CancellationToken cancellationToken = default);
}