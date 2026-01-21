namespace Ecng.ComponentModel;

using System;
using System.Threading;

/// <summary>
/// Interface for application lifecycle management.
/// Provides abstraction over shutdown and restart operations.
/// </summary>
public interface ILifetimeService
{
	/// <summary>
	/// Gets the cancellation token that is triggered when the application is shutting down.
	/// </summary>
	CancellationToken Token { get; }

	/// <summary>
	/// Initiates application shutdown.
	/// </summary>
	void Shutdown();

	/// <summary>
	/// Initiates application restart.
	/// </summary>
	void Restart();
}

/// <summary>
/// Default implementation of <see cref="ILifetimeService"/> using <see cref="AppToken"/>.
/// </summary>
/// <param name="restart">Delegate to handle restart logic.</param>
public class AppTokenLifetimeService(Action restart) : ILifetimeService
{
	private readonly Action _restart = restart ?? throw new ArgumentNullException(nameof(restart));

	/// <inheritdoc />
	public CancellationToken Token => AppToken.Value;

	/// <inheritdoc />
	public void Shutdown() => AppToken.Shutdown();

	/// <inheritdoc />
	public void Restart()
	{
		_restart();
		Shutdown();
	}
}
