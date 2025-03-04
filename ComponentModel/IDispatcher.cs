namespace Ecng.ComponentModel;

using System;
using System.Threading.Tasks;

/// <summary>
/// Threads dispatcher.
/// </summary>
public interface IDispatcher
{
	/// <summary>
	/// Call action in dispatcher thread.
	/// </summary>
	/// <param name="action">Action.</param>
	void Invoke(Action action);

	/// <summary>
	/// Call action in dispatcher thread.
	/// </summary>
	/// <param name="action">Action.</param>
	void InvokeAsync(Action action);

	/// <summary>
	/// Verify that current thread is dispatcher thread.
	/// </summary>
	/// <returns>Operation result.</returns>
	bool CheckAccess();
}

/// <summary>
/// Dummy dispatcher.
/// </summary>
public class DummyDispatcher : IDispatcher
{
	bool IDispatcher.CheckAccess() => true;
	void IDispatcher.Invoke(Action action) => action();
	void IDispatcher.InvokeAsync(Action action) => Task.Run(action);
}