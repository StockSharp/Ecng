namespace Ecng.ComponentModel;

/// <summary>
/// Convenience overloads for <see cref="IMessenger"/> covering the common no-token and
/// <see cref="IRecipient{TMessage}"/> cases.
/// </summary>
public static class IMessengerExtensions
{
	/// <summary>
	/// The token used when none is supplied. Sends and registrations that omit a token all share it.
	/// </summary>
	public static readonly object DefaultToken = new();

	private static readonly MethodInfo _registerRecipientCore =
		typeof(IMessengerExtensions).GetMethod(nameof(RegisterRecipientCore), BindingFlags.NonPublic | BindingFlags.Static);

	/// <summary>
	/// Registers an <see cref="IRecipient{TMessage}"/> for the default token.
	/// </summary>
	public static void Register<TMessage>(this IMessenger messenger, IRecipient<TMessage> recipient)
		where TMessage : class
		=> messenger.Register(recipient, DefaultToken);

	/// <summary>
	/// Registers an <see cref="IRecipient{TMessage}"/> for the given token.
	/// </summary>
	public static void Register<TMessage>(this IMessenger messenger, IRecipient<TMessage> recipient, object token)
		where TMessage : class
	{
		if (messenger is null)	throw new ArgumentNullException(nameof(messenger));
		if (recipient is null)	throw new ArgumentNullException(nameof(recipient));

		messenger.Register<IRecipient<TMessage>, TMessage>(recipient, token, static (r, m) => r.Receive(m));
	}

	/// <summary>
	/// Registers a handler for the default token.
	/// </summary>
	public static void Register<TRecipient, TMessage>(this IMessenger messenger, TRecipient recipient, MessageHandler<TRecipient, TMessage> handler)
		where TRecipient : class
		where TMessage : class
	{
		if (messenger is null)
			throw new ArgumentNullException(nameof(messenger));

		messenger.Register(recipient, DefaultToken, handler);
	}

	/// <summary>
	/// Registers every <see cref="IRecipient{TMessage}"/> the recipient implements, for the default token.
	/// </summary>
	public static void RegisterAll(this IMessenger messenger, object recipient)
	{
		if (messenger is null)	throw new ArgumentNullException(nameof(messenger));
		if (recipient is null)	throw new ArgumentNullException(nameof(recipient));

		foreach (var iface in recipient.GetType().GetInterfaces())
		{
			if (!iface.IsGenericType || iface.GetGenericTypeDefinition() != typeof(IRecipient<>))
				continue;

			var messageType = iface.GenericTypeArguments[0];
			_registerRecipientCore.MakeGenericMethod(messageType).Invoke(null, [messenger, recipient]);
		}
	}

	private static void RegisterRecipientCore<TMessage>(IMessenger messenger, IRecipient<TMessage> recipient)
		where TMessage : class
		=> messenger.Register<IRecipient<TMessage>, TMessage>(recipient, DefaultToken, static (r, m) => r.Receive(m));

	/// <summary>
	/// Sends a message for the default token.
	/// </summary>
	public static TMessage Send<TMessage>(this IMessenger messenger, TMessage message)
		where TMessage : class
	{
		if (messenger is null)
			throw new ArgumentNullException(nameof(messenger));

		return messenger.Send(message, DefaultToken);
	}

	/// <summary>
	/// Sends a new parameterless message for the default token.
	/// </summary>
	public static TMessage Send<TMessage>(this IMessenger messenger)
		where TMessage : class, new()
	{
		if (messenger is null)
			throw new ArgumentNullException(nameof(messenger));

		return messenger.Send(new TMessage(), DefaultToken);
	}

	/// <summary>
	/// Determines whether a recipient is registered for the default token.
	/// </summary>
	public static bool IsRegistered<TMessage>(this IMessenger messenger, object recipient)
		where TMessage : class
	{
		if (messenger is null)
			throw new ArgumentNullException(nameof(messenger));

		return messenger.IsRegistered<TMessage>(recipient, DefaultToken);
	}

	/// <summary>
	/// Unregisters a recipient from a message type for the default token.
	/// </summary>
	public static void Unregister<TMessage>(this IMessenger messenger, object recipient)
		where TMessage : class
	{
		if (messenger is null)
			throw new ArgumentNullException(nameof(messenger));

		messenger.Unregister<TMessage>(recipient, DefaultToken);
	}
}
