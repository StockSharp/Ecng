﻿namespace Ecng.Backup.Mega;

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

using Nito.AsyncEx;

/// <summary>
/// The data storage service based on Mega https://mega.nz/ .
/// </summary>
/// <param name="email">Email.</param>
/// <param name="password">Password.</param>
public class MegaService(string email, SecureString password) : Disposable, IBackupService
{
	private readonly MegaApiClient _client = new();
	private readonly string _email = email.ThrowIfEmpty(nameof(email));
	private readonly SecureString _password = password ?? throw new ArgumentNullException(nameof(password));
	private readonly List<INode> _nodes = [];

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		if (_client.IsLoggedIn)
			AsyncContext.Run(() => _client.LogoutAsync());

		base.DisposeManaged();
	}

	private async Task<MegaApiClient> EnsureLogin(CancellationToken cancellationToken)
	{
		if (!_client.IsLoggedIn)
		{
			await _client.LoginAsync(_email, _password.UnSecure(), default, cancellationToken).NoWait();

			_nodes.AddRange(await _client.GetNodesAsync(cancellationToken).NoWait());
		}

		return _client;
	}

	bool IBackupService.CanPublish => false;
	bool IBackupService.CanFolders => true;
	bool IBackupService.CanPartialDownload => false;

	private INode Find(BackupEntry entry)
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

	private INode GetRoot() => _nodes.First(n => n.Type == NodeType.Root);

	private IEnumerable<INode> GetChild(INode parent)
	{
		if (parent is null)
			throw new ArgumentNullException(nameof(parent));

		return _nodes.Where(n => n.ParentId == parent.Id);
	}

	async Task IBackupService.CreateFolder(BackupEntry entry, CancellationToken cancellationToken)
	{
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
					curr = await client.CreateFolderAsync(name, curr, cancellationToken).NoWait();
					_nodes.Add(curr);
				}
				else
					curr = found;
			}
			else
			{
				curr = await client.CreateFolderAsync(name, curr, cancellationToken).NoWait();
				_nodes.Add(curr);
			}
		}
	}

	async Task IBackupService.DeleteAsync(BackupEntry entry, CancellationToken cancellationToken)
		=> await (await EnsureLogin(cancellationToken)).DeleteAsync(Find(entry), cancellationToken: cancellationToken).NoWait();

	async Task IBackupService.DownloadAsync(BackupEntry entry, Stream stream, long? offset, long? length, Action<int> progress, CancellationToken cancellationToken)
	{
		using var temp = await (await EnsureLogin(cancellationToken)).Download(Find(entry), cancellationToken).NoWait();
		await temp.CopyToAsync(stream, cancellationToken).NoWait();
	}

	async Task IBackupService.UploadAsync(BackupEntry entry, Stream stream, Action<int> progress, CancellationToken cancellationToken)
		=> await (await EnsureLogin(cancellationToken)).Upload(stream, entry.Name, entry.Parent is null ? GetRoot() : Find(entry.Parent), cancellationToken: cancellationToken).NoWait();

	async Task IBackupService.FillInfoAsync(BackupEntry entry, CancellationToken cancellationToken)
	{
		await EnsureLogin(cancellationToken);

		var node = Find(entry);
		entry.LastModified = node.ModificationDate ?? default;
		entry.Size = node.Size;
	}

	async IAsyncEnumerable<BackupEntry> IBackupService.FindAsync(BackupEntry parent, string criteria, [EnumeratorCancellation]CancellationToken cancellationToken)
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
				LastModified = item.ModificationDate ?? default,
				Size = item.Size,
			};
		}
	}

	Task<string> IBackupService.PublishAsync(BackupEntry entry, CancellationToken cancellationToken) => throw new NotSupportedException();
	Task IBackupService.UnPublishAsync(BackupEntry entry, CancellationToken cancellationToken) => throw new NotSupportedException();
}