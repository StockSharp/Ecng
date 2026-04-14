namespace Ecng.Data;

using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Net;
using System.Transactions;

using Ecng.Common;
using Ecng.Data.Sql;

using Nito.AsyncEx;

/// <summary>
/// Provides database access and entity persistence via an ORM layer.
/// </summary>
public class Database : Disposable, IStorage
{
	private class BulkLoadInfo
	{
		private readonly Database _database;
		private CachedSynchronizedDictionary<object, object> _cachedEntities;

		public BulkLoadInfo(Database database, Schema meta)
		{
			_database = database ?? throw new ArgumentNullException(nameof(database));
			Meta = meta ?? throw new ArgumentNullException(nameof(meta));

			if (Meta.Identity is null)
				throw new ArgumentException(Meta.EntityType.AssemblyQualifiedName, nameof(meta));
		}

		public Schema Meta { get; }

		public async ValueTask<CachedSynchronizedDictionary<object, object>> EnsureInit(CancellationToken cancellationToken)
		{
			if (_cachedEntities is null)
			{
				object[] cachedEntities;

				using (new Scope<BulkLoadInfo>(this))
					cachedEntities = await _database.ReadAllAsync(Meta, 0, _database.MaxBulkLoadRows, default, Meta.Identity.Name, ListSortDirection.Ascending, cancellationToken).NoWait();
				var dict = new CachedSynchronizedDictionary<object, object>();

				foreach (var e in cachedEntities)
					dict.Add(((IDbPersistable)e).GetIdentity(), e);

				_cachedEntities = dict;
			}

			return _cachedEntities;
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
			var token = _token;

			var cacheLock = _parent._cacheLock;
			var cache = _parent._cache;

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

							res = (Array)await processor.ReadRange(batch, token).NoWait();						}
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
							res = await _parent.ReadAllAsync(cmd, meta, input, token).NoWait();						}

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

					var t = cache[key];

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
				}
			}

			_scope.Dispose();
		}
	}

	#region Private Fields

	private readonly SynchronizedDictionary<Query, TaskCompletionSource<DatabaseCommand>> _commandsByQuery = [];
	private readonly SynchronizedDictionary<string, DatabaseCommand> _commandsByText = [];
	private readonly AsyncLock _cacheLock = new();
	private readonly Dictionary<(Type, string, object), (object entity, bool complete)> _cache = [];
	private readonly SynchronizedDictionary<Type, BulkLoadInfo> _bulkLoad = [];

	private readonly Regex _parameterRegex = new("@(?<parameterName>[_a-zA-Z0-9]+)", RegexOptions.Multiline);

	private readonly Stat<string> _stat = new();
	Stat<string> IStorage.Stat => _stat;

	#endregion

	#region Database.ctor()

	/// <summary>
	/// Initializes a new instance of the <see cref="Database"/> class.
	/// </summary>
	public Database(string name, string connectionString, DbProviderFactory factory, ISqlDialect dialect)
	{
		Debug.WriteLine($"{nameof(Database)}.ctor()");

		Name = name;
		ConnectionString = connectionString;
		Factory = factory ?? throw new ArgumentNullException(nameof(factory));
		Dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
	}

	/// <summary>
	/// Gets the query provider used to generate SQL commands.
	/// </summary>
	public QueryProvider QueryProvider { get; } = new();

	#endregion

	private string _name;

	/// <summary>
	/// Gets or sets the database name.
	/// </summary>
	public string Name
	{
		get => _name;
		set => _name = value.IsEmpty() ? throw new ArgumentNullException(nameof(value)) : value;
	}

	private string _connectionString;

	/// <summary>
	/// Gets or sets the database connection string.
	/// </summary>
	public string ConnectionString
	{
		get => _connectionString;
		set => _connectionString = value.IsEmpty() ? throw new ArgumentNullException(nameof(value)) : value;
	}

	private DbProviderFactory _factory;

	/// <summary>
	/// Gets or sets the <see cref="DbProviderFactory"/> used to create database objects.
	/// </summary>
	public DbProviderFactory Factory
	{
		get => _factory;
		set => _factory = value ?? throw new ArgumentNullException(nameof(value));
	}

	private ISqlDialect _dialect;

	/// <summary>
	/// Gets or sets the SQL dialect used for query generation.
	/// </summary>
	public ISqlDialect Dialect
	{
		get => _dialect;
		set => _dialect = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>
	/// Gets or sets the default command type for database commands.
	/// </summary>
	public CommandType CommandType { get; set; } = CommandType.Text;

	/// <summary>
	/// Gets or sets the culture used for data conversions.
	/// </summary>
	public CultureInfo CultureInfo { get; set; } = CultureInfo.InvariantCulture;

	private ValueTask<T> Do<T>(Func<ValueTask<T>> func) => CultureInfo.DoInCulture(func);

	/// <summary>
	/// Gets or sets whether delete-all operations are permitted.
	/// </summary>
	public bool AllowDeleteAll { get; set; }

	/// <summary>
	/// Gets or sets the maximum number of rows to preload in bulk-load mode.
	/// </summary>
	public int MaxBulkLoadRows { get; set; } = 100000;

	void IStorage.AddBulkLoad<TEntity>() => _bulkLoad.Add(typeof(TEntity), new(this, SchemaRegistry.Get(typeof(TEntity))));

	/// <summary>
	/// Removes all bulk-load registrations and their cached data.
	/// </summary>
	public void ClearBulkLoad() => _bulkLoad.Clear();

	/// <summary>
	/// Creates and opens a new database connection asynchronously.
	/// </summary>
	public async ValueTask<DbConnection> CreateConnectionAsync(CancellationToken cancellationToken)
	{
		var connection = Factory.CreateConnection();

		if (connection == null)
			throw new InvalidOperationException();

		connection.ConnectionString = ConnectionString;
		await connection.OpenAsync(cancellationToken).NoWait();
		return connection;
	}

	private class StorageTransaction : IStorageTransaction
	{
		private readonly TransactionScope _underlying = new(TransactionScopeAsyncFlowOption.Enabled);

		void IStorageTransaction.Commit() => _underlying.Complete();
		void IDisposable.Dispose() => _underlying.Dispose();
	}

	IStorageTransaction IStorage.CreateTransaction() => new StorageTransaction();

	#region GetCommand

	/// <summary>
	/// Gets or creates a cached <see cref="DatabaseCommand"/> for the specified schema and command type.
	/// </summary>
	public virtual ValueTask<DatabaseCommand> GetCommand(Schema meta, SqlCommandTypes type, IReadOnlyList<SchemaColumn> keyColumns, IReadOnlyList<SchemaColumn> valueColumns, CancellationToken cancellationToken)
	{
		var commandQuery = QueryProvider.Create(meta, type, keyColumns, valueColumns);

		return _commandsByQuery.SafeAddAsync(commandQuery, (key, t) =>
		{
			var query = key.Render(Dialect);
			var dbCommand = CreateDbCommand(query, CommandType.Text);

			var command = new DatabaseCommand(Factory, Dialect, CreateConnectionAsync, dbCommand);

			foreach (Match match in _parameterRegex.Matches(query))
			{
				if (match.Success)
				{
					var group = match.Groups["parameterName"];

					var fieldName = group.Value;

					SchemaColumn col = null;

					foreach (var c in keyColumns)
					{
						if (c.Name.EqualsIgnoreCase(fieldName))
						{
							col = c;
							break;
						}
					}

					if (col is null)
					{
						foreach (var c in valueColumns)
						{
							if (c.Name.EqualsIgnoreCase(fieldName))
							{
								col = c;
								break;
							}
						}
					}

					col ??= meta.TryGetColumn(fieldName);

					if (col is null)
						throw new InvalidOperationException($"Column '{fieldName}' not found in {meta.EntityType}.");

					command.Parameters.Add(
						Factory.CreateDbParameter(
							Dialect.ParameterPrefix + fieldName,
							ParameterDirection.Input,
							col.ClrType.ToDbType(),
							DBNull.Value));
				}
			}

			return Task.FromResult(command);
		}, cancellationToken).AsValueTask();
	}

	private DbCommand CreateDbCommand(string text, CommandType type)
	{
		var command = Factory.CreateCommand();

		if (command == null)
			throw new InvalidOperationException();

		command.CommandText = text;
		command.CommandType = type;

		return command;
	}

	#endregion

	/// <summary>
	/// Tests the internal cache for incomplete entries and throws if any are found.
	/// </summary>
	public async ValueTask Test(CancellationToken cancellationToken)
	{
		using var _ = await _cacheLock.LockAsync(cancellationToken).ConfigureAwait(false);
		var res = _cache.Where(t => !t.Value.complete).ToArray();
		if (res.Length > 0)
		{
			throw new Exception();
		}
	}

	#region GetCountAsync

	/// <summary>
	/// Gets the total number of entities of type <typeparamref name="TEntity"/> in the database.
	/// </summary>
	public virtual async ValueTask<long> GetCountAsync<TEntity>(CancellationToken cancellationToken)
		where TEntity : IDbPersistable
	{
		var meta = SchemaRegistry.Get(typeof(TEntity));

		var command = await GetCommand(meta, SqlCommandTypes.Count, [], [], cancellationToken).NoWait();
		var source = new SerializationItemCollection();

		return await Do(() => ExecuteScalar<long>(command, source, cancellationToken)).NoWait();	}

	#endregion

	#region CreateAsync

	/// <summary>
	/// Creates (inserts) a new entity in the database asynchronously.
	/// </summary>
	public virtual ValueTask<object> CreateAsync(Schema meta, IDbPersistable entity, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(entity);

		if (meta.ReadOnly)
			throw new InvalidOperationException();

		return Do(async () =>
		{
			var readOnlyColumns = meta.ReadOnlyColumns;
			var nonReadOnlyColumns = meta.NonReadOnlyColumns;

			var keyColumns = meta.Identity is not null ? new[] { meta.Identity } : Array.Empty<SchemaColumn>();
			var command = await GetCommand(meta, SqlCommandTypes.Create, keyColumns, meta.AllColumns, cancellationToken).NoWait();
			var storage = new SettingsStorage();
			entity.Save(storage);
			var input = storage.ToItems(nonReadOnlyColumns);

			var output = await Execute(command, input, readOnlyColumns.Count > 0, cancellationToken).NoWait();
			if (readOnlyColumns.Count > 0 && meta.Identity is not null)
			{
				if (output.TryGetItem(meta.Identity.Name, out var identityItem))
					entity.SetIdentity(identityItem.Value);
			}

			entity.InitLists(this);

			if (meta.NoCache || meta.Identity is null)
				return (object)entity;

			var id = entity.GetIdentity();
			var key = (meta.EntityType, meta.Identity.Name, id);

			using (await _cacheLock.LockAsync(cancellationToken).ConfigureAwait(false))
				Update(key, entity);

			if (_bulkLoad.TryGetValue(meta.EntityType, out var info))
			{
				var dict = await info.EnsureInit(cancellationToken).NoWait();
				dict.Add(id, entity);
			}

			return (object)entity;
		});
	}

	#endregion

	#region ReadAsync

	/// <summary>
	/// Reads a single entity by identity from the database asynchronously.
	/// </summary>
	public virtual async ValueTask<object> ReadAsync(Schema meta, object id, Func<Schema, ValueTask<DatabaseCommand>> getCommand, Func<Schema, ValueTask<SerializationItem>> getIdItem, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(meta);

		ArgumentNullException.ThrowIfNull(getCommand);

		ArgumentNullException.ThrowIfNull(getIdItem);

		var by = meta.Identity ?? throw new ArgumentException(meta.EntityType.AssemblyQualifiedName);
		var key = (meta.EntityType, by.Name, id);

		BulkLoadInfo bulkInfo = null;

		if (!meta.NoCache)
		{
			if (_bulkLoad.TryGetValue(meta.EntityType, out var info))
			{
				if (!Scope<BulkLoadInfo>.All.Any(i => i.Value.Meta == meta))
				{
					bulkInfo = info;
					var dict = await info.EnsureInit(cancellationToken).NoWait();
					if (dict.TryGetValue(id, out var cached))
						return cached;

					// cache miss — fall through to DB query below
				}
			}

			var scope = BulkScope.Instance;

			using (await _cacheLock.LockAsync(cancellationToken).ConfigureAwait(false))
			{
				if (_cache.TryGetValue(key, out var t))
				{
					if (t.complete)
						return t.entity;
					else if (scope is not null)
					{
						scope.SetDep(key, null);
						return t.entity;
					}
				}
				else if (scope is not null)
				{
					var entity = CreateEntity(meta, id);

					_cache.Add(key, (entity, false));
					scope.SetDep(key, null);

					return entity;
				}
			}
		}

		return await Do(async () =>
		{
			var command = await getCommand(meta).NoWait();
			var idItem = await getIdItem(meta).NoWait();
			var input = UngroupSource(meta, new[] { idItem });

			var entity = await Read(command, meta, input, cancellationToken).NoWait();
			// cache result (or null) back into bulk dict so subsequent lookups are fast
			if (bulkInfo is not null)
			{
				var dict = await bulkInfo.EnsureInit(cancellationToken).NoWait();
				dict[id] = entity;
			}

			if (!meta.NoCache && entity is null)
			{
				using var _ = await _cacheLock.LockAsync(cancellationToken).ConfigureAwait(false);
				if (_cache.TryGetValue(key, out var t))
				{
					if (t.complete)
						entity = t.entity;
				}
				else
					_cache.Add(key, (default, true));
			}
			return entity;
		}).NoWait();
	}

	/// <summary>
	/// Reads a single entity using the specified command and input parameters.
	/// </summary>
	public virtual ValueTask<object> Read(DatabaseCommand command, Schema meta, SerializationItemCollection input, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(command);

		return Do(async () =>
		{
			var output = await ExecuteRow(command, input, cancellationToken).NoWait();
			if (output is null)
				return default;

			output = GroupSource(meta, output);

			ValueTask<object> ReadInternal(BulkScope scope)
				=> GetOrAddCache(
				scope,
				meta,
				output,
				cancellationToken);

			var scope = BulkScope.Instance;
			if (scope is not null)
				return await ReadInternal(scope).NoWait();
			await using var _ = new BulkScope(this, cancellationToken);
			return await ReadInternal(_).NoWait();		});
	}

	#endregion

	#region ReadAllAsync

	/// <summary>
	/// Reads a paged set of entities from the database asynchronously.
	/// </summary>
	public virtual async ValueTask<object[]> ReadAllAsync(Schema meta, long startIndex, long count, bool deleted, string orderByColumn, ListSortDirection direction, CancellationToken cancellationToken)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(startIndex);

		ArgumentOutOfRangeException.ThrowIfNegative(count);

		if (count == 0)
			return [];

		var orderByClause = orderByColumn is not null
			? "{0} {1}".Put(Dialect.QuoteIdentifier(orderByColumn), (direction == ListSortDirection.Ascending) ? "asc" : "desc")
			: null;

		var sql = Query.CreateSelect(meta.Name, null, orderByClause, startIndex > 0 ? startIndex : null, count < long.MaxValue ? count : null).Render(Dialect);

		var command = _commandsByText.SafeAdd(sql, key =>
			new DatabaseCommand(Factory, Dialect, CreateConnectionAsync, CreateDbCommand(key, CommandType.Text)));

		return await ReadAllAsync(command, meta, new SerializationItemCollection(), cancellationToken).NoWait();	}

	/// <summary>
	/// Reads all entities matching the specified command and input parameters.
	/// </summary>
	public virtual ValueTask<object[]> ReadAllAsync(DatabaseCommand command, Schema meta, SerializationItemCollection input, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(command);

		return Do(async () =>
		{
			input = UngroupSource(meta, input);
			return await GetOrAddCacheTable<object>(meta, await ExecuteTable(command, input, cancellationToken), cancellationToken).NoWait();		});
	}

	#endregion

	#region UpdateAsync

	/// <summary>
	/// Updates an existing entity in the database asynchronously.
	/// </summary>
	public virtual ValueTask<TEntity> UpdateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken)
		where TEntity : IDbPersistable
	{
		ArgumentNullException.ThrowIfNull(entity);

		var meta = entity.Schema;

		if (meta.ReadOnly)
			throw new InvalidOperationException();

		IReadOnlyList<SchemaColumn> keyColumns;
		List<SchemaColumn> valueColumns;

		if (meta.Identity is not null)
		{
			keyColumns = [meta.Identity];
			valueColumns = meta.Columns.Where(c => !c.IsReadOnly).ToList();
		}
		else
		{
			keyColumns = meta.UniqueColumns;
			valueColumns = meta.Columns.Where(c => !c.IsReadOnly && !c.IsUnique).ToList();
		}

		return Do(async () =>
		{
			var command = await GetCommand(meta, SqlCommandTypes.UpdateBy, keyColumns, valueColumns, cancellationToken).NoWait();
			var storage = new SettingsStorage();
			entity.Save(storage);

			var input = new SerializationItemCollection();
			foreach (var col in keyColumns)
			{
				if (storage.TryGetValue(col.Name, out var v))
					input.Add(new(col.Name, col.ClrType, v));
				else if (col.Name == "Id")
					input.Add(new(col.Name, col.ClrType, entity.GetIdentity()));
			}
			foreach (var col in valueColumns)
			{
				if (storage.TryGetValue(col.Name, out var v))
					input.Add(new(col.Name, col.ClrType, v));
			}

			await Execute(command, input, false, cancellationToken).NoWait();
			await UpdateCache(meta, entity, cancellationToken).NoWait();
			return entity;
		});
	}

	#endregion

	#region DeleteAsync

	/// <summary>
	/// Deletes an entity from the database asynchronously and returns the number of affected rows.
	/// </summary>
	public virtual ValueTask<int> DeleteAsync<TEntity>(TEntity entity, CancellationToken cancellationToken)
		where TEntity : IDbPersistable
	{
		var meta = entity.Schema;

		var by = new SerializationItemCollection();
		IReadOnlyList<SchemaColumn> keyColumns;

		if (meta.Identity is null)
		{
			var storage = new SettingsStorage();
			entity.Save(storage);

			var keys = new List<SchemaColumn>();
			foreach (var col in meta.UniqueColumns)
			{
				storage.TryGetValue(col.Name, out var v);
				by.Add(new(col.Name, col.ClrType, v));
				keys.Add(col);
			}
			keyColumns = keys;
		}
		else
		{
			by.Add(new(meta.Identity.Name, meta.Identity.ClrType, entity.GetIdentity()));
			keyColumns = [meta.Identity];
		}

		return Do(async () =>
		{
			var cmd = await GetCommand(meta, SqlCommandTypes.DeleteBy, keyColumns, [], cancellationToken).NoWait();
			var retVal = await ExecuteNonQuery(cmd, by, cancellationToken).NoWait();
			await DeleteCache(meta, by, cancellationToken).NoWait();
			return retVal;
		});
	}

	#endregion

	#region DeleteAllAsync

	/// <summary>
	/// Deletes all entities of type <typeparamref name="TEntity"/> from the database.
	/// </summary>
	public virtual async ValueTask DeleteAllAsync<TEntity>(CancellationToken cancellationToken)
		where TEntity : IDbPersistable
	{
		if (!AllowDeleteAll)
			throw new NotSupportedException();

		var meta = SchemaRegistry.Get(typeof(TEntity));
		var command = await GetCommand(meta, SqlCommandTypes.DeleteAll, [], [], cancellationToken).NoWait();
		await Do(() => ExecuteNonQuery(command, [], cancellationToken)).NoWait();	}

	#endregion

	#region Cache

	async ValueTask IStorage.AddCacheAsync<TId, TEntity>(TId id, TEntity entity, CancellationToken cancellationToken)
	{
		var meta = entity.Schema;

		if (meta.Identity is null || meta.NoCache)
			return;

		var key = (meta.EntityType, meta.Identity.Name, (object)id);

		using var _ = await _cacheLock.LockAsync(cancellationToken).ConfigureAwait(false);
		Update(key, entity);
	}

	async ValueTask IStorage.ClearCacheAsync(CancellationToken cancellationToken)
	{
		using var _ = await _cacheLock.LockAsync(cancellationToken).ConfigureAwait(false);
		_cache.Clear();
	}

	#endregion

	/// <summary>
	/// Executes a database command, optionally returning output values.
	/// </summary>
	public virtual async ValueTask<SerializationItemCollection> Execute(DatabaseCommand command, SerializationItemCollection source, bool needRetVal, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(command);

		if (needRetVal)
			return await ExecuteRow(command, source, cancellationToken).NoWait();
		else
		{
			await ExecuteNonQuery(command, source, cancellationToken).NoWait();
			return [];
		}
	}

	private async ValueTask<TEntity[]> GetOrAddCacheTable<TEntity>(Schema meta, SerializationItemCollection table, CancellationToken cancellationToken)
	{
		static SerializationItemCollection GetRow(IEnumerable<SerializationItem> table, int rowIndex)
		{
			var row = new SerializationItemCollection();

			foreach (var item in table)
				row.Add(new(item.Name, item.Type, ((IList)item.Value)[rowIndex]));

			return row;
		}

		ArgumentNullException.ThrowIfNull(meta);

		ArgumentNullException.ThrowIfNull(table);

		if (table.IsEmpty())
			return [];

		async ValueTask<TEntity[]> GetOrAddCacheTableInternal(BulkScope scope)
		{
			ArgumentNullException.ThrowIfNull(scope);

			var entityCount = ((ICollection)table[0].Value).Count;
			table = GroupSource(meta, table);

			var entities = new TEntity[entityCount];

			for (var i = 0; i < entities.Length; i++)
			{
				try
				{
					entities[i] = (TEntity)await GetOrAddCache(scope, meta, GetRow(table, i), cancellationToken).NoWait();				}
				catch (Exception ex)
				{
					if (ContinueOnExceptionContext.TryProcess(ex))
					{
						entityCount--;
						continue;
					}

					throw;
				}
			}

			if (entities.Length > entityCount)
				entities = [.. entities.Where(e => e is not null)];

			return [.. entities];
		}

		var scope = BulkScope.Instance;

		if (scope is not null)
			return await GetOrAddCacheTableInternal(scope).NoWait();
		await using var _ = new BulkScope(this, cancellationToken);
		return await GetOrAddCacheTableInternal(_).NoWait();	}

	private static IDbPersistable CreateEntity(Schema meta, object id)
	{
		var entity = (IDbPersistable)meta.CreateEntity();

		if (id is not null)
			entity.SetIdentity(id);

		return entity;
	}

	private async ValueTask<object> GetOrAddCache(BulkScope scope, Schema meta, SerializationItemCollection input, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(scope);

		ArgumentNullException.ThrowIfNull(meta);

		ArgumentNullException.ThrowIfNull(input);

		if (meta.NoCache || meta.Identity is null)
		{
			var noCache = CreateEntity(meta, null);

			if (meta.Identity is not null)
				noCache.SetIdentity(input[meta.Identity.Name].Value);

			var noCacheStorage = input.ToStorage();
			await noCache.LoadAsync(noCacheStorage, this, cancellationToken).NoWait();
			noCache.InitLists(this);

			return noCache;
		}

		var id = input[meta.Identity.Name].Value;
		var key = (meta.EntityType, meta.Identity.Name, id);

		IDbPersistable entity = default;

		using (await _cacheLock.LockAsync(cancellationToken).ConfigureAwait(false))
		{
			if (_cache.TryGetValue(key, out var t))
			{
				if (t.complete)
					return t.entity;
				else
				{
					if (scope.TryGetDep(key, out var e))
					{
						if (e is not null)
							return t.entity;
						else
						{
							entity = (IDbPersistable)t.entity;

							if (entity is null)
								throw new InvalidOperationException(key.To<string>());

							scope.SetDep(key, entity);
						}
					}
					else
					{
						entity = (IDbPersistable)t.entity;
						scope.SetDep(key, entity);
					}
				}
			}
			else
			{
				entity = CreateEntity(meta, id);

				_cache.Add(key, (entity, false));
				scope.SetDep(key, entity);
			}
		}

		{
			var storage = input.ToStorage();
			await entity.LoadAsync(storage, this, cancellationToken).NoWait();
			entity.InitLists(this);
		}

		return entity;
	}

	private void Update((Type, string, object) key, object entity)
	{
		if (_cache.TryGetValue(key, out var t))
		{
			if (t.complete)
			{
				if (t.entity is null && entity is not null)
					_cache[key] = (entity, true);
			}
			else
				_cache[key] = (entity, true);
		}
		else
			_cache.Add(key, (entity, true));
	}

	private async ValueTask UpdateCache(Schema meta, IDbPersistable entity, CancellationToken cancellationToken)
	{
		var uniqueColumns = meta.UniqueColumns;

		if (uniqueColumns.Count == 0)
			return;

		SettingsStorage storage = null;

		var keys = uniqueColumns.Select(c =>
		{
			object v;
			if (c == meta.Identity)
				v = entity.GetIdentity();
			else
			{
				storage ??= new SettingsStorage();
				if (storage.Count == 0)
					entity.Save(storage);

				storage.TryGetValue(c.Name, out v);
			}

			if (v is null)
				return default;

			return (meta.EntityType, c.Name, v);
		}).Where(k => k != default).ToArray();

		if (keys.Length == 0)
			return;

		using var _ = await _cacheLock.LockAsync(cancellationToken).ConfigureAwait(false);
		foreach (var key in keys)
			Update(key, entity);
	}

	private async ValueTask DeleteCache(Schema meta, IEnumerable<SerializationItem> by, CancellationToken cancellationToken)
	{
		var uniqueNames = new HashSet<string>(meta.UniqueColumns.Select(c => c.Name), StringComparer.OrdinalIgnoreCase);
		var keys = by.Where(i => uniqueNames.Contains(i.Name)).Select(i => (meta.EntityType, i.Name, i.Value)).ToArray();

		if (keys.Length == 0)
			return;

		using var _ = await _cacheLock.LockAsync(cancellationToken).ConfigureAwait(false);
		foreach (var key in keys)
			_cache.Remove(key);
	}

	private static SerializationItemCollection GroupSource(Schema meta, SerializationItemCollection input)
	{
		ArgumentNullException.ThrowIfNull(meta);
		ArgumentNullException.ThrowIfNull(input);

		var output = new SerializationItemCollection();

		foreach (var col in meta.AllColumns)
		{
			if (input.TryGetItem(col.Name, out var item))
				output.Add(new(col.Name, col.ClrType, item.Value));
		}

		return output;
	}

	private static SerializationItemCollection UngroupSource(Schema meta, IEnumerable<SerializationItem> input)
	{
		ArgumentNullException.ThrowIfNull(meta);
		ArgumentNullException.ThrowIfNull(input);

		var output = new SerializationItemCollection();

		foreach (var item in input)
		{
			if (meta.TryGetColumn(item.Name) is not null)
				output.Add(item);
			else
				output.Add(item);
		}

		return output;
	}

#region Disposable Members

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		//foreach (var command in _commands.Values)
		//	command.Dispose();

		base.DisposeManaged();
	}

	#endregion

	async ValueTask<TEntity> IStorage.AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken)
		=> (TEntity)await CreateAsync(entity.Schema, entity, cancellationToken).NoWait();
	async ValueTask<TEntity> IStorage.GetByAsync<TEntity>(IQueryable<TEntity> source, CancellationToken cancellationToken)
	{
		var meta = SchemaRegistry.Get(typeof(TEntity));

		var id = source.Expression.GetId();

		return (TEntity)await ReadAsync(meta, id,
			meta =>
			{
				var sourceMeta = meta;
				if (meta.IsView)
				{
					var processor = ViewProcessorRegistry.GetProcessor(meta.EntityType);
					sourceMeta = SchemaRegistry.Get(processor.TableType);
				}

				var (translator, query, _) = GetQuery(sourceMeta, source.Expression);
				var (command, input) = CreateCommand(query, translator);

				return new(command);
			},
			meta => new(new SerializationItem("id0", meta.Identity.ClrType, id)),
			cancellationToken).NoWait();
	}

	async ValueTask<TEntity> IStorage.GetByIdAsync<TId, TEntity>(TId id, CancellationToken cancellationToken)
	{
		var meta = SchemaRegistry.Get(typeof(TEntity));

		if (meta.IsView)
		{
			var processor = ViewProcessorRegistry.GetProcessor<TEntity, TId>();
			return await processor.ReadById(id, cancellationToken).NoWait();
		}

		return (TEntity)await ReadAsync(meta, id,
			meta => GetCommand(meta, SqlCommandTypes.ReadBy, [meta.Identity], [], cancellationToken),
			meta => new(new SerializationItem(meta.Identity.Name, meta.Identity.ClrType, id)),
			cancellationToken).NoWait();
	}

	async ValueTask<TEntity[]> IStorage.GetByIdsAsync<TId, TEntity>(IEnumerable<TId> ids, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(ids);

		var meta = SchemaRegistry.Get(typeof(TEntity));
		var identity = meta.Identity ?? throw new ArgumentException($"Entity {meta.EntityType.Name} has no identity column.");

		var allIds = ids.ToList();
		if (allIds.Count == 0)
			return [];

		// deduplicate for DB queries, but preserve original order with duplicates for output
		var uniqueIds = allIds.Distinct().ToList();
		var resolved = new Dictionary<object, TEntity>();

		CachedSynchronizedDictionary<object, object> bulkDict = null;

		if (!meta.NoCache && _bulkLoad.TryGetValue(meta.EntityType, out var info))
			bulkDict = await info.EnsureInit(cancellationToken).NoWait();
		var uncachedIds = new List<TId>();

		foreach (var id in uniqueIds)
		{
			var found = false;

			if (bulkDict is not null && bulkDict.TryGetValue(id, out var cached))
			{
				if (cached is TEntity entity)
					resolved[(object)id] = entity;

				// null cached = known DB miss
				found = true;
			}

			if (!found && !meta.NoCache)
			{
				var key = (meta.EntityType, identity.Name, (object)id);

				using (await _cacheLock.LockAsync(cancellationToken).ConfigureAwait(false))
				{
					if (_cache.TryGetValue(key, out var t) && t.complete)
					{
						if (t.entity is TEntity entity)
							resolved[(object)id] = entity;

						found = true;
					}
				}
			}

			if (!found)
				uncachedIds.Add(id);
		}

		if (uncachedIds.Count > 0)
		{
			// query DB in batches using IN clause
			var chunkSize = 500.Min(Dialect.MaxParameters);

			foreach (var batch in uncachedIds.Chunk(chunkSize))
			{
				IEnumerable<TEntity> entities;

				if (meta.IsView)
				{
					var processor = ViewProcessorRegistry.GetProcessor<TEntity, TId>();
					entities = await processor.ReadRange([.. batch], cancellationToken).NoWait();				}
				else
				{
					var valueColumns = new List<SchemaColumn>();
					var input = new SerializationItemCollection();

					var idx = 0;
					foreach (var id in batch)
					{
						var colName = $"Id{idx++}";
						valueColumns.Add(new() { Name = colName, ClrType = identity.ClrType });
						input.Add(new(colName, identity.ClrType, id));
					}

					var cmd = await GetCommand(meta, SqlCommandTypes.ReadRange, [identity], valueColumns, cancellationToken).NoWait();
					entities = (await ReadAllAsync(cmd, meta, input, cancellationToken).NoWait()).Cast<TEntity>();
				}

				var foundIds = new HashSet<object>();

				foreach (var typedEntity in entities)
				{
					var entityId = ((IDbPersistable)typedEntity).GetIdentity();
					resolved[entityId] = typedEntity;
					foundIds.Add(entityId);

					bulkDict?[entityId] = typedEntity;
				}

				// cache misses in bulk dict so subsequent lookups return null fast
				if (bulkDict is not null)
				{
					foreach (var id in batch)
					{
						if (!foundIds.Contains(id))
							bulkDict[(object)id] = null;
					}
				}
			}
		}

		// return in input order, preserving duplicates, skipping not-found
		var result = new List<TEntity>(allIds.Count);

		foreach (var id in allIds)
		{
			if (resolved.TryGetValue((object)id, out var entity))
				result.Add(entity);
		}

		return [.. result];
	}

	async ValueTask<TEntity[]> IStorage.GetGroupAsync<TEntity>(long startIndex, long count, bool deleted, string orderBy, ListSortDirection direction, CancellationToken cancellationToken)
		=> [.. (await ReadAllAsync(SchemaRegistry.Get(typeof(TEntity)), startIndex, count, deleted, orderBy, direction, cancellationToken).NoWait()).Cast<TEntity>()];
	async ValueTask<bool> IStorage.RemoveAsync<TEntity>(TEntity entity, CancellationToken cancellationToken)
		=> await DeleteAsync(entity, cancellationToken).NoWait() > 0;
	ValueTask IStorage.ClearAsync<TEntity>(CancellationToken cancellationToken)
		=> DeleteAllAsync<TEntity>(cancellationToken);

	private class SelectAsyncEnumerable<TSource, TResult>(Database database, Expression expression) : IAsyncEnumerable<TResult>
	{
		private class SelectAsyncEnumerator(Database.SelectAsyncEnumerable<TSource, TResult> parent, CancellationToken cancellationToken) : IAsyncEnumerator<TResult>
		{
			private readonly SelectAsyncEnumerable<TSource, TResult> _parent = parent ?? throw new ArgumentNullException(nameof(parent));
			private readonly CancellationToken _cancellationToken = cancellationToken;
			private readonly Schema _meta = typeof(TResult).IsSerializablePrimitive() ? null : SchemaRegistry.Get(typeof(TResult));
			private IAsyncEnumerator<TResult> _underlying;

			TResult IAsyncEnumerator<TResult>.Current => _underlying.Current;

			async ValueTask IAsyncDisposable.DisposeAsync()
			{
				if (_underlying is null)
					return;

				await _underlying.DisposeAsync().NoWait();
				_underlying = null;
			}

			async ValueTask<bool> IAsyncEnumerator<TResult>.MoveNextAsync()
			{
				if (_underlying is null)
				{
					var db = _parent._database;
					var exp = _parent._expression;

					var (translator, query, _) = GetQuery<TSource>(exp);
					var (command, input) = db.CreateCommand(query, translator);

					var table = await db.ExecuteTable(command, input, _cancellationToken).NoWait();
					var buffer = _meta is null
						? [.. ((IEnumerable<object>)table.First().Value).Select(e => e.To<TResult>())]
						: await db.GetOrAddCacheTable<TResult>(_meta, table, _cancellationToken).NoWait()
					;

					_underlying = buffer.ToAsyncEnumerable().GetAsyncEnumerator(_cancellationToken);
				}

				return await _underlying.MoveNextAsync().NoWait();			}
		}

		private readonly Database _database = database ?? throw new ArgumentNullException(nameof(database));
		private readonly Expression _expression = expression ?? throw new ArgumentNullException(nameof(expression));

		IAsyncEnumerator<TResult> IAsyncEnumerable<TResult>.GetAsyncEnumerator(CancellationToken cancellationToken)
			=> new SelectAsyncEnumerator(this, cancellationToken);
	}

	private (DatabaseCommand command, SerializationItemCollection input) CreateCommand(Query query, ExpressionQueryTranslator translator)
	{
		var input = new SerializationItemCollection();

		foreach (var arg in translator.Parameters)
			input.Add(new(arg.Key, arg.Value.Item1, arg.Value.Item2));

		var sql = query.Render(Dialect);

		var command = _commandsByText.SafeAdd(sql, key =>
		{
			var command = new DatabaseCommand(Factory, Dialect, CreateConnectionAsync, CreateDbCommand(key, CommandType.Text));

			foreach (var item in input)
			{
				command.Parameters.Add(
					Factory.CreateDbParameter(
						Dialect.ParameterPrefix + item.Name,
						ParameterDirection.Input,
						item.Type.ToDbType(),
						item.Value));
			}

			return command;
		});

		return (command, input);
	}

	IEnumerable<TResult> IQueryContext.ExecuteEnum<TSource, TResult>(Expression expression)
		=> AsyncHelper.Run(() => ((IQueryContext)this).ExecuteEnumAsync<TSource, TResult>(expression).ToArrayAsync());

	private static (ExpressionQueryTranslator translator, Query query, CancellationToken token) GetQuery<TEntity>(Expression expression)
		=> GetQuery(SchemaRegistry.Get(typeof(TEntity)), expression);

	private static (ExpressionQueryTranslator translator, Query query, CancellationToken token) GetQuery(Schema meta, Expression expression)
	{
		var translator = new ExpressionQueryTranslator(meta);
		var query = translator.GenerateSql(expression);

		CancellationToken token = default;

		if (translator.Parameters.TryGetAndRemove(ExpressionQueryTranslator.CancellationTokenKey, out var t))
			token = (CancellationToken)t.Item2;

		return (translator, query, token);
	}

	IAsyncEnumerable<TResult> IQueryContext.ExecuteEnumAsync<TSource, TResult>(Expression expression)
		=> new SelectAsyncEnumerable<TSource, TResult>(this, expression);

	TResult IQueryContext.ExecuteResult<TSource, TResult>(Expression expression)
		=> AsyncHelper.Run(() => ExecuteResultAsync<TSource, TResult>(expression));

	/// <summary>
	/// Executes a LINQ expression and returns a single result asynchronously.
	/// </summary>
	public async ValueTask<TResult> ExecuteResultAsync<TSource, TResult>(Expression expression)
	{
		var (translator, query, token) = GetQuery<TSource>(expression);
		var (command, input) = CreateCommand(query, translator);

		if (typeof(TResult).IsSerializablePrimitive())
			return await ExecuteScalar<TResult>(command, input, token).NoWait();
		else
			return (TResult)await Read(command, SchemaRegistry.Get(typeof(TResult)), input, token).NoWait();	}

	ValueTask IQueryContext.ExecuteAsync<TSource>(Expression expression)
	{
		var (translator, query, token) = GetQuery<TSource>(expression);
		var (command, input) = CreateCommand(query, translator);

		return ExecuteNonQuery(command, input, token).AsValueTask();
	}

	#region Stat

	private IDisposable BeginTrack(DatabaseCommand command)
		=> _stat.Begin(command.CommandText, IPAddress.Loopback);

	private async ValueTask<int> ExecuteNonQuery(DatabaseCommand command, IEnumerable<SerializationItem> input, CancellationToken cancellationToken)
	{
		using var _ = BeginTrack(command);
		return await command.ExecuteNonQuery(input, cancellationToken).NoWait();	}

	private async ValueTask<TScalar> ExecuteScalar<TScalar>(DatabaseCommand command, IEnumerable<SerializationItem> input, CancellationToken cancellationToken)
	{
		using var _ = BeginTrack(command);
		return await command.ExecuteScalar<TScalar>(input, cancellationToken).NoWait();	}

	private async ValueTask<SerializationItemCollection> ExecuteRow(DatabaseCommand command, IEnumerable<SerializationItem> input, CancellationToken cancellationToken)
	{
		using var _ = BeginTrack(command);
		return await command.ExecuteRow(input, cancellationToken).NoWait();	}

	private async ValueTask<SerializationItemCollection> ExecuteTable(DatabaseCommand command, SerializationItemCollection input, CancellationToken cancellationToken)
	{
		using var _ = BeginTrack(command);
		return await command.ExecuteTable(input, cancellationToken).NoWait();	}

	#endregion
}
