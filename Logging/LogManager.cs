namespace Ecng.Logging;

using Nito.AsyncEx;

/// <summary>
/// Messages logging manager that monitors the <see cref="ILogSource.Log"/> event and forwards messages to the <see cref="LogManager.Listeners"/>.
/// </summary>
public class LogManager : Disposable, IPersistable
{
	private sealed class ApplicationReceiver : BaseLogReceiver
	{
		public ApplicationReceiver()
		{
			Name = TypeHelper.ApplicationName;
			LogLevel = LogLevels.Info;
		}
	}

	private sealed class LogSourceList(LogManager parent) : BaseList<ILogSource>
	{
		private readonly LogManager _parent = parent ?? throw new ArgumentNullException(nameof(parent));

		protected override bool OnAdding(ILogSource item)
		{
			item.Log += _parent.SourceLog;
			return base.OnAdding(item);
		}

		protected override bool OnRemoving(ILogSource item)
		{
			item.Log -= _parent.SourceLog;
			return base.OnRemoving(item);
		}

		protected override bool OnClearing()
		{
			foreach (var item in this)
				OnRemoving(item);

			return base.OnClearing();
		}
	}

	private sealed class DisposeLogMessage : LogMessage
	{
		private readonly object _syncRoot = new();
		private bool _processed;

		public DisposeLogMessage()
			: base(new ApplicationReceiver(), DateTime.MinValue, LogLevels.Off, string.Empty)
		{
			IsDispose = true;
		}

		public void Wait(TimeSpan timeout)
		{
			lock (_syncRoot)
			{
				// Bounded wait so a missed Pulse (e.g. an exception before the message was
				// delivered) degrades to a delayed dispose instead of a permanent hang.
				if (!_processed)
					Monitor.Wait(_syncRoot, timeout);

				_processed = false;
			}
		}

		public void Pulse()
		{
			lock (_syncRoot)
			{
				_processed = true;
				Monitor.Pulse(_syncRoot);
			}
		}
	}

	private readonly DisposeLogMessage _disposeMessage = new();

	private readonly Lock _syncRoot = new();
	private readonly List<LogMessage> _pendingMessages = [];
	private readonly ControllablePeriodicTimer _flushTimer;
	private bool _isFlushing;
	private readonly bool _asyncMode;
	private readonly UnhandledExceptionSource _unhandledExceptionSource;

	private static LogManager _instance;

	/// <summary>
	/// The ambient <see cref="LogManager"/> singleton — the first instance constructed.
	/// </summary>
	public static LogManager Instance => _instance;

	/// <summary>
	/// Initializes a new instance of the <see cref="LogManager"/>.
	/// </summary>
	public LogManager()
		: this(true)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="LogManager"/>.
	/// </summary>
	/// <param name="asyncMode">Asynchronous mode.</param>
	public LogManager(bool asyncMode)
	{
		_instance ??= this;
		_unhandledExceptionSource = new();

		Sources = new LogSourceList(this)
		{
			Application,
			_unhandledExceptionSource
		};

		_asyncMode = asyncMode;

		if (!_asyncMode)
			return;

		_flushTimer = AsyncHelper.CreatePeriodicTimer(FlushAsync);

		FlushInterval = TimeSpan.FromMilliseconds(500);
	}

	private async Task FlushAsync()
	{
		// Loop so messages added while a flush is in progress (and the dispose marker) are drained
		// by this same call rather than waiting for the next timer tick - in sync mode there is no
		// timer, and the >1M overflow safeguard relies on a flush actually emptying the backlog.
		while (true)
		{
			LogMessage[] temp;

			using (_syncRoot.EnterScope())
			{
				if (_isFlushing)
					return;

				temp = _pendingMessages.CopyAndClear();

				if (temp.Length == 0)
					return;

				_isFlushing = true;
			}

			// Capture the dispose marker up-front so DisposeManaged's Wait is always released, even
			// if building or delivering the messages throws below.
			DisposeLogMessage disposeMessage = null;

			foreach (var message in temp)
			{
				if (message.IsDispose)
				{
					disposeMessage = (DisposeLogMessage)message;
					break;
				}
			}

			try
			{
				var messages = new List<LogMessage>();

				ILogSource prevSource = null;
				var level = default(LogLevels);

				foreach (var message in temp)
				{
					if (prevSource == null || prevSource != message.Source)
					{
						prevSource = message.Source;
						level = prevSource.GetLogLevel();
					}

					if (level == LogLevels.Inherit)
						level = Application.LogLevel;

					if (level <= message.Level)
						messages.Add(message);
				}

				if (messages.Count > 0)
				{
					var listeners = _listeners.Cache;

					await listeners.Select(async listener =>
					{
						try
						{
							if (listener is IAsyncLogListener all)
								await all.WriteMessagesAsync(messages);
							else
								listener.WriteMessages(messages);
						}
						catch (Exception ex)
						{
							Trace.WriteLine(ex);
						}
					}).WhenAll();
				}
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex);
			}
			finally
			{
				disposeMessage?.Pulse();

				using (_syncRoot.EnterScope())
					_isFlushing = false;
			}
		}
	}

	private ILogReceiver _application = new ApplicationReceiver();

	/// <summary>
	/// The all application level logs recipient.
	/// </summary>
	public ILogReceiver Application
	{
		get => _application;
		set
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			if (value == _application)
				return;

			Sources.Remove(_application);
			_application = value;
			Sources.Add(_application);
		}
	}

	private readonly CachedSynchronizedSet<ILogListener> _listeners = new(true);

	/// <summary>
	/// Messages loggers arriving from <see cref="Sources"/>.
	/// </summary>
	public IList<ILogListener> Listeners => _listeners;

	/// <summary>
	/// Logs sources which are listened to the event <see cref="ILogSource.Log"/>.
	/// </summary>
	public IList<ILogSource> Sources { get; }

	/// <summary>
	/// Sending interval of messages collected from <see cref="Sources"/> to the <see cref="Listeners"/>. The default is 500 ms.
	/// </summary>
	public TimeSpan FlushInterval
	{
		get => _flushTimer?.Interval ?? TimeSpan.MaxValue;
		set
		{
			if (!_asyncMode)
				return;

			if (value < TimeSpan.FromMilliseconds(1))
				throw new ArgumentOutOfRangeException(nameof(value), value, "Cannot be less than 1 millisecond.");

			// PeriodicTimer's period maxes out near uint.MaxValue ms; clamp so a persisted
			// TimeSpan.MaxValue (the value the getter returns in sync mode) loaded into an async
			// manager can't throw inside the timer task and kill periodic flushing.
			var maxInterval = TimeSpan.FromMilliseconds(int.MaxValue);
			if (value > maxInterval)
				value = maxInterval;

			if (_flushTimer.IsRunning)
				_flushTimer.ChangeInterval(value);
			else
				_flushTimer.Start(value);
		}
	}

	private void SourceLog(LogMessage message)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		var callFlush = false;
		var callImmediate = false;

		using (_syncRoot.EnterScope())
		{
			_pendingMessages.Add(message);

			if (!_asyncMode)
				callFlush = true;
			else
			{
				// mika: force flush in case too many messages
				if (_pendingMessages.Count > 1000000)
					callImmediate = true;
			}
		}

		try
		{
			if (callFlush)
				_ = FlushAsync();
			else if (callImmediate)
				ImmediateFlush();
		}
		catch (Exception ex)
		{
			Trace.WriteLine(ex);
		}
	}

	private void ImmediateFlush()
	{
		// Actually flush now. FlushAsync drains its whole backlog and is serialized by _isFlushing,
		// and it works in sync mode (no timer). The old timer restart only reset the countdown and
		// did not flush until a full interval elapsed, defeating the overflow safeguard and delaying
		// dispose.
		_ = FlushAsync();
	}

	/// <summary>
	/// Clear pending messages on dispose.
	/// </summary>
	public bool ClearPendingOnDispose { get; set; } = true;

	/// <summary>
	/// Release resources.
	/// </summary>
	protected override void DisposeManaged()
	{
		Sources.Clear();
		_unhandledExceptionSource.Dispose();

		using (_syncRoot.EnterScope())
		{
			if (ClearPendingOnDispose)
				_pendingMessages.Clear();

			_pendingMessages.Add(_disposeMessage);
		}

		// Drain pending (incl. the dispose marker) and wait for listeners to be notified in both
		// async and sync modes - sync mode has no timer but FlushAsync still delivers and pulses,
		// so listeners that act on the dispose message (e.g. FileLogListener) are notified.
		ImmediateFlush();

		_disposeMessage.Wait(TimeSpan.FromSeconds(10));
		_flushTimer?.Dispose();

		if (ReferenceEquals(_instance, this))
			_instance = null;

		base.DisposeManaged();
	}

	/// <summary>
	/// Load settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public virtual void Load(SettingsStorage storage)
	{
		if (storage.Contains(nameof(FlushInterval)))
			FlushInterval = storage.GetValue<TimeSpan>(nameof(FlushInterval));

		//MaxMessageCount = storage.GetValue<int>(nameof(MaxMessageCount));
		Listeners.AddRange(storage.GetValue<IEnumerable<SettingsStorage>>(nameof(Listeners)).Select(s =>
		{
			// TODO 2025-02-04: remove 1 year after
			s.Set("type", s.GetValue<string>("type").Replace("StockSharp.Logging", "Ecng.Logging"));
			return s.LoadEntire<ILogListener>();
		}));

		if (storage.Contains(nameof(Application)) && Application is IPersistable appPers)
			appPers.Load(storage, nameof(Application));
	}

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public virtual void Save(SettingsStorage storage)
	{
		storage.SetValue(nameof(FlushInterval), FlushInterval);
		//storage.SetValue(nameof(MaxMessageCount), MaxMessageCount);
		storage.SetValue(nameof(Listeners), Listeners.Where(l => l.CanSave).Select(l => l.SaveEntire(false)).ToArray());

		if (Application is IPersistable appPers)
			storage.SetValue(nameof(Application), appPers.Save());
	}
}
