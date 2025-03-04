namespace Ecng.ComponentModel;

using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Runtime.CompilerServices;

using Ecng.Common;

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
}