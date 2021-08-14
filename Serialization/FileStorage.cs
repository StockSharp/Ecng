namespace Ecng.Serialization
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.IO;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	public class FileStorage : IStorage
	{
		private sealed class EntityInfo
		{
			private readonly string _fileName;

			public EntityInfo(Schema schema, ISerializer serializer, string directory)
			{
				Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
				Schema = schema ?? throw new ArgumentNullException(nameof(schema));

				_fileName = Path.Combine(directory, schema.Name + "." + serializer.FileExtension);

				Cache = (IDictionary<object, object>)Serializer.Deserialize(_fileName);
			}

			public ISerializer Serializer { get; }
			public Schema Schema { get; }
			public IDictionary<object, object> Cache { get; }

			public void Save()
			{
				Serializer.Serialize(Cache, _fileName);
			}
		}

		private readonly string _directory;
		private readonly ISerializer _serializer;
		//private readonly IList<Type> _types;
		private readonly Dictionary<Type, EntityInfo> _info = new Dictionary<Type, EntityInfo>();

		public FileStorage()
			: this(Directory.GetCurrentDirectory())
		{
		}

		public FileStorage(string directory)
			: this(directory, new XmlSerializer<int>())
		{
		}

		public FileStorage(string directory, ISerializer serializer)
		{
			if (directory.IsEmpty())
				throw new ArgumentNullException(nameof(directory));
			_directory = directory;
			_serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
			//_types = _serializer.GetSerializer<IList<Type>>().Deserialize("meta.info");
		}

		public long GetCount<TEntity>()
		{
			return GetInfo<TEntity>().Cache.Count;
		}

		public TEntity Add<TEntity>(TEntity entity)
		{
			var info = GetInfo<TEntity>();
			info.Cache.Add(info.Serializer.GetId(entity), entity);
			Added?.Invoke(entity);
			return entity;
		}

		public TEntity GetBy<TEntity>(SerializationItemCollection by)
		{
			throw new NotImplementedException();
		}

		public TEntity GetById<TEntity>(object id)
		{
			var info = GetInfo<TEntity>();
			return (TEntity)info.Cache.TryGetValue(id);
		}

		public IEnumerable<TEntity> GetGroup<TEntity>(long startIndex, long count, Field orderBy, ListSortDirection direction)
		{
			var info = GetInfo<TEntity>();
			return info.Cache.Values.Cast<TEntity>();
		}

		public TEntity Update<TEntity>(TEntity entity)
		{
			var info = GetInfo<TEntity>();
			info.Save();
			Updated?.Invoke(entity);
			return entity;
		}

		public void Remove<TEntity>(TEntity entity)
		{
			var info = GetInfo<TEntity>();
			info.Cache.Remove(info.Serializer.GetId(entity));
			info.Save();
			Removed?.Invoke(entity);
		}

		public void Clear<TEntity>()
		{
			var info = GetInfo<TEntity>();
			info.Cache.Clear();
			info.Save();
		}

		private EntityInfo GetInfo<TEntity>()
		{
			return _info.SafeAdd(typeof(TEntity), key => new EntityInfo(typeof(TEntity).GetSchema(), _serializer.GetSerializer<IEnumerable<TEntity>>(), _directory));
		}

		public void ClearCache()
		{
		}

		public IBatchContext BeginBatch()
		{
			return new BatchContext(this);
		}

		public void CommitBatch()
		{
		}

		public void EndBatch()
		{
		}

		public event Action<object> Added;
		public event Action<object> Updated;
		public event Action<object> Removed;
	}
}