#if !NET5_0_OR_GREATER
namespace System.Net.Mail;

using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

/// <summary>
/// Extension methods for <see cref="SmtpClient"/>.
/// </summary>
public static class SmtpClientExtensions
{
	/// <summary>
	/// Sends the specified message to an SMTP server for delivery as an asynchronous operation.
	/// </summary>
	/// <param name="client">The SMTP client.</param>
	/// <param name="message">The message to send.</param>
	/// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
	/// <returns>A task that represents the asynchronous send operation.</returns>
	public static async Task SendMailAsync(this SmtpClient client, MailMessage message, CancellationToken cancellationToken)
	{
		if (client is null)
			throw new ArgumentNullException(nameof(client));
		if (message is null)
			throw new ArgumentNullException(nameof(message));

		cancellationToken.ThrowIfCancellationRequested();

		using (cancellationToken.Register(() => client.SendAsyncCancel()))
		{
			try
			{
				await client.SendMailAsync(message).NoWait();
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception) when (cancellationToken.IsCancellationRequested)
			{
				throw new OperationCanceledException(cancellationToken);
			}
		}
	}

	/// <summary>
	/// Sends the specified message to an SMTP server for delivery as an asynchronous operation.
	/// </summary>
	/// <param name="client">The SMTP client.</param>
	/// <param name="from">The address of the sender of the mail message.</param>
	/// <param name="recipients">The address of the recipient of the mail message.</param>
	/// <param name="subject">The subject line for the mail message.</param>
	/// <param name="body">The body of the mail message.</param>
	/// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
	/// <returns>A task that represents the asynchronous send operation.</returns>
	public static async Task SendMailAsync(this SmtpClient client, string from, string recipients, string subject, string body, CancellationToken cancellationToken)
	{
		if (client is null)
			throw new ArgumentNullException(nameof(client));

		cancellationToken.ThrowIfCancellationRequested();

		using (cancellationToken.Register(() => client.SendAsyncCancel()))
		{
			try
			{
				await client.SendMailAsync(from, recipients, subject, body).NoWait();
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception) when (cancellationToken.IsCancellationRequested)
			{
				throw new OperationCanceledException(cancellationToken);
			}
		}
	}
}
#endif
