namespace Ecng.ComponentModel;

using System;

/// <summary>
/// Placed alongside <see cref="ObservablePropertyAttribute"/> on a field or partial property of an
/// <see cref="ObservableRecipient"/>: when the generated property changes, the generator also
/// broadcasts a <see cref="PropertyChangedMessage{T}"/> via the recipient's messenger. Mirrors
/// <c>CommunityToolkit.Mvvm.ComponentModel.NotifyPropertyChangedRecipientsAttribute</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class NotifyPropertyChangedRecipientsAttribute : Attribute
{
}
