namespace Ecng.ComponentModel;

/// <summary>
/// An <see cref="IMessenger"/> that keeps strong references to recipients: a recipient stays
/// registered (and reachable) until it is explicitly unregistered.
/// </summary>
public sealed class StrongReferenceMessenger : MessengerBase
{
	/// <inheritdoc />
	protected override bool IsWeak => false;

	/// <summary>
	/// The shared default instance.
	/// </summary>
	public static StrongReferenceMessenger Default { get; } = new();
}
