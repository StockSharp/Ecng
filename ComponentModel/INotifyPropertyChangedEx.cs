namespace Ecng.ComponentModel;

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

/// <summary>
/// Extended version <see cref="INotifyPropertyChanged"/>.
/// </summary>
public interface INotifyPropertyChangedEx : INotifyPropertyChanged
{
	/// <summary>
	/// Raise event <see cref="INotifyPropertyChanged.PropertyChanged"/>.
	/// </summary>
	/// <param name="propertyName">Property name.</param>
	void NotifyPropertyChanged([CallerMemberName]string propertyName = null);
}

/// <summary>
/// Extension class for <see cref="INotifyPropertyChangedEx"/>.
/// </summary>
public static class NotifyPropertyChangedExHelper
{
	/// <summary>
	/// Filter.
	/// </summary>
	public static Func<object, string, bool> Filter { get; set; }

	/// <summary>
	/// Invoke <see cref="INotifyPropertyChangedEx.NotifyPropertyChanged"/>.
	/// </summary>
	/// <param name="entity">Notify based object.</param>
	/// <param name="propertyName">Property name.</param>
	public static void Notify<T>(this T entity, [CallerMemberName]string propertyName = null)
		where T : INotifyPropertyChangedEx
	{
		if (null == Filter || Filter(entity, propertyName))
			entity.NotifyPropertyChanged(propertyName);
	}
}