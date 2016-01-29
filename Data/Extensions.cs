namespace Ecng.Data
{
	using Ecng.Data.Sql;
	using Ecng.Serialization;

	public static class Extensions
	{
		/// <summary>
		/// Execute the query.
		/// </summary>
		/// <param name="database">The database.</param>
		/// <param name="query">The query to execute.</param>
		/// <param name="schema">The schema.</param>
		public static void Execute(this Database database, Query query, Schema schema)
		{
			database.Execute(database.GetCommand(query, schema, new FieldList(), new FieldList()), new SerializationItemCollection(), false);
		}

		/// <summary>
		/// Execute the query.
		/// </summary>
		/// <param name="database">The database.</param>
		/// <param name="query">The query to execute.</param>
		public static void Execute(this Database database, string query)
		{
			database
				.GetCommand(Query.Execute(query), null, new FieldList(), new FieldList(), false)
				.ExecuteNonQuery(new SerializationItemCollection());
		}
	}
}