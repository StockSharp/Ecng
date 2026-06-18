namespace Ecng.ComponentModel;

using System;

/// <summary>
/// Placed alongside <see cref="ObservablePropertyAttribute"/> on a backing field: when the generated
/// property changes, the generator also raises <c>PropertyChanged</c> for the listed dependent
/// properties. Mirrors <c>CommunityToolkit.Mvvm.ComponentModel.NotifyPropertyChangedForAttribute</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
public sealed class NotifyPropertyChangedForAttribute : Attribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="NotifyPropertyChangedForAttribute"/> class.
	/// </summary>
	/// <param name="propertyName">The dependent property whose change notification must be raised.</param>
	public NotifyPropertyChangedForAttribute(string propertyName)
	{
		PropertyName = propertyName;
		OtherPropertyNames = [];
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="NotifyPropertyChangedForAttribute"/> class.
	/// </summary>
	/// <param name="propertyName">The dependent property whose change notification must be raised.</param>
	/// <param name="otherPropertyNames">Additional dependent property names.</param>
	public NotifyPropertyChangedForAttribute(string propertyName, params string[] otherPropertyNames)
	{
		PropertyName = propertyName;
		OtherPropertyNames = otherPropertyNames ?? [];
	}

	/// <summary>
	/// The primary dependent property name.
	/// </summary>
	public string PropertyName { get; }

	/// <summary>
	/// Additional dependent property names.
	/// </summary>
	public string[] OtherPropertyNames { get; }
}
