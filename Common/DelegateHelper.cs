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

		public static void SafeInvoke(this Action handler)
		{
			var h = handler;

			if (h != null)
				h();
		}

		public static void SafeInvoke<T>(this Action<T> handler, T arg)
		{
			var h = handler;

			if (h != null)
				h(arg);
		}

		public static void SafeInvoke<T1, T2>(this Action<T1, T2> handler, T1 arg1, T2 arg2)
		{
			var h = handler;

			if (h != null)
				h(arg1, arg2);
		}

		public static void SafeInvoke<T1, T2, T3>(this Action<T1, T2, T3> handler, T1 arg1, T2 arg2, T3 arg3)
		{
			var h = handler;

			if (h != null)
				h(arg1, arg2, arg3);
		}

		public static void SafeInvoke<T1, T2, T3, T4>(this Action<T1, T2, T3, T4> handler, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
		{
			var h = handler;

			if (h != null)
				h(arg1, arg2, arg3, arg4);
		}

		public static void SafeInvoke<T1, T2, T3, T4, T5>(this Action<T1, T2, T3, T4, T5> handler, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
		{
			var h = handler;

			if (h != null)
				h(arg1, arg2, arg3, arg4, arg5);
		}

		public static void SafeInvoke<T1, T2, T3, T4, T5, T6>(this Action<T1, T2, T3, T4, T5, T6> handler, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
		{
			var h = handler;

			if (h != null)
				h(arg1, arg2, arg3, arg4, arg5, arg6);
		}

		public static void SafeInvoke<T1, T2, T3, T4, T5, T6, T7>(this Action<T1, T2, T3, T4, T5, T6, T7> handler, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
		{
			var h = handler;

			if (h != null)
				h(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
		}

		public static void SafeInvoke<T1, T2, T3, T4, T5, T6, T7, T8>(this Action<T1, T2, T3, T4, T5, T6, T7, T8> handler, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
		{
			var h = handler;

			if (h != null)
				h(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
		}

		public static void SafeInvoke<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> handler, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
		{
			var h = handler;

			if (h != null)
				h(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
		}

		public static void SafeInvoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> handler, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
		{
			var h = handler;

			if (h != null)
				h(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
		}

		public static void SafeInvoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> handler, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11)
		{
			var h = handler;

			if (h != null)
				h(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
		}

		public static void SafeInvoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> handler, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12)
		{
			var h = handler;

			if (h != null)
				h(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
		}

		public static void SafeInvoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> handler, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13)
		{
			var h = handler;

			if (h != null)
				h(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13);
		}

		public static void SafeInvoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> handler, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14)
		{
			var h = handler;

			if (h != null)
				h(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14);
		}

		public static void SafeInvoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> handler, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15)
		{
			var h = handler;

			if (h != null)
				h(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15);
		}

		public static void SafeInvoke(this EventHandler<EventArgs> handler, object sender)
		{
			handler.SafeInvoke(sender, EventArgs.Empty);
		}

		public static void SafeInvoke<T>(this EventHandler<T> handler, object sender, T args)
			where T : EventArgs
		{
			handler.SafeInvoke(sender, args, args2 => { });
		}

		public static void SafeInvoke<T>(this EventHandler<T> handler, object sender, T args, Action<T> action)
			where T : EventArgs
		{
			if (sender == null)
				throw new ArgumentNullException(nameof(sender));

			if (args == null)
				throw new ArgumentNullException(nameof(args));

			if (action == null)
				throw new ArgumentNullException(nameof(action));

			var handlerLocal = handler;
			if (handlerLocal != null)
			{
				handlerLocal(sender, args);
				action(args);
			}
		}

		public static EventHandler<TArgs> Cast<TArgs>(this Delegate handler)
			where TArgs : EventArgs 
		{
			if (handler == null)
				return null;

			dynamic h = handler;

			// Resharper shows wrong hint
			// DO NOT convert to method GROUP
			return (sender, e) => h(sender, e);
		}

		public static EventHandler<EventArgs> Cast(this EventHandler handler)
		{
			return handler.Cast<EventArgs>();
		}

		public static void SafeInvoke(this PropertyChangedEventHandler handler, object sender, string name)
		{
			if (handler == null)
				return;

			handler(sender, new PropertyChangedEventArgs(name));
		}

		public static void SafeInvoke(this PropertyChangingEventHandler handler, object sender, string name)
		{
			if (handler == null)
				return;

			handler(sender, new PropertyChangingEventArgs(name));
		}

		public static T CreateDelegate<T>(this MethodInfo method)
		{
			return Delegate.CreateDelegate(typeof(T), method, true).To<T>();
		}

		public static T CreateDelegate<I, T>(this MethodInfo method, I instance)
		{
			return Delegate.CreateDelegate(typeof(T), instance, method, true).To<T>();
		}

		public static T AddDelegate<T>(this T source, T value)
		{
			return Delegate.Combine(source.To<Delegate>(), value.To<Delegate>()).To<T>();
		}

		public static T RemoveDelegate<T>(this T source, T value)
		{
			return Delegate.Remove(source.To<Delegate>(), value.To<Delegate>()).To<T>();
		}

		public static void RemoveAllDelegates<T>(this T source)
		{
			foreach (var item in source.GetInvocationList())
				source.RemoveDelegate(item);
		}

		public static IEnumerable<T> GetInvocationList<T>(this T @delegate)
		{
			var dlg = @delegate.To<Delegate>();
			var list = dlg != null ? dlg.GetInvocationList() : ArrayHelper.Empty<Delegate>();
			return list.Cast<T>();
		}
	}
}