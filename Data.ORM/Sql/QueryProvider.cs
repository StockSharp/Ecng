namespace Ecng.Data.Sql;

using Ecng.Common;

/// <summary>
/// Builds and caches SQL <see cref="Query"/> objects for standard CRUD operations.
/// </summary>
public class QueryProvider
{
	private readonly Dictionary<(Schema, SqlCommandTypes, string, bool), Query> _queries = [];

	/// <summary>
	/// Creates or retrieves a cached SQL query for the specified schema, command type, and columns.
	/// </summary>
	public Query Create(Schema meta, SqlCommandTypes type, IReadOnlyList<SchemaColumn> keyColumns, IReadOnlyList<SchemaColumn> valueColumns, ISqlDialect dialect = null)
	{
		ArgumentNullException.ThrowIfNull(meta);
		ArgumentNullException.ThrowIfNull(keyColumns);
		ArgumentNullException.ThrowIfNull(valueColumns);

		var supportsReturning = dialect?.SupportsInsertReturning == true;
		var cacheKey = (meta, type, keyColumns.Select(c => c.Name).JoinComma() + "|" + valueColumns.Select(c => c.Name).JoinComma(), supportsReturning);

		return _queries.SafeAdd(cacheKey, key =>
		{
			const string tableAlias = "e";

			var query = new Query();

			switch (type)
			{
				case SqlCommandTypes.Count:

					return query
								.Select()
								.Count()
									.OpenBracket()
									.Star()
									.CloseBracket()
								.NewLine()
								.From()
								.Table(meta.Name, string.Empty);

				case SqlCommandTypes.Create:

					var nonReadOnlyNames = valueColumns.Where(c => !c.IsReadOnly).Select(c => c.Name).ToArray();
					var readOnlyNames = valueColumns.Where(c => c.IsReadOnly).Select(c => c.Name).ToArray();
					var readOnlyNonIdentityNames = readOnlyNames.Where(n => !n.EqualsIgnoreCase(meta.Identity?.Name)).ToArray();

					query = query
									.Insert()
									.Into(meta.Name, nonReadOnlyNames)
									.Values(nonReadOnlyNames);

					if (readOnlyNames.Length > 0)
					{
						// Single-statement INSERT … RETURNING when (a) the only
						// readonly column is identity and (b) the dialect supports
						// inline RETURNING. Pool-safe; sidesteps lastval()/scope_identity().
						if (readOnlyNonIdentityNames.Length == 0 && supportsReturning)
						{
							query = query.Returning(meta.Identity.Name);
							return query;
						}

						var batch = new BatchQuery();
						batch.Queries.Add(query);

						if (readOnlyNonIdentityNames.Length > 0)
						{
							var keyNames = keyColumns.Select(c => c.Name).ToArray();

							batch.Queries.Add(new Query()
												.Select()
												.Exact(tableAlias, readOnlyNames)
												.NewLine()
												.From()
												.Table(meta.Name, tableAlias)
												.NewLine()
												.Where()
												.NewLine()
												.Equals(tableAlias, keyNames));
						}
						else
							batch.Queries.Add(new Query().Select().Identity(meta.Identity.Name));

						query = batch;
					}

					return query;
				case SqlCommandTypes.ReadBy:

					return query
								.Select()
								.All(tableAlias)
								.NewLine()
								.From()
								.Table(meta.Name, tableAlias)
								.NewLine()
								.Where()
								.NewLine()
								.Equals(tableAlias, keyColumns.Select(c => c.Name).ToArray());

				case SqlCommandTypes.ReadRange:

					query = query
							.Select()
							.All(tableAlias)
							.NewLine()
							.From()
							.Table(meta.Name, tableAlias)
							.NewLine()
							.Where()
							.NewLine()
							.Column(tableAlias, keyColumns[0].Name)
							.In()
							.OpenBracket();

					var idx = 0;

					foreach (var valueCol in valueColumns)
					{
						if (idx > 0)
							query.Comma();

						idx++;
						query.Param(valueCol.Name);
					}

					return query
							.CloseBracket();

				case SqlCommandTypes.ReadAll:

					return query
								.Select()
								.All(tableAlias)
								.NewLine()
								.From()
								.Table(meta.Name, tableAlias);

				case SqlCommandTypes.UpdateBy:
				{
					var nonReadOnlyNonIdentity = valueColumns
						.Where(c => !c.IsReadOnly && !c.Name.EqualsIgnoreCase(meta.Identity?.Name))
						.Select(c => c.Name).ToArray();

					if (nonReadOnlyNonIdentity.Length == 0)
						throw new NotSupportedException();

					var readOnlyNonIdentity = valueColumns
						.Where(c => c.IsReadOnly && !c.Name.EqualsIgnoreCase(meta.Identity?.Name))
						.Select(c => c.Name).ToArray();

					var keyNames = keyColumns.Select(c => c.Name).ToArray();

					query = query.AddAction((dialect, sb) =>
						dialect.AppendUpdateBy(sb, meta.Name, nonReadOnlyNonIdentity, keyNames));

					if (readOnlyNonIdentity.Length > 0)
					{
						var batch = new BatchQuery();
						batch.Queries.Add(query);

						batch.Queries.Add(new Query()
											.Select()
											.Exact(tableAlias, readOnlyNonIdentity)
											.NewLine()
											.From()
											.Table(meta.Name, tableAlias)
											.NewLine()
											.Where()
											.NewLine()
											.Equals(tableAlias, keyColumns.Select(c => c.Name).ToArray()));

						query = batch;
					}

					return query;
				}
				case SqlCommandTypes.DeleteBy:
				{
					var delKeyNames = keyColumns.Select(c => c.Name).ToArray();

					return query.AddAction((dialect, sb) =>
						dialect.AppendDeleteBy(sb, meta.Name, delKeyNames));
				}

				case SqlCommandTypes.DeleteAll:

					return query
								.Delete()
								.NewLine()
								.From()
								.Table(meta.Name, string.Empty);

				default:
					throw new ArgumentException("type");
			}
		});
	}
}
