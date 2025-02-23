namespace Ecng.Net.Sms;

/// <summary>
/// Represents an SMS service for sending text messages.
/// </summary>
public interface ISmsService
{
	/// <summary>
	/// Sends an SMS message asynchronously.
	/// </summary>
	/// <param name="phone">The recipient phone number.</param>
	/// <param name="message">The content of the SMS message.</param>
	/// <param name="cancellationToken">A token that may be used to cancel the operation.</param>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result contains the response or status returned after sending the message.
	/// </returns>
	Task<string> SendAsync(string phone, string message, CancellationToken cancellationToken = default);
}