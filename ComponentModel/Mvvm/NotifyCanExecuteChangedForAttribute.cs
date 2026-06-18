namespace Ecng.ComponentModel;

using System;

/// <summary>
/// Placed alongside <see cref="ObservablePropertyAttribute"/> on a backing field: when the generated
/// property changes, the generator calls <c>RaiseCanExecuteChanged</c> on the listed commands. Mirrors
/// <c>CommunityToolkit.Mvvm.ComponentModel.NotifyCanExecuteChangedForAttribute</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
public sealed class NotifyCanExecuteChangedForAttribute : Attribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="NotifyCanExecuteChangedForAttribute"/> class.
	/// </summary>
	/// <param name="commandName">The command whose <c>CanExecuteChanged</c> must be raised.</param>
	public NotifyCanExecuteChangedForAttribute(string commandName)
	{
		CommandName = commandName;
		OtherCommandNames = [];
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="NotifyCanExecuteChangedForAttribute"/> class.
	/// </summary>
	/// <param name="commandName">The command whose <c>CanExecuteChanged</c> must be raised.</param>
	/// <param name="otherCommandNames">Additional command names.</param>
	public NotifyCanExecuteChangedForAttribute(string commandName, params string[] otherCommandNames)
	{
		CommandName = commandName;
		OtherCommandNames = otherCommandNames ?? [];
	}

	/// <summary>
	/// The primary command name.
	/// </summary>
	public string CommandName { get; }

	/// <summary>
	/// Additional command names.
	/// </summary>
	public string[] OtherCommandNames { get; }
}
