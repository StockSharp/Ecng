namespace Ecng.ComponentModel;

/// <summary>
/// Provides base functionality for notifying property changes to registered observers.
/// Implements both <see cref="INotifyPropertyChangedEx"/> and <see cref="INotifyPropertyChanging"/>.
/// </summary>
[Serializable]
[DataContract]
public abstract class NotifiableObject : INotifyPropertyChangedEx, INotifyPropertyChanging
{
	/// <summary>
	/// Occurs when a property value has changed.
	/// </summary>
	public event PropertyChangedEventHandler PropertyChanged;

	/// <summary>
	/// Occurs when a property value is changing.
	/// </summary>
	public event PropertyChangingEventHandler PropertyChanging;

	/// <summary>
	/// Raises the <see cref="PropertyChanged"/> event.
	/// </summary>
	/// <param name="propertyName">The name of the property that changed. This value is optional and can be provided automatically by the compiler.</param>
	public void NotifyPropertyChanged([CallerMemberName]string propertyName = null)
	{
		PropertyChanged?.Invoke(this, propertyName);
	}

	/// <summary>
	/// Invokes the property change notification.
	/// </summary>
	/// <param name="propertyName">The name of the property that changed. This value is optional and can be provided automatically by the compiler.</param>
	protected void NotifyChanged([CallerMemberName]string propertyName = null)
	{
		this.Notify(propertyName);
	}

	/// <summary>
	/// Raises the <see cref="PropertyChanging"/> event.
	/// </summary>
	/// <param name="propertyName">The name of the property that is changing. This value is optional and can be provided automatically by the compiler.</param>
	protected void NotifyChanging([CallerMemberName]string propertyName = null)
	{
		PropertyChanging?.Invoke(this, propertyName);
	}

	/// <summary>
	/// Raises <see cref="PropertyChanged"/> for the given property. Alias for <see cref="NotifyChanged"/>.
	/// </summary>
	/// <param name="propertyName">The property name. This value is optional and can be provided automatically by the compiler.</param>
	protected void OnPropertyChanged([CallerMemberName]string propertyName = null)
		=> NotifyChanged(propertyName);

	/// <summary>
	/// Raises <see cref="PropertyChanging"/> for the given property. Alias for <see cref="NotifyChanging"/>.
	/// </summary>
	/// <param name="propertyName">The property name. This value is optional and can be provided automatically by the compiler.</param>
	protected void OnPropertyChanging([CallerMemberName]string propertyName = null)
		=> NotifyChanging(propertyName);

	/// <summary>
	/// Assigns <paramref name="value"/> to <paramref name="field"/> and raises the changing/changed notifications,
	/// but only when the value actually differs.
	/// </summary>
	/// <typeparam name="T">Property type.</typeparam>
	/// <param name="field">Reference to the backing field.</param>
	/// <param name="value">The new value.</param>
	/// <param name="propertyName">The property name. This value is optional and can be provided automatically by the compiler.</param>
	/// <returns><see langword="true"/> if the value changed; otherwise <see langword="false"/>.</returns>
	protected bool SetProperty<T>(ref T field, T value, [CallerMemberName]string propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value))
			return false;

		NotifyChanging(propertyName);
		field = value;
		NotifyChanged(propertyName);
		return true;
	}
}