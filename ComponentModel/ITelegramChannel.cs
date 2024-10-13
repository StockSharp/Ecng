namespace Ecng.ComponentModel;

/// <summary>
/// Telegram channel.
/// </summary>
public interface ITelegramChannel
{
	long Id { get; }
	string Name { get; }
}