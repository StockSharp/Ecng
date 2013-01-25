namespace Ecng.Xaml
{
	using System;
	using System.Windows.Threading;

	using Ecng.ComponentModel;

	public class XamlEventDispatcher : EventDispatcher
	{
		private readonly Dispatcher _dispatcher;

		public XamlEventDispatcher(Action<Exception> errorHandler)
			: this(errorHandler, XamlHelper.CurrentThreadDispatcher)
		{
		}

		public XamlEventDispatcher(Action<Exception> errorHandler, Dispatcher dispatcher)
			: base(errorHandler)
		{
			if (dispatcher == null)
				throw new ArgumentNullException("dispatcher");

			_dispatcher = dispatcher;
		}

		public override void Add(Action evt, string syncToken)
		{
			if (evt == null)
				throw new ArgumentNullException("evt");

			base.Add(() => _dispatcher.GuiSync(evt), syncToken);
		}
	}
}