namespace Ecng.ComponentModel;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Ecng.Collections;
using Ecng.Common;

/// <summary>
/// Forward <see cref="INotifyPropertyChanged"/> notifications to dispatcher thread.
/// Multiple notifications for the same property may be forwarded only once.
/// </summary>
public class DispatcherNotifiableObject<T> : CustomObjectWrapper<T>
	where T : class, INotifyPropertyChanged
{
	private readonly IDispatcher _dispatcher;
    private readonly IDisposable _subscription;
    private readonly SynchronizedSet<string> _names = [];

	/// <summary>
	/// Instance of <see cref="DispatcherNotifiableObject{T}"/>.
	/// </summary>
	/// <param name="dispatcher">Dispatcher to use for invoking property changed notifications.</param>
	/// <param name="obj">Parent object to wrap.</param>
	/// <param name="interval">Interval between property changed notifications.</param>
	public DispatcherNotifiableObject(IDispatcher dispatcher, T obj, TimeSpan interval)
		: base(obj)
	{
		_dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
		_subscription = _dispatcher.InvokePeriodically(TimerOnTick, interval);

		Obj.PropertyChanged += ObjOnPropertyChanged;
	}

    private void ObjOnPropertyChanged(object sender, PropertyChangedEventArgs e)
		=> _names.Add(e.PropertyName);

	private void TimerOnTick()
	{
		if (IsDisposed)
			return;

		string[] names;

		using (_names.EnterScope())
		{
			names = [.. _names.Where(NeedToNotify)];
			_names.Clear();
		}

		if (names.Length == 0)
			return;

		names.ForEach(OnPropertyChanged);
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		Obj.PropertyChanged -= ObjOnPropertyChanged;
		_subscription.Dispose();

		base.DisposeManaged();
	}

	/// <summary>
	/// Called when property changed.
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
				.Concat([descriptor]);
	}
}