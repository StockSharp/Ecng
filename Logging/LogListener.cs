namespace Ecng.Logging;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// The base class that monitors the event <see cref="ILogSource.Log"/> and saves to some storage.
/// </summary>
public abstract class LogListener : Disposable, ILogListener, IAsyncLogListener
{
	/// <summary>
	/// Initialize <see cref="LogListener"/>.
	/// </summary>
	protected LogListener()
	{
		Filters = [];

		CanSave = GetType().GetConstructor([]) is not null;
	}

	/// <summary>
	/// Messages filters that specify which messages should be handled.
	/// </summary>
	public IList<Func<LogMessage, bool>> Filters { get; }

	private string _dateFormat = "yyyy/MM/dd";

	/// <summary>
	/// Date format. By default yyyy/MM/dd.
	/// </summary>
	public string DateFormat
	{
		get => _dateFormat;
		set
		{
			if (value.IsEmpty())
				throw new ArgumentNullException(nameof(value));

			_dateFormat = value;
		}
	}

	private string _timeFormat = "HH:mm:ss.fff";

	/// <summary>
	/// Time format. By default HH:mm:ss.fff.
	/// </summary>
	public string TimeFormat
	{
		get => _timeFormat;
		set
		{
			if (value.IsEmpty())
				throw new ArgumentNullException(nameof(value));
			
			_timeFormat = value;
		}
	}

	/// <inheritdoc />
	public virtual bool CanSave { get; }

	/// <summary>
	/// To convert message time to local time.
	/// </summary>
	public bool IsLocalTime { get; set; }

	/// <summary>
	/// To convert message time to local time.
	/// </summary>
	/// <param name="time">A time to convert.</param>
	/// <returns>Converted time.</returns>
	protected virtual DateTime ConvertToLocalTime(DateTime time)
		=> IsLocalTime ? time.ToLocalTime() : time;

	/// <inheritdoc />
	public void WriteMessages(IEnumerable<LogMessage> messages)
	{
		OnWriteMessages(messages.Filter(Filters));
	}

	/// <summary>
	/// To record messages.
	/// </summary>
	/// <param name="messages">Debug messages.</param>
	protected virtual void OnWriteMessages(IEnumerable<LogMessage> messages)
	{
		messages.ForEach(OnWriteMessage);
	}

	/// <summary>
	/// To record a message.
	/// </summary>
	/// <param name="message">A debug message.</param>
	protected virtual void OnWriteMessage(LogMessage message)
	{
		throw new NotSupportedException();
	}

	/// <inheritdoc />
	public virtual ValueTask WriteMessagesAsync(IEnumerable<LogMessage> messages, CancellationToken cancellationToken = default)
	{
		return OnWriteMessagesAsync(messages.Filter(Filters), cancellationToken);
	}

	/// <summary>
	/// To record messages asynchronously.
	/// </summary>
	/// <param name="messages">Debug messages.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task that represents the asynchronous write operation.</returns>
	protected virtual ValueTask OnWriteMessagesAsync(IEnumerable<LogMessage> messages, CancellationToken cancellationToken = default)
	{
		throw new NotSupportedException();
	}

	/// <summary>
	/// Load settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public virtual void Load(SettingsStorage storage)
	{
		DateFormat = storage.GetValue(nameof(DateFormat), DateFormat);
		TimeFormat = storage.GetValue(nameof(TimeFormat), TimeFormat);
		IsLocalTime = storage.GetValue(nameof(IsLocalTime), IsLocalTime);
	}

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public virtual void Save(SettingsStorage storage)
	{
		storage
			.Set(nameof(DateFormat), DateFormat)
			.Set(nameof(TimeFormat), TimeFormat)
			.Set(nameof(IsLocalTime), IsLocalTime)
		;
	}
}