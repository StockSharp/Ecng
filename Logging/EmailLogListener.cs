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
	private readonly Channel<(string from, string to, string subj, string body)> _channel = Channel.CreateBounded<(string, string, string, string)>(10);
	private readonly Task _processingTask;
	private readonly CancellationTokenSource _cts = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="EmailLogListener"/>.
	/// </summary>
	public EmailLogListener()
	{
		_processingTask = ProcessMessagesAsync(_cts.Token);
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
	protected virtual (string from, string to, string subj, string body) GetInfo(LogMessage message)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		var from = From.ThrowIfEmpty(nameof(From));
		var to = To.ThrowIfEmpty(nameof(To));
		var subj = $"[{message.Source.Name}] ({message.Level}) {ConvertToLocalTime(message.Time).ToString(TimeFormat)}";
		return (from, to, subj, message.Message);
	}

	/// <summary>
	/// Processes messages from the channel asynchronously.
	/// </summary>
	private async Task ProcessMessagesAsync(CancellationToken token)
	{
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
						await email.SendMailAsync(message.from, message.to, message.subj, message.body, token);
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

	/// <inheritdoc />
	protected override ValueTask OnWriteMessagesAsync(IEnumerable<LogMessage> messages, CancellationToken cancellationToken = default)
	{
		foreach (var message in messages)
		{
			if (message.IsDispose)
			{
				Dispose();
				return default;
			}

			_channel.Writer.TryWrite(GetInfo(message));
		}

		return default;
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