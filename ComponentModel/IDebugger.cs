﻿namespace Ecng.ComponentModel;

using System;

using Ecng.Serialization;

/// <summary>
/// The interfaces describes debugger.
/// </summary>
public interface IDebugger : IPersistable, IDisposable
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
	/// Current executing block.
	/// </summary>
	object ExecBlock { get; }

	/// <summary>
	/// Current hover block.
	/// </summary>
	object HoverBlock { get; }

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