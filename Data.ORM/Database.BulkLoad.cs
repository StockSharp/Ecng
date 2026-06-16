namespace Ecng.Data;

using System.ComponentModel;

using Ecng.Common;
using Ecng.Data.Sql;

/// <summary>
/// Bulk-load helpers extracted from the main <see cref="Database"/> file
/// so the CRUD/transaction/cache concerns there don't have to coexist
/// with the bulk-load state machine. Both inner classes remain
/// <c>private</c> to <see cref="Database"/> via the partial-class split,
/// keeping their access to its private members intact.
/// </summary>
public partial class Database
{
	private class BulkLoadInfo
	{
		private readonly Database _database;
		private readonly Lock _initGate = new();
		// Single-flight: the first caller starts the full-table load and every other caller awaits
		// the SAME task, so the expensive ReadAllAsync runs once and no lock is ever held across the
		// DB round-trip (callers wait on the shared task, not on a mutex).
		private Task<CachedSynchronizedDictionary<object, object>> _initTask;
		private CachedSynchronizedDictionary<object, object> _cachedEntities;

		public BulkLoadInfo(Database database, Schema meta)
		{
			_database = database ?? throw new ArgumentNullException(nameof(database));
			Meta = meta ?? throw new ArgumentNullException(nameof(meta));

			if (Meta.Identity is null)
				throw new ArgumentException(Meta.EntityType.AssemblyQualifiedName, nameof(meta));
		}

		public Schema Meta { get; }

		public ValueTask<CachedSynchronizedDictionary<object, object>> EnsureInit(CancellationToken cancellationToken)
		{
			var cached = _cachedEntities;

			if (cached is not null)
				return new(cached);

			Task<CachedSynchronizedDictionary<object, object>> task;

			// The gate is held only to publish the shared task — never across the DB read below.
			using (_initGate.EnterScope())
				task = _initTask ??= LoadAllAsync();

			// Each caller honours its own cancellation without cancelling the shared load for others.
			return new(task.WaitAsync(cancellationToken));
		}

		private async Task<CachedSynchronizedDictionary<object, object>> LoadAllAsync()
		{
			try
			{
				object[] cachedEntities;

				using (new Scope<BulkLoadInfo>(this))
					cachedEntities = await _database.ReadAllAsync(Meta, 0, _database.MaxBulkLoadRows, default, Meta.Identity.Name, ListSortDirection.Ascending, default).NoWait();

				var dict = new CachedSynchronizedDictionary<object, object>();

				foreach (var e in cachedEntities)
					dict.Add(((IDbPersistable)e).GetIdentity(), e);

				_cachedEntities = dict;
				return dict;
			}
			catch
			{
				// Drop the faulted task so a later caller can retry instead of caching the failure.
				using (_initGate.EnterScope())
					_initTask = null;

				throw;
			}
		}
	}

	private class BulkScope : IAsyncDisposable
	{
		private readonly Scope<BulkScope> _scope;
		private readonly Database _parent;
		private readonly CancellationToken _token;
		private readonly Dictionary<(Type, string, object), object> _pendingDeps = [];
		private readonly Dictionary<(Type, string, object), object> _newDeps = [];

		public static BulkScope Instance => Scope<BulkScope>.Current?.Value;

		public BulkScope(Database parent, CancellationToken token)
		{
			if (Instance is not null)
				throw new InvalidOperationException();

			_scope = new Scope<BulkScope>(this);
			_parent = parent ?? throw new ArgumentNullException(nameof(parent));
			_token = token;
		}

		public bool HasDep((Type, string, object) key) => _pendingDeps.ContainsKey(key);
		public bool TryGetDep((Type, string, object) key, out object entity) => _pendingDeps.TryGetValue(key, out entity);
		public void SetDep((Type, string, object) key, object entity)
		{
			if (entity is null)
				_pendingDeps.TryAdd2(key, entity);
			else
				_pendingDeps[key] = entity;

			_newDeps[key] = entity;
		}

		async ValueTask IAsyncDisposable.DisposeAsync()
		{
			// Always pop the ambient Scope even if dependency flushing throws, otherwise the
			// AsyncLocal scope leaks into unrelated continuations on this execution context.
			try
			{
				await FlushDepsAsync();
			}
			finally
			{
				_scope.Dispose();
			}
		}

		private async ValueTask FlushDepsAsync()
		{
			var token = _token;

			var cacheLock = _parent._cacheLock;
			var cache = _parent._cache;
			var cacheStore = _parent._cacheStore;

			while (_newDeps.Count > 0)
			{
				var newDeps = _newDeps.CopyAndClear();

				foreach (var g in newDeps.Where(p => p.Value is null).GroupBy(k => k.Key.Item1))
				{
					var entityType = g.Key;
					var meta = SchemaRegistry.Get(entityType);
					var ids = g.Select(i => i.Key.Item3).ToList();

					using (await cacheLock.LockAsync(token).ConfigureAwait(false))
					{
						foreach (var id in ids.ToArray())
						{
							var key = (entityType, meta.Identity.Name, id);

							if (TryGetDep(key, out var e) && e is not null)
								ids.Remove(id);
							else if (cache.TryGetValue(key, out var t) && t.complete)
								ids.Remove(id);
						}
					}

					if (ids.Count == 0)
						continue;

					foreach (var b in ids.Chunk(500))
					{
						var batch = b.ToArray();
						Array res;

						if (meta.IsView)
						{
							var processor = ViewProcessorRegistry.GetProcessor(meta.EntityType);

							res = (Array)await processor.ReadRange(batch, token).NoWait();
						}
						else
						{
							var valueColumns = new List<SchemaColumn>();
							var input = new SerializationItemCollection();

							var idx = 0;
							foreach (var id in batch)
							{
								var colName = $"Id{idx++}";
								valueColumns.Add(new() { Name = colName, ClrType = meta.Identity.ClrType });
								input.Add(new(colName, meta.Identity.ClrType, id));
							}

							var cmd = await _parent.GetCommand(meta, SqlCommandTypes.ReadRange, [meta.Identity], valueColumns, token).NoWait();
							res = await _parent.ReadAllAsync(cmd, meta, input, token).NoWait();
						}

						if (res.Length != batch.Length)
							throw new InvalidOperationException($"Res={res.Length} <> Batch={batch.Length}");
					}
				}
			}

			using (await cacheLock.LockAsync(token).ConfigureAwait(false))
			{
				foreach (var pair in _pendingDeps)
				{
					var key = pair.Key;
					var entity = pair.Value;

					if (!cache.TryGetValue(key, out var t))
						continue;

					if (entity is null)
					{
						if (t.complete)
							continue;
						else
							throw new InvalidOperationException(key.To<string>());
					}

					if (t.complete)
						continue;

					t.complete = true;
					cache[key] = t;
					cacheStore.Touch(key);
				}
			}
		}
	}
}
