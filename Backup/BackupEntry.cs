namespace Ecng.Backup;

using System;
using System.Collections.Generic;

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

	/// <summary>
	/// Gets the full path of the element.
	/// </summary>
	/// <returns>Full path.</returns>
	public string GetFullPath()
	{
		var visited = new HashSet<BackupEntry>();
		return GetFullPath(visited);
	}

	private string GetFullPath(HashSet<BackupEntry> visited)
	{
		if (!visited.Add(this))
			throw new InvalidOperationException("Circular reference detected in parent chain.");

		var path = Name;

		if (path.IsEmpty())
			throw new InvalidOperationException("Entry name is empty.");

		if (Parent is not null)
			path = Parent.GetFullPath(visited) + $"/{path}";

		return path;
	}
}