namespace Ecng.Server.Utils
{
	using Ecng.Logging;

	/// <summary>
	/// Base server settings.
	/// </summary>
	public abstract class ServiceSettingsBase
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ServiceSettingsBase"/>.
		/// </summary>
		protected ServiceSettingsBase()
		{
		}

		/// <summary>
		/// WebAPI address.
		/// </summary>
		public string WebApiAddress { get; set; }

		/// <summary>
		/// <see cref="LogLevels"/>.
		/// </summary>
		public LogLevels LogLevel { get; set; }
	}
}