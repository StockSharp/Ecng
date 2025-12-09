namespace Ecng.Logging;

using System.Net.Mail;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

/// <summary>
/// The logger sending data to the email.
/// </summary>
public class EmailLogListener : LogListener
{
	private readonly Channel<(string subj, string body)> _channel = Channel.CreateUnbounded<(string subj, string body)>();
	private readonly Task _processingTask;
	private readonly CancellationTokenSource _cts = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="EmailLogListener"/>.
	/// </summary>
	public EmailLogListener()
	{
		_processingTask = Task.Run(ProcessMessagesAsync);
	}

	/// <summary>
	/// The address, on whose behalf the message will be sent.
	/// </summary>
	public virtual string From { get; set; }

	/// <summary>
	/// The address to which the message will be sent to.
	/// </summary>
	public virtual string To { get; set; }

	/// <summary>
	/// To create the email client.
	/// </summary>
	/// <returns>The email client.</returns>
	protected virtual SmtpClient CreateClient() => new();

	/// <summary>
	/// To create a header.
	/// </summary>
	/// <param name="message">A debug message.</param>
	/// <returns>Header.</returns>
	protected virtual string GetSubject(LogMessage message)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		return message.Source.Name + " " + message.Level + " " + ConvertToLocalTime(message.TimeUtc).ToString(TimeFormat);
	}

	/// <summary>
	/// Processes messages from the channel asynchronously.
	/// </summary>
	private async Task ProcessMessagesAsync()
	{
		var token = _cts.Token;

		try
		{
			var reader = _channel.Reader;

			while (await reader.WaitToReadAsync(token).NoWait())
			{
				while (reader.TryRead(out var message))
				{
					if (token.IsCancellationRequested)
						break;

					try
					{
						using var email = CreateClient();
						await email.SendMailAsync(From, To, message.subj, message.body
#if NET6_0_OR_GREATER
							, token
#endif
						);
					}
					catch (Exception ex)
					{
						if (!token.IsCancellationRequested)
							Trace.WriteLine(ex);
					}
				}
			}
		}
		catch (Exception ex)
		{
			if (!token.IsCancellationRequested)
				Trace.WriteLine(ex);
		}
	}

	/// <summary>
	/// To add a message in a queue for sending.
	/// </summary>
	/// <param name="message">Message.</param>
	private void EnqueueMessage(LogMessage message)
	{
		if (message.IsDispose)
		{
			Dispose();
			return;
		}

		if (From.IsEmpty() || To.IsEmpty())
			return;

		_channel.Writer.TryWrite((GetSubject(message), message.Message));
	}

	/// <inheritdoc />
	protected override void OnWriteMessage(LogMessage message)
	{
		EnqueueMessage(message);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		From = storage.GetValue<string>(nameof(From));
		To = storage.GetValue<string>(nameof(To));
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(From), From);
		storage.SetValue(nameof(To), To);
	}

	/// <summary>
	/// Release resources.
	/// </summary>
	protected override void DisposeManaged()
	{
		_cts.Cancel();
		_channel.Writer.Complete();

		try
		{
			_processingTask.Wait(TimeSpan.FromSeconds(5));
		}
		catch (Exception ex)
		{
			Trace.WriteLine(ex);
		}

		_cts.Dispose();

		base.DisposeManaged();
	}
}