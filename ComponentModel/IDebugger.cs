namespace Ecng.ComponentModel;

using System;

using Ecng.Serialization;

/// <summary>
/// The interfaces describes debugger.
/// </summary>
public interface IDebugger : IPersistable
{
	/// <summary>
	/// <see langword="true" />, if it is possible to go inside of the current method. Otherwise, <see langword="false" />.
	/// </summary>
	bool CanStepInto { get; }

	/// <summary>
	/// <see langword="true" />, if it is possible to go outside from the current method. Otherwise, <see langword="false" />.
	/// </summary>
	bool CanStepOut { get; }

	/// <summary>
	/// <see langword="true" />, if the debugger is stopped at the entry or exit of the method. Otherwise, <see langword="false" />.
	/// </summary>
	bool IsWaiting { get; }

	/// <summary>
	/// The event of continue execution.
	/// </summary>
	event Action Continued;

	/// <summary>
	/// The event of the stop at the breakpoint.
	/// </summary>
	event Action Break;

	/// <summary>
	/// The event of changes breakpoints.
	/// </summary>
	event Action Changed;

	/// <summary>
	/// Continue.
	/// </summary>
	void Continue();

	/// <summary>
	/// To go to the next line.
	/// </summary>
	void StepNext();

	/// <summary>
	/// Remove all breakpoints from the code.
	/// </summary>
	void RemoveAllBreaks();
}

/// <summary>
/// The interfaces describes debugger.
/// </summary>
public interface IDebugger<TLine, TMethod> : IDebugger
{
	/// <summary>
	/// The event of the stop at the breakpoint.
	/// </summary>
	new event Action<TLine> Break;

	/// <summary>
	/// To add a breakpoint in the line.
	/// </summary>
	/// <param name="line">Line.</param>
	void AddBreak(TLine line);

	/// <summary>
	/// To remove the breakpoint from the line.
	/// </summary>
	/// <param name="line">Line.</param>
	void RemoveBreak(TLine line);

	/// <summary>
	/// Whether the line is the breakpoint.
	/// </summary>
	/// <param name="line">Line.</param>
	/// <returns><see langword="true" />, if the line is the breakpoint, otherwise, <see langword="false" />.</returns>
	bool IsBreak(TLine line);

	/// <summary>
	/// To go inside the method.
	/// </summary>
	/// <param name="method">Method.</param>
	void StepInto(TMethod method);

	/// <summary>
	/// To exit from the method.
	/// </summary>
	/// <param name="method">Method.</param>
	void StepOut(TMethod method);
}