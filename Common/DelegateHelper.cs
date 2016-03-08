namespace Ecng.Common
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Reflection;
	using System.Linq;

	public static class DelegateHelper
	{
		public static void Do(this Action action, Action<Exception> error)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));

			if (error == null)
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

		public static void DoAsync(this Action action, Action<Exception> error)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));

			if (error == null)
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

		//public static EventHandler<TArgs> Cast<TArgs>(this Delegate handler)
		//	where TArgs : EventArgs 
		//{
		//	if (handler == null)
		//		return null;

		//	dynamic h = handler;

		//	// Resharper shows wrong hint
		//	// DO NOT convert to method GROUP
		//	return (sender, e) => h(sender, e);
		//}

		//public static EventHandler<EventArgs> Cast(this EventHandler handler)
		//{
		//	return handler.Cast<EventArgs>();
		//}

		public static void Invoke(this PropertyChangedEventHandler handler, object sender, string name)
		{
			handler(sender, new PropertyChangedEventArgs(name));
		}

		public static void Invoke(this PropertyChangingEventHandler handler, object sender, string name)
		{
			handler(sender, new PropertyChangingEventArgs(name));
		}

		public static TDelegate CreateDelegate<TDelegate>(this MethodInfo method)
		{
			return Delegate.CreateDelegate(typeof(TDelegate), method, true).To<TDelegate>();
		}

		public static TDelegate CreateDelegate<TInstance, TDelegate>(this MethodInfo method, TInstance instance)
		{
			return Delegate.CreateDelegate(typeof(TDelegate), instance, method, true).To<TDelegate>();
		}

		public static TDelegate AddDelegate<TDelegate>(this TDelegate source, TDelegate value)
		{
			return Delegate.Combine(source.To<Delegate>(), value.To<Delegate>()).To<TDelegate>();
		}

		public static TDelegate RemoveDelegate<TDelegate>(this TDelegate source, TDelegate value)
		{
			return Delegate.Remove(source.To<Delegate>(), value.To<Delegate>()).To<TDelegate>();
		}

		public static void RemoveAllDelegates<TDelegate>(this TDelegate source)
		{
			foreach (var item in source.GetInvocationList())
				source.RemoveDelegate(item);
		}

		public static IEnumerable<TDelegate> GetInvocationList<TDelegate>(this TDelegate @delegate)
		{
			return @delegate.To<Delegate>()?.GetInvocationList().Cast<TDelegate>() ?? Enumerable.Empty<TDelegate>();
		}
	}
}