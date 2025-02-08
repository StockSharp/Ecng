﻿namespace Ecng.ComponentModel;

using System;

using Ecng.Serialization;

/// <summary>
/// The interfaces describes debugger.
/// </summary>
public interface IDebugger : IPersistable
{
	/// <summary>
	/// <see langword="false" />, if the debugger is used. Otherwise, <see langword="true" />.
	/// </summary>
	bool IsDisabled { get; set; }

	/// <summary>
	/// <see langword="true" />, if it is possible to go inside of the current method. Otherwise, <see langword="false" />.
	/// </summary>
	bool CanStepInto { get; }

	/// <summary>
	/// <see langword="true" />, if it is possible to go outside from the current method. Otherwise, <see langword="false" />.
	/// </summary>
	bool CanStepOut { get; }

	/// <summary>
	/// <see langword="true" />, if the debugger is stopped at the error. Otherwise, <see langword="false" />.
	/// </summary>
	bool IsWaitingOnError { get; }

	/// <summary>
	/// <see langword="true" />, if the debugger is stopped at the entry of the diagram element. Otherwise, <see langword="false" />.
	/// </summary>
	bool IsWaitingOnInput { get; }

	/// <summary>
	/// <see langword="true" />, if the debugger is stopped at the exit of the diagram element. Otherwise, <see langword="false" />.
	/// </summary>
	bool IsWaitingOnOutput { get; }

	/// <summary>
	/// Current block.
	/// </summary>
	object CurrentBlock { get; }

	/// <summary>
	/// The event of continue execution.
	/// </summary>
	event Action Continued;

	/// <summary>
	/// The event of the stop at the breakpoint.
	/// </summary>
	event Action<object> Break;

	/// <summary>
	/// The event of the stop at the error.
	/// </summary>
	event Action<object> Error;

	/// <summary>
	/// The event of the bock changed.
	/// </summary>
	event Action<object> BlockChanged;

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
	/// Step into the method.
	/// </summary>
	void StepInto();

	/// <summary>
	/// Step out from the method.
	/// </summary>
	void StepOut();

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
	/// The event of the stop at the error.
	/// </summary>
	new event Action<TMethod> Error;

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