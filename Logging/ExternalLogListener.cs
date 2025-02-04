namespace Ecng.Logging;

/// <summary>
/// The logger sending messages to the external recipient <see cref="ILogListener"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ExternalLogListener"/>.
/// </remarks>
/// <param name="logger">External recipient of messages.</param>
public class ExternalLogListener(ILogListener logger) : LogListener
{
	/// <summary>
	/// External recipient of messages.
	/// </summary>
	public ILogListener Logger { get; } = logger ?? throw new ArgumentNullException(nameof(logger));

	/// <inheritdoc />
	protected override void OnWriteMessages(IEnumerable<LogMessage> messages)
	{
		Logger.WriteMessages(messages);
	}
}