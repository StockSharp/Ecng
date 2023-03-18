namespace Ecng.Backup;

using System;

using Ecng.Common;

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

	/// <inheritdoc />
	public override string ToString() => GetFullPath();

	public string GetFullPath()
	{
		var path = Name;

		if (path.IsEmpty())
			throw new InvalidOperationException();

		if (Parent is not null)
			path = Parent.GetFullPath() + $"/{path}";

		return path;
	}
}