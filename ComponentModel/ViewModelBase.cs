namespace Ecng.ComponentModel;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Linq.Expressions;

using Ecng.Common;

/// <summary>
/// Base class for implementing View-Model in MVVM pattern.
/// </summary>
public abstract class ViewModelBase : Disposable, INotifyPropertyChanged
{
	private List<IDisposable> _commands;
	private readonly Dictionary<string, object> _values = [];

	/// <inheritdoc />
	public event PropertyChangedEventHandler PropertyChanged;

	/// <summary>
	/// Raises the PropertyChanged event.
	/// </summary>
	/// <param name="name">Property name.</param>
	protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}

	/// <summary>
	/// Raises the PropertyChanged event using expression.
	/// </summary>
	/// <typeparam name="T">Property type.</typeparam>
	/// <param name="selectorExpression">Property selector expression.</param>
	protected void OnPropertyChanged<T>(Expression<Func<T>> selectorExpression)
	{
		OnPropertyChanged(PropertyName(selectorExpression));
	}

	/// <summary>
	/// Gets property value from internal storage.
	/// </summary>
	/// <typeparam name="T">Property type.</typeparam>
	/// <param name="name">Property name (auto-filled by compiler).</param>
	/// <returns>Property value or default if not set.</returns>
	protected T GetValue<T>([CallerMemberName] string name = null)
		=> _values.TryGetValue(name, out var value) ? (T)value : default;

	/// <summary>
	/// Sets property value and raises PropertyChanged if value changed.
	/// </summary>
	/// <typeparam name="T">Property type.</typeparam>
	/// <param name="value">New value.</param>
	/// <param name="name">Property name (auto-filled by compiler).</param>
	/// <returns>True if value was changed.</returns>
	protected bool SetValue<T>(T value, [CallerMemberName] string name = null)
	{
		if (_values.TryGetValue(name, out var existing) && EqualityComparer<T>.Default.Equals((T)existing, value))
			return false;

		_values[name] = value;
		OnPropertyChanged(name);
		return true;
	}

	/// <summary>
	/// Sets field value and raises PropertyChanged if value changed.
	/// </summary>
	/// <typeparam name="T">Field type.</typeparam>
	/// <param name="field">Field reference.</param>
	/// <param name="value">New value.</param>
	/// <param name="selectorExpression">Property selector expression.</param>
	/// <returns>True if value was changed.</returns>
	protected bool SetField<T>(ref T field, T value, Expression<Func<T>> selectorExpression)
	{
		return SetField(ref field, value, PropertyName(selectorExpression));
	}

	/// <summary>
	/// Sets field value and raises PropertyChanged if value changed.
	/// </summary>
	/// <typeparam name="T">Field type.</typeparam>
	/// <param name="field">Field reference.</param>
	/// <param name="value">New value.</param>
	/// <param name="name">Property name.</param>
	/// <returns>True if value was changed.</returns>
	protected virtual bool SetField<T>(ref T field, T value, [CallerMemberName] string name = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value)) return false;

		field = value;
		OnPropertyChanged(name);

		return true;
	}

	/// <summary>
	/// Registers a command to be disposed when this ViewModel is disposed.
	/// </summary>
	/// <typeparam name="TCommand">Command type.</typeparam>
	/// <param name="command">The command to register.</param>
	/// <returns>The registered command for method chaining.</returns>
	protected TCommand RegisterCommand<TCommand>(TCommand command)
		where TCommand : IDisposable
	{
		_commands ??= [];
		_commands.Add(command);
		return command;
	}

	/// <summary>
	/// Creates and registers a command without parameter.
	/// </summary>
	/// <param name="execute">The execution logic.</param>
	/// <param name="canExecute">The execution status logic.</param>
	/// <returns>The created command.</returns>
	protected DelegateCommand CreateCommand(Action execute, Func<object, bool> canExecute = null)
		=> RegisterCommand(new DelegateCommand(_ => execute(), canExecute));

	/// <summary>
	/// Creates and registers a command with parameter.
	/// </summary>
	/// <typeparam name="T">Command parameter type.</typeparam>
	/// <param name="execute">The execution logic.</param>
	/// <param name="canExecute">The execution status logic.</param>
	/// <returns>The created command.</returns>
	protected DelegateCommand<T> CreateCommand<T>(Action<T> execute, Func<T, bool> canExecute = null)
		=> RegisterCommand(new DelegateCommand<T>(execute, canExecute));

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		if (_commands != null)
		{
			foreach (var cmd in _commands)
				cmd.Dispose();

			_commands.Clear();
		}

		base.DisposeManaged();
	}

	/// <summary>
	/// Gets property name from expression.
	/// </summary>
	/// <typeparam name="T">Property type.</typeparam>
	/// <param name="property">Property expression.</param>
	/// <returns>Property name.</returns>
	public static string PropertyName<T>(Expression<Func<T>> property)
	{
		var lambda = (LambdaExpression)property;

		MemberExpression memberExpression;

		if (lambda.Body is UnaryExpression exp)
		{
			var unaryExpression = exp;
			memberExpression = (MemberExpression)unaryExpression.Operand;
		}
		else
		{
			memberExpression = (MemberExpression)lambda.Body;
		}

		return memberExpression.Member.Name;
	}
}
