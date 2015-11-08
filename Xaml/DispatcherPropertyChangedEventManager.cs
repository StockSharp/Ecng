namespace Ecng.Xaml
{
	using System;
	using System.Collections;
	using System.ComponentModel;
	using System.Reflection;
	using System.Windows;

	using ClrPatch;

	using Ecng.Common;
	using Ecng.Reflection;
	using Ecng.Collections;

	[CLSCompliant(false)]
	public class DispatcherPropertyChangedEventManager : DerivablePropertyChangedEventManager
	{
		public static void Init()
		{
			GuiDispatcher.InitGlobalDispatcher();
			new DispatcherPropertyChangedEventManager(GuiDispatcher.GlobalDispatcher);
		}

		private readonly GuiDispatcher _dispatcher;
		private readonly PropertyChangedEventManager _underlyingManager;
		private readonly FastInvoker<PropertyChangedEventManager, object[], VoidType> _propMethod;
		private readonly object _sourceTable;
		private readonly object _destinationTable;
		private readonly PropertyInfo _indexer;

		private readonly SynchronizedSet<Tuple<object,string>> _propChangedEvents = new SynchronizedSet<Tuple<object, string>>();

		private DispatcherPropertyChangedEventManager(GuiDispatcher dispatcher)
		{
			if (dispatcher == null)
				throw new ArgumentNullException(nameof(dispatcher));

			var tableType = "MS.Internal.WeakEventTable, WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35".To<Type>();
			var table = tableType.GetValue<VoidType, object>("CurrentWeakEventTable", null);

			this.SetValue("_table", table);

			_dispatcher = dispatcher;

			_underlyingManager = ReflectionHelper.CreateInstance<PropertyChangedEventManager>();
			_underlyingManager.SetValue("_table", tableType.CreateInstance());

			WeakEventManager.SetCurrentManager(typeof(PropertyChangedEventManager), this);

			_indexer = table.GetType().GetMember<PropertyInfo>("Item", typeof(WeakEventManager), typeof(object));
			_sourceTable = table;
			_destinationTable = _underlyingManager.GetValue<PropertyChangedEventManager, VoidType, object>("_table", null);

			var sourceHashTable = table.GetValue<object, VoidType, Hashtable>("_dataTable", null);

			var sourceInfo = "MS.Internal.WeakEventTable+EventKey, WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35".To<Type>().GetMember<FieldInfo>("_source");

			foreach (var key in sourceHashTable.Keys)
			{
				_indexer.SetValue(_destinationTable, sourceHashTable[key], new[] { this, sourceInfo.GetValue(key) });
			}

			_propMethod = FastInvoker<PropertyChangedEventManager, object[], VoidType>.Create(typeof(PropertyChangedEventManager).GetMember<MethodInfo>("OnPropertyChanged"));

			_dispatcher.AddPeriodicalAction(FlushPropertyChangedEvents);
		}

		public class Listener : IWeakEventListener
		{
			private readonly Action<object, PropertyChangedEventArgs> _action;

			public string PropertyName { get; }

			public INotifyPropertyChanged Source { get; }

			public Listener(INotifyPropertyChanged source, string property, Action<object, PropertyChangedEventArgs> action)
			{
				Source = source;
				PropertyName = property;
				_action = action;
			}

			public bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
			{
				_action(sender, e as PropertyChangedEventArgs);
				return true;
			}
		}

		public static Listener AddListener(INotifyPropertyChanged source, string property, Action<object, PropertyChangedEventArgs> action)
		{
			var listener = new Listener(source, property, action);
			AddListener(source, listener, property);
			return listener;
		}

		public static void RemoveListener(Listener listener)
		{
			RemoveListener(listener.Source, listener, listener.PropertyName);
		}

		private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			_propChangedEvents.Add(new Tuple<object, string>(sender, e.PropertyName));
		}

		private void FlushPropertyChangedEvents()
		{
			var events = _propChangedEvents.SyncGet(c => c.CopyAndClear());

			foreach (var ev in events)
			{
				_propMethod.VoidInvoke(_underlyingManager, new[] {ev.Item1, new PropertyChangedEventArgs(ev.Item2)});
			}
		}

		protected override void StartListening(object source)
		{
			var listener = _indexer.GetValue(_sourceTable, new[] { this, source });
			_indexer.SetValue(_destinationTable, listener, new[] { _underlyingManager, source });

			((INotifyPropertyChanged)source).PropertyChanged += OnPropertyChanged;
		}

		protected override void StopListening(object source)
		{
			var pc = source as INotifyPropertyChanged;

			if (pc == null)
			{
				var wr = source as WeakReference;

				if (wr != null)
				{
					pc = wr.Target as INotifyPropertyChanged;
				}
			}

			if (pc != null)
				pc.PropertyChanged -= OnPropertyChanged;
		}
	}
}