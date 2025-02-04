namespace Ecng.Logging;

using System.ComponentModel;

using Ecng.ComponentModel;

/// <summary>
/// Logs source interface.
/// </summary>
public interface ILogSource : IDisposable
{
	/// <summary>
	/// The unique identifier of the source.
	/// </summary>
	Guid Id { get; }

	/// <summary>
	/// The source name.
	/// </summary>
	string Name { get; set; }

	/// <summary>
	/// Parental logs source.
	/// </summary>
	ILogSource Parent { get; set; }

	/// <summary>
	/// <see cref="Parent"/> removed.
	/// </summary>
	event Action<ILogSource> ParentRemoved;

	/// <summary>
	/// The logging level for the source.
	/// </summary>
	LogLevels LogLevel { get; set; }

	/// <summary>
	/// Current time, which will be passed to the <see cref="LogMessage.Time"/>.
	/// </summary>
	DateTimeOffset CurrentTime { get; }

	/// <summary>
	/// Whether the source is the root (even if <see cref="ILogSource.Parent"/> is not equal to <see langword="null" />).
	/// </summary>
	bool IsRoot { get; }

	/// <summary>
	/// New debug message event.
	/// </summary>
	event Action<LogMessage> Log;
}

/// <summary>
/// The base implementation <see cref="ILogSource"/>.
/// </summary>
public abstract class BaseLogSource : Disposable, ILogSource, IPersistable
{
	/// <summary>
	/// Initialize <see cref="BaseLogSource"/>.
	/// </summary>
	protected BaseLogSource()
	{
		_name = GetType().GetDisplayName();
	}

	/// <inheritdoc />
	[Browsable(false)]
	//[Display(
	//	ResourceType = typeof(LocalizedStrings),
	//	Name = LocalizedStrings.IdKey,
	//	Description = LocalizedStrings.IdKey,
	//	GroupName = LocalizedStrings.LoggingKey,
	//	Order = 1000)]
	[ReadOnly(true)]
	public virtual Guid Id { get; set; } = Guid.NewGuid();

	private string _name;

	/// <inheritdoc />
	[ReadOnly(true)]
	//[Display(
	//	ResourceType = typeof(LocalizedStrings),
	//	Name = LocalizedStrings.NameKey,
	//	Description = LocalizedStrings.LogSourceNameKey,
	//	GroupName = LocalizedStrings.LoggingKey,
	//	Order = 1001)]
	public virtual string Name
	{
		get => _name;
		set
		{
			if (value.IsEmpty())
				throw new ArgumentNullException(nameof(value));

			_name = value;
		}
	}

	private ILogSource _parent;

	/// <inheritdoc />
	[Browsable(false)]
	public ILogSource Parent
	{
		get => _parent;
		set
		{
			if (value == _parent)
				return;

			if (value != null && _parent != null)
				throw new ArgumentException($"Parent for {this} already set to {_parent}", nameof(value));

			if (value == this)
				throw new ArgumentException($"Cyclic dependency detected for {this}.", nameof(value));

			_parent = value;

			if (_parent == null)
				ParentRemoved?.Invoke(this);
		}
	}

	/// <inheritdoc />
	public event Action<ILogSource> ParentRemoved;

	/// <inheritdoc />
	//[Display(
	//	ResourceType = typeof(LocalizedStrings),
	//	Name = LocalizedStrings.LogLevelKey,
	//	Description = LocalizedStrings.LogLevelKey + LocalizedStrings.Dot,
	//	GroupName = LocalizedStrings.LoggingKey,
	//	Order = 1001)]
	public virtual LogLevels LogLevel { get; set; } = LogLevels.Inherit;

	/// <inheritdoc />
	[Browsable(false)]
	public virtual DateTimeOffset CurrentTime => TimeHelper.NowWithOffset;

	/// <inheritdoc />
	[Browsable(false)]
	public bool IsRoot { get; set; }

	private Action<LogMessage> _log;

	/// <inheritdoc />
	public event Action<LogMessage> Log
	{
		add => _log += value;
		remove => _log -= value;
	}

	/// <summary>
	/// To call the event <see cref="ILogSource.Log"/>.
	/// </summary>
	/// <param name="message">A debug message.</param>
	protected virtual void RaiseLog(LogMessage message)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		if (message.Level < message.Source.LogLevel)
			return;

		_log?.Invoke(message);

		var parent = Parent as ILogReceiver;

		parent?.AddLog(message);
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return Name;
	}

	/// <summary>
	/// Load settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public virtual void Load(SettingsStorage storage)
	{
		LogLevel = storage.GetValue(nameof(LogLevel), LogLevels.Inherit);
		Name = storage.GetValue(nameof(Name), Name);
	}

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public virtual void Save(SettingsStorage storage)
	{
		storage.SetValue(nameof(LogLevel), LogLevel.To<string>());
		storage.SetValue(nameof(Name), Name);
	}
}