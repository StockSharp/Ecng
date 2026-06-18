namespace Ecng.ComponentModel;

using System;

/// <summary>
/// Marks a backing field so the Ecng MVVM source generator emits a public observable property
/// for it, integrating with the Ecng notification bases (<see cref="NotifiableObject"/>,
/// <see cref="ViewModelBase"/>). The attribute name mirrors CommunityToolkit's for familiarity.
/// </summary>
/// <remarks>
/// The containing class must be declared <c>partial</c> and derive (directly or transitively) from a
/// type exposing a property-change notification method. The generated property name is derived from
/// the field name (<c>_name</c>/<c>name</c>/<c>m_name</c> → <c>Name</c>). The generator also emits the
/// partial hooks <c>On{Name}Changing</c>/<c>On{Name}Changed</c> (value and old/new overloads).
/// </remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class ObservablePropertyAttribute : Attribute
{
}
