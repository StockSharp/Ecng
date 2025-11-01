namespace Ecng.Logging;

using System.Net.Mail;

/// <summary>
/// The logger sending data to the email.
/// </summary>
public class EmailLogListener : LogListener
{
	private readonly BlockingQueue<(string subj, string body)> _queue = new();

	private bool _isThreadStarted;

	/// <summary>
	/// Initializes a new instance of the <see cref="EmailLogListener"/>.
	/// </summary>
	public EmailLogListener()
	{
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

		_queue.Enqueue((GetSubject(message), message.Message));

		lock (_queue.SyncRoot)
		{
			if (_isThreadStarted)
				return;

			_isThreadStarted = true;

			ThreadingHelper.Thread(() =>
			{
				try
				{
					using var email = CreateClient();

					while (true)
					{
						if (!_queue.TryDequeue(out var m))
							break;

						email.Send(From, To, m.subj, m.body);
					}
				}
				catch (Exception ex)
				{
					Trace.WriteLine(ex);
				}
				finally
				{
					lock (_queue.SyncRoot)
						_isThreadStarted = false;
				}
			}).Name("Email log queue").Launch();
		}
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
		_queue.Close();
		base.DisposeManaged();
	}
}