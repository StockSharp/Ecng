namespace Ecng.ComponentModel;

/// <summary>
/// Telegram channel.
/// </summary>
public interface ITelegramChannel
{
	/// <summary>
	/// Channel id.
	/// </summary>
	long Id { get; }

	/// <summary>
	/// Channel name.
	/// </summary>
	string Name { get; }
}