namespace Ecng.ComponentModel;

/// <summary>
/// Shared <see cref="IMessenger"/> implementation. Registrations are kept per message type and token;
/// derived messengers decide whether recipients are held strongly or weakly. Handlers receive the
/// recipient as an argument, so a weak messenger never roots it. All operations are thread-safe;
/// handlers are invoked outside the internal lock so they may register, unregister or send re-entrantly.
/// </summary>
public abstract class MessengerBase : IMessenger
{
	private sealed class Registration
	{
		public object RecipientRef;
		public Action<object, object> Invoker;
	}

	private readonly object _sync = new();

	// message type -> token -> registrations.
	private readonly Dictionary<Type, Dictionary<object, List<Registration>>> _map = [];

	/// <summary>
	/// When <c>true</c>, recipients are held through weak references and may be collected while still
	/// registered; when <c>false</c>, they are held strongly until explicitly unregistered.
	/// </summary>
	protected abstract bool IsWeak { get; }

	private object Wrap(object recipient) => IsWeak ? new WeakReference(recipient) : recipient;

	private object Unwrap(object recipientRef) => IsWeak ? ((WeakReference)recipientRef).Target : recipientRef;

	void IMessenger.Register<TRecipient, TMessage>(TRecipient recipient, object token, MessageHandler<TRecipient, TMessage> handler)
	{
		if (recipient is null)	throw new ArgumentNullException(nameof(recipient));
		if (token is null)		throw new ArgumentNullException(nameof(token));
		if (handler is null)	throw new ArgumentNullException(nameof(handler));

		lock (_sync)
		{
			if (!_map.TryGetValue(typeof(TMessage), out var byToken))
				_map[typeof(TMessage)] = byToken = [];

			if (!byToken.TryGetValue(token, out var list))
				byToken[token] = list = [];

			foreach (var reg in list)
			{
				if (ReferenceEquals(Unwrap(reg.RecipientRef), recipient))
					throw new InvalidOperationException($"The recipient is already registered for message '{typeof(TMessage).Name}' and the given token.");
			}

			list.Add(new Registration
			{
				RecipientRef = Wrap(recipient),
				Invoker = (r, m) => handler((TRecipient)r, (TMessage)m),
			});
		}
	}

	bool IMessenger.IsRegistered<TMessage>(object recipient, object token)
	{
		if (recipient is null)	throw new ArgumentNullException(nameof(recipient));
		if (token is null)		throw new ArgumentNullException(nameof(token));

		lock (_sync)
		{
			if (_map.TryGetValue(typeof(TMessage), out var byToken) && byToken.TryGetValue(token, out var list))
			{
				foreach (var reg in list)
				{
					if (ReferenceEquals(Unwrap(reg.RecipientRef), recipient))
						return true;
				}
			}

			return false;
		}
	}

	void IMessenger.Unregister<TMessage>(object recipient, object token)
	{
		if (recipient is null)	throw new ArgumentNullException(nameof(recipient));
		if (token is null)		throw new ArgumentNullException(nameof(token));

		lock (_sync)
		{
			if (!_map.TryGetValue(typeof(TMessage), out var byToken) || !byToken.TryGetValue(token, out var list))
				return;

			list.RemoveAll(reg =>
			{
				var target = Unwrap(reg.RecipientRef);
				return target is null || ReferenceEquals(target, recipient);
			});

			if (list.Count == 0)
				byToken.Remove(token);

			if (byToken.Count == 0)
				_map.Remove(typeof(TMessage));
		}
	}

	void IMessenger.UnregisterAll(object recipient)
	{
		if (recipient is null)
			throw new ArgumentNullException(nameof(recipient));

		lock (_sync)
		{
			foreach (var messageType in _map.Keys.ToArray())
			{
				var byToken = _map[messageType];

				foreach (var token in byToken.Keys.ToArray())
				{
					var list = byToken[token];

					list.RemoveAll(reg =>
					{
						var target = Unwrap(reg.RecipientRef);
						return target is null || ReferenceEquals(target, recipient);
					});

					if (list.Count == 0)
						byToken.Remove(token);
				}

				if (byToken.Count == 0)
					_map.Remove(messageType);
			}
		}
	}

	TMessage IMessenger.Send<TMessage>(TMessage message, object token)
	{
		if (message is null)	throw new ArgumentNullException(nameof(message));
		if (token is null)		throw new ArgumentNullException(nameof(token));

		Registration[] snapshot;

		lock (_sync)
		{
			if (!_map.TryGetValue(typeof(TMessage), out var byToken) || !byToken.TryGetValue(token, out var list) || list.Count == 0)
				return message;

			snapshot = [.. list];
		}

		foreach (var reg in snapshot)
		{
			var target = Unwrap(reg.RecipientRef);

			if (target is not null)
				reg.Invoker(target, message);
		}

		return message;
	}

	void IMessenger.Cleanup()
	{
		lock (_sync)
		{
			foreach (var messageType in _map.Keys.ToArray())
			{
				var byToken = _map[messageType];

				foreach (var token in byToken.Keys.ToArray())
				{
					var list = byToken[token];

					list.RemoveAll(reg => Unwrap(reg.RecipientRef) is null);

					if (list.Count == 0)
						byToken.Remove(token);
				}

				if (byToken.Count == 0)
					_map.Remove(messageType);
			}
		}
	}

	void IMessenger.Reset()
	{
		lock (_sync)
			_map.Clear();
	}
}
