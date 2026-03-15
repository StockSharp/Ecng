namespace Ecng.Data;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[Generator]
public class EntityGenerator : IIncrementalGenerator
{
	void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterSourceOutput(context.CompilationProvider, (spc, compilation) =>
		{
			var globalNs = compilation.SourceModule.ContainingAssembly.GlobalNamespace;

			var entityTypes = GetAllTypes(globalNs)
				.Where(m => m.TypeKind == TypeKind.Class && !m.IsStatic)
				.Where(m => HasIdentityInHierarchy(m))
				.Where(m => m.DeclaringSyntaxReferences.Any(r =>
					r.GetSyntax() is ClassDeclarationSyntax cls
					&& cls.Modifiers.Any(mod => mod.Text == "partial")))
				.ToArray();

			// Group by namespace for SchemaInitializer
			var byNamespace = new Dictionary<string, List<string>>();

			foreach (var entityType in entityTypes)
			{
				GenerateEntity(spc, entityType);

				if (!entityType.IsAbstract)
				{
					var ns = entityType.ContainingNamespace.ToDisplayString();
					if (!byNamespace.TryGetValue(ns, out var list))
						byNamespace[ns] = list = new List<string>();
					list.Add(entityType.Name);
				}
			}

			foreach (var kvp in byNamespace)
				EmitSchemaInitializer(spc, kvp.Key, kvp.Value);
		});
	}

	#region Type discovery

	private static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol ns)
	{
		foreach (var type in ns.GetTypeMembers())
			yield return type;

		foreach (var child in ns.GetNamespaceMembers())
			foreach (var type in GetAllTypes(child))
				yield return type;
	}

	private static bool HasIdentityInHierarchy(INamedTypeSymbol type)
	{
		var current = type.BaseType;
		while (current is not null)
		{
			if (HasIdentityMember(current))
				return true;
			current = current.BaseType;
		}
		return false;
	}

	private static bool HasIdentityMember(INamedTypeSymbol type)
	{
		return type.GetMembers().OfType<IPropertySymbol>()
			.Any(p => HasAttribute(p, "IdentityAttribute"));
	}

	/// <summary>
	/// Finds the class in the hierarchy that declares a member with [Identity].
	/// </summary>
	private static INamedTypeSymbol FindIdentityDeclaringType(INamedTypeSymbol type)
	{
		var current = type;
		while (current is not null)
		{
			if (HasIdentityMember(current))
				return current;
			current = current.BaseType;
		}
		return null;
	}

	#endregion

	private static void GenerateEntity(SourceProductionContext spc, INamedTypeSymbol entityType)
	{
		var entityName = entityType.Name;
		var entityNs = entityType.ContainingNamespace.ToDisplayString();
		var ownProps = GetOwnProperties(entityType).ToArray();
		var ownRelationManyProps = GetOwnRelationManyProperties(entityType).ToArray();
		var isAbstract = entityType.IsAbstract;

		if (isAbstract)
		{
			// Abstract base: generate Save/Load only (own properties), no Schema
			if (ownProps.Length == 0 && ownRelationManyProps.Length == 0)
				return;
			if (ownProps.Length > 0 && !CanHandleAllProps(ownProps))
				return;

			var sbSave = ownProps.Length > 0 ? BuildSave(ownProps) : null;
			var sbLoad = (ownProps.Length > 0 || ownRelationManyProps.Length > 0)
				? BuildLoad(ownProps, ownRelationManyProps) : null;
			var sbInitLists = ownRelationManyProps.Length > 0 ? BuildInitLists(ownRelationManyProps) : null;

			EmitSource(spc, entityNs, entityName, sbSave, sbLoad, sbInitLists, null);
		}
		else
		{
			// Concrete class: Save/Load for own props + Schema for ALL hierarchy props
			var allProps = GetAllHierarchyProperties(entityType).ToArray();

			if (ownProps.Length == 0 && allProps.Length == 0 && ownRelationManyProps.Length == 0)
				return;

			// Skip entirely if any property in the hierarchy can't be handled
			if (!CanHandleAllProps(allProps))
				return;

			var sbSave = ownProps.Length > 0 ? BuildSave(ownProps) : null;
			var sbLoad = (ownProps.Length > 0 || ownRelationManyProps.Length > 0)
				? BuildLoad(ownProps, ownRelationManyProps) : null;
			var sbInitLists = ownRelationManyProps.Length > 0 ? BuildInitLists(ownRelationManyProps) : null;
			var sbMeta = BuildMeta(entityType, entityName, allProps);

			EmitSource(spc, entityNs, entityName, sbSave, sbLoad, sbInitLists, sbMeta);
		}
	}

	#region BuildSave

	private static StringBuilder BuildSave(IPropertySymbol[] props)
	{
		var sb = new StringBuilder();
		sb.AppendLine($"\tpublic override void Save(SettingsStorage storage)");
		sb.AppendLine("\t{");
		sb.AppendLine("\t\tbase.Save(storage);");
		sb.AppendLine();
		sb.AppendLine("\t\tstorage");

		foreach (var prop in props)
			EmitSaveProperty(sb, prop);

		sb.AppendLine("\t\t;");
		sb.AppendLine("\t}");
		return sb;
	}

	private static void EmitSaveProperty(StringBuilder sb, IPropertySymbol prop)
	{
		if (IsRelationSingle(prop))
		{
			sb.AppendLine($"\t\t\t.SetFk(nameof({prop.Name}), {prop.Name}?.Id)");
		}
		else if (prop.Type.TypeKind == TypeKind.Enum)
		{
			sb.AppendLine($"\t\t\t.Set(nameof({prop.Name}), ({GetEnumUnderlyingType(prop.Type)}){prop.Name})");
		}
		else if (IsPriceType(prop.Type))
		{
			var q = IsNullableType(prop.Type) ? "?" : "";
			sb.AppendLine($"\t\t\t.Set(nameof({prop.Name}), {prop.Name}{q}.ToString())");
		}
		else if (IsInnerSchema(prop))
		{
			EmitSaveInnerSchema(sb, prop);
		}
		else
		{
			sb.AppendLine($"\t\t\t.Set(nameof({prop.Name}), {prop.Name})");
		}
	}

	private static void EmitSaveInnerSchema(StringBuilder sb, IPropertySymbol prop)
	{
		var innerType = UnwrapNullable(prop.Type);
		var innerProps = GetInnerTypeProperties(innerType);
		var nameOverrides = GetNameOverrides(prop);

		foreach (var inner in innerProps)
		{
			var colName = GetColumnName(prop.Name, inner.Name, nameOverrides);

			if (IsRelationSingle(inner))
			{
				sb.AppendLine($"\t\t\t.SetFk(\"{colName}\", {prop.Name}?.{inner.Name}?.Id)");
			}
			else if (inner.Type.TypeKind == TypeKind.Enum)
			{
				var underlying = GetEnumUnderlyingType(inner.Type);
				sb.AppendLine($"\t\t\t.Set(\"{colName}\", ({underlying}?)({prop.Name}?.{inner.Name}))");
			}
			else if (IsPriceType(inner.Type))
			{
				sb.AppendLine($"\t\t\t.Set(\"{colName}\", {prop.Name}?.{inner.Name}?.ToString())");
			}
			else
			{
				sb.AppendLine($"\t\t\t.Set(\"{colName}\", {prop.Name}?.{inner.Name})");
			}
		}
	}

	#endregion

	#region BuildLoad

	private static StringBuilder BuildLoad(IPropertySymbol[] props, IPropertySymbol[] relationManyProps)
	{
		var sb = new StringBuilder();
		sb.AppendLine($"\tpublic override async ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken ct)");
		sb.AppendLine("\t{");
		sb.AppendLine("\t\tawait base.LoadAsync(storage, db, ct);");
		sb.AppendLine();

		foreach (var prop in props)
			EmitLoadProperty(sb, prop);

		if (relationManyProps.Length > 0)
			sb.AppendLine("\t\tInitLists(db);");

		sb.AppendLine("\t}");
		return sb;
	}

	private static StringBuilder BuildInitLists(IPropertySymbol[] relationManyProps)
	{
		var sb = new StringBuilder();
		sb.AppendLine("\tpublic override void InitLists(IStorage db)");
		sb.AppendLine("\t{");
		sb.AppendLine("\t\tbase.InitLists(db);");
		sb.AppendLine();

		foreach (var prop in relationManyProps)
			EmitLoadRelationMany(sb, prop);

		sb.AppendLine("\t}");
		return sb;
	}

	private static void EmitLoadProperty(StringBuilder sb, IPropertySymbol prop)
	{
		if (IsRelationSingle(prop))
		{
			sb.AppendLine($"\t\t{prop.Name} = await storage.LoadFkAsync<{FullType(prop.Type)}>(nameof({prop.Name}), db, ct);");
		}
		else if (IsPriceType(prop.Type))
		{
			if (IsNullableType(prop.Type))
				sb.AppendLine($"\t\t{prop.Name} = storage.GetValue<string>(nameof({prop.Name}))?.ToPriceType();");
			else
				sb.AppendLine($"\t\t{prop.Name} = storage.GetValue<string>(nameof({prop.Name})).ToPriceType();");
		}
		else if (IsInnerSchema(prop))
		{
			EmitLoadInnerSchema(sb, prop);
		}
		else
		{
			sb.AppendLine($"\t\t{prop.Name} = storage.GetValue<{FullType(prop.Type)}>(nameof({prop.Name}));");
		}
	}

	private static void EmitLoadInnerSchema(StringBuilder sb, IPropertySymbol prop)
	{
		var innerType = UnwrapNullable(prop.Type);
		var innerProps = GetInnerTypeProperties(innerType);
		var nameOverrides = GetNameOverrides(prop);
		var typeName = FullType(innerType);

		sb.AppendLine($"\t\t{prop.Name} = new {typeName}");
		sb.AppendLine("\t\t{");

		foreach (var inner in innerProps)
		{
			var colName = GetColumnName(prop.Name, inner.Name, nameOverrides);

			if (IsRelationSingle(inner))
			{
				sb.AppendLine($"\t\t\t{inner.Name} = await storage.LoadFkAsync<{FullType(inner.Type)}>(\"{colName}\", db, ct),");
			}
			else if (inner.Type.TypeKind == TypeKind.Enum)
			{
				sb.AppendLine($"\t\t\t{inner.Name} = ({FullType(inner.Type)})storage.GetValue<{GetEnumUnderlyingType(inner.Type)}>(\"{colName}\"),");
			}
			else if (IsPriceType(inner.Type))
			{
				if (IsNullableType(inner.Type))
					sb.AppendLine($"\t\t\t{inner.Name} = storage.GetValue<string>(\"{colName}\")?.ToPriceType(),");
				else
					sb.AppendLine($"\t\t\t{inner.Name} = storage.GetValue<string>(\"{colName}\").ToPriceType(),");
			}
			else
			{
				sb.AppendLine($"\t\t\t{inner.Name} = storage.GetValue<{FullType(inner.Type)}>(\"{colName}\"),");
			}
		}

		sb.AppendLine("\t\t};");
	}

	private static void EmitLoadRelationMany(StringBuilder sb, IPropertySymbol prop)
	{
		var attr = prop.GetAttributes().First(a => a.AttributeClass?.Name == "RelationManyAttribute");

		// Get ListType from constructor argument
		var listType = attr.ConstructorArguments.Length > 0 ? attr.ConstructorArguments[0].Value as INamedTypeSymbol : null;
		if (listType is null)
			return;

		var listTypeName = FullType(listType);

		// Collect named argument initializers
		var inits = new List<string>();
		foreach (var namedArg in attr.NamedArguments)
		{
			if (namedArg.Key == "BulkLoad" && namedArg.Value.Value is true)
				inits.Add("BulkLoad = true");
			else if (namedArg.Key == "CacheCount" && namedArg.Value.Value is true)
				inits.Add("CacheCount = true");
			else if (namedArg.Key == "BufferSize" && namedArg.Value.Value is int bufSize && bufSize > 0)
				inits.Add($"BufferSize = {bufSize}");
		}

		var initStr = inits.Count > 0 ? $" {{ {string.Join(", ", inits)} }}" : "";
		sb.AppendLine($"\t\t{prop.Name} = new {listTypeName}(db, this){initStr};");
	}

	#endregion

	#region BuildMeta

	private static StringBuilder BuildMeta(INamedTypeSymbol entityType, string entityName, IPropertySymbol[] allProps)
	{
		var sb = new StringBuilder();
		sb.AppendLine($"\tprivate static readonly Schema _schema = CreateSchema();");
		sb.AppendLine();
		sb.AppendLine($"\tprivate static Schema CreateSchema()");
		sb.AppendLine("\t{");

		sb.AppendLine("\t\tvar columns = new List<SchemaColumn>()");
		sb.AppendLine("\t\t{");

		foreach (var prop in allProps)
			EmitMetaColumns(sb, prop);

		sb.AppendLine("\t\t};");
		sb.AppendLine();
		sb.AppendLine($"\t\tvar meta = new Schema");
		sb.AppendLine("\t\t{");

		var (entityAttrName, noCache) = GetEntityAttribute(entityType);
		var tableName = entityAttrName ?? entityName;
		var isView = IsViewEntity(entityType);

		sb.AppendLine($"\t\t\tTableName = \"{tableName}\",");
		sb.AppendLine($"\t\t\tEntityType = typeof({entityName}),");
		sb.AppendLine($"\t\t\tFactory = () => new {entityName}(),");
		sb.AppendLine($"\t\t\tIdentity = new() {{ Name = nameof(Id), ClrType = typeof(long), IsReadOnly = true }},");
		sb.AppendLine("\t\t\tColumns = columns,");

		if (noCache)
			sb.AppendLine("\t\t\tNoCache = true,");

		if (isView)
			sb.AppendLine("\t\t\tIsView = true,");

		sb.AppendLine("\t\t};");
		sb.AppendLine();
		sb.AppendLine("\t\tSchemaRegistry.Register(meta);");
		sb.AppendLine("\t\treturn meta;");
		sb.AppendLine("\t}");
		sb.AppendLine();
		sb.AppendLine("\tSchema IDbPersistable.Schema => _schema;");
		return sb;
	}

	private static void EmitMetaColumns(StringBuilder sb, IPropertySymbol prop)
	{
		if (IsInnerSchema(prop))
		{
			var innerType = UnwrapNullable(prop.Type);
			var innerProps = GetInnerTypeProperties(innerType);
			var nameOverrides = GetNameOverrides(prop);

			foreach (var inner in innerProps)
			{
				var colName = GetColumnName(prop.Name, inner.Name, nameOverrides);
				var parts = new List<string> { $"Name = \"{colName}\"" };

				if (IsRelationSingle(inner))
					parts.Add("ClrType = typeof(long)");
				else if (inner.Type.TypeKind == TypeKind.Enum)
					parts.Add($"ClrType = typeof({GetEnumUnderlyingType(inner.Type)})");
				else if (IsPriceType(inner.Type))
					parts.Add("ClrType = typeof(string)");
				else
					parts.Add($"ClrType = typeof({FullType(inner.Type)})");

				var (colNullable, colMaxLen) = GetColumnAttribute(inner);
				var nullable = colNullable ?? InferIsNullable(inner);
				if (nullable)
					parts.Add("IsNullable = true");
				if (colMaxLen > 0)
					parts.Add($"MaxLength = {colMaxLen}");

				sb.AppendLine($"\t\t\tnew() {{ {string.Join(", ", parts)} }},");
			}
		}
		else
		{
			var parts = new List<string> { $"Name = nameof({prop.Name})" };

			if (IsRelationSingle(prop))
				parts.Add("ClrType = typeof(long)");
			else if (prop.Type.TypeKind == TypeKind.Enum)
				parts.Add($"ClrType = typeof({GetEnumUnderlyingType(prop.Type)})");
			else if (IsPriceType(prop.Type))
				parts.Add("ClrType = typeof(string)");
			else
				parts.Add($"ClrType = typeof({FullType(prop.Type)})");

			if (IsUnique(prop))
				parts.Add("IsUnique = true");
			if (IsIndex(prop))
				parts.Add("IsIndex = true");

			var (colNullable, colMaxLen) = GetColumnAttribute(prop);
			var nullable = colNullable ?? InferIsNullable(prop);
			if (nullable)
				parts.Add("IsNullable = true");
			if (colMaxLen > 0)
				parts.Add($"MaxLength = {colMaxLen}");

			sb.AppendLine($"\t\t\tnew() {{ {string.Join(", ", parts)} }},");
		}
	}

	#endregion

	#region Emit

	private static void EmitSource(SourceProductionContext spc, string entityNs, string entityName, StringBuilder sbSave, StringBuilder sbLoad, StringBuilder sbInitLists, StringBuilder sbMeta)
	{
		var sb = new StringBuilder();
		sb.AppendLine("// <auto-generated />");
		sb.AppendLine($"namespace {entityNs};");
		sb.AppendLine();
		sb.AppendLine("using System;");
		sb.AppendLine("using System.Collections.Generic;");
		sb.AppendLine("using System.Threading;");
		sb.AppendLine("using System.Threading.Tasks;");
		sb.AppendLine();
		sb.AppendLine("using Ecng.Serialization;");
		sb.AppendLine();
		if (sbMeta is not null)
			sb.AppendLine($"partial class {entityName} : IDbPersistable");
		else
			sb.AppendLine($"partial class {entityName}");
		sb.AppendLine("{");

		if (sbSave is not null)
		{
			sb.Append(sbSave);
			sb.AppendLine();
		}

		if (sbLoad is not null)
		{
			sb.Append(sbLoad);
			sb.AppendLine();
		}

		if (sbInitLists is not null)
		{
			sb.Append(sbInitLists);
			sb.AppendLine();
		}

		if (sbMeta is not null)
			sb.Append(sbMeta);

		sb.AppendLine("}");

		spc.AddSource($"{entityName}_DbPersistable.cs", sb.ToString());
	}

	private static void EmitSchemaInitializer(SourceProductionContext spc, string entityNs, List<string> concreteNames)
	{
		var sb = new StringBuilder();
		sb.AppendLine("// <auto-generated />");
		sb.AppendLine($"namespace {entityNs};");
		sb.AppendLine();
		sb.AppendLine("using System.Runtime.CompilerServices;");
		sb.AppendLine();
		sb.AppendLine("static file class SchemaInitializer");
		sb.AppendLine("{");
		sb.AppendLine("\t[ModuleInitializer]");
		sb.AppendLine("\tinternal static void Init()");
		sb.AppendLine("\t{");

		foreach (var name in concreteNames)
			sb.AppendLine($"\t\tRuntimeHelpers.RunClassConstructor(typeof({name}).TypeHandle);");

		sb.AppendLine("\t}");
		sb.AppendLine("}");

		spc.AddSource("SchemaInitializer.cs", sb.ToString());
	}

	#endregion

	#region Property helpers

	private static IEnumerable<IPropertySymbol> GetOwnProperties(INamedTypeSymbol type)
	{
		return type.GetMembers().OfType<IPropertySymbol>()
			.Where(p => !p.IsReadOnly
				&& !p.IsStatic
				&& p.ExplicitInterfaceImplementations.Length == 0
				&& !HasAttribute(p, "IgnoreAttribute")
				&& !HasAttribute(p, "IdentityAttribute")
				&& !IsRelationMany(p));
	}

	private static IPropertySymbol[] GetOwnRelationManyProperties(INamedTypeSymbol type)
	{
		return type.GetMembers().OfType<IPropertySymbol>()
			.Where(p => !p.IsStatic
				&& p.ExplicitInterfaceImplementations.Length == 0
				&& IsRelationMany(p))
			.ToArray();
	}

	private static IPropertySymbol[] GetAllHierarchyProperties(INamedTypeSymbol type)
	{
		var identityType = FindIdentityDeclaringType(type);
		var props = new List<IPropertySymbol>();
		var current = type;
		while (current is not null && !SymbolEqualityComparer.Default.Equals(current, identityType))
		{
			props.InsertRange(0, GetOwnProperties(current));
			current = current.BaseType;
		}
		return props.ToArray();
	}

	/// <summary>
	/// Gets writable non-static properties of an inner type, walking inheritance.
	/// </summary>
	private static IPropertySymbol[] GetInnerTypeProperties(ITypeSymbol type)
	{
		type = UnwrapNullable(type);
		var props = new List<IPropertySymbol>();
		var current = type as INamedTypeSymbol;
		while (current is not null && current.SpecialType == SpecialType.None)
		{
			props.InsertRange(0, current.GetMembers().OfType<IPropertySymbol>()
				.Where(p => !p.IsReadOnly && !p.IsStatic
					&& !HasAttribute(p, "IgnoreAttribute")
					&& !IsRelationMany(p)));
			current = current.BaseType;
		}
		return props.ToArray();
	}

	/// <summary>
	/// Reads [NameOverride] attributes from a property.
	/// Returns Dictionary mapping innerPropName → columnName.
	/// </summary>
	private static Dictionary<string, string> GetNameOverrides(IPropertySymbol prop)
	{
		var result = new Dictionary<string, string>();
		foreach (var attr in prop.GetAttributes())
		{
			if (attr.AttributeClass?.Name != "NameOverrideAttribute")
				continue;
			if (attr.ConstructorArguments.Length < 2)
				continue;
			var innerProp = attr.ConstructorArguments[0].Value as string;
			var colName = attr.ConstructorArguments[1].Value as string;
			if (innerProp is not null && colName is not null)
				result[innerProp] = colName;
		}
		return result;
	}

	private static string GetColumnName(string outerPropName, string innerPropName, Dictionary<string, string> nameOverrides)
	{
		if (nameOverrides.TryGetValue(innerPropName, out var colName))
			return colName;
		return outerPropName + innerPropName;
	}

	#endregion

	#region Type classification

	/// <summary>
	/// Returns true if the property type is "complex" — not a primitive, enum, DateTime, byte[], or similar simple type.
	/// Complex types are either Price (handled specially), InnerSchema (flattened), or unknown (entity skipped).
	/// </summary>
	private static bool IsComplexType(IPropertySymbol prop)
	{
		if (IsRelationSingle(prop))
			return false;

		var type = UnwrapNullable(prop.Type);

		// byte[] is simple (stored as binary column)
		if (type is IArrayTypeSymbol arrayType)
			return arrayType.ElementType.SpecialType != SpecialType.System_Byte;

		// Enums are simple
		if (type.TypeKind == TypeKind.Enum)
			return false;

		// Check for known simple special types (string, int, long, bool, decimal, byte, char, etc.)
		if (type.SpecialType != SpecialType.None)
			return false;

		// Check well-known types by name
		var fullName = FullType(type);
		return fullName switch
		{
			"global::System.DateTime" => false,
			"global::System.DateTimeOffset" => false,
			"global::System.TimeSpan" => false,
			"global::System.Guid" => false,
			"global::System.DateOnly" => false,
			"global::System.TimeOnly" => false,
			_ => true,
		};
	}

	private static bool IsPriceType(ITypeSymbol type)
	{
		type = UnwrapNullable(type);
		return type.Name == "Price" && type.ContainingNamespace?.ToDisplayString() == "Ecng.ComponentModel";
	}

	private static bool IsNullableType(ITypeSymbol type)
		=> type is INamedTypeSymbol { IsGenericType: true } named
			&& named.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T;

	private static ITypeSymbol UnwrapNullable(ITypeSymbol type)
	{
		if (type is INamedTypeSymbol { IsGenericType: true } named
			&& named.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
			return named.TypeArguments[0];
		return type;
	}

	/// <summary>
	/// Check if the property is an InnerSchema type whose inner properties can all be flattened.
	/// </summary>
	private static bool IsInnerSchema(IPropertySymbol prop)
	{
		if (IsRelationSingle(prop) || IsRelationMany(prop))
			return false;
		if (!IsComplexType(prop))
			return false;
		if (IsPriceType(prop.Type))
			return false;
		return CanFlattenInnerType(UnwrapNullable(prop.Type));
	}

	/// <summary>
	/// Check if all inner properties of a type are simple enough to flatten into columns.
	/// </summary>
	private static bool CanFlattenInnerType(ITypeSymbol type)
	{
		var innerProps = GetInnerTypeProperties(type);
		if (innerProps.Length == 0)
			return false;

		foreach (var inner in innerProps)
		{
			if (IsRelationSingle(inner)) continue;
			if (IsPriceType(inner.Type)) continue;

			var t = UnwrapNullable(inner.Type);

			// byte[] is OK
			if (t is IArrayTypeSymbol arr)
			{
				if (arr.ElementType.SpecialType == SpecialType.System_Byte)
					continue;
				return false;
			}

			if (t.TypeKind == TypeKind.Enum) continue;
			if (t.SpecialType != SpecialType.None) continue;

			var fullName = FullType(t);
			if (fullName is "global::System.DateTime" or "global::System.DateTimeOffset"
				or "global::System.TimeSpan" or "global::System.Guid"
				or "global::System.DateOnly" or "global::System.TimeOnly")
				continue;

			return false; // unknown complex inner type
		}
		return true;
	}

	/// <summary>
	/// Check if ALL properties can be handled (simple, FK, Price, or flattenable InnerSchema).
	/// </summary>
	private static bool CanHandleAllProps(IPropertySymbol[] props)
	{
		foreach (var prop in props)
		{
			if (!IsComplexType(prop)) continue;
			if (IsPriceType(prop.Type)) continue;
			if (IsInnerSchema(prop)) continue;
			return false;
		}
		return true;
	}

	#endregion

	#region Symbol helpers

	private static bool IsRelationSingle(IPropertySymbol prop)
		=> HasAttribute(prop, "RelationSingleAttribute");

	private static bool IsRelationMany(IPropertySymbol prop)
		=> HasAttribute(prop, "RelationManyAttribute");

	private static bool IsUnique(IPropertySymbol prop)
		=> HasAttribute(prop, "UniqueAttribute");

	private static bool IsIndex(IPropertySymbol prop)
		=> HasAttribute(prop, "IndexAttribute") || IsUnique(prop);

	private static bool IsViewEntity(INamedTypeSymbol type)
		=> HasAttribute(type, "ViewProcessorAttribute");

	private static bool HasAttribute(ISymbol symbol, string attrName)
		=> symbol.GetAttributes().Any(a => a.AttributeClass?.Name == attrName);

	private static (bool? isNullable, int maxLength) GetColumnAttribute(IPropertySymbol prop)
	{
		var attr = prop.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "ColumnAttribute");
		if (attr is null)
			return (null, 0);

		bool? isNullable = null;
		var maxLength = 0;

		foreach (var arg in attr.NamedArguments)
		{
			switch (arg.Key)
			{
				case "IsNullable":
					isNullable = arg.Value.Value is true;
					break;
				case "MaxLength":
					maxLength = arg.Value.Value is int v ? v : 0;
					break;
			}
		}

		return (isNullable, maxLength);
	}

	private static bool InferIsNullable(IPropertySymbol prop)
		=> IsNullableType(prop.Type);

	private static (string name, bool noCache) GetEntityAttribute(INamedTypeSymbol type)
	{
		var attr = type.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "EntityAttribute");
		if (attr is null)
			return (null, false);

		string name = null;
		var noCache = false;

		foreach (var arg in attr.NamedArguments)
		{
			switch (arg.Key)
			{
				case "Name":
					name = arg.Value.Value as string;
					break;
				case "NoCache":
					noCache = arg.Value.Value is true;
					break;
			}
		}

		return (name, noCache);
	}

	private static string GetEnumUnderlyingType(ITypeSymbol type)
	{
		type = UnwrapNullable(type);

		if (type is INamedTypeSymbol named && named.EnumUnderlyingType is not null)
			return named.EnumUnderlyingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

		return "int";
	}

	private static string FullType(ITypeSymbol type)
		=> type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

	#endregion
}
