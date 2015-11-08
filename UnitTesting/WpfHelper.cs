namespace Ecng.UnitTesting
{
	using System;
	using System.Threading;
	using System.Windows;
	using System.Windows.Threading;

	using Ecng.Common;

	public static class WpfHelper
	{
		/// <summary>
		/// wrapps a control in a window so that it can be unit tested
		/// </summary>
		private sealed class ControlWrapper : Window
		{
			/// <summary>
			/// constructor to wrap a control inside a window
			/// </summary>
			/// <param name="controlToWrap">The control to wrap in the window</param>
			public ControlWrapper(object controlToWrap)
			{
				Content = controlToWrap;
			}
		}

		/// <summary>
		/// Checks if the object is of type window
		/// If not it creates a wrapper window
		/// </summary>
		/// <param name="instance">The instance of the object to check</param>
		/// <returns>Returns a window instance</returns>
		public static Window VerifyObjectType(object instance)
		{
			var returnValue = instance as Window;

			//return the value if it is alread a window
			if (returnValue != null)
				return returnValue;

			//wrap the control in a window and return it
			return new ControlWrapper(instance);
		}

		public static Action InitWpf(Action init)
		{
			if (init == null)
				throw new ArgumentNullException(nameof(init));

			using (var waitHandle = new AutoResetEvent(false))
			{
				Action finish = null;

				Action create = () =>
				{
					if (Application.Current == null)
						new Application();

					init();

					var frame = new DispatcherFrame();
					finish = () => frame.Continue = false;
					waitHandle.Set();
					Dispatcher.PushFrame(frame);
				};

				Action a = () => create.InvokeAsSTA();
				a.BeginInvoke(null, null);
				waitHandle.WaitOne();
				return finish;
			}
		}
	}
}