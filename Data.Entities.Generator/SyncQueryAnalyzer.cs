namespace Ecng.Data;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

/// <summary>
/// Reports synchronous LINQ terminals (<c>ToList</c>/<c>First</c>/<c>Any</c>/<c>Count</c>/…)
/// applied to an Ecng.Data.ORM query. Such terminals route through
/// <c>IQueryProvider.Execute</c> → <c>AsyncHelper.Run</c>, which blocks a thread-pool
/// thread for the entire database round-trip; under load that starves the pool and makes
/// every request time out (only a restart clears it). The async terminals
/// (<c>ToArrayAsyncEx</c>/<c>FirstAsyncEx</c>/<c>CountAsyncEx</c>/…) must be used instead.
/// </summary>
// The Roslyn analyzer surface (DiagnosticAnalyzer base, ImmutableArray, AnalysisContext)
// is not CLS-compliant, and the assembly is [CLSCompliant(true)]; an analyzer must stay
// public to be discovered, so opt this type out of CLS compliance.
[CLSCompliant(false)]
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SyncQueryAnalyzer : DiagnosticAnalyzer
{
	private const string _ormAssembly = "Ecng.Data.ORM";

	internal static readonly DiagnosticDescriptor Rule = new(
		id: "ECNGORM001",
		title: "Synchronous LINQ terminal on a database query",
		messageFormat: "'{0}' blocks a thread-pool thread for the whole DB round-trip - use an async terminal such as ToArrayAsyncEx/FirstAsyncEx/CountAsyncEx/AnyAsyncEx instead",
		category: "Ecng.Data.ORM",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "Synchronous LINQ terminals on a Database-backed IQueryable dispatch to IQueryContext.ExecuteEnum/ExecuteResult, which call AsyncHelper.Run and block the calling thread for the entire query. Under parallel load this exhausts the thread pool.");

	// LINQ terminals (System.Linq.Enumerable/Queryable) that force enumeration/execution
	// of the query synchronously.
	private static readonly HashSet<string> _syncTerminals = new()
	{
		"ToList", "ToArray", "ToDictionary", "ToHashSet", "ToLookup",
		"First", "FirstOrDefault", "Single", "SingleOrDefault", "Last", "LastOrDefault",
		"Count", "LongCount", "Any", "All", "Contains",
		"Sum", "Min", "Max", "Average", "Aggregate",
		"ElementAt", "ElementAtOrDefault",
	};

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
	}

	private static void AnalyzeInvocation(OperationAnalysisContext ctx)
	{
		var op = (IInvocationOperation)ctx.Operation;
		var method = op.TargetMethod;

		var containing = method.ContainingType;

		if (containing is null ||
			(containing.Name != "Enumerable" && containing.Name != "Queryable") ||
			containing.ContainingNamespace?.ToDisplayString() != "System.Linq")
			return;

		if (!_syncTerminals.Contains(method.Name))
			return;

		// The source is the receiver — for an extension method it is the first argument.
		var source = op.Instance ?? (op.Arguments.Length > 0 ? op.Arguments[0].Value : null);

		if (source is null || !IsFromOrmQuery(source))
			return;

		ctx.ReportDiagnostic(Diagnostic.Create(Rule, op.Syntax.GetLocation(), method.Name));
	}

	/// <summary>
	/// Walks the source chain of a terminal looking for evidence that the query originates
	/// from Ecng.Data.ORM: either a value typed <c>DefaultQueryable&lt;T&gt;</c> or a call to
	/// a <c>ToQueryable()</c> method, both declared in the Ecng.Data.ORM assembly.
	/// </summary>
	private static bool IsFromOrmQuery(IOperation op)
	{
		while (op is not null)
		{
			if (IsOrmType(op.Type))
				return true;

			switch (op)
			{
				case IInvocationOperation inv:
					if (inv.TargetMethod.Name == "ToQueryable" && IsFromOrmAssembly(inv.TargetMethod.ContainingAssembly))
						return true;

					op = inv.Instance ?? (inv.Arguments.Length > 0 ? inv.Arguments[0].Value : null);
					break;

				case IConversionOperation conv:
					op = conv.Operand;
					break;

				case IArgumentOperation arg:
					op = arg.Value;
					break;

				case IParenthesizedOperation paren:
					op = paren.Operand;
					break;

				default:
					return false;
			}
		}

		return false;
	}

	private static bool IsOrmType(ITypeSymbol type)
		=> type is INamedTypeSymbol named &&
			named.Name == "DefaultQueryable" &&
			IsFromOrmAssembly(named.ContainingAssembly);

	private static bool IsFromOrmAssembly(IAssemblySymbol assembly)
		=> assembly?.Name == _ormAssembly;
}
