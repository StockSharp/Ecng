namespace Ecng.Xaml
{
	using System;
	using System.Threading;
	using System.Windows.Threading;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Reflection;

	/// <summary>
	/// Специальный класс, обеспечивающий исполнение действий в графическом потоке.
	/// </summary>
	public class GuiDispatcher : Disposable
	{
		private sealed class ActionInfo
		{
			private readonly Action _action;
			private readonly Func<object> _func;
			private object _lock;
			private object _result;
			private bool _processed;

			public ActionInfo(Action action)
			{
				if (action == null)
					throw new ArgumentNullException("action");

				_action = action;
			}

			public ActionInfo(Func<object> func)
			{
				if (func == null)
					throw new ArgumentNullException("func");

				_func = func;
			}

			public void Process()
			{
				if (_action != null)
					_action();
				else
					_result = _func();

				if (_lock == null)
					return;

				lock (_lock)
				{
					_processed = true;
					Monitor.Pulse(_lock);
				}
			}

			public void Sync()
			{
				_lock = new object();
			}

			public T Wait<T>()
			{
				lock (_lock)
				{
					if (!_processed)
						Monitor.Wait(_lock);
				}

				return _result.To<T>();
			}
		}

		private DispatcherTimer _timer;
		private readonly object _lock = new object();
		private DateTime _lastTime;
		private long _counter;
		private bool _flushSignal;
		private readonly TimeSpan _timeOut = TimeSpan.FromSeconds(30);
		private readonly SynchronizedList<ActionInfo> _actions = new SynchronizedList<ActionInfo>();
		private readonly CachedSynchronizedList<Action> _periodicalActions = new CachedSynchronizedList<Action>();

		/// <summary>
		/// Создать <see cref="GuiDispatcher"/>.
		/// </summary>
		public GuiDispatcher()
			: this(XamlHelper.CurrentThreadDispatcher)
		{
		}

		/// <summary>
		/// Создать <see cref="GuiDispatcher"/>.
		/// </summary>
		/// <param name="dispatcher">Объект для доступа к графическому потоку.</param>
		public GuiDispatcher(Dispatcher dispatcher)
		{
			if (dispatcher == null)
				throw new ArgumentNullException("dispatcher");

			Dispatcher = dispatcher;
		}

		/// <summary>
		/// Объект для доступа к графическому потоку.
		/// </summary>
		public Dispatcher Dispatcher { get; private set; }

		private TimeSpan _interval = TimeSpan.FromMilliseconds(100);

		/// <summary>
		/// Интервал обработки накопленных действий. По-умолчанию равен 100 млс.
		/// </summary>
		public TimeSpan Interval
		{
			get { return _interval; }
			set
			{
				if (value <= TimeSpan.Zero)
					throw new ArgumentOutOfRangeException("value");

				_interval = value;
				StopTimer();
				StartTimer();
			}
		}

		/// <summary>
		/// Количество действий, которое ожидает обработку.
		/// </summary>
		public int PendingActionsCount
		{
			get { return _actions.Count + _periodicalActions.Count; }
		}

		public void AddPeriodicalAction(Action action)
		{
			if (action == null)
				throw new ArgumentNullException("action");

			_periodicalActions.Add(action);
			StartTimer();
		}

		/// <summary>
		/// Выполнить все действия в очереди.
		/// </summary>
		public void FlushPendingActions()
		{
			_flushSignal = true;
		}

		public void RemovePeriodicalAction(Action action)
		{
			if (action == null)
				throw new ArgumentNullException("action");

			_periodicalActions.Remove(action);
		}

		/// <summary>
		/// Добавить действие.
		/// </summary>
		/// <param name="action">Действие.</param>
		public void AddAction(Action action)
		{
			if (Dispatcher.CheckAccess())
			{
				action();
				return;
			}

			AddAction(new ActionInfo(action));
		}

		/// <summary>
		/// Добавить действие. Пока оно не будет обработано, метод не отдаст управление программе.
		/// </summary>
		/// <param name="action">Действие.</param>
		public void AddSyncAction(Action action)
		{
			if (Dispatcher.CheckAccess())
			{
				action();
				return;
			}

			var info = new ActionInfo(action);
			info.Sync();

			AddAction(info);

			info.Wait<VoidType>();
		}

		/// <summary>
		/// Добавить действие. Пока оно не будет обработано, метод не отдаст управление программе.
		/// </summary>
		/// <param name="action">Действие, возвращающее результат.</param>
		public T AddSyncAction<T>(Func<T> action)
		{
			if (Dispatcher.CheckAccess())
			{
				return action();
			}

			var info = new ActionInfo(() => action());
			info.Sync();

			AddAction(info);

			return info.Wait<T>();
		}

		private void AddAction(ActionInfo info)
		{
			_actions.Add(info);
			StartTimer();
		}

		private void StartTimer()
		{
			_lastTime = DateTime.Now;

			lock (_lock)
			{
				if (_timer != null)
					return;

				_timer = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher);
				_timer.Tick += OnTimerTick;
				_timer.Interval = new TimeSpan(Interval.Ticks / 10);
				_timer.Start();
			}
		}

		private void OnTimerTick(object sender, EventArgs e)
		{
			if (++_counter % 10 != 0 && !_flushSignal)
				return;

			_flushSignal = false;

			var hasActions = false;

			foreach (var action in _actions.SyncGet(c => c.CopyAndClear()))
			{
				hasActions = true;
				action.Process();
			}

			foreach (var action in _periodicalActions.Cache)
			{
				hasActions = true;
				action();
			}

			if (hasActions)
				return;

			if ((DateTime.Now - _lastTime) > _timeOut)
			{
				StopTimer();
			}
		}

		private void StopTimer()
		{
			lock (_lock)
			{
				if (_timer != null)
				{
					_timer.Stop();
					_timer = null;
				}
			}
		}

		private static GuiDispatcher _globalDispatcher;

		public static GuiDispatcher GlobalDispatcher
		{
			get { return _globalDispatcher ?? (_globalDispatcher = new GuiDispatcher()); }
		}

		public static void InitGlobalDispatcher()
		{
			//if (_globalDispatcher != null)
			//	throw new InvalidOperationException("GlobalDispatcher is already initialized.");
			if (_globalDispatcher == null)
				_globalDispatcher = new GuiDispatcher();
		}

		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			StopTimer();
			base.DisposeManaged();
		}
	}
}
