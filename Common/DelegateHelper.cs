namespace Ecng.Common;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Linq;

/// <summary>
/// Provides helper methods for working with delegates.
/// </summary>
public static class DelegateHelper
{
	/// <summary>
	/// Executes the specified action and handles any exception using the provided error action.
	/// </summary>
	/// <param name="action">The action to execute.</param>
	/// <param name="error">The action to handle exceptions.</param>
	/// <exception cref="ArgumentNullException">Thrown when action or error is null.</exception>
	public static void Do(this Action action, Action<Exception> error)
	{
		if (action is null)
			throw new ArgumentNullException(nameof(action));

		if (error is null)
			throw new ArgumentNullException(nameof(error));

		try
		{
			action();
		}
		catch (Exception ex)
		{
			error(ex);
		}
	}

	/// <summary>
	/// Executes the specified action asynchronously and handles any exception using the provided error action.
	/// </summary>
	/// <param name="action">The action to execute asynchronously.</param>
	/// <param name="error">The action to handle exceptions.</param>
	/// <exception cref="ArgumentNullException">Thrown when action or error is null.</exception>
	public static void DoAsync(this Action action, Action<Exception> error)
	{
		if (action is null)
			throw new ArgumentNullException(nameof(action));

		if (error is null)
			throw new ArgumentNullException(nameof(error));

		Do(() => action.BeginInvoke(result =>
		{
			try
			{
				action.EndInvoke(result);
			}
			catch (Exception ex)
			{
				error(ex);
			}
		}, null), error);
	}

	//// The following methods are commented out.
	///// <summary>
	///// Casts a delegate to an EventHandler of the specified event argument type.
	///// </summary>
	///// <typeparam name="TArgs">The type of the event arguments.</typeparam>
	///// <param name="handler">The delegate to cast.</param>
	///// <returns>An EventHandler of type TArgs if the cast is successful; otherwise, null.</returns>
	//public static EventHandler<TArgs> Cast<TArgs>(this Delegate handler)
	//	where TArgs : EventArgs 
	//{
	//	if (handler is null)
	//		return null;

	//	dynamic h = handler;

	//	// Resharper shows wrong hint
	//	// DO NOT convert to method GROUP
	//	return (sender, e) => h(sender, e);
	//}

	///// <summary>
	///// Casts an EventHandler to EventHandler of EventArgs.
	///// </summary>
	///// <param name="handler">The EventHandler to cast.</param>
	///// <returns>An EventHandler of EventArgs if the cast is successful; otherwise, null.</returns>
	//public static EventHandler<EventArgs> Cast(this EventHandler handler)
	//{
	//	return handler.Cast<EventArgs>();
	//}

	/// <summary>
	/// Invokes the PropertyChangedEventHandler with the specified sender and property name.
	/// </summary>
	/// <param name="handler">The PropertyChangedEventHandler to invoke.</param>
	/// <param name="sender">The sender object.</param>
	/// <param name="name">The name of the property that changed.</param>
	public static void Invoke(this PropertyChangedEventHandler handler, object sender, string name)
	{
		handler(sender, new PropertyChangedEventArgs(name));
	}

	/// <summary>
	/// Invokes the PropertyChangingEventHandler with the specified sender and property name.
	/// </summary>
	/// <param name="handler">The PropertyChangingEventHandler to invoke.</param>
	/// <param name="sender">The sender object.</param>
	/// <param name="name">The name of the property that is changing.</param>
	public static void Invoke(this PropertyChangingEventHandler handler, object sender, string name)
	{
		handler(sender, new PropertyChangingEventArgs(name));
	}

	/// <summary>
	/// Creates a delegate of type TDelegate for the specified method.
	/// </summary>
	/// <typeparam name="TDelegate">The type of delegate to create.</typeparam>
	/// <param name="method">The method information.</param>
	/// <returns>A delegate of type TDelegate.</returns>
	public static TDelegate CreateDelegate<TDelegate>(this MethodInfo method)
	{
		return Delegate.CreateDelegate(typeof(TDelegate), method, true).To<TDelegate>();
	}

	/// <summary>
	/// Creates a delegate of type TDelegate for the specified method and instance.
	/// </summary>
	/// <typeparam name="TInstance">The type of the instance.</typeparam>
	/// <typeparam name="TDelegate">The type of delegate to create.</typeparam>
	/// <param name="method">The method information.</param>
	/// <param name="instance">The instance on which the method is invoked.</param>
	/// <returns>A delegate of type TDelegate bound to the specified instance.</returns>
	public static TDelegate CreateDelegate<TInstance, TDelegate>(this MethodInfo method, TInstance instance)
	{
		return Delegate.CreateDelegate(typeof(TDelegate), instance, method, true).To<TDelegate>();
	}

	/// <summary>
	/// Combines two delegates of the same type.
	/// </summary>
	/// <typeparam name="TDelegate">The type of the delegate.</typeparam>
	/// <param name="source">The source delegate.</param>
	/// <param name="value">The delegate to combine with the source delegate.</param>
	/// <returns>A combined delegate of type TDelegate.</returns>
	public static TDelegate AddDelegate<TDelegate>(this TDelegate source, TDelegate value)
	{
		return Delegate.Combine(source.To<Delegate>(), value.To<Delegate>()).To<TDelegate>();
	}

	/// <summary>
	/// Removes the specified delegate from the source delegate.
	/// </summary>
	/// <typeparam name="TDelegate">The type of the delegate.</typeparam>
	/// <param name="source">The source delegate.</param>
	/// <param name="value">The delegate to remove from the source.</param>
	/// <returns>The resulting delegate of type TDelegate after removal.</returns>
	public static TDelegate RemoveDelegate<TDelegate>(this TDelegate source, TDelegate value)
	{
		return Delegate.Remove(source.To<Delegate>(), value.To<Delegate>()).To<TDelegate>();
	}

	/// <summary>
	/// Removes all invocation entries from the delegate.
	/// </summary>
	/// <typeparam name="TDelegate">The type of the delegate.</typeparam>
	/// <param name="source">The delegate from which all delegates are removed.</param>
	public static void RemoveAllDelegates<TDelegate>(this TDelegate source)
	{
		foreach (var item in source.GetInvocationList())
			source.RemoveDelegate(item);
	}

	/// <summary>
	/// Returns an enumerable collection of delegates from the delegate's invocation list.
	/// </summary>
	/// <typeparam name="TDelegate">The type of the delegate.</typeparam>
	/// <param name="delegate">The delegate whose invocation list is retrieved.</param>
	/// <returns>An IEnumerable of TDelegate representing the invocation list.</returns>
	public static IEnumerable<TDelegate> GetInvocationList<TDelegate>(this TDelegate @delegate)
	{
		return @delegate.To<Delegate>()?.GetInvocationList().Cast<TDelegate>() ?? [];
	}
}