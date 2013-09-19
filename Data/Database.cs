namespace Ecng.Data
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Data;
	using System.Data.Common;
	using System.Diagnostics;
	using System.IO;
	using System.Web.UI.WebControls;
	using System.Linq;
	using System.Text.RegularExpressions;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Serialization;
	using Ecng.Data.Sql;
	using Ecng.Transactions;

	using Microsoft.Practices.EnterpriseLibrary.Caching.BackingStoreImplementations;
	using Microsoft.Practices.EnterpriseLibrary.Caching.Instrumentation;
	using Microsoft.Practices.EnterpriseLibrary.Caching;

	using Wintellect.PowerCollections;

	public class Database : Disposable, IStorage
	{
		private sealed class BatchInfo : Disposable
		{
			private readonly Database _database;
			private readonly SynchronizedList<Action> _commands = new SynchronizedList<Action>();

			public BatchInfo(Database database)
			{
				_database = database;
			}

			public DbConnection Connection { get; private set; }

			public void AddAction<TEntity>(Action action, TEntity entity)
			{
				if (action == null)
					throw new ArgumentNullException("action");

				_commands.Add(() =>
				{
					try
					{
						action();
					}
					catch (Exception ex)
					{
						throw new BatchException<TEntity>(entity, ex);
					}
				});
			}

			public void Commit()
			{
				Action action = () => AutoComplete.Do(() =>
				{
					using (_batchInfo.Connection = _database.CreateConnection())
						_commands.ForEach(a => a());

					_batchInfo.Connection = null;
				});

				if (_database.SupportParallelBatch)
					action();
				else
				{
					lock (_parallelBatchLock)
						action();
				}
			}

			protected override void DisposeManaged()
			{
				_commands.Clear();

				if (Connection != null)
				{
					Connection.Dispose();
					Connection = null;
				}

				base.DisposeManaged();
			}
		}

		#region Private Fields

		private readonly object _cacheManagerLock = new object();
		private readonly SynchronizedDictionary<Type, string[]> _cacheKeys = new SynchronizedDictionary<Type, string[]>();
		private readonly SynchronizedDictionary<Query, DatabaseCommand> _commands = new SynchronizedDictionary<Query, DatabaseCommand>();
		private readonly Dictionary<Type, ISerializer> _serializers = new Dictionary<Type, ISerializer>();

		private readonly Regex _parameterRegex = new Regex("@(?<parameterName>[_a-zA-Z]+)", RegexOptions.Multiline);

		[ThreadStatic]
		private static BatchInfo _batchInfo;

		private static readonly object _parallelBatchLock = new object();

		#endregion

		public event Action<object> Added;
		public event Action<object> Updated;
		public event Action<object> Removed;

		#region Database.ctor()

		public Database(string name, string connectionString)
		{
			Debug.WriteLine("Database.ctor()");

			Name = name;
			ConnectionString = connectionString;
			CommandType = CommandType.Text;
			Cache = new Cache(new NullBackingStore(), new CachingInstrumentationProvider("Database cache", false, false, "database"));

			SupportParallelBatch = true;
		}

		#endregion

		private string _name;

		public string Name
		{
			get { return _name; }
			set
			{
				if (value.IsEmpty())
					throw new ArgumentNullException("value");

				_name = value;
			}
		}

		private string _connectionString;

		public string ConnectionString
		{
			get { return _connectionString; }
			set
			{
				if (value.IsEmpty())
					throw new ArgumentNullException("value");

				_connectionString = value;
			}
		}

		private DatabaseProvider _provider = new SqlServerDatabaseProvider();

		public DatabaseProvider Provider
		{
			get { return _provider; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_provider = value;
			}
		}

		public CommandType CommandType { get; set; }

		private Cache _cache;

		public Cache Cache
		{
			get { return _cache; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_cache = value;
			}
		}

		private Type _serializerType = typeof(BinarySerializer<>);

		public Type SerializerType
		{
			get { return _serializerType; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_serializerType = value;
			}
		}

		public bool AllowDeleteAll { get; set; }

		private DbConnection CreateConnection()
		{
			return Provider.CreateConnection(ConnectionString);
		}

		internal void GetConnection(Action<DbConnection> action)
		{
			if (action == null)
				throw new ArgumentNullException("action");

			if (_batchInfo != null && _batchInfo.Connection != null)
				action(_batchInfo.Connection);
			else
			{
				using (var connection = CreateConnection())
					action(connection);
			}
		}

		#region Batch

		public bool SupportParallelBatch { get; set; }

		public BatchContext BeginBatch()
		{
			_batchInfo = new BatchInfo(this);
			return new BatchContext(this);
		}

		public void CommitBatch()
		{
			if (_batchInfo == null)
				throw new InvalidOperationException();

			_batchInfo.Commit();
		}

		public void EndBatch()
		{
			if (_batchInfo == null)
				throw new InvalidOperationException();

			try
			{
				_batchInfo.Dispose();
			}
			finally
			{
				_batchInfo = null;
			}
		}

		#endregion

		#region GetCommand

		public virtual DatabaseCommand GetCommand(Schema schema, SqlCommandTypes type, FieldList keyFields, FieldList valueFields)
		{
			var commandQuery = CommandType == CommandType.Text
				? Query.Create(schema, type, keyFields, valueFields)
				: Query.Execute(schema, type, keyFields, valueFields);

			return GetCommand(commandQuery, schema, keyFields, valueFields);
		}

		public virtual DatabaseCommand GetCommand(Query commandQuery, Schema schema, FieldList keyFields, FieldList valueFields)
		{
			if (commandQuery == null)
				throw new ArgumentNullException("commandQuery");

			return _commands.SafeAdd(commandQuery, key =>
			{
				var query = key.Render(Provider.Renderer);
				var dbCommand = Provider.CreateCommand(query, CommandType);

				var command = new DatabaseCommand(this, dbCommand);

				if (dbCommand.CommandType == CommandType.StoredProcedure)
				{
					using (var cn = CreateConnection())
					{
						dbCommand.Connection = cn;
						Provider.DeriveParameters(dbCommand);
					}
				}
				else
				{
					foreach (Match match in _parameterRegex.Matches(query))
					{
						if (match.Success)
						{
							var group = match.Groups["parameterName"];

							var fieldName = group.Value;

							Field field = null;

							if (keyFields != null)
								field = GetField(keyFields, fieldName, Enumerable.Empty<PairSet<string, string>>());

							if (field == null)
							{
								if (valueFields == null)
									throw new InvalidOperationException();

								field = GetField(valueFields, fieldName, Enumerable.Empty<PairSet<string, string>>());

								if (field == null)
								{
									field = schema.Fields[fieldName];
									//throw new InvalidOperationException("Field {0} doesn't exist.".Put(fieldName));
								}
							}

							var sourceType = field.Factory.SourceType;

							if (field.IsCollection())
								sourceType = typeof(string);

							command.Parameters.Add(
								this.Parameter(
									Provider.Renderer.FormatParameter(fieldName),
									ParameterDirection.Input,
									sourceType.To<DbType>(),
									DBNull.Value));
						}
					}
				}

				return command;
			});
		}

		private Field GetField(FieldList fields, string paramName, IEnumerable<PairSet<string, string>> innerSchemaNameOverrides)
		{
			var originalParamName = paramName;

			foreach (var nameOverride in innerSchemaNameOverrides)
			{
				if (nameOverride.ContainsValue(paramName))
					paramName = nameOverride.GetKey(paramName);
			}

			if (fields.Contains(paramName))
				return fields[paramName];
			else
			{
				paramName = originalParamName;

				return fields
					.Where(f => f.IsInnerSchema())
					.Select(field => GetField(field.Type.GetSchema().Fields, paramName, innerSchemaNameOverrides.Concat(new[] { field.InnerSchemaNameOverrides })))
					.FirstOrDefault(f => f != null);
			}
		}

		#endregion

		#region GetCount

		public virtual long GetCount<TEntity>()
		{
			return GetCount(SchemaManager.GetSchema<TEntity>());
		}

		public virtual long GetCount(Schema schema)
		{
			return GetCount(GetCommand(schema, SqlCommandTypes.Count, new FieldList(), new FieldList()));
		}

		public virtual long GetCount(DatabaseCommand command)
		{
			return GetCount(command, new SerializationItemCollection());
		}

		public virtual long GetCount(DatabaseCommand command, SerializationItemCollection source)
		{
			if (command == null)
				throw new ArgumentNullException("command");

			return command.ExecuteScalar<long>(source);
		}

		#endregion

		#region Create

		public virtual TEntity Create<TEntity>(TEntity entity)
		{
			if (entity.IsNull())
				throw new ArgumentNullException("entity");

			var schema = SchemaManager.GetSchema<TEntity>();

			if (schema.ReadOnly)
				throw new InvalidOperationException();

			var serializer = GetSerializer<TEntity>();

			var serFields = schema.Fields.SerializableFields;
			var readOnlyFields = serFields.ReadOnlyFields;
			var nonReadOnlyFields = serFields.NonReadOnlyFields;

			var command = GetCommand(schema, SqlCommandTypes.Create, new FieldList(), serFields);

			Action action = () =>
			{
				var input = new SerializationItemCollection();
				serializer.Serialize(entity, nonReadOnlyFields, input);
				input = UngroupSource(schema.Fields, input);

				var output = Create(command, input, !readOnlyFields.IsEmpty());
				output = GroupSource(schema.Fields, output, Enumerable.Empty<PairSet<string, string>>());

				if (!readOnlyFields.IsEmpty())
					entity = GetSerializer<TEntity>().Deserialize(output, readOnlyFields, entity);

				var databaseFields = schema.Fields.RelationManyFields;
				if (!databaseFields.IsEmpty())
					entity = serializer.Deserialize(CreateSource(databaseFields), databaseFields, entity);

				AddCache(entity, output);

				Added.SafeInvoke(entity);
			};

			if (_batchInfo != null)
				_batchInfo.AddAction(action, entity);
			else
				action();

			return entity;
		}

		public virtual SerializationItemCollection Create(DatabaseCommand command, SerializationItemCollection input, bool needRetVal)
		{
			return Execute(command, input, needRetVal);
		}

		#endregion

		#region Read

		public virtual TEntity Read<TEntity>(object id)
		{
			if (id == null)
				throw new ArgumentNullException("id");

			return Read<TEntity>(new SerializationItem(SchemaManager.GetSchema<TEntity>().Identity, id));
		}

		public virtual TEntity Read<TEntity>(SerializationItem by)
		{
			if (by == null)
				throw new ArgumentNullException("by");

			return Read<TEntity>(new SerializationItemCollection { by });
		}

		public virtual TEntity Read<TEntity>(SerializationItemCollection by)
		{
			if (by == null)
				throw new ArgumentNullException("by");

			if (by.IsEmpty())
				throw new ArgumentOutOfRangeException("by");

			foreach (var item in by)
			{
				if (item.Field.IsIndex)
				{
					var entity = GetCache<TEntity>(item.Field, item.Value);

					if (!entity.IsNull())
						return entity;
				}
			}

			var input = new SerializationItemCollection();
			var keyFields = new FieldList();

			var serializer = GetSerializer<TEntity>();

			foreach (var item in by)
			{
				input.Add(item.Field.Factory.CreateSource(serializer, item.Value));
				keyFields.Add(item.Field);
			}

			var schema = SchemaManager.GetSchema<TEntity>();
			input = UngroupSource(schema.Fields, input);
			var command = GetCommand(schema, SqlCommandTypes.ReadBy, keyFields, new FieldList());
			return Read<TEntity>(command, input);
		}

		public virtual TEntity Read<TEntity>(DatabaseCommand command, SerializationItemCollection input)
		{
			if (command == null)
				throw new ArgumentNullException("command");

			input = command.ExecuteRow(input);

			return input != null ? GetOrAddCache<TEntity>(input) : default(TEntity);
		}

		#endregion

		#region ReadAll

		public virtual IEnumerable<TEntity> ReadAll<TEntity>()
		{
			return ReadAll<TEntity>(0, GetCount(SchemaManager.GetSchema<TEntity>()));
		}

		public virtual IEnumerable<TEntity> ReadAll<TEntity>(long startIndex, long count)
		{
			return ReadAll<TEntity>(startIndex, count, SchemaManager.GetSchema<TEntity>().Identity);
		}

		public virtual IEnumerable<TEntity> ReadAll<TEntity>(long startIndex, long count, Field orderBy, SortDirection direction = SortDirection.Ascending)
		{
			if (startIndex < 0)
				throw new ArgumentOutOfRangeException("startIndex");

			if (count < 0)
				throw new ArgumentOutOfRangeException("count");

			//if (orderBy == null)
			//	throw new ArgumentNullException("orderBy");

			if (count > 0)
			{
				var fields = new FieldList(new VoidField<long>("StartIndex"), new VoidField<long>("Count"));

				if (orderBy != null)
					fields.Add(new VoidField<string>("OrderBy"));

				var input = new SerializationItemCollection
				{
					new SerializationItem(fields[0], startIndex),
					new SerializationItem(fields[1], count),
				};

				if (orderBy != null)
					input.Add(new SerializationItem(fields[2], "[{0}] {1}".Put(orderBy.Name, (direction == SortDirection.Ascending) ? "asc" : "desc")));

				return ReadAll<TEntity>(GetCommand(SchemaManager.GetSchema<TEntity>(), SqlCommandTypes.ReadAll, fields, new FieldList()), input);
			}
			else
				return new TEntity[0];
		}

		public virtual IEnumerable<TEntity> ReadAll<TEntity>(DatabaseCommand command, SerializationItemCollection input)
		{
			if (command == null)
				throw new ArgumentNullException("command");

			input = UngroupSource(SchemaManager.GetSchema<TEntity>().Fields, input);
			return GetOrAddCacheTable<TEntity>(command.ExecuteTable(input));
		}

		#endregion

		#region Update

		public virtual TEntity Update<TEntity>(TEntity entity)
		{
			return Update(entity, SchemaManager.GetSchema<TEntity>().Fields.NonIdentityFields.SerializableFields);
		}

		public virtual TEntity Update<TEntity>(TEntity entity, FieldList valueFields)
		{
			var fields = new FieldList();

			var identity = SchemaManager.GetSchema<TEntity>().Identity;
			if (identity != null)
				fields.Add(identity);

			return Update(entity, fields, valueFields);
		}

		public virtual TEntity Update<TEntity>(TEntity entity, FieldList keyFields, FieldList valueFields)
		{
			if (entity.IsNull())
				throw new ArgumentNullException("entity");

			if (keyFields == null)
				throw new ArgumentNullException("keyFields");

			if (valueFields == null)
				throw new ArgumentNullException("valueFields");

			var schema = SchemaManager.GetSchema<TEntity>();
			if (schema.ReadOnly)
				throw new InvalidOperationException();

			var command = GetCommand(schema, SqlCommandTypes.UpdateBy, keyFields, valueFields);

			Action action = () =>
			{
				var input = new SerializationItemCollection();

				var serializer = GetSerializer<TEntity>();

				serializer.Serialize(entity, keyFields, input);
				serializer.Serialize(entity, valueFields.NonReadOnlyFields, input);

				var readOnlyFields = valueFields.ReadOnlyFields;

				if (!readOnlyFields.IsEmpty() && schema.Identity != null && schema.Identity.IsReadOnly && !keyFields.Contains(schema.Identity))
					serializer.Serialize(entity, new FieldList(schema.Identity), input);

				input = UngroupSource(schema.Fields, input);

				var output = Update(command, input, !readOnlyFields.IsEmpty());

				if (!readOnlyFields.IsEmpty())
					entity = GetSerializer<TEntity>().Deserialize(output, readOnlyFields, entity);

				UpdateCache(entity, output);

				Updated.SafeInvoke(entity);
			};

			if (_batchInfo != null)
				_batchInfo.AddAction(action, entity);
			else
				action();

			return entity;
		}

		public virtual SerializationItemCollection Update(DatabaseCommand command, SerializationItemCollection source, bool needRetVal)
		{
			return Execute(command, source, needRetVal);
		}

		#endregion

		#region Delete

		public virtual void Delete<TEntity>(TEntity entity)
		{
			Delete(entity, SchemaManager.GetSchema<TEntity>().Identity);
		}

		public virtual void Delete(Schema schema, object id)
		{
			if (schema == null)
				throw new ArgumentNullException("schema");

			Delete(new SerializationItem(schema.Identity, id));
		}

		public virtual void Delete<TEntity>(TEntity entity, Field field)
		{
			if (field == null)
				throw new ArgumentNullException("field");

			Delete(new SerializationItem(field, field.GetAccessor<TEntity>().GetValue(entity)));
		}

		public virtual void Delete(SerializationItem by)
		{
			Delete(new SerializationItemCollection { by });
		}

		public virtual void Delete(SerializationItemCollection by)
		{
			if (by == null)
				throw new ArgumentNullException("by");

			if (by.IsEmpty())
				throw new ArgumentOutOfRangeException("by");

			var schema = by[0].Field.Schema;

			var input = new SerializationItemCollection();
			var keyFields = new FieldList();

			var serializer = GetSerializer<SerializationItemCollection>();

			foreach (var item in by)
			{
				input.Add(item.Field.Factory.CreateSource(serializer, item.Value));
				keyFields.Add(item.Field);
			}

			input = UngroupSource(schema.Fields, input);

			var cmd = GetCommand(schema, SqlCommandTypes.DeleteBy, keyFields, new FieldList());

			Action action = () =>
			{
				Delete(cmd, input);

				IEnumerable<object> entities;

				lock (_cacheManagerLock)
					entities = by.Select(item => Cache.GetData(CreateKey(item.Field, item.Value)));

				entities.ForEach(Removed.SafeInvoke);

				DeleteCache(by);
			};

			if (_batchInfo != null)
				_batchInfo.AddAction(action, by);
			else
				action();
		}

		public virtual void Delete(DatabaseCommand command, SerializationItemCollection input)
		{
			if (command == null)
				throw new ArgumentNullException("command");

			command.ExecuteNonQuery(input);
		}

		#endregion

		#region DeleteAll

		public virtual void DeleteAll<TEntity>()
		{
			DeleteAll(SchemaManager.GetSchema<TEntity>());
		}

		public virtual void DeleteAll(Schema schema)
		{
			DeleteAll(schema, new SerializationItemCollection());
		}

		public virtual void DeleteAll(Schema schema, SerializationItem by)
		{
			DeleteAll(schema, new SerializationItemCollection { by });
		}

		public virtual void DeleteAll(Schema schema, SerializationItemCollection by)
		{
			DeleteAll(GetCommand(schema, SqlCommandTypes.DeleteAll, new FieldList(by.Select(item => item.Field)), new FieldList()), by);
		}

		//public virtual void DeleteAll(DatabaseCommand command)
		//{
		//    DeleteAll(command, new SerializationItemCollection());
		//}

		public virtual void DeleteAll(DatabaseCommand command, SerializationItemCollection source)
		{
			if (command == null)
				throw new ArgumentNullException("command");

			if (source == null)
				throw new ArgumentNullException("source");

			if (!AllowDeleteAll)
				throw new NotSupportedException();

			Action action = () => command.ExecuteNonQuery(source);

			if (_batchInfo != null)
				_batchInfo.AddAction(action, source);
			else
				action();
		}

		#endregion

		#region ClearCache

		public void ClearCache()
		{
			lock (_cacheManagerLock)
				Cache.Flush();

			CacheCleared();
		}

		#endregion

		public virtual SerializationItemCollection Execute(DatabaseCommand command, SerializationItemCollection source, bool needRetVal)
		{
			if (command == null)
				throw new ArgumentNullException("command");

			if (needRetVal)
				return command.ExecuteRow(source);
			else
			{
				command.ExecuteNonQuery(source);
				return new SerializationItemCollection();
			}
		}

		private static SerializationItemCollection CreateSource(IEnumerable<Field> fields)
		{
			return new SerializationItemCollection(fields.Select(field => new SerializationItem(field, null)));
		}

		private static string CreateKey(Field field, object fieldValue)
		{
			if (field == null)
				throw new ArgumentNullException("field");

			if (fieldValue == null)
				throw new ArgumentNullException("fieldValue");

			return new Triple<Type, Field, object>(field.Schema.EntityType, field, fieldValue).ToString();
		}

		private TEntity GetCache<TEntity>(Field field, object value)
		{
			lock (_cacheManagerLock)
				return (TEntity)Cache.GetData(CreateKey(field, value));
		}

		private IEnumerable<TEntity> GetOrAddCacheTable<TEntity>(SerializationItemCollection table)
		{
			if (table == null)
				throw new ArgumentNullException("table");

			var schema = SchemaManager.GetSchema<TEntity>();
			var serializer = GetSerializer<TEntity>();

			var entities = new List<TEntity>();

			if (!table.IsEmpty())
			{
				var entityCount = ((ICollection)table[0].Value).Count;
				table = GroupSource(schema.Fields, table, Enumerable.Empty<PairSet<string, string>>());

				lock (_cacheManagerLock)
				{
					for (var i = 0; i < entityCount; i++)
					{
						var source = GetRow(table, i);

						if (schema.Identity != null)
						{
							var id = schema.Identity.Factory.CreateInstance(serializer, source[schema.Identity.Name]);
							var key = CreateKey(schema.Identity, id);

							if (!Cache.Contains(key))
							{
								var entity = schema.GetFactory<TEntity>().CreateEntity(serializer, source);
								entities.Add(entity);

								AddCache(entity, key, id, source, false, () =>
								{
									source.AddRange(CreateSource(schema.Fields.RelationManyFields));
									entity = serializer.Deserialize(source, schema.Fields.NonIdentityFields, entity);
									//UngroupSource(schema, item.Fourth);
								});
							}
							else
								entities.Add(GetCache<TEntity>(schema.Identity, id));
						}
						else
						{
							var entity = schema.GetFactory<TEntity>().CreateEntity(serializer, source);
							entities.Add(entity);

							source.AddRange(CreateSource(schema.Fields.RelationManyFields));
							serializer.Deserialize(source, schema.Fields.NonIdentityFields, entity);
						}
					}
				}
			}

			return entities;
		}

		private static SerializationItemCollection GetRow(IEnumerable<SerializationItem> table, int rowIndex)
		{
			var row = new SerializationItemCollection();

			foreach (var item in table)
			{
				row.Add(new SerializationItem(item.Field, item.Field.IsInnerSchema() ? GetRow((SerializationItemCollection)item.Value, rowIndex) : ((IList)item.Value)[rowIndex]));
			}

			return row;
		}

		private TEntity GetOrAddCache<TEntity>(SerializationItemCollection input)
		{
			if (input == null)
				throw new ArgumentNullException("input");

			var schema = SchemaManager.GetSchema<TEntity>();
			var serializer = GetSerializer<TEntity>();

			input = GroupSource(schema.Fields, input, Enumerable.Empty<PairSet<string, string>>());

			var entity = schema.GetFactory<TEntity>().CreateEntity(serializer, input);

			if (schema.Identity == null)
				return serializer.Deserialize(input, schema.Fields.NonIdentityFields, entity);

			var id = schema.Identity.Factory.CreateInstance(serializer, input[schema.Identity.Name]);
			var key = CreateKey(schema.Identity, id);

			lock (_cacheManagerLock)
			{
				if (!Cache.Contains(key))
				{
					AddCache(entity, key, id, input, false, () =>
					{
						input.AddRange(CreateSource(schema.Fields.RelationManyFields));
						entity = serializer.Deserialize(input, schema.Fields.NonIdentityFields, entity);
					});
				}

				return GetCache<TEntity>(schema.Identity, id);
			}
		}

		private void AddCache<TEntity>(TEntity entity, SerializationItemCollection source)
		{
			var schema = SchemaManager.GetSchema<TEntity>();

			if (schema.Identity == null)
				return;

			var serializer = GetSerializer<TEntity>();

			var id = serializer.GetId(entity);
			var key = CreateKey(schema.Identity, id);

			lock (_cacheManagerLock)
				AddCache(entity, key, id, source, true, () => { });
		}

		private void AddCache<TEntity>(TEntity entity, string key, object id, SerializationItemCollection source, bool newEntry, Action action)
		{
			if (entity.IsNull())
				throw new ArgumentNullException("entity");

			if (key.IsEmpty())
				throw new ArgumentNullException("key");

			if (id == null)
				throw new ArgumentNullException("id");

			if (source == null)
				throw new ArgumentNullException("source");

			if (action == null)
				throw new ArgumentNullException("action");

			var schema = SchemaManager.GetSchema<TEntity>();
			var serializer = GetSerializer<TEntity>();

			Cache.Add(key, entity);

			try
			{
				entity = serializer.SetId(entity, id);

				var keys = new List<string>();

				using (new Scope<IStorage>(this, false))
				{
					action();

					foreach (var field in schema.Fields.IndexFields)
					{
						if (!(field is IdentityField))
							Cache.Add(CreateKey(field, field.GetAccessor<TEntity>().GetValue(entity)), entity);

						keys.Add(field.Name);
					}
				}

				_cacheKeys.SafeAdd(schema.EntityType, k => keys.ToArray());
			}
			catch
			{
				Cache.Remove(key);
				throw;
			}

			CacheAdded(entity, source, newEntry);
		}

		private void UpdateCache<TEntity>(TEntity entity, SerializationItemCollection source)
		{
			var schema = SchemaManager.GetSchema<TEntity>();

			lock (_cacheManagerLock)
			{
				var keys = _cacheKeys.TryGetValue(schema.EntityType);

				if (keys != null)
				{
					foreach (var key in keys)
						Cache.Remove(key);
				}

				foreach (var field in schema.Fields.IndexFields)
				{
					var key = CreateKey(field, field.GetAccessor<TEntity>().GetValue(entity));
					Cache.Add(key, entity);
				}
			}

			CacheUpdated(entity, source);
		}

		private void DeleteCache(IEnumerable<SerializationItem> by)
		{
			lock (_cacheManagerLock)
			{
				foreach (var item in by)
				{
					if (item.Field.IsIndex)
						Cache.Remove(CreateKey(item.Field, item.Value));
				}
			}
		}

		protected virtual void CacheAdded<TEntity>(TEntity entity, SerializationItemCollection source, bool newEntity)
		{
		}

		protected virtual void CacheUpdated<TEntity>(TEntity entity, SerializationItemCollection newSource)
		{
		}

		protected virtual void CacheCleared()
		{
		}

		private static SerializationItemCollection GroupSource(IEnumerable<Field> fields, SerializationItemCollection input, IEnumerable<PairSet<string, string>> innerSchemaNameOverrides)
		{
			if (fields == null)
				throw new ArgumentNullException("fields");

			if (input == null)
				throw new ArgumentNullException("input");

			if (innerSchemaNameOverrides == null)
				throw new ArgumentNullException("innerSchemaNameOverrides");

			var output = new SerializationItemCollection();

			foreach (var field in fields)
			{
				if (field.IsInnerSchema())
				{
					var innerSchema = field.Factory is IDynamicSchema ? ((IDynamicSchema)field.Factory).Schema : field.Type.GetSchema();

					var field1 = field;
					var innerSchemaSource = GroupSource(
						innerSchema.Fields.Where(f => !field1.InnerSchemaIgnoreFields.Contains(f.Name)),
						input, innerSchemaNameOverrides.Concat(new[] { field.InnerSchemaNameOverrides }));

					output.Add(new SerializationItem(field, innerSchemaSource));
				}
				//else if (field.IsExternal())
				//{
				//    output.Add(new SerializationItem(field, input));
				//}
				else if (!field.IsRelationMany())
				{
					var name = field.Name;

					foreach (var nameOverride in innerSchemaNameOverrides)
					{
						if (nameOverride.ContainsKey(name))
							name = nameOverride.GetValue(name);
					}

					var item = input.TryGetItem(name);

					if (item != null)
					{
						var value = item.Value;

						if (field.IsCollection())
						{
							var serializer = (IXmlSerializer)new XmlSerializer<int>().GetSerializer(field.Type);

							if (value is IList<object>)
							{
								value = ((IList<object>)value).AsParallel().Select(v =>
								{
									var source = new SerializationItemCollection();
									serializer.Deserialize(serializer.Encoding.GetBytes((string)v).To<Stream>(), source);
									return source;
								}).ToList();
							}
							else
							{
								var source = new SerializationItemCollection();
								serializer.Deserialize(serializer.Encoding.GetBytes((string)value).To<Stream>(), source);
								value = source;
							}
						}

						output.Add(new SerializationItem(field, value));
					}
				}
			}

			return output;
		}

		private static SerializationItemCollection UngroupSource(FieldList fields, IEnumerable<SerializationItem> input)
		{
			if (fields == null)
				throw new ArgumentNullException("fields");

			if (input == null)
				throw new ArgumentNullException("input");

			var output = new SerializationItemCollection();

			foreach (var item in input)
			{
				if (fields.Contains(item.Field.Name))
				{
					if (item.Field.IsInnerSchema())
					{
						var innerSource = (SerializationItemCollection)item.Value;

						if (innerSource != null)
						{
							var innerSchema = item.Field.Factory is IDynamicSchema ? ((IDynamicSchema)item.Field.Factory).Schema : item.Field.Type.GetSchema();
							innerSource = UngroupSource(innerSchema.Fields, innerSource);
							innerSource = ConvertSourceNames(item.Field.InnerSchemaNameOverrides, innerSource, true);
							output.AddRange(innerSource);
						}
					}
					else if (!item.Field.IsRelationMany())
					{
						if (item.Field.IsCollection())
						{
							output.Remove(item);

							var serializer = (IXmlSerializer)new XmlSerializer<int>().GetSerializer(item.Field.Type);
							var stream = new MemoryStream();
							serializer.Serialize((SerializationItemCollection)item.Value, stream);

							output.Add(new SerializationItem<string>(new VoidField<string>(item.Field.Name), serializer.Encoding.GetString(stream.To<byte[]>())));
						}
						else
						{
							output.Add(item);
						}
					}
				}
				else
					output.Add(item);
			}

			return output;
		}

		private static SerializationItemCollection ConvertSourceNames(PairSet<string, string> innerSchemaNameOverrides, SerializationItemCollection source, bool isNewName)
		{
			if (innerSchemaNameOverrides == null)
				throw new ArgumentNullException("innerSchemaNameOverrides");

			if (source == null)
				throw new ArgumentNullException("source");

			if (!innerSchemaNameOverrides.IsEmpty())
			{
				var newSource = new SerializationItemCollection();

				foreach (var item in source)
				{
					var name = item.Field.Name;//.ToLowerInvariant();

					if (isNewName && innerSchemaNameOverrides.ContainsKey(name))
						name = innerSchemaNameOverrides.GetValue(name);
					else if (!isNewName && innerSchemaNameOverrides.ContainsValue(name))
						name = innerSchemaNameOverrides.GetKey(name);

					var value = item.Value;
					newSource.Add(new SerializationItem(new VoidField(name, (value != null) ? value.GetType() : item.Field.Type), value));
				}

				return newSource;
			}
			else
				return source;
		}

		public Serializer<TEntity> GetSerializer<TEntity>()
		{
			return (Serializer<TEntity>)_serializers.SafeAdd(typeof(TEntity), key => SerializerType.Make(key).CreateInstance<ISerializer>());
		}

		#region Disposable Members

		protected override void DisposeManaged()
		{
			foreach (var command in _commands.Values)
				command.Dispose();

			base.DisposeManaged();
		}

		#endregion

		TEntity IStorage.Add<TEntity>(TEntity entity)
		{
			return Create(entity);
		}

		TEntity IStorage.GetBy<TEntity>(SerializationItemCollection by)
		{
			return Read<TEntity>(by);
		}

		TEntity IStorage.GetById<TEntity>(object id)
		{
			return Read<TEntity>(id);
		}

		IEnumerable<TEntity> IStorage.GetGroup<TEntity>(long startIndex, long count, Field orderBy, SortDirection direction)
		{
			return ReadAll<TEntity>(startIndex, count, orderBy, direction);
		}

		void IStorage.Remove<TEntity>(TEntity entity)
		{
			Delete(entity);
		}

		void IStorage.Clear<TEntity>()
		{
			DeleteAll<TEntity>();
		}
	}
}
