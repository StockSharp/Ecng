namespace Ecng.Logging;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// The class interface that monitors the event <see cref="ILogSource.Log"/> and saves to some storage.
/// </summary>
public interface ILogListener : IPersistable, IDisposable
{
	/// <summary>
	/// Can save listener.
	/// </summary>
	bool CanSave { get; }

	/// <summary>
	/// To record messages.
	/// </summary>
	/// <param name="messages">Debug messages.</param>
	void WriteMessages(IEnumerable<LogMessage> messages);
}

/// <summary>
/// The class interface that monitors the event <see cref="ILogSource.Log"/> and saves to some storage asynchronously.
/// </summary>
public interface IAsyncLogListener : IPersistable, IDisposable
{
	/// <summary>
	/// To record messages asynchronously.
	/// </summary>
	/// <param name="messages">Debug messages.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task that represents the asynchronous write operation.</returns>
	ValueTask WriteMessagesAsync(IEnumerable<LogMessage> messages, CancellationToken cancellationToken = default);
}