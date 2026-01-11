namespace Ecng.Backup.Mega;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Backup.Mega.Native;

using Ecng.Common;
using Ecng.Collections;

using Nito.AsyncEx;

/// <summary>
/// The data storage service based on Mega https://mega.nz/ .
/// </summary>
/// <param name="email">Email.</param>
/// <param name="password">Password.</param>
public class MegaService(string email, SecureString password) : Disposable, IBackupService
{
	private class ProgressHandler(Action<int> progress) : IProgress<double>
	{
		private readonly Action<int> _progress = progress ?? throw new ArgumentNullException(nameof(progress));

		void IProgress<double>.Report(double value) => _progress((int)value);
	}

	private readonly Client _client = new();
	private readonly string _email = email.ThrowIfEmpty(nameof(email));
	private readonly SecureString _password = password ?? throw new ArgumentNullException(nameof(password));
	private readonly CachedSynchronizedList<Node> _nodes = [];

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		if (_client.IsLoggedIn)
			AsyncContext.Run(() => _client.LogoutAsync());

		base.DisposeManaged();
	}

	private async Task<Client> EnsureLogin(CancellationToken cancellationToken)
	{
		if (!_client.IsLoggedIn)
		{
			await _client.LoginAsync(_email, _password.UnSecure(), cancellationToken).NoWait();

			_nodes.AddRange(await _client.GetNodesAsync(cancellationToken).NoWait());
		}

		return _client;
	}

	bool IBackupService.CanFolders => true;
	bool IBackupService.CanPartialDownload => false;
	bool IBackupService.CanPublish => true;
	bool IBackupService.CanExpirable => false;

	private Node Find(BackupEntry entry)
	{
		if (entry is null)
			throw new ArgumentNullException(nameof(entry));

		var folders = new List<BackupEntry>();

		do
		{
			folders.Add(entry);
			entry = entry.Parent;
		}
		while (entry is not null);

		folders.Reverse();

		var curr = GetRoot();

		foreach (var folder in folders)
		{
			curr = GetChild(curr).FirstOrDefault(n => n.Name.EqualsIgnoreCase(folder.Name));

			if (curr is null)
				break;
		}

		return curr;
	}

	private Node GetRoot() => _nodes.Cache.First(n => n.Type == NodeType.Root);

	private IEnumerable<Node> GetChild(Node parent)
	{
		if (parent is null)
			throw new ArgumentNullException(nameof(parent));

		return _nodes.Cache.Where(n => n.ParentId == parent.Id);
	}

	async Task IBackupService.CreateFolder(BackupEntry entry, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(entry);

		var client = await EnsureLogin(cancellationToken);

		var folders = new List<BackupEntry>();

		do
		{
			folders.Add(entry);
			entry = entry.Parent;
		}
		while (entry is not null);

		folders.Reverse();

		var needCheck = true;
		var curr = GetRoot();

		foreach (var folder in folders)
		{
			var name = folder.Name;

			if (needCheck)
			{
				var found = GetChild(curr).FirstOrDefault(n => n.Name.EqualsIgnoreCase(name));

				if (found is null)
				{
					needCheck = false;
					curr = await client.CreateFolderAsync(curr.Id, name, cancellationToken).NoWait();
					_nodes.Add(curr);
				}
				else
					curr = found;
			}
			else
			{
				curr = await client.CreateFolderAsync(curr.Id, name, cancellationToken).NoWait();
				_nodes.Add(curr);
			}
		}
	}

	async Task IBackupService.DeleteAsync(BackupEntry entry, CancellationToken cancellationToken)
	{
		var client = await EnsureLogin(cancellationToken);

		var node = Find(entry);

		if (node is null)
			return;

		await client.DeleteAsync(node.Id, cancellationToken).NoWait();
		_nodes.Remove(node);
	}

	async Task IBackupService.DownloadAsync(BackupEntry entry, Stream stream, long? offset, long? length, Action<int> progress, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(entry);
		ArgumentNullException.ThrowIfNull(stream);
		ArgumentNullException.ThrowIfNull(progress);

		if (offset is not null || length is not null)
			throw new NotSupportedException("Partial download is not supported.");

		var client = await EnsureLogin(cancellationToken);

		var node = Find(entry) ?? throw new ArgumentOutOfRangeException(nameof(entry), "Entry not found.");

		await client.DownloadAsync(node, stream, new ProgressHandler(progress), cancellationToken).NoWait();
	}

	async Task IBackupService.UploadAsync(BackupEntry entry, Stream stream, Action<int> progress, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(entry);
		ArgumentNullException.ThrowIfNull(stream);
		ArgumentNullException.ThrowIfNull(progress);

		var client = await EnsureLogin(cancellationToken);

		var parent = (entry.Parent is null ? GetRoot() : Find(entry.Parent))
			?? throw new ArgumentOutOfRangeException(nameof(entry), "Parent entry not found.");

		var node = await client.UploadAsync(parent.Id, entry.Name, stream, modificationDate: null, new ProgressHandler(progress), cancellationToken).NoWait();
		_nodes.Add(node);
	}

	async Task IBackupService.FillInfoAsync(BackupEntry entry, CancellationToken cancellationToken)
	{
		await EnsureLogin(cancellationToken);

		var node = Find(entry) ?? throw new ArgumentOutOfRangeException(nameof(entry), "Entry not found.");

		entry.LastModified = node.ModificationDate ?? node.CreationDate ?? default;
		entry.Size = node.Size;
	}

	IAsyncEnumerable<BackupEntry> IBackupService.FindAsync(BackupEntry parent, string criteria)
		=> FindAsyncImpl(parent, criteria);

	private async IAsyncEnumerable<BackupEntry> FindAsyncImpl(BackupEntry parent, string criteria, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		await EnsureLogin(cancellationToken);

		var pe = parent is null ? GetRoot() : Find(parent);

		if (pe is null)
			yield break;

		var child = GetChild(pe);

		if (!criteria.IsEmpty())
			child = child.Where(i => i.Name.ContainsIgnoreCase(criteria));

		foreach (var item in child)
		{
			yield return new()
			{
				Name = item.Name,
				Parent = parent,
				LastModified = item.ModificationDate ?? item.CreationDate ?? default,
				Size = item.Size,
			};
		}
	}

	async Task<string> IBackupService.PublishAsync(BackupEntry entry, TimeSpan? expiresIn, CancellationToken cancellationToken)
	{
		if (expiresIn is not null)
			throw new NotSupportedException("Expiring links are not supported by MEGA export links.");

		var client = await EnsureLogin(cancellationToken);

		var node = Find(entry) ?? throw new ArgumentOutOfRangeException(nameof(entry), "Entry not found.");

		var url = await client.PublishAsync(node, cancellationToken).NoWait();

		_nodes.Clear();
		_nodes.AddRange(await _client.GetNodesAsync(cancellationToken).NoWait());

		return url;
	}

	async Task IBackupService.UnPublishAsync(BackupEntry entry, CancellationToken cancellationToken)
	{
		var client = await EnsureLogin(cancellationToken);

		var node = Find(entry);

		if (node is null)
			return;

		await client.UnpublishAsync(node.Id, cancellationToken).NoWait();

		_nodes.Clear();
		_nodes.AddRange(await _client.GetNodesAsync(cancellationToken).NoWait());
	}
}
