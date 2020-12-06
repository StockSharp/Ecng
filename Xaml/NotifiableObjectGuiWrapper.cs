namespace Ecng.Xaml
{
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;

    using Ecng.ComponentModel;
    using Ecng.Collections;

    using MoreLinq;

    static class NotifiableObjectGuiWrapperTimer
    {
        private static readonly TimeSpan MinInterval = TimeSpan.FromMilliseconds(100);
        private static TimeSpan _interval = TimeSpan.FromMilliseconds(1000);
        public static event Action Tick;

        public static TimeSpan Interval
        {
            get => _interval;
            set
            {
                if (value > _interval)
                    return;

                if (value < MinInterval)
                    value = MinInterval;

                _interval = value;
            }
        }

        static NotifiableObjectGuiWrapperTimer() => Task.Run(TimerTask);

        static async Task TimerTask()
        {
            while (true)
            {
                await Task.Delay(_interval);
                Tick?.Invoke();
            }
        }
    }

    public abstract class NotifiableObjectGuiWrapper<T> : CustomObjectWrapper<T> where T : class, INotifyPropertyChanged
    {
        protected TimeSpan NotifyInterval
        {
            get => _notifyInterval;
            set
            {
                _notifyInterval = value;
                NotifiableObjectGuiWrapperTimer.Interval = value;
            }
        }

        private readonly SynchronizedSet<string> _names = new SynchronizedSet<string>();
        private DateTime _nextTime;
        private TimeSpan _notifyInterval;

        protected NotifiableObjectGuiWrapper(T obj) : base(obj)
        {
            NotifyInterval = TimeSpan.FromMilliseconds(333);
            NotifiableObjectGuiWrapperTimer.Tick += NotifiableObjectGuiWrapperTimerOnTick;

            Obj.PropertyChanged += (_, args) => _names.Add(args.PropertyName);
        }

        private void NotifiableObjectGuiWrapperTimerOnTick()
        {
            if (IsDisposed) return;

            var now = DateTime.UtcNow;
            if (now < _nextTime)
                return;

            var interval = NotifyInterval;
            if (interval < NotifiableObjectGuiWrapperTimer.Interval)
                interval = NotifiableObjectGuiWrapperTimer.Interval;

            _nextTime = now + interval;

            string[] names;

            lock(_names.SyncRoot)
            {
                names = _names.Where(NeedToNotify).ToArray();
                _names.Clear();
            }

            if (names.Length == 0)
                return;

            GuiDispatcher.GlobalDispatcher.Dispatcher.GuiAsync(() => names.ForEach(OnPropertyChanged));
        }

        protected override void DisposeManaged()
        {
            NotifiableObjectGuiWrapperTimer.Tick -= NotifiableObjectGuiWrapperTimerOnTick;

            base.DisposeManaged();
        }

        protected virtual bool NeedToNotify(string propName) => true;

        protected override IEnumerable<EventDescriptor> OnGetEvents()
        {
            var myEventDescriptor = TypeDescriptor.GetEvents(this, true).OfType<EventDescriptor>().First(ed => ed.Name == nameof(PropertyChanged));

            return
                base.OnGetEvents()
                    .Where(ed => ed.Name != nameof(PropertyChanged))
                    .Concat(new[] { myEventDescriptor });
        }
    }
}