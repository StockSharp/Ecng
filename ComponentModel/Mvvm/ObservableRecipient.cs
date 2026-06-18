namespace Ecng.ComponentModel;

/// <summary>
/// A view-model base that participates in <see cref="IMessenger"/> messaging. While
/// <see cref="IsActive"/> is <c>true</c> the instance is registered for every
/// <see cref="IRecipient{TMessage}"/> it implements; property changes can be broadcast as
/// <see cref="PropertyChangedMessage{T}"/>. The shape mirrors
/// <c>CommunityToolkit.Mvvm.ComponentModel.ObservableRecipient</c>.
/// </summary>
public abstract class ObservableRecipient : NotifiableObject
{
	/// <summary>
	/// Initializes a new instance using <see cref="WeakReferenceMessenger.Default"/>.
	/// </summary>
	protected ObservableRecipient()
		: this(WeakReferenceMessenger.Default)
	{
	}

	/// <summary>
	/// Initializes a new instance with the specified messenger.
	/// </summary>
	/// <param name="messenger">The messenger to use.</param>
	protected ObservableRecipient(IMessenger messenger)
	{
		Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
	}

	/// <summary>
	/// The messenger this recipient uses.
	/// </summary>
	protected IMessenger Messenger { get; }

	private bool _isActive;

	/// <summary>
	/// Gets or sets whether this recipient is active. Activating registers its
	/// <see cref="IRecipient{TMessage}"/> handlers; deactivating unregisters them.
	/// </summary>
	public bool IsActive
	{
		get => _isActive;
		set
		{
			if (_isActive == value)
				return;

			NotifyChanging(nameof(IsActive));
			_isActive = value;
			NotifyChanged(nameof(IsActive));

			if (value)
				OnActivated();
			else
				OnDeactivated();
		}
	}

	/// <summary>
	/// Called when <see cref="IsActive"/> becomes <c>true</c>. Registers all implemented
	/// <see cref="IRecipient{TMessage}"/> handlers by default.
	/// </summary>
	protected virtual void OnActivated()
		=> Messenger.RegisterAll(this);

	/// <summary>
	/// Called when <see cref="IsActive"/> becomes <c>false</c>. Unregisters this recipient by default.
	/// </summary>
	protected virtual void OnDeactivated()
		=> Messenger.UnregisterAll(this);

	/// <summary>
	/// Broadcasts a <see cref="PropertyChangedMessage{T}"/> for a property change.
	/// </summary>
	/// <typeparam name="T">The property value type.</typeparam>
	/// <param name="oldValue">The previous value.</param>
	/// <param name="newValue">The new value.</param>
	/// <param name="propertyName">The property name.</param>
	protected void Broadcast<T>(T oldValue, T newValue, string propertyName)
		=> Messenger.Send(new PropertyChangedMessage<T>(this, propertyName, oldValue, newValue));

	/// <summary>
	/// Sets a field, raising change notification and optionally broadcasting the change.
	/// </summary>
	/// <typeparam name="T">The field type.</typeparam>
	/// <param name="field">The backing field.</param>
	/// <param name="newValue">The new value.</param>
	/// <param name="broadcast">Whether to broadcast a <see cref="PropertyChangedMessage{T}"/>.</param>
	/// <param name="propertyName">The property name (auto-filled).</param>
	/// <returns><c>true</c> if the value changed.</returns>
	protected bool SetProperty<T>(ref T field, T newValue, bool broadcast, [CallerMemberName] string propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, newValue))
			return false;

		var oldValue = field;

		NotifyChanging(propertyName);
		field = newValue;
		NotifyChanged(propertyName);

		if (broadcast)
			Broadcast(oldValue, newValue, propertyName);

		return true;
	}
}
