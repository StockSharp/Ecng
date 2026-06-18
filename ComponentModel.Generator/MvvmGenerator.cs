namespace Ecng.ComponentModel;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Incremental source generator that emits MVVM boilerplate (observable properties and commands) for
/// the Ecng notification bases, mirroring the used subset of CommunityToolkit.Mvvm.
/// </summary>
[Generator]
public class MvvmGenerator : IIncrementalGenerator
{
	private const string _category = "Ecng.ComponentModel.Generator";

	// The analyzer has no runtime reference to Ecng.ComponentModel, so attributes are matched by their
	// simple type name rather than via nameof on the actual symbols.
	private const string _observableProperty = "ObservablePropertyAttribute";
	private const string _relayCommand = "RelayCommandAttribute";
	private const string _notifyPropertyChangedFor = "NotifyPropertyChangedForAttribute";
	private const string _notifyCanExecuteChangedFor = "NotifyCanExecuteChangedForAttribute";
	private const string _notifyPropertyChangedRecipients = "NotifyPropertyChangedRecipientsAttribute";

	private static readonly DiagnosticDescriptor _failure = new(
		id: "ECNGMVVM001",
		title: "MVVM generator failed",
		messageFormat: "Ecng MVVM generator threw an exception while generating sources: {0}",
		category: _category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	private static readonly DiagnosticDescriptor _notPartial = new(
		id: "ECNGMVVM002",
		title: "Type must be partial",
		messageFormat: "Type '{0}' uses [ObservableProperty]/[RelayCommand] but is not declared 'partial'",
		category: _category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	private static readonly DiagnosticDescriptor _noNotifyBase = new(
		id: "ECNGMVVM003",
		title: "Missing notification base",
		messageFormat: "Type '{0}' uses [ObservableProperty] but no base type exposes a property-change notification method (expected NotifiableObject or ViewModelBase in the hierarchy)",
		category: _category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	private static readonly DiagnosticDescriptor _nameConflict = new(
		id: "ECNGMVVM004",
		title: "Generated member name conflict",
		messageFormat: "Field '{0}' on type '{1}' would generate member '{2}', which already exists",
		category: _category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	private static readonly DiagnosticDescriptor _badCanExecute = new(
		id: "ECNGMVVM005",
		title: "Invalid CanExecute member",
		messageFormat: "CanExecute = \"{0}\" on '{1}' does not resolve to a suitable bool member; the command is generated without a CanExecute predicate",
		category: _category,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	private static readonly DiagnosticDescriptor _badSignature = new(
		id: "ECNGMVVM006",
		title: "Unsupported command signature",
		messageFormat: "Method '{0}' has a signature not supported by [RelayCommand] (expected void/Task with at most one data parameter and an optional trailing CancellationToken)",
		category: _category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	private static readonly DiagnosticDescriptor _unknownTarget = new(
		id: "ECNGMVVM007",
		title: "Unknown notify target",
		messageFormat: "[{0}] on '{1}' references '{2}', which is neither a generated member nor an existing member; the notification is skipped",
		category: _category,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	private static readonly DiagnosticDescriptor _broadcastNoRecipient = new(
		id: "ECNGMVVM008",
		title: "Broadcast requires ObservableRecipient",
		messageFormat: "[NotifyPropertyChangedRecipients] on '{0}' requires a base type with a Broadcast method (ObservableRecipient); the broadcast is skipped",
		category: _category,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterSourceOutput(context.CompilationProvider, (spc, compilation) =>
		{
			try
			{
				Run(spc, compilation);
			}
			catch (Exception ex)
			{
				// A swallowed generator exception leaves the build "succeeded" with no output and no
				// clue why — surface it as a diagnostic instead.
				spc.ReportDiagnostic(Diagnostic.Create(_failure, Location.None, ex.ToString()));
			}
		});
	}

	private static void Run(SourceProductionContext spc, Compilation compilation)
	{
		var globalNs = compilation.SourceModule.ContainingAssembly.GlobalNamespace;

		foreach (var type in GetAllTypes(globalNs))
		{
			if (type.TypeKind != TypeKind.Class || type.IsStatic)
				continue;

			var fields = type.GetMembers().OfType<IFieldSymbol>()
				.Where(f => HasAttribute(f, _observableProperty))
				.ToArray();

			// [ObservableProperty] on a C# 13 partial property: take the defining declaration that has no
			// implementation yet — the generator supplies the implementing part.
			var partialProps = type.GetMembers().OfType<IPropertySymbol>()
				.Where(p => HasAttribute(p, _observableProperty) && p.IsPartialDefinition && p.PartialImplementationPart is null)
				.ToArray();

			var methods = type.GetMembers().OfType<IMethodSymbol>()
				.Where(m => HasAttribute(m, _relayCommand))
				.ToArray();

			if (fields.Length == 0 && partialProps.Length == 0 && methods.Length == 0)
				continue;

			if (!IsPartial(type))
			{
				spc.ReportDiagnostic(Diagnostic.Create(_notPartial, GetLocation(type), type.Name));
				continue;
			}

			GenerateForType(spc, type, fields, partialProps, methods);
		}
	}

	private static void GenerateForType(SourceProductionContext spc, INamedTypeSymbol type, IFieldSymbol[] fields, IPropertySymbol[] partialProps, IMethodSymbol[] methods)
	{
		var changed = FindNotify(type, "NotifyChanged") ?? FindNotify(type, "OnPropertyChanged") ?? FindNotify(type, "NotifyPropertyChanged");
		var changing = FindNotify(type, "NotifyChanging");
		var hasRegister = HierarchyHasMethod(type, "RegisterCommand");
		var hasBroadcast = HierarchyHasMethod(type, "Broadcast");

		if ((fields.Length > 0 || partialProps.Length > 0) && changed is null)
		{
			spc.ReportDiagnostic(Diagnostic.Create(_noNotifyBase, GetLocation(type), type.Name));
			return;
		}

		var existingMembers = new HashSet<string>(type.GetMembers().Select(m => m.Name), StringComparer.Ordinal);

		// Unify field-backed and partial-property observables; track generated names for Notify* targets.
		var observables = new List<ObservableInfo>();
		var generatedProps = new HashSet<string>(StringComparer.Ordinal);

		foreach (var field in fields)
		{
			var prop = ToPropertyName(field.Name);

			if (prop == field.Name || existingMembers.Contains(prop) || !generatedProps.Add(prop))
			{
				spc.ReportDiagnostic(Diagnostic.Create(_nameConflict, GetLocation(field), field.Name, type.Name, prop));
				continue;
			}

			observables.Add(new ObservableInfo
			{
				AttrSource = field,
				PropName = prop,
				TypeName = field.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
				BackingRef = field.Name,
				Header = "public",
				IsPartialProperty = false,
			});
		}

		foreach (var p in partialProps)
		{
			// The property name already exists as the partial definition; we implement it, not add it.
			if (!generatedProps.Add(p.Name))
			{
				spc.ReportDiagnostic(Diagnostic.Create(_nameConflict, GetLocation(p), p.Name, type.Name, p.Name));
				continue;
			}

			observables.Add(new ObservableInfo
			{
				AttrSource = p,
				PropName = p.Name,
				TypeName = p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
				BackingRef = "field",
				Header = AccessibilityText(p.DeclaredAccessibility) + " partial",
				IsPartialProperty = true,
			});
		}

		// Resolve command methods first so [NotifyCanExecuteChangedFor] can be validated against them.
		var commands = new List<CommandInfo>();
		var generatedCommands = new HashSet<string>(StringComparer.Ordinal);

		foreach (var method in methods)
		{
			var info = AnalyzeCommand(spc, type, method);
			if (info is null)
				continue;

			commands.Add(info);
			generatedCommands.Add(info.CommandName);
		}

		var sb = new StringBuilder();
		sb.AppendLine("// <auto-generated />");

		var ns = type.ContainingNamespace.IsGlobalNamespace ? null : type.ContainingNamespace.ToDisplayString();
		var indent = "\t";

		if (ns is not null)
		{
			sb.AppendLine($"namespace {ns}");
			sb.AppendLine("{");
		}
		else
		{
			indent = "";
		}

		sb.AppendLine($"{indent}partial class {type.Name}");
		sb.AppendLine($"{indent}{{");

		foreach (var obs in observables)
			EmitProperty(sb, indent + "\t", obs, changed, changing, hasBroadcast, generatedProps, generatedCommands, existingMembers, type, spc);

		foreach (var cmd in commands)
			EmitCommand(sb, indent + "\t", cmd, hasRegister);

		sb.AppendLine($"{indent}}}");

		if (ns is not null)
			sb.AppendLine("}");

		var hint = (ns?.Replace('.', '_') ?? "global") + "_" + type.Name + ".Mvvm.g.cs";
		spc.AddSource(hint, sb.ToString());
	}

	#region ObservableProperty

	private sealed class ObservableInfo
	{
		public ISymbol AttrSource;
		public string PropName;
		public string TypeName;
		public string BackingRef;
		public string Header;
		public bool IsPartialProperty;
	}

	private static void EmitProperty(StringBuilder sb, string indent, ObservableInfo obs,
		string changed, string changing, bool hasBroadcast, HashSet<string> generatedProps, HashSet<string> generatedCommands,
		HashSet<string> existingMembers, INamedTypeSymbol type, SourceProductionContext spc)
	{
		var prop = obs.PropName;
		var fieldType = obs.TypeName;
		var fieldRef = obs.BackingRef;

		var changedFors = GetStringList(obs.AttrSource, _notifyPropertyChangedFor, "PropertyName", "OtherPropertyNames");
		var canExecFors = GetStringList(obs.AttrSource, _notifyCanExecuteChangedFor, "CommandName", "OtherCommandNames");
		var broadcast = HasAttribute(obs.AttrSource, _notifyPropertyChangedRecipients);

		sb.AppendLine($"{indent}/// <summary>Gets or sets the <c>{prop}</c> observable property.</summary>");
		sb.AppendLine($"{indent}{obs.Header} {fieldType} {prop}");
		sb.AppendLine($"{indent}{{");
		sb.AppendLine($"{indent}\tget => {fieldRef};");
		sb.AppendLine($"{indent}\tset");
		sb.AppendLine($"{indent}\t{{");
		sb.AppendLine($"{indent}\t\tif (global::System.Collections.Generic.EqualityComparer<{fieldType}>.Default.Equals({fieldRef}, value))");
		sb.AppendLine($"{indent}\t\t\treturn;");
		sb.AppendLine();
		sb.AppendLine($"{indent}\t\tvar __old = {fieldRef};");
		sb.AppendLine($"{indent}\t\tOn{prop}Changing(value);");
		sb.AppendLine($"{indent}\t\tOn{prop}Changing(__old, value);");

		if (changing is not null)
			sb.AppendLine($"{indent}\t\t{changing}(nameof({prop}));");

		sb.AppendLine($"{indent}\t\t{fieldRef} = value;");
		sb.AppendLine($"{indent}\t\tOn{prop}Changed(value);");
		sb.AppendLine($"{indent}\t\tOn{prop}Changed(__old, value);");
		sb.AppendLine($"{indent}\t\t{changed}(nameof({prop}));");

		if (broadcast)
		{
			if (hasBroadcast)
				sb.AppendLine($"{indent}\t\tBroadcast(__old, value, nameof({prop}));");
			else
				spc.ReportDiagnostic(Diagnostic.Create(_broadcastNoRecipient, GetLocation(obs.AttrSource), type.Name));
		}

		foreach (var dep in changedFors)
		{
			if (generatedProps.Contains(dep) || existingMembers.Contains(dep))
				sb.AppendLine($"{indent}\t\t{changed}(\"{dep}\");");
			else
				spc.ReportDiagnostic(Diagnostic.Create(_unknownTarget, GetLocation(obs.AttrSource), _notifyPropertyChangedFor, type.Name, dep));
		}

		foreach (var cmd in canExecFors)
		{
			if (generatedCommands.Contains(cmd) || existingMembers.Contains(cmd))
				sb.AppendLine($"{indent}\t\t{cmd}.RaiseCanExecuteChanged();");
			else
				spc.ReportDiagnostic(Diagnostic.Create(_unknownTarget, GetLocation(obs.AttrSource), _notifyCanExecuteChangedFor, type.Name, cmd));
		}

		sb.AppendLine($"{indent}\t}}");
		sb.AppendLine($"{indent}}}");
		sb.AppendLine();

		// Partial hooks — defining declarations only; the user optionally implements them.
		sb.AppendLine($"{indent}partial void On{prop}Changing({fieldType} value);");
		sb.AppendLine($"{indent}partial void On{prop}Changing({fieldType} oldValue, {fieldType} newValue);");
		sb.AppendLine($"{indent}partial void On{prop}Changed({fieldType} value);");
		sb.AppendLine($"{indent}partial void On{prop}Changed({fieldType} oldValue, {fieldType} newValue);");
		sb.AppendLine();
	}

	#endregion

	#region RelayCommand

	private sealed class CommandInfo
	{
		public IMethodSymbol Method;
		public string CommandName;
		public string BaseName;
		public string FieldName;
		public string CommandType;
		public string CtorArgs;
		public bool IsAsync;
		public bool IncludeCancel;
		public string CancelType;
	}

	private static CommandInfo AnalyzeCommand(SourceProductionContext spc, INamedTypeSymbol type, IMethodSymbol method)
	{
		var attr = method.GetAttributes().First(a => a.AttributeClass?.Name == _relayCommand);

		var canExecuteName = attr.NamedArguments.FirstOrDefault(a => a.Key == "CanExecute").Value.Value as string;
		var includeCancel = attr.NamedArguments.FirstOrDefault(a => a.Key == "IncludeCancelCommand").Value.Value is true;
		var allowConcurrent = attr.NamedArguments.FirstOrDefault(a => a.Key == "AllowConcurrentExecutions").Value.Value is true;

		var isAsync = IsTask(method.ReturnType);
		var isVoid = method.ReturnsVoid;

		if (!isAsync && !isVoid)
		{
			spc.ReportDiagnostic(Diagnostic.Create(_badSignature, GetLocation(method), method.Name));
			return null;
		}

		var pars = method.Parameters;
		var hasCt = isAsync && pars.Length > 0 && IsCancellationToken(pars[pars.Length - 1].Type);
		var dataPars = hasCt ? pars.Take(pars.Length - 1).ToArray() : pars.ToArray();

		if (dataPars.Length > 1)
		{
			spc.ReportDiagnostic(Diagnostic.Create(_badSignature, GetLocation(method), method.Name));
			return null;
		}

		var hasParam = dataPars.Length == 1;
		var paramType = hasParam ? dataPars[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) : null;
		var methodName = method.Name;

		var baseName = methodName.EndsWith("Async", StringComparison.Ordinal) && methodName.Length > "Async".Length
			? methodName.Substring(0, methodName.Length - "Async".Length)
			: methodName;

		var commandName = baseName + "Command";

		string cmdType;
		if (isAsync)
			cmdType = hasParam ? $"global::Ecng.ComponentModel.AsyncCommand<{paramType}>" : "global::Ecng.ComponentModel.AsyncCommand";
		else
			cmdType = hasParam ? $"global::Ecng.ComponentModel.DelegateCommand<{paramType}>" : "global::Ecng.ComponentModel.DelegateCommand";

		var predicate = BuildCanExecute(spc, type, method, canExecuteName, isAsync, hasParam);

		// Build constructor arguments.
		var args = new List<string>();

		if (!isAsync && !hasParam)
		{
			// DelegateCommand: Action ctor (no predicate) vs Action<object>+Func<object,bool> ctor.
			if (predicate is null)
				args.Add(methodName);
			else
			{
				args.Add($"_ => {methodName}()");
				args.Add(predicate);
			}
		}
		else
		{
			// All other command types accept the method group directly, with an optional predicate.
			args.Add(methodName);

			if (isAsync)
			{
				// AsyncCommand ctor is (execute, canExecute = null, allowMultipleExecution = false);
				// pass an explicit null when only the concurrency flag is set.
				if (predicate is not null)
					args.Add(predicate);
				else if (allowConcurrent)
					args.Add("null");

				if (allowConcurrent)
					args.Add("true");
			}
			else if (predicate is not null)
			{
				args.Add(predicate);
			}
		}

		return new CommandInfo
		{
			Method = method,
			CommandName = commandName,
			BaseName = baseName,
			FieldName = "__cmd_" + methodName,
			CommandType = cmdType,
			CtorArgs = string.Join(", ", args),
			IsAsync = isAsync,
			IncludeCancel = isAsync && includeCancel,
			CancelType = "global::Ecng.ComponentModel.IRevalidatableCommand",
		};
	}

	// Returns the predicate lambda text for the command kind, or null when no/invalid CanExecute.
	private static string BuildCanExecute(SourceProductionContext spc, INamedTypeSymbol type, IMethodSymbol method, string canExecuteName, bool isAsync, bool hasParam)
	{
		if (canExecuteName.IsEmpty())
			return null;

		var member = FindBoolMember(type, canExecuteName, out var memberHasParam, out var isMethod);

		if (!member)
		{
			spc.ReportDiagnostic(Diagnostic.Create(_badCanExecute, GetLocation(method), canExecuteName, method.Name));
			return null;
		}

		// A predicate that consumes the command parameter is only valid for parameterized commands.
		if (memberHasParam && !hasParam)
		{
			spc.ReportDiagnostic(Diagnostic.Create(_badCanExecute, GetLocation(method), canExecuteName, method.Name));
			return null;
		}

		string call;
		if (!isMethod)
			call = canExecuteName;                                   // bool property
		else if (memberHasParam)
			call = $"{canExecuteName}(__p)";                          // bool Method(T)
		else
			call = $"{canExecuteName}()";                             // bool Method()

		if (hasParam)
			return $"__p => {call}";

		// Parameterless command: DelegateCommand wants Func<object,bool>, AsyncCommand wants Func<bool>.
		return isAsync ? $"() => {call}" : $"_ => {call}";
	}

	private static void EmitCommand(StringBuilder sb, string indent, CommandInfo cmd, bool hasRegister)
	{
		sb.AppendLine($"{indent}private {cmd.CommandType} {cmd.FieldName};");
		sb.AppendLine($"{indent}/// <summary>Gets the command wrapping <see cref=\"{cmd.Method.Name}\"/>.</summary>");
		sb.AppendLine($"{indent}public {cmd.CommandType} {cmd.CommandName}");
		sb.AppendLine($"{indent}{{");
		sb.AppendLine($"{indent}\tget");
		sb.AppendLine($"{indent}\t{{");
		sb.AppendLine($"{indent}\t\tvar __existing = {cmd.FieldName};");
		sb.AppendLine($"{indent}\t\tif (__existing is not null)");
		sb.AppendLine($"{indent}\t\t\treturn __existing;");
		sb.AppendLine();
		sb.AppendLine($"{indent}\t\tvar __new = new {cmd.CommandType}({cmd.CtorArgs});");
		sb.AppendLine($"{indent}\t\t__existing = global::System.Threading.Interlocked.CompareExchange(ref {cmd.FieldName}, __new, null);");
		sb.AppendLine($"{indent}\t\tif (__existing is not null)");
		sb.AppendLine($"{indent}\t\t{{");
		sb.AppendLine($"{indent}\t\t\t((global::System.IDisposable)__new).Dispose();");
		sb.AppendLine($"{indent}\t\t\treturn __existing;");
		sb.AppendLine($"{indent}\t\t}}");
		sb.AppendLine();

		if (hasRegister)
			sb.AppendLine($"{indent}\t\tRegisterCommand(__new);");

		sb.AppendLine($"{indent}\t\treturn __new;");
		sb.AppendLine($"{indent}\t}}");
		sb.AppendLine($"{indent}}}");
		sb.AppendLine();

		if (cmd.IncludeCancel)
		{
			sb.AppendLine($"{indent}/// <summary>Gets the command that cancels <see cref=\"{cmd.CommandName}\"/>.</summary>");
			sb.AppendLine($"{indent}public {cmd.CancelType} {cmd.BaseName}CancelCommand => {cmd.CommandName}.CancelCommand;");
			sb.AppendLine();
		}
	}

	#endregion

	#region Symbol helpers

	private static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol ns)
	{
		foreach (var type in ns.GetTypeMembers())
		{
			yield return type;

			foreach (var nested in GetNestedTypes(type))
				yield return nested;
		}

		foreach (var child in ns.GetNamespaceMembers())
			foreach (var type in GetAllTypes(child))
				yield return type;
	}

	private static IEnumerable<INamedTypeSymbol> GetNestedTypes(INamedTypeSymbol type)
	{
		foreach (var nested in type.GetTypeMembers())
		{
			yield return nested;

			foreach (var deeper in GetNestedTypes(nested))
				yield return deeper;
		}
	}

	private static bool IsPartial(INamedTypeSymbol type)
		=> type.DeclaringSyntaxReferences.Any(r =>
			r.GetSyntax() is ClassDeclarationSyntax cls
			&& cls.Modifiers.Any(m => m.Text == "partial"));

	// Returns the notification method name if a void method taking a single string exists and is
	// accessible from the generated (same-class) code anywhere in the hierarchy; otherwise null.
	private static string FindNotify(INamedTypeSymbol type, string name)
		=> HierarchyHasVoidStringMethod(type, name) ? name : null;

	private static bool HierarchyHasVoidStringMethod(INamedTypeSymbol type, string name)
	{
		for (var current = type; current is not null; current = current.BaseType)
		{
			if (current.GetMembers(name).OfType<IMethodSymbol>().Any(m =>
				m.ReturnsVoid &&
				m.Parameters.Length == 1 &&
				m.Parameters[0].Type.SpecialType == SpecialType.System_String &&
				IsAccessibleToDerived(m)))
				return true;
		}

		return false;
	}

	private static bool HierarchyHasMethod(INamedTypeSymbol type, string name)
	{
		for (var current = type; current is not null; current = current.BaseType)
		{
			if (current.GetMembers(name).OfType<IMethodSymbol>().Any(IsAccessibleToDerived))
				return true;
		}

		return false;
	}

	// True if the bool member (property or method) named 'name' exists and is reachable; reports back
	// whether it takes a single parameter and whether it is a method.
	private static bool FindBoolMember(INamedTypeSymbol type, string name, out bool hasParam, out bool isMethod)
	{
		for (var current = type; current is not null; current = current.BaseType)
		{
			// Private members declared on the type itself are reachable from the generated (same-class)
			// code; on base types only protected/internal/public members are.
			var allowPrivate = SymbolEqualityComparer.Default.Equals(current, type);

			foreach (var member in current.GetMembers(name))
			{
				if (member is IPropertySymbol p && p.Type.SpecialType == SpecialType.System_Boolean && IsAccessible(p, allowPrivate))
				{
					hasParam = false;
					isMethod = false;
					return true;
				}

				if (member is IMethodSymbol m && m.ReturnType.SpecialType == SpecialType.System_Boolean &&
					m.Parameters.Length <= 1 && IsAccessible(m, allowPrivate))
				{
					hasParam = m.Parameters.Length == 1;
					isMethod = true;
					return true;
				}
			}
		}

		hasParam = false;
		isMethod = false;
		return false;
	}

	private static bool IsAccessibleToDerived(ISymbol symbol)
		=> symbol.DeclaredAccessibility is
			Accessibility.Public or
			Accessibility.Protected or
			Accessibility.Internal or
			Accessibility.ProtectedOrInternal or
			Accessibility.ProtectedAndInternal;

	private static bool IsAccessible(ISymbol symbol, bool allowPrivate)
		=> IsAccessibleToDerived(symbol) || (allowPrivate && symbol.DeclaredAccessibility == Accessibility.Private);

	private static bool IsTask(ITypeSymbol type)
	{
		if (type is not INamedTypeSymbol named)
			return false;

		// System.Threading.Tasks.Task or Task<T>.
		return named.Name == "Task" && named.ContainingNamespace?.ToDisplayString() == "System.Threading.Tasks";
	}

	private static bool IsCancellationToken(ITypeSymbol type)
		=> type.Name == "CancellationToken" && type.ContainingNamespace?.ToDisplayString() == "System.Threading";

	private static bool HasAttribute(ISymbol symbol, string attrName)
		=> symbol.GetAttributes().Any(a => a.AttributeClass?.Name == attrName);

	// Reads the "first + params" string-list shape used by NotifyPropertyChangedFor/NotifyCanExecuteChangedFor.
	private static List<string> GetStringList(ISymbol symbol, string attrName, string firstProp, string restProp)
	{
		var result = new List<string>();

		foreach (var attr in symbol.GetAttributes().Where(a => a.AttributeClass?.Name == attrName))
		{
			if (attr.ConstructorArguments.Length == 0)
				continue;

			if (attr.ConstructorArguments[0].Value is string first && !first.IsEmpty())
				result.Add(first);

			if (attr.ConstructorArguments.Length > 1 && attr.ConstructorArguments[1].Kind == TypedConstantKind.Array)
			{
				foreach (var v in attr.ConstructorArguments[1].Values)
					if (v.Value is string s && !s.IsEmpty())
						result.Add(s);
			}
		}

		return result;
	}

	private static string AccessibilityText(Accessibility accessibility)
		=> accessibility switch
		{
			Accessibility.Private => "private",
			Accessibility.Protected => "protected",
			Accessibility.Internal => "internal",
			Accessibility.ProtectedOrInternal => "protected internal",
			Accessibility.ProtectedAndInternal => "private protected",
			_ => "public",
		};

	// _name / name / m_name -> Name (mirrors CommunityToolkit field-to-property naming).
	private static string ToPropertyName(string fieldName)
	{
		var n = fieldName;

		if (n.StartsWith("m_", StringComparison.Ordinal) && n.Length > 2)
			n = n.Substring(2);
		else
			n = n.TrimStart('_');

		if (n.Length == 0)
			return fieldName;

		return char.ToUpperInvariant(n[0]) + n.Substring(1);
	}

	private static Location GetLocation(ISymbol symbol)
		=> symbol.Locations.FirstOrDefault() ?? Microsoft.CodeAnalysis.Location.None;

	#endregion
}

file static class StringExtensions
{
	public static bool IsEmpty(this string value)
		=> string.IsNullOrEmpty(value);
}
