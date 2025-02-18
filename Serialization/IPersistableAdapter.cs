namespace Ecng.Serialization
{
	/// <summary>
	/// Provides an adapter for persisting values.
	/// </summary>
	public interface IPersistableAdapter
	{
		/// <summary>
		/// Gets or sets the underlying value that is persisted.
		/// </summary>
		object UnderlyingValue { get; set; }
	}
}