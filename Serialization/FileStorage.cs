namespace Ecng.Serialization
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Web.UI.WebControls;

	using Ecng.Collections;
	using Ecng.Common;

	public class FileStorage : IStorage
	{
		private sealed class EntityInfo
		{
			private readonly string _fileName;

			public EntityInfo(Schema schema, ISerializer serializer, string directory)
			{
				if (schema == null)
					throw new ArgumentNullException("schema");

				if (serializer == null)
					throw new ArgumentNullException("serializer");

				Serializer = serializer;
				Schema = schema;

				_fileName = Path.Combine(directory, schema.Name + "." + serializer.FileExtension);

				Cache = (IDictionary<object, object>)Serializer.Deserialize(_fileName);
			}

			public ISerializer Serializer { get; private set; }
			public Schema Schema { get; private set; }
			public IDictionary<object, object> Cache { get; private set; }

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
				throw new ArgumentNullException("directory");

			if (serializer == null)
				throw new ArgumentNullException("serializer");

			_directory = directory;
			_serializer = serializer;
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

		public IEnumerable<TEntity> GetGroup<TEntity>(long startIndex, long count, Field orderBy, SortDirection direction)
		{
			var info = GetInfo<TEntity>();
			return info.Cache.Values.Cast<TEntity>();
		}

		public TEntity Update<TEntity>(TEntity entity)
		{
			var info = GetInfo<TEntity>();
			info.Save();
			return entity;
		}

		public void Remove<TEntity>(TEntity entity)
		{
			var info = GetInfo<TEntity>();
			info.Cache.Remove(info.Serializer.GetId(entity));
			info.Save();
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

		public BatchContext BeginBatch()
		{
			return new BatchContext(this);
		}

		public void CommitBatch()
		{
		}

		public void EndBatch()
		{
		}
	}
}