namespace Ecng.Data.Sql
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Text;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	public enum SqlCommandTypes
	{
		Create,
		ReadBy,
		ReadAll,
		UpdateBy,
		DeleteBy,
		//DeleteBy,
		DeleteAll,
		Count,
		Custom,
	}

	[Serializable]
	public class Query
	{
		#region Private Fields

		private readonly static Dictionary<Tuple<Schema, SqlCommandTypes, string>, Query> _queries = new Dictionary<Tuple<Schema, SqlCommandTypes, string>, Query>();
		private readonly static Dictionary<Tuple<Schema, SqlCommandTypes, int, int>, Query> _queries2 = new Dictionary<Tuple<Schema, SqlCommandTypes, int, int>, Query>();
		private readonly static Dictionary<Tuple<Type, SqlCommandTypes>, string> _procedures = new Dictionary<Tuple<Type, SqlCommandTypes>, string>();
		private readonly List<Action<SqlRenderer, StringBuilder>> _actions = new List<Action<SqlRenderer, StringBuilder>>();
		private readonly Dictionary<Tuple<SqlRenderer, Query>, string> _renderedQueries = new Dictionary<Tuple<SqlRenderer, Query>, string>();

		#endregion

		#region Query.ctor()

		internal Query()
		{
		}

		#endregion

		#region Render

		public virtual string Render(SqlRenderer renderer)
		{
			return _renderedQueries.SafeAdd(new Tuple<SqlRenderer, Query>(renderer, this), delegate
			{
				var builder = new StringBuilder();

				foreach (var action in _actions)
					action(renderer, builder);

				return builder.ToString();
			});
		}
		#endregion

		#region Execute

		public static Query Execute(Schema schema, string morph)
		{
			return GetQuery(schema, SqlCommandTypes.Custom, morph, delegate
			{
				return Query.CreateQuery((renderer, builder) => Execute(builder, schema, morph));
			});
		}

		public static Query Execute(Schema schema, SqlCommandTypes commandType, FieldList keyFields, FieldList valueFields)
		{
			if (keyFields == null)
				throw new ArgumentNullException("keyFields");

			if (valueFields == null)
				throw new ArgumentNullException("valueFields");

			var keyFieldsMorph = commandType == SqlCommandTypes.ReadAll ? string.Empty : GetMorphByFields(keyFields);

			var valueFieldsMorph = (commandType == SqlCommandTypes.UpdateBy && schema.Fields.NonIdentityFields.SerializableFields.SequenceEqual(valueFields))
				? string.Empty : GetMorphByFields(valueFields);

			return Execute(schema, commandType, keyFieldsMorph, valueFieldsMorph);
		}

		public static Query Execute(Schema schema, SqlCommandTypes commandType, string keyFieldsMorph, string valueFieldsMorph)
		{
			var morph = keyFieldsMorph + valueFieldsMorph;

			return GetQuery(schema, commandType, morph, delegate
			{
				return Query.CreateQuery((renderer, builder) =>
				{
					string storedProcedure = null;

					if (morph.IsEmpty())
					{
						storedProcedure = _procedures.SafeAdd(new Tuple<Type, SqlCommandTypes>(schema.EntityType, commandType), delegate
						{
							var attr = schema.EntityType.GetAttributes<ProcedureAttribute>().FirstOrDefault(arg => arg.CommandType == commandType);

							return attr != null ? attr.ProcedureName : null;
						});
					}

					if (storedProcedure == null)
						builder.Append(GetProcName(schema, commandType, keyFieldsMorph, valueFieldsMorph));
					else
						Execute(builder, schema, storedProcedure);
				});
			});
		}

		private static void Execute(StringBuilder builder, Schema schema, string storedProcedure)
		{
			builder.AppendFormat("{0}_{1}", schema.Name, storedProcedure);
		}

		#endregion

		#region Create

		public static Query Create(Schema schema, SqlCommandTypes type, FieldList keyFields, FieldList valueFields)
		{
			if (keyFields == null)
				throw new ArgumentNullException("keyFields");

			if (valueFields == null)
				throw new ArgumentNullException("valueFields");

			return _queries2.SafeAdd(new Tuple<Schema, SqlCommandTypes, int, int>(schema, type, keyFields.GetHashCodeEx(), valueFields.GetHashCodeEx()), key =>
			{
				switch (type)
				{
					case SqlCommandTypes.Count:

						return Query
									.Select("count(*)")
									.From(schema);

					case SqlCommandTypes.Create:

						var insert = Query
										.Insert()
										.Into(schema, valueFields.NonReadOnlyFields)
										.Values(valueFields.NonReadOnlyFields);

						if (!valueFields.ReadOnlyFields.IsEmpty())
						{
							//if (schema.Identity.IsComplex && schema.Identity.ReadOnly)
							//	throw new NotSupportedException();

							var batch = new BatchQuery();
							batch.Queries.Add(insert);

							if (!valueFields.ReadOnlyFields.NonIdentityFields.IsEmpty() || schema.Identity.IsInnerSchema())
							{
								batch.Queries.Add(Query
													.Select(valueFields.ReadOnlyFields)
													.From(schema)
													.Where()
													.Equals(keyFields));
							}
							else
								batch.Queries.Add(Query.SelectIdentity(schema));

							insert = batch;
						}

						return insert;
					case SqlCommandTypes.ReadBy:

						return Query
									.Select(schema)
									.From(schema)
									.Where()
									.Equals(keyFields);

					case SqlCommandTypes.ReadAll:

						return Query
									.Select(schema)
									.From(schema);

						/*return Query
									.Execute("PageSelect")
									.Params("Article",
											renderer.FormatParameter("SortExpression"),
											renderer.FormatParameter("StartIndex"),
											renderer.FormatParameter("Count"));*/
						//throw new NotImplementedException();

					case SqlCommandTypes.UpdateBy:

						if (schema.Fields.NonReadOnlyFields.NonIdentityFields.IsEmpty())
							throw new NotSupportedException();

						var update = Query
										.Update(schema)
										.Set(valueFields.NonReadOnlyFields.NonIdentityFields)
										.Where()
										.Equals(keyFields);

						if (!valueFields.ReadOnlyFields.NonIdentityFields.IsEmpty())
						{
							var batch = new BatchQuery();
							batch.Queries.Add(update);

							batch.Queries.Add(Query
												.Select(valueFields.ReadOnlyFields.NonIdentityFields)
												.From(schema)
												.Where()
												.Equals(keyFields));

							update = batch;
						}

						return update;
					case SqlCommandTypes.DeleteBy:

						return Query
									.Delete()
									.From(schema)
									.Where()
									.Equals(keyFields);

					//case SqlCommandTypes.DeleteBy:
					//    return Query
					//                .Delete()
					//                .From(schema)
					//                .Where()
					//                .Equals(fields[0]);

					case SqlCommandTypes.DeleteAll:

						return Query
									.Delete()
									.From(schema);

					default:
						throw new ArgumentException("type");
				}
			});
		}

		#endregion

		#region GetQuery

		private static Query GetQuery(Schema schema, SqlCommandTypes commandType, string morph, Func<Tuple<Schema, SqlCommandTypes, string>, Query> handler)
		{
			if (schema == null)
				throw new ArgumentNullException("schema");

			if (handler == null)
				throw new ArgumentNullException("handler");

			return _queries.SafeAdd(new Tuple<Schema, SqlCommandTypes, string>(schema, commandType, morph), handler);
		}

		#endregion

		#region Join

		public Query Join(Schema schema)
		{
			if (schema == null)
				throw new ArgumentNullException("schema");

			return Join(schema.Name);
		}

		public Query Join(string tableName)
		{
			return AddAction((renderer, builder) =>
				builder
						.AppendFormat(" join {0}", renderer.FormatReserver(tableName))
						.AppendLine());
		}

		#endregion

		#region On

		public Query On(Field leftField, Field rightField)
		{
			if (leftField == null)
				throw new ArgumentNullException("leftField");

			if (rightField == null)
				throw new ArgumentNullException("rightField");

			return On("{0}.{1}".Put(leftField.Schema.Name, leftField.Name), "{0}.{1}".Put(rightField.Schema.Name, rightField.Name));
		}

		public Query On(string leftField, string rightField)
		{
			return AddAction((renderer, builder) =>
				builder
						.AppendFormat(" on {0} = {1}", renderer.FormatReserver(leftField), renderer.FormatReserver(rightField))
						.AppendLine());
		}

		#endregion

		#region Select

		public static Query Select(Schema schema)
		{
			if (schema == null)
				throw new ArgumentNullException("schema");

			return Select(r => new[] { r.FormatReserver(schema.Name) + ".*" });
		}

		public static Query Select(params string[] columns)
		{
			return Select(r => columns);
		}

		private static Query Select(Func<SqlRenderer, IEnumerable<string>> getColumns)
		{
			return CreateQuery((renderer, builder) =>
			{
				builder.Append("select ");

				var columns = getColumns(renderer);

				if (!columns.IsEmpty())
				{
					foreach (var column in columns)
						builder.AppendFormat("{0}, ", column);

					RemoveLastChars(builder, 2);
				}
				else
					builder.Append("*");
			});
		}

		public static Query Select(params Field[] fields)
		{
			return Select(r => GetFieldNames(fields, new PairSet<string, string>()).Select(r.FormatReserver));
		}

		public static Query Select(FieldList fields)
		{
			if (fields == null)
				throw new ArgumentNullException("fields");

			return Select(fields.ToArray());
		}

		public static Query SelectIdentity(Schema schema)
		{
			return CreateQuery((renderer, builder) =>
				builder.AppendFormat("select {0}", renderer.GetIdentitySelect(schema)));
		}

		#endregion

		#region From

		public Query From(string tableName)
		{
			return AddAction((renderer, builder) =>
				builder
						.AppendFormat(" from {0}", renderer.FormatReserver(tableName))
						.AppendLine());
		}

		public Query From(Schema schema)
		{
			if (schema == null)
				throw new ArgumentNullException("schema");

			return From(schema.Name);
		}

		#endregion

		#region Where

		public Query Where()
		{
			return AddAction((renderer, builder) =>
				builder
						.AppendLine("where")
						.Append("\t"));
		}

		public Query And()
		{
			return AddAction((renderer, builder) => builder.AppendLine(" and "));
		}

		#endregion

		private static Query CreateQuery(Action<SqlRenderer, StringBuilder> action)
		{
			return new Query().AddAction(action);
		}

		private Query AddAction(Action<SqlRenderer, StringBuilder> action)
		{
			if (action == null)
				throw new ArgumentNullException("action");

			_actions.Add(action);
			return this;
		}

		#region Insert

		public static Query Insert()
		{
			return CreateQuery((renderer, builder) => builder.Append("insert"));
		}

		#endregion

		#region Into

		public Query Into(string tableName, params string[] columns)
		{
			return AddAction((renderer, builder) =>
			{
				builder.AppendFormat(" into {0}", renderer.FormatReserver(tableName));
				builder.Append(" (");

				foreach (string column in columns)
					builder.AppendFormat("{0}, ", renderer.FormatReserver(column));

				if (columns.Length > 0)
					RemoveLastChars(builder, 2);

				builder.Append(")");
			});
		}

		public Query Into(Schema schema, params Field[] fields)
		{
			if (schema == null)
				throw new ArgumentNullException("schema");

			return Into(schema.Name, GetFieldNames(fields, new PairSet<string, string>()));
		}

		public Query Into(Schema schema, FieldList fields)
		{
			if (fields == null)
				throw new ArgumentNullException("fields");

			return Into(schema, fields.ToArray());
		}

		#endregion

		#region Values

		public Query Values(params string[] valueNames)
		{
			return AddAction((renderer, builder) =>
			{
				builder
					.AppendLine()
					.Append("\tvalues (");

				foreach (var valueName in valueNames)
					builder.AppendFormat("{0}, ", renderer.FormatParameter(valueName));

				if (!valueNames.IsEmpty())
					RemoveLastChars(builder, 2);

				builder.Append(") ");
			});
		}

		public Query Values(params Field[] fields)
		{
			return Values(GetFieldNames(fields, new PairSet<string, string>()));
		}

		public Query Values(FieldList fields)
		{
			if (fields == null)
				throw new ArgumentNullException("fields");

			return Values(fields.ToArray());
		}

		#endregion

		#region Delete

		public static Query Delete()
		{
			return CreateQuery((renderer, builder) =>
				builder.Append("delete"));
		}

		#endregion

		#region Update

		public static Query Update(string tableName)
		{
			return CreateQuery((renderer, builder) =>
				builder
						.AppendFormat("update {0} ", renderer.FormatReserver(tableName))
						.AppendLine());
		}

		public static Query Update(Schema schema)
		{
			if (schema == null)
				throw new ArgumentNullException("schema");

			return Update(schema.Name);
		}

		public static Query Update(Type entityType)
		{
			return Update(entityType.GetSchema());
		}

		#endregion

		#region Set

		public Query Set(params SetPart[] parts)
		{
			return AddAction((renderer, builder) => Set(parts, renderer, builder));
		}

		public Query Set(FieldList fields)
		{
			if (fields == null)
				throw new ArgumentNullException("fields");

			return Set(fields.ToArray());
		}

		public Query Set(params Field[] fields)
		{
			return AddAction((renderer, builder) => Set(GetSetParts(fields, renderer), renderer, builder));
		}

		private static void Set(ICollection<SetPart> parts, SqlRenderer renderer, StringBuilder builder)
		{
			if (parts.IsEmpty())
				throw new ArgumentOutOfRangeException("parts");

			builder.AppendLine("set");

			foreach (var part in parts)
			{
				builder
						.AppendFormat("\t{0} = {1},", renderer.FormatReserver(part.Column), part.ValueName)
						.AppendLine();
			}

			RemoveLastChars(builder, 3);
			builder.AppendLine();
		}

		#endregion

		#region Equals

		public Query Equals(string column, string valueName)
		{
			return AddAction((renderer, builder) => Equals(renderer.FormatReserver(column), valueName, builder));
		}

		public Query Equals(Field field)
		{
			return Equals(new FieldList(field));
		}

		public Query Equals(params Field[] fields)
		{
			return Equals(new FieldList(fields));
		}

		public Query Equals(FieldList fields)
		{
			return AddAction((renderer, builder) =>
			{
				foreach (var field in fields)
				{
					if (field.IsInnerSchema())
					{
						foreach (var innerField in field.Type.GetSchema().Fields)
						{
							Equals(renderer.FormatReserver(innerField.Name), renderer.FormatParameter(innerField.Name), builder);
							builder.AppendFormat(" and ");
						}

						RemoveLastChars(builder, 5);
					}
					else
						Equals(renderer.FormatReserver(field.Name), renderer.FormatParameter(field.Name), builder);

					builder.AppendFormat(" and ");
				}

				if (fields.Count > 0)
					RemoveLastChars(builder, 5);
			});
		}

		private static void Equals(string column, string valueName, StringBuilder builder)
		{
			builder.AppendFormat("{0} = {1}", column, valueName);
		}

		#endregion

		#region CreateTable

		public static Query CreateTable(Type entityType)
		{
			return CreateTable(entityType.GetSchema());
		}

		public static Query CreateTable(Schema schema)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region CreateProc

		public static Query CreateProc(Schema schema, SqlCommandTypes type, FieldList keyFields, FieldList valueFields)
		{
			var procName = GetProcName(schema, type, GetMorphByFields(keyFields), (type == SqlCommandTypes.UpdateBy && valueFields.Count == schema.Fields.Count) ? string.Empty : GetMorphByFields(valueFields));

			FieldList inputParams;

			switch (type)
			{
				case SqlCommandTypes.Create:
					inputParams = schema.Fields.NonIdentityFields.NonReadOnlyFields;
					break;
				case SqlCommandTypes.ReadBy:
					inputParams = new FieldList(keyFields.Concat(valueFields));
					break;
				case SqlCommandTypes.ReadAll:
					inputParams = new FieldList();
					break;
				case SqlCommandTypes.UpdateBy:
					inputParams = schema.Fields.NonReadOnlyFields;
					if (schema.Identity.IsReadOnly)
						inputParams.Add(schema.Identity);
					break;
				case SqlCommandTypes.DeleteBy:
					inputParams = new FieldList(keyFields.Concat(valueFields));
					break;
				case SqlCommandTypes.DeleteAll:
					inputParams = new FieldList();
					break;
				case SqlCommandTypes.Count:
					inputParams = new FieldList();
					break;
				default:
					throw new ArgumentException("type");
			}

			var query = new BatchQuery();
			query.Queries.Add(Create()
									.Procedure(procName)
									.Params(inputParams)
									.As());
			query.Queries.Add(Create(schema, type, keyFields, valueFields));
			return query;
		}

		#endregion

		#region Params

		public Query Params(FieldList fields)
		{
			return AddAction((renderer, builder) =>
				Params(GetParams(fields, new PairSet<string, string>()), renderer, builder));
		}

		public Query Params(ProcParam[] @params)
		{
			return AddAction((renderer, builder) =>
				Params(@params, renderer, builder));
		}

		private static void Params(ICollection<ProcParam> @params, SqlRenderer renderer, StringBuilder builder)
		{
			if (!@params.IsEmpty())
			{
				builder.AppendLine("(");

				foreach (var param in @params)
				{
					builder
						.AppendFormat("\t{0} {1},", renderer.FormatParameter(param.Name), renderer.GetTypeName(param.Type, param.Length))
						.AppendLine();
				}

				RemoveLastChars(builder, 3);
				builder
						.AppendLine()
						.AppendLine(")");
			}
		}

		#endregion

		private static string GetMorphByFields(ICollection<Field> fields)
		{
			if (fields == null)
				throw new ArgumentNullException("fields");

			var morph = new StringBuilder();

			foreach (var field in fields)
				morph.AppendFormat("{0}And", field.Name);

			if (!fields.IsEmpty())
				RemoveLastChars(morph, 3);

			return morph.ToString();
		}

		private static ProcParam[] GetParams(IEnumerable<Field> fields, PairSet<string, string> names)
		{
			var @params = new List<ProcParam>();

			foreach (var field in fields)
			{
				if (!field.IsRelationMany())
				{
					if (!field.IsInnerSchema())
					{
						var paramType = field.Factory.SourceType;

						//if (IsRelation(param))
						//	paramType = Schema.GetSchema(paramType).Identity.Type;

						var length = new Range<int>(0, int.MaxValue);
						var attr = field.Member.GetAttribute<LengthAttribute>();
						if (attr != null)
							length = attr.Length;

						var name = field.Name;

						if (names.ContainsKey(name))
							name = names.GetValue(name);

						@params.Add(new ProcParam(name, paramType.To<DbType>(), length));
					}
					else
					{
						var field1 = field;
						@params.AddRange(GetParams(field.Type.GetSchema().Fields.Where(f => !field1.InnerSchemaIgnoreFields.Contains(f.Name)), field.InnerSchemaNameOverrides));
					}
				}
			}

			return @params.ToArray();
		}

		#region DDL

		//private static string _keyId = Guid.NewGuid().ToString();

		public static Query Create()
		{
			return CreateQuery((renderer, builder) => builder.Append("create"));
		}

		public static Query Drop()
		{
			return CreateQuery((renderer, builder) => builder.Append("drop"));
		}

		public Query Table(string name)
		{
			return AddAction((renderer, builder) =>
				builder
						.AppendFormat(" table {0}", name)
						.AppendLine());
		}

		public Query Procedure(string name)
		{
			return AddAction((renderer, builder) =>
				builder
						.AppendFormat(" procedure {0}", name)
						.AppendLine());
		}

		public static Query Execute(string procName, string args)
		{
			return CreateQuery((renderer, builder) =>
				builder
						.AppendFormat("exec {0} {1}", procName, args)
						.AppendLine());
		}

		public static Query Execute(string sqlBatch)
		{
			return CreateQuery((renderer, builder) => builder.AppendLine(sqlBatch));
		}

		public Query As()
		{
			return AddAction((renderer, builder) => builder.Append("as"));
		}

		#endregion

		#region GetProcName

		private static string GetProcName(Schema schema, SqlCommandTypes commandType, string keyFieldsMorph, string valueFieldsMorph)
		{
			string postfix;

			switch (commandType)
			{
				case SqlCommandTypes.Count:
					postfix = keyFieldsMorph + "Count";
					break;
				case SqlCommandTypes.Create:
					postfix = "Create" + keyFieldsMorph;
					break;
				case SqlCommandTypes.ReadBy:
					postfix = "Read" + valueFieldsMorph + "By" + keyFieldsMorph;
					break;
				case SqlCommandTypes.ReadAll:
					postfix = keyFieldsMorph + "ReadAll" + (valueFieldsMorph.IsEmpty() ? string.Empty : "By" + valueFieldsMorph);
					break;
				case SqlCommandTypes.UpdateBy:
					postfix = "Update" + valueFieldsMorph + "By" + keyFieldsMorph;
					break;
				case SqlCommandTypes.DeleteBy:
					postfix = "DeleteBy" + keyFieldsMorph;
					break;
				case SqlCommandTypes.DeleteAll:
					postfix = keyFieldsMorph + "DeleteAll";
					break;
				default:
					throw new ArgumentException("type");
			}

			return schema.Name + "_" + postfix;
		}

		#endregion

		#region GetFieldNames

		private static string[] GetFieldNames(IEnumerable<Field> fields, PairSet<string, string> names)
		{
			if (fields == null)
				throw new ArgumentNullException("fields");

			if (fields.IsEmpty())
				throw new ArgumentOutOfRangeException("fields");

			var fieldNames = new List<string>();

			foreach (var field in fields)
			{
				if (!field.IsRelationMany())
				{
					if (field.IsInnerSchema())
					{
						var field1 = field;
						fieldNames.AddRange(GetFieldNames(field.Type.GetSchema().Fields.Where(f => !field1.InnerSchemaIgnoreFields.Contains(f.Name)), field.InnerSchemaNameOverrides));
					}
					else
					{
						var name = field.Name;

						if (names.ContainsKey(name))
							name = names.GetValue(name);

						fieldNames.Add(name);
					}
				}
			}

			return fieldNames.ToArray();
		}

		#endregion

		#region GetSetParts

		private static SetPart[] GetSetParts(IEnumerable<Field> fields, SqlRenderer renderer)
		{
			return GetFieldNames(fields, new PairSet<string, string>())
				.Select(fieldName => new SetPart(fieldName, renderer.FormatParameter(fieldName)))
				.ToArray();
		}

		#endregion

		#region RemoveLastChars

		private static void RemoveLastChars(StringBuilder builder, int count)
		{
			builder.Remove(builder.Length - count, count);
		}

		#endregion
	}

	public class SetPart
	{
		#region SetPart.ctor()

		public SetPart(string column, string valueName)
		{
			_column = column;
			_valueName = valueName;
		}

		#endregion

		#region Column

		private readonly string _column;

		public string Column
		{
			get { return _column; }
		}

		#endregion

		#region ValueName

		private readonly string _valueName;

		public string ValueName
		{
			get { return _valueName; }
		}

		#endregion
	}

	[Serializable]
	public class BatchQuery : Query
	{
		#region Queries

		private readonly List<Query> _queries = new List<Query>();

		public List<Query> Queries
		{
			get { return _queries; }
		}

		#endregion

		#region Query Members

		public override string Render(SqlRenderer renderer)
		{
			var retVal = new StringBuilder();

			foreach (var query in Queries)
				retVal.AppendLine(query.Render(renderer) + ";");

			return retVal.ToString();
		}

		#endregion
	}
}