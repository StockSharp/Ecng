namespace Ecng.Data
{
	using System.Data;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Data.Sql;
	using Ecng.Serialization;

	public class HierarchicalDatabase : Database
	{
		public HierarchicalDatabase(string name, string connectionString)
			: base(name, connectionString)
		{
		}

		#region Database Members

		public override DatabaseCommand GetCommand(Schema schema, SqlCommandTypes type, FieldList keyFields, FieldList valueFields)
		{
			if (CommandType == CommandType.StoredProcedure)
			{
				switch (type)
				{
					case SqlCommandTypes.ReadAll:
					case SqlCommandTypes.DeleteAll:
					case SqlCommandTypes.Count:
						if (!Scope<HierarchicalDatabaseContext>.All.IsEmpty())
						{
							var morph = Scope<HierarchicalDatabaseContext>.All.Select(scope => scope.Value).Aggregate(string.Empty, (current, context) => current + context.Morph);
							return base.GetCommand(Query.Execute(schema, type, morph, null), schema, keyFields, valueFields);
						}
						else
							break;
				}
			}
			else
			{
				var context = Scope<HierarchicalDatabaseContext>.Current;

				if (context != null && context.Value.Schema == schema)
				{
					var query = context.Value.Query;

					if (query != null)
						return base.GetCommand(query, schema, keyFields, valueFields);
				}
			}

			return base.GetCommand(schema, type, keyFields, valueFields);
		}

		public override long GetCount(DatabaseCommand command, SerializationItemCollection source)
		{
			InitSource(source);
			return base.GetCount(command, source);
		}

		public override IEnumerable<TEntity> ReadAll<TEntity>(DatabaseCommand command, SerializationItemCollection source)
		{
			InitSource(source);
			return base.ReadAll<TEntity>(command, source);
		}

		#endregion

		private static void InitSource(SerializationItemCollection source)
		{
			foreach (var scope in Scope<HierarchicalDatabaseContext>.All)
			{
				source.AddRange(scope.Value.Source);
			}
		}
	}
}