namespace Ecng.ComponentModel;

using System;

/// <summary>
/// Marks a method so the Ecng MVVM source generator emits a public command property
/// (<c>{Method}Command</c>) wrapping it, producing Ecng commands (<see cref="DelegateCommand"/> /
/// <see cref="DelegateCommand{T}"/> for synchronous methods, <see cref="AsyncCommand"/> /
/// <see cref="AsyncCommand{T}"/> for <see cref="System.Threading.Tasks.Task"/>-returning methods).
/// The attribute name mirrors CommunityToolkit's for familiarity.
/// </summary>
/// <remarks>
/// Supported method shapes: <c>void()</c>, <c>void(T)</c>, <c>Task()</c>, <c>Task(T)</c>,
/// <c>Task(CancellationToken)</c>, <c>Task(T, CancellationToken)</c>. The trailing <c>Async</c> in a
/// method name is dropped when forming the command name (<c>SaveAsync</c> → <c>SaveCommand</c>).
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class RelayCommandAttribute : Attribute
{
	/// <summary>
	/// Name of a parameterless <c>bool</c> member (method or property) — or a <c>bool</c> method
	/// taking the command parameter — used as the command's <c>CanExecute</c> predicate.
	/// </summary>
	public string CanExecute { get; set; }

	/// <summary>
	/// For asynchronous commands, also emit a <c>{Method}CancelCommand</c> property that cancels the
	/// running execution.
	/// </summary>
	public bool IncludeCancelCommand { get; set; }

	/// <summary>
	/// For asynchronous commands, allow more than one execution to run at the same time.
	/// </summary>
	public bool AllowConcurrentExecutions { get; set; }
}
