namespace Ecng.Data
{
	using System;

	using Ecng.Data.Sql;
	using Ecng.Serialization;

	public class HierarchicalDatabaseContext
	{
		public HierarchicalDatabaseContext(string morph, Schema schema, SerializationItemCollection source)
			: this(schema, source)
		{
			if (morph == null)
				throw new ArgumentNullException(nameof(morph));

			Morph = morph;
			
		}

		public HierarchicalDatabaseContext(Query query, Schema schema, SerializationItemCollection source)
			: this(schema, source)
		{
			if (query == null)
				throw new ArgumentNullException(nameof(query));

			Query = query;
		}

		private HierarchicalDatabaseContext(Schema schema, SerializationItemCollection source)
		{
			if (schema == null)
				throw new ArgumentNullException(nameof(schema));

			if (source == null)
				throw new ArgumentNullException(nameof(source));

			Schema = schema;
			Source = source;
		}

		public string Morph { get; }
		public Schema Schema { get; }
		public SerializationItemCollection Source { get; }
		public Query Query { get; }
	}
}