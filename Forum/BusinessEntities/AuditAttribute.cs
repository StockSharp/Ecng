namespace Ecng.Forum.BusinessEntities
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Reflection;
	using Ecng.Serialization;
	using Ecng.Collections;

	using Wintellect.PowerCollections;

	[AttributeUsage(ReflectionHelper.Types)]
	public class AuditAttribute : Attribute
	{
		public AuditAttribute(byte id)
		{
			this.Id = id;
		}

		public byte Id { get; private set; }

		public static byte GetId(Type entityType)
		{
			if (entityType == null)
				throw new ArgumentNullException("entityType");

			var attr = entityType.GetAttribute<AuditAttribute>();

			if (attr == null)
				throw new ArgumentException("Entity type '{0}' does not have AuditAttribute.".Put(entityType), "entityType");

			return attr.Id;
		}

		public static Dictionary<Pair<Field, string>, byte> GetAuditFields(Type entityType)
		{
			return GetAuditFields(entityType, new AuditFieldAttribute[0], new PairSet<string, string>());
		}

		private static Dictionary<Pair<Field, string>, byte> GetAuditFields(Type entityType, IEnumerable<AuditFieldAttribute> attrs, IDictionary<string, string> names)
		{
			if (entityType == null)
				throw new ArgumentNullException("entityType");

			if (attrs == null)
				throw new ArgumentNullException("attrs");

			var fields = new Dictionary<Pair<Field, string>, byte>();

			foreach (var field in Schema.GetSchema(entityType).Fields)
			{
				if (field.IsInnerSchema)
				{
					GetAuditFields(field.Type, field.Member.GetAttributes<AuditFieldAttribute>(), names.Concat(field.Names).ToDictionary()).CopyTo(fields);
				}
				else
				{
					var field1 = field;
					var attr = attrs.FirstOrDefault(item => item.FieldName == field1.Name) ?? field.Member.GetAttribute<AuditFieldAttribute>();

					if (attr != null)
						fields.Add(new Pair<Field, string>(field, names.TryGetValue(field.Name) ?? field.Name), attr.Id);
				}
			}

			return fields;
		}
	}
}