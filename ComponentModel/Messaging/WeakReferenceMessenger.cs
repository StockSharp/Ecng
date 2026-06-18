namespace Ecng.ComponentModel;

/// <summary>
/// An <see cref="IMessenger"/> that keeps weak references to recipients: a recipient may be collected
/// while still registered, so explicit unregistration is optional. This is the recommended default.
/// </summary>
public sealed class WeakReferenceMessenger : MessengerBase
{
	/// <inheritdoc />
	protected override bool IsWeak => true;

	/// <summary>
	/// The shared default instance.
	/// </summary>
	public static WeakReferenceMessenger Default { get; } = new();
}
