namespace Ecng.Backup
{
	using System;

	/// <summary>
	/// Storage element.
	/// </summary>
	public class BackupEntry
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BackupEntry"/>.
		/// </summary>
		public BackupEntry()
		{
		}

		/// <summary>
		/// Element name.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Parent element.
		/// </summary>
		public BackupEntry Parent { get; set; }

		/// <summary>
		/// Size in bytes.
		/// </summary>
		public long Size { get; set; }

		/// <summary>
		/// Last time modified.
		/// </summary>
		public DateTime LastModified { get; set; }

		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </returns>
		public override string ToString()
		{
			return Name;
		}
	}
}