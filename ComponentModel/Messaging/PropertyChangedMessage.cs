namespace Ecng.ComponentModel;

/// <summary>
/// A message broadcast by <see cref="ObservableRecipient"/> when one of its properties changes.
/// Mirrors <c>CommunityToolkit.Mvvm.Messaging.Messages.PropertyChangedMessage{T}</c>.
/// </summary>
/// <typeparam name="T">The property value type.</typeparam>
public class PropertyChangedMessage<T>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="PropertyChangedMessage{T}"/> class.
	/// </summary>
	/// <param name="sender">The object that raised the change.</param>
	/// <param name="propertyName">The changed property name.</param>
	/// <param name="oldValue">The previous value.</param>
	/// <param name="newValue">The new value.</param>
	public PropertyChangedMessage(object sender, string propertyName, T oldValue, T newValue)
	{
		Sender = sender ?? throw new ArgumentNullException(nameof(sender));
		PropertyName = propertyName;
		OldValue = oldValue;
		NewValue = newValue;
	}

	/// <summary>
	/// The object that raised the change.
	/// </summary>
	public object Sender { get; }

	/// <summary>
	/// The changed property name.
	/// </summary>
	public string PropertyName { get; }

	/// <summary>
	/// The previous value.
	/// </summary>
	public T OldValue { get; }

	/// <summary>
	/// The new value.
	/// </summary>
	public T NewValue { get; }
}
