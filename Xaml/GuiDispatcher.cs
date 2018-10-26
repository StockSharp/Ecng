namespace Ecng.Xaml
{
	using System;
	using System.Threading;
	using System.Windows.Threading;
	using System.Diagnostics;

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
				_action = action ?? throw new ArgumentNullException(nameof(action));
			}

			public ActionInfo(Func<object> func)
			{
				_func = func ?? throw new ArgumentNullException(nameof(func));
			}

			public void Process()
			{
				try
				{
					if (_action != null)
						_action();
					else
						_result = _func();
				}
				finally
				{
					if (_lock != null)
					{
						lock (_lock)
						{
							_processed = true;
							Monitor.Pulse(_lock);
						}
					}
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
		private readonly CachedSynchronizedDictionary<object, Action> _periodicalActions = new CachedSynchronizedDictionary<object, Action>();
		private readonly SynchronizedDictionary<Action, int> _periodicalActionsErrors = new SynchronizedDictionary<Action, int>();

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
			Dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
		}

		/// <summary>
		/// Объект для доступа к графическому потоку.
		/// </summary>
		public Dispatcher Dispatcher { get; }

		public event Action<Exception> Error;

		private int _maxPeriodicalActionErrors = 100;

		public int MaxPeriodicalActionErrors
		{
			get => _maxPeriodicalActionErrors;
			set
			{
				if (value < -1)
					throw new ArgumentOutOfRangeException(nameof(value));

				_maxPeriodicalActionErrors = value;
			}
		}

		private TimeSpan _interval = TimeSpan.FromMilliseconds(1);

		/// <summary>
		/// Интервал обработки накопленных действий. По-умолчанию равен 1 млс.
		/// </summary>
		public TimeSpan Interval
		{
			get => _interval;
			set
			{
				if (value <= TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(value));

				_interval = value;
				StopTimer();
				StartTimer();
			}
		}

		/// <summary>
		/// Количество действий, которое ожидает обработку.
		/// </summary>
		public int PendingActionsCount => _actions.Count + _periodicalActions.Count;

		public object AddPeriodicalAction(Action action)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));

			var token = new object();

			_periodicalActions.Add(token, action);
			StartTimer();

			return token;
		}

		/// <summary>
		/// Выполнить все действия в очереди.
		/// </summary>
		public void FlushPendingActions()
		{
			_flushSignal = true;
		}

		public void RemovePeriodicalAction(object token)
		{
			if (token == null)
				throw new ArgumentNullException(nameof(token));

			var action = _periodicalActions.TryGetAndRemove(token);

			if (action != null)
				_periodicalActionsErrors.Remove(action);
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

				try
				{
					action.Process();
				}
				catch (Exception ex)
				{
					try
					{
						Error?.Invoke(ex);
					}
					catch (Exception ex2)
					{
						Debug.WriteLine(ex2);
					}
				}
			}

			foreach (var pair in _periodicalActions.CachedPairs)
			{
				var action = pair.Value;

				hasActions = true;

				try
				{
					action();
					_periodicalActionsErrors.Remove(action);
				}
				catch (Exception ex)
				{
					try
					{
						Error?.Invoke(ex);
					}
					catch (Exception ex2)
					{
						Debug.WriteLine(ex2);
					}

					if (MaxPeriodicalActionErrors >= 0)
					{
						if (!_periodicalActionsErrors.TryGetValue(action, out var counter))
							counter = 0;

						counter++;

						if (counter >= MaxPeriodicalActionErrors)
							_periodicalActions.Remove(pair.Key);

						_periodicalActionsErrors[action] = counter;
					}
				}
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

		public static GuiDispatcher GlobalDispatcher => _globalDispatcher ?? (_globalDispatcher = new GuiDispatcher());

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
