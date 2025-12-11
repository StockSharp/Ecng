namespace Ecng.Logging;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// The logger sending messages to the external recipient <see cref="ILogListener"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ExternalLogListener"/>.
/// </remarks>
/// <param name="logger">External recipient of messages.</param>
public class ExternalLogListener(IAsyncLogListener logger) : LogListener
{
	/// <summary>
	/// External recipient of messages.
	/// </summary>
	public IAsyncLogListener Logger { get; } = logger ?? throw new ArgumentNullException(nameof(logger));

	/// <inheritdoc />
	protected override ValueTask OnWriteMessagesAsync(IEnumerable<LogMessage> messages, CancellationToken cancellationToken = default)
		=> Logger.WriteMessagesAsync(messages, cancellationToken);
}