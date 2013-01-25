namespace Ecng.Logic.BusinessEntities
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Data;
	using Ecng.Serialization;

	public class LogicDatabase<TUser, TRole> : HierarchicalDatabase
		where TUser : BaseUser<TUser, TRole>
		where TRole : BaseRole<TUser, TRole>
	{
		private static readonly object _auditLock = new object();
		private static readonly Dictionary<Tuple<Type, long>, SerializationItemCollection> _auditPrevValues = new Dictionary<Tuple<Type, long>, SerializationItemCollection>();
		private static readonly Dictionary<Type, Tuple<byte, IDictionary<string, byte>>> _auditIds = new Dictionary<Type, Tuple<byte, IDictionary<string, byte>>>();

		public LogicDatabase(string name, string connectionString)
			: base(name, connectionString)
		{
		}

		protected override void CacheAdded<TEntity>(TEntity entity, SerializationItemCollection source, bool newEntry)
		{
			var attr = typeof(TEntity).GetAttribute<AuditAttribute>();
			if (attr != null)
			{
				lock (_auditLock)
				{
					var schema = SchemaManager.GetSchema<TEntity>();

					var baseEntity = entity.To<BaseEntity<TUser, TRole>>();

					var info = _auditIds.SafeAdd(schema.EntityType, key =>
						new Tuple<byte, IDictionary<string, byte>>
						(
							attr.Id,
							GetAuditFields(schema.EntityType)
										.Select(p => new KeyValuePair<string, byte>(p.Key.Item2, p.Value))
										.ToDictionary()
						));

					_auditPrevValues.Add(new Tuple<Type, long>(typeof(TEntity), baseEntity.Id), source);

					if (newEntry)
						AddAudit(baseEntity.Id, info, source);
				}
			}

			base.CacheAdded(entity, source, newEntry);
		}

		protected override void CacheUpdated<TEntity>(TEntity entity, SerializationItemCollection newSource)
		{
			if (typeof(TEntity).GetAttribute<AuditAttribute>() != null)
			{
				var info = _auditIds[typeof(TEntity)];
				var diff = new SerializationItemCollection();
				var id = entity.To<BaseEntity<TUser, TRole>>().Id;

				lock (_auditLock)
				{
					var key = new Tuple<Type, long>(typeof(TEntity), id);
					var oldSource = _auditPrevValues[key];

					foreach (var newItem in newSource)
					{
						var oldItem = oldSource.TryGetItem(newItem.Field.Name);

						if (oldItem != null)
						{
							if (!object.Equals(oldItem.Value, newItem.Value))
							{
								oldItem.Value = newItem.Value;
								diff.Add(newItem);
							}
						}
					}
				}

				AddAudit(id, info, diff);
			}

			base.CacheUpdated(entity, newSource);
		}

		protected override void CacheCleared()
		{
			lock (_auditLock)
				_auditPrevValues.Clear();

			base.CacheCleared();
		}

		public AuditList<TUser, TRole> Audit { get; set; }

		private static Dictionary<Tuple<Field, string>, byte> GetAuditFields(Type entityType)
		{
			return GetAuditFields(entityType, new AuditFieldAttribute[0], new PairSet<string, string>(), new List<string>());
		}

		private static Dictionary<Tuple<Field, string>, byte> GetAuditFields(Type entityType, IEnumerable<AuditFieldAttribute> attrs, IDictionary<string, string> innerSchemaNameOverrides, IList<string> innerSchemaIgnoreFields)
		{
			if (entityType == null)
				throw new ArgumentNullException("entityType");

			if (attrs == null)
				throw new ArgumentNullException("attrs");

			if (innerSchemaNameOverrides == null)
				throw new ArgumentNullException("innerSchemaNameOverrides");

			if (innerSchemaIgnoreFields == null)
				throw new ArgumentNullException("innerSchemaIgnoreFields");

			var fields = new Dictionary<Tuple<Field, string>, byte>();

			foreach (var field in entityType.GetSchema().Fields)
			{
				if (field.IsInnerSchema())
				{
					GetAuditFields(field.Type, field.Member.GetAttributes<AuditFieldAttribute>(), innerSchemaNameOverrides.Concat(field.InnerSchemaNameOverrides).ToDictionary(), field.InnerSchemaIgnoreFields).CopyTo(fields);
				}
				else
				{
					var field1 = field;
					var attr = attrs.FirstOrDefault(item => item.FieldName == field1.Name) ?? field.Member.GetAttribute<AuditFieldAttribute>();

					if (attr != null)
						fields.Add(new Tuple<Field, string>(field, innerSchemaNameOverrides.TryGetValue(field.Name) ?? field.Name), attr.Id);
				}
			}

			return fields;
		}

		private void AddAudit(long entityId, Tuple<byte, IDictionary<string, byte>> info, IEnumerable<SerializationItem> items)
		{
			var transactionId = Guid.NewGuid();

			var auditList = items.Select(item => new Audit<TUser, TRole>
			{
				SchemaId = info.Item1,
				FieldId = info.Item2[item.Field.Name],
				TransactionId = transactionId,
				EntityId = entityId,
				Value = item.Value,
			}).ToList();

			Audit.AddRange(auditList);
		}
	}
}