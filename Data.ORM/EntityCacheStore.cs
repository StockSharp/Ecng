namespace Ecng.Data;

using Ecng.Common;
using Nito.AsyncEx;

/// <summary>
/// Encapsulates the entity-cache state previously held directly inside
/// <see cref="Database"/>: the underlying <see cref="Dictionary{TKey,TValue}"/>,
/// the <see cref="AsyncLock"/> guarding it, the per-entry timestamps used for
/// TTL eviction, and the eviction policy. Exists primarily so the cache
/// machinery can evolve (TTL, size cap, sliding window…) without touching
/// the 1400-line <see cref="Database"/> file every time, and so call-sites
/// dealing with the cache do not have to juggle three separate fields.
/// </summary>
internal sealed class EntityCacheStore
{
	private readonly Dictionary<(Type, string, object), (object entity, bool complete)> _entries = [];
	private readonly Dictionary<(Type, string, object), DateTime> _timestamps = [];

	/// <summary>
	/// Async lock guarding both <see cref="Entries"/> and the per-entry
	/// timestamp map. Exposed so the call-sites that still need to hold
	/// the lock across multiple operations can wrap them under a single
	/// <c>using await _cacheStore.Lock.LockAsync(...)</c>.
	/// </summary>
	public AsyncLock Lock { get; } = new();

	/// <summary>
	/// Direct access to the underlying entry dictionary. Mutations require
	/// the caller to hold <see cref="Lock"/>. Intended for the hot CRUD
	/// paths inside <see cref="Database"/>; new code should prefer the
	/// helper methods below.
	/// </summary>
	public Dictionary<(Type, string, object), (object entity, bool complete)> Entries => _entries;

	/// <summary>
	/// Maximum age of an entry. <see cref="TimeSpan.MaxValue"/> disables
	/// TTL eviction.
	/// </summary>
	public TimeSpan Timeout { get; set; } = TimeSpan.MaxValue;

	/// <summary>
	/// Records the moment the given key was last written. Caller must hold
	/// <see cref="Lock"/>. No-op when TTL is disabled.
	/// </summary>
	public void Touch((Type, string, object) key)
	{
		if (Timeout != TimeSpan.MaxValue)
			_timestamps[key] = DateTime.UtcNow;
	}

	/// <summary>
	/// Removes all entries (entries + timestamps). Caller must hold the lock.
	/// </summary>
	public void Clear()
	{
		_entries.Clear();
		_timestamps.Clear();
	}

	/// <summary>
	/// Forgets a single key. Caller must hold the lock.
	/// </summary>
	public bool Remove((Type, string, object) key)
	{
		_timestamps.Remove(key);
		return _entries.Remove(key);
	}

	/// <summary>
	/// Drops every entry whose timestamp is older than <see cref="Timeout"/>.
	/// Acquires <see cref="Lock"/> internally — do not call from within a
	/// scope that already holds it.
	/// </summary>
	public async ValueTask TrimExpiredAsync(CancellationToken cancellationToken)
	{
		if (Timeout == TimeSpan.MaxValue)
			return;

		var cutoff = DateTime.UtcNow - Timeout;
		using var _ = await Lock.LockAsync(cancellationToken).ConfigureAwait(false);

		var stale = _timestamps.Where(kv => kv.Value < cutoff).Select(kv => kv.Key).ToArray();
		foreach (var key in stale)
			Remove(key);
	}
}
