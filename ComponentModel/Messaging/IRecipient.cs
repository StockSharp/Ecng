namespace Ecng.ComponentModel;

/// <summary>
/// Implemented by a type that receives messages of a given type. Register all such interfaces on an
/// instance at once with <see cref="IMessengerExtensions.RegisterAll(IMessenger, object)"/>.
/// </summary>
/// <typeparam name="TMessage">The message type.</typeparam>
public interface IRecipient<in TMessage>
	where TMessage : class
{
	/// <summary>
	/// Receives a broadcast message.
	/// </summary>
	/// <param name="message">The message.</param>
	void Receive(TMessage message);
}
