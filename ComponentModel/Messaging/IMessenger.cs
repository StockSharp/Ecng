namespace Ecng.ComponentModel;

/// <summary>
/// A handler invoked when a message is broadcast to a recipient. The recipient is passed as an
/// argument (rather than captured) so weak-reference messengers do not root it.
/// </summary>
/// <typeparam name="TRecipient">The recipient type.</typeparam>
/// <typeparam name="TMessage">The message type.</typeparam>
/// <param name="recipient">The recipient receiving the message.</param>
/// <param name="message">The message.</param>
public delegate void MessageHandler<in TRecipient, in TMessage>(TRecipient recipient, TMessage message)
	where TRecipient : class
	where TMessage : class;

/// <summary>
/// A loosely-coupled message bus: recipients register for a message type (optionally scoped by a
/// token) and senders broadcast messages without referencing the recipients. The shape mirrors the
/// commonly used surface of <c>CommunityToolkit.Mvvm.Messaging.IMessenger</c>.
/// </summary>
public interface IMessenger
{
	/// <summary>
	/// Registers a recipient for a given message type and token.
	/// </summary>
	/// <typeparam name="TRecipient">The recipient type.</typeparam>
	/// <typeparam name="TMessage">The message type.</typeparam>
	/// <param name="recipient">The recipient.</param>
	/// <param name="token">The channel token (use <see cref="object"/> equality to match sends).</param>
	/// <param name="handler">The handler invoked on broadcast.</param>
	void Register<TRecipient, TMessage>(TRecipient recipient, object token, MessageHandler<TRecipient, TMessage> handler)
		where TRecipient : class
		where TMessage : class;

	/// <summary>
	/// Determines whether a recipient is registered for a message type and token.
	/// </summary>
	/// <typeparam name="TMessage">The message type.</typeparam>
	/// <param name="recipient">The recipient.</param>
	/// <param name="token">The channel token.</param>
	/// <returns><c>true</c> if registered.</returns>
	bool IsRegistered<TMessage>(object recipient, object token)
		where TMessage : class;

	/// <summary>
	/// Unregisters a recipient from a message type and token.
	/// </summary>
	/// <typeparam name="TMessage">The message type.</typeparam>
	/// <param name="recipient">The recipient.</param>
	/// <param name="token">The channel token.</param>
	void Unregister<TMessage>(object recipient, object token)
		where TMessage : class;

	/// <summary>
	/// Unregisters a recipient from every message type and token.
	/// </summary>
	/// <param name="recipient">The recipient.</param>
	void UnregisterAll(object recipient);

	/// <summary>
	/// Broadcasts a message to all recipients registered for its type and the given token.
	/// </summary>
	/// <typeparam name="TMessage">The message type.</typeparam>
	/// <param name="message">The message.</param>
	/// <param name="token">The channel token.</param>
	/// <returns>The same message instance (after delivery), enabling request/response patterns.</returns>
	TMessage Send<TMessage>(TMessage message, object token)
		where TMessage : class;

	/// <summary>
	/// Drops dead recipient references (relevant for weak messengers).
	/// </summary>
	void Cleanup();

	/// <summary>
	/// Removes all registrations.
	/// </summary>
	void Reset();
}
