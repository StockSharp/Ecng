namespace Ecng.Interop.Dde;

using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

using Ecng.Serialization;

/// <summary>
/// Represents the settings for DDE (Dynamic Data Exchange) communication.
/// </summary>
[DisplayName("DDE settings")]
public class DdeSettings : Cloneable<DdeSettings>, IAsyncPersistable
{
	/// <summary>
	/// Initializes a new instance of the <see cref="DdeSettings"/> class with default values.
	/// </summary>
	public DdeSettings()
	{
		Server = "EXCEL";
		Topic = "[Book1.xlsx]Sheet1";
	}

	/// <summary>
	/// Gets or sets the DDE server name.
	/// </summary>
	[Display(Name = "Server", Description = "DDE server name.", Order = 0)]
	public string Server { get; set; }

	/// <summary>
	/// Gets or sets the DDE topic name (for example, "[Book1.xlsx]Sheet1").
	/// </summary>
	[Display(Name = "Topic", Description = "Topic name (like [Book1.xlsx].Sheet1).", Order = 1)]
	public string Topic { get; set; }

	private int _columnOffset;

	/// <summary>
	/// Gets or sets the column offset from the left top corner.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when a negative value is assigned.</exception>
	[Display(Name = "Column offset", Description = "Column offset from left top corner.", Order = 2)]
	public int ColumnOffset
	{
		get { return _columnOffset; }
		set
		{
			if (value < 0)
				throw new ArgumentOutOfRangeException(nameof(value), value, "Value cannot be negative.");

			_columnOffset = value;
		}
	}

	private int _rowOffset;

	/// <summary>
	/// Gets or sets the row offset from the left top corner.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when a negative value is assigned.</exception>
	[Display(Name = "Row offset", Description = "Row offset from left top corner.", Order = 2)]
	public int RowOffset
	{
		get { return _rowOffset; }
		set
		{
			if (value < 0)
				throw new ArgumentOutOfRangeException(nameof(value), value, "Value cannot be negative.");

			_rowOffset = value;
		}
	}

	/// <summary>
	/// Gets or sets a value indicating whether header names should be displayed.
	/// </summary>
	[Display(Name = "Headers", Description = "Show headers name.", Order = 2)]
	public bool ShowHeaders { get; set; }

	/// <summary>
	/// Applies the settings from the specified <see cref="DdeSettings"/> instance.
	/// </summary>
	/// <param name="clone">The instance containing the new settings.</param>
	public void Apply(DdeSettings clone)
	{
		if (clone is null)
			throw new ArgumentNullException(nameof(clone));

		Server = clone.Server;
		Topic = clone.Topic;
		ColumnOffset = clone.ColumnOffset;
		RowOffset = clone.RowOffset;
		ShowHeaders = clone.ShowHeaders;
	}

	/// <summary>
	/// Creates a deep copy of the current <see cref="DdeSettings"/> instance.
	/// </summary>
	/// <returns>A new instance that is a deep copy of this instance.</returns>
	public override DdeSettings Clone()
	{
		return (DdeSettings)MemberwiseClone();
	}

	/// <summary>
	/// Loads the settings from the provided <see cref="SettingsStorage"/>.
	/// </summary>
	/// <param name="storage">The storage from which to load the settings.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="Task"/></returns>
	public Task LoadAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		Server = storage.GetValue<string>(nameof(Server));
		Topic = storage.GetValue<string>(nameof(Topic));
		ColumnOffset = storage.GetValue<int>(nameof(ColumnOffset));
		RowOffset = storage.GetValue<int>(nameof(RowOffset));
		ShowHeaders = storage.GetValue<bool>(nameof(ShowHeaders));

		return Task.CompletedTask;
	}

	/// <summary>
	/// Saves the current settings to the provided <see cref="SettingsStorage"/>.
	/// </summary>
	/// <param name="storage">The storage to which the settings will be saved.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="Task"/></returns>
	public Task SaveAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		storage
			.Set(nameof(Server), Server)
			.Set(nameof(Topic), Topic)
			.Set(nameof(ColumnOffset), ColumnOffset)
			.Set(nameof(RowOffset), RowOffset)
			.Set(nameof(ShowHeaders), ShowHeaders);

		return Task.CompletedTask;
	}
}