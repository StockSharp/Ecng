namespace Ecng.Backup.Mega.Native;

using System;

/// <summary>
/// MEGA node type.
/// </summary>
public enum NodeType
{
	/// <summary>
	/// File node.
	/// </summary>
	File = 0,
	/// <summary>
	/// Directory node.
	/// </summary>
	Directory = 1,
	/// <summary>
	/// Root node.
	/// </summary>
	Root = 2,
	/// <summary>
	/// Inbox node.
	/// </summary>
	Inbox = 3,
	/// <summary>
	/// Trash node.
	/// </summary>
	Trash = 4,
}

/// <summary>
/// MEGA node (file or directory) with decrypted metadata.
/// </summary>
public sealed record Node
{
	/// <summary>
	/// Node id (handle).
	/// </summary>
	public string Id { get; init; } = string.Empty;

	/// <summary>
	/// Node type.
	/// </summary>
	public NodeType Type { get; init; }

	/// <summary>
	/// Parent node id (handle).
	/// </summary>
	public string ParentId { get; init; }

	/// <summary>
	/// Node name (from decrypted attributes).
	/// </summary>
	public string Name { get; init; }

	/// <summary>
	/// Public handle for exported nodes (null when not exported).
	/// </summary>
	public string PublicHandle { get; init; }

	/// <summary>
	/// File size in bytes (0 for folders).
	/// </summary>
	public long Size { get; init; }

	/// <summary>
	/// Creation date, if available.
	/// </summary>
	public DateTime? CreationDate { get; init; }

	/// <summary>
	/// Modification date, if available.
	/// </summary>
	public DateTime? ModificationDate { get; init; }

	// Present for file nodes.

	/// <summary>
	/// File key (AES).
	/// </summary>
	public byte[] FileKey { get; init; }

	/// <summary>
	/// IV for AES-CTR.
	/// </summary>
	public byte[] Iv { get; init; }

	/// <summary>
	/// Expected file meta-MAC.
	/// </summary>
	public byte[] MetaMac { get; init; }

	/// <summary>
	/// Decrypted node key used for public links (16 bytes for folders, 32 bytes for files).
	/// </summary>
	public byte[] NodeKey { get; init; }
}
