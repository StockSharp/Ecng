namespace Ecng.ComponentModel
{
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;

    using Ecng.Collections;

    using MoreLinq;

	class DispatcherNotifiableObjectTimer
	{
		private readonly TimeSpan _minInterval = TimeSpan.FromMilliseconds(100);
		private TimeSpan _interval = TimeSpan.FromMilliseconds(1000);

		public event Action Tick;

		public TimeSpan Interval
		{
			get => _interval;
			set
			{
				if (value > _interval)
					return;

				if (value < _minInterval)
					value = _minInterval;

				_interval = value;
			}
		}

		public DispatcherNotifiableObjectTimer() => Task.Run(TimerTask);

		private async Task TimerTask()
		{
			while (true)
			{
				await Task.Delay(_interval);
				Tick?.Invoke();
			}
			// ReSharper disable once FunctionNeverReturns
		}

		private static readonly Lazy<DispatcherNotifiableObjectTimer> _instance = new(true);
		public static DispatcherNotifiableObjectTimer Instance => _instance.Value;
	}
	
	/// <summary>
	/// Forward <see cref="INotifyPropertyChanged"/> notifications to dispatcher thread.
	/// Multiple notifications for the same property may be forwarded only once.
	/// </summary>
	public class DispatcherNotifiableObject<T> : CustomObjectWrapper<T>
		where T : class, INotifyPropertyChanged
    {
		private static DispatcherNotifiableObjectTimer Timer => DispatcherNotifiableObjectTimer.Instance;

		/// <summary>
		/// </summary>
		protected TimeSpan NotifyInterval
        {
            get => _notifyInterval;
            set
            {
                _notifyInterval = value;
				Timer.Interval = value;
            }
        }

		private readonly IDispatcher _dispatcher;
		private readonly SynchronizedSet<string> _names = new();
        private DateTime _nextTime;
        private TimeSpan _notifyInterval;

        /// <summary>
        /// </summary>
        public DispatcherNotifiableObject(IDispatcher dispatcher, T obj)
			: base(obj)
        {
			_dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
			NotifyInterval = TimeSpan.FromMilliseconds(333);
			Timer.Tick += NotifiableObjectGuiWrapperTimerOnTick;

            Obj.PropertyChanged += (_, args) => _names.Add(args.PropertyName);
        }

        private void NotifiableObjectGuiWrapperTimerOnTick()
        {
            if (IsDisposed) return;

            var now = DateTime.UtcNow;
            if (now < _nextTime)
                return;

            var interval = NotifyInterval;
            if (interval < Timer.Interval)
                interval = Timer.Interval;

            _nextTime = now + interval;

            string[] names;

            lock(_names.SyncRoot)
            {
                names = _names.Where(NeedToNotify).ToArray();
                _names.Clear();
            }

            if (names.Length == 0)
                return;

			_dispatcher.InvokeAsync(() => names.ForEach(OnPropertyChanged));
        }

        /// <inheritdoc />
        protected override void DisposeManaged()
        {
			Timer.Tick -= NotifiableObjectGuiWrapperTimerOnTick;

            base.DisposeManaged();
        }

        /// <summary>
        /// </summary>
        protected virtual bool NeedToNotify(string propName) => true;

        /// <inheritdoc />
        protected override IEnumerable<EventDescriptor> OnGetEvents()
        {
            var descriptor = TypeDescriptor
				.GetEvents(this, true)
				.OfType<EventDescriptor>()
				.First(ed => ed.Name == nameof(PropertyChanged));

            return
                base.OnGetEvents()
                    .Where(ed => ed.Name != nameof(PropertyChanged))
                    .Concat(new[] { descriptor });
        }
    }
}