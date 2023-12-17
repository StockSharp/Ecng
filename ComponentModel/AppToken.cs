namespace Ecng.ComponentModel;

using System.Threading;

/// <summary>
/// Application level <see cref="CancellationToken"/>.
/// </summary>
public static class AppToken
{
	private static readonly CancellationTokenSource _cts = new();

	/// <summary>
	/// <see cref="CancellationToken"/>.
	/// </summary>
	public static CancellationToken Value => _cts.Token;

	/// <summary>
	/// Cancel <see cref="Value"/>.
	/// </summary>
	public static void Shutdown() => _cts.Cancel();
}