namespace Ecng.Data;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

/// <summary>
/// Reports the three ways of reading an Ecng.Data.ORM query synchronously:
/// <list type="bullet">
/// <item>ECNGORM001 - a synchronous LINQ terminal (<c>ToList</c>/<c>First</c>/<c>Any</c>/<c>Count</c>/…),
/// which routes through <c>IQueryProvider.Execute</c> → <c>AsyncHelper.Run</c> and blocks a thread-pool
/// thread for the entire database round-trip; under load that starves the pool and makes every request
/// time out (only a restart clears it). The async terminals
/// (<c>ToArrayAsyncEx</c>/<c>FirstAsyncEx</c>/<c>CountAsyncEx</c>/…) must be used instead.</item>
/// <item>ECNGORM002 - a <c>foreach</c> over the query, which calls the synchronous <c>GetEnumerator</c>
/// the ORM refuses outright with <c>NotSupportedException</c>.</item>
/// <item>ECNGORM003 - <c>ToAsyncEnumerable</c> over the query, which wraps that same synchronous
/// enumerator and so only defers the failure to the first <c>MoveNextAsync</c>.</item>
/// </list>
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

	internal static readonly DiagnosticDescriptor ForEachRule = new(
		id: "ECNGORM002",
		title: "Synchronous foreach over a database query",
		messageFormat: "a foreach walks a database query with its synchronous enumerator, which the ORM refuses at runtime - read the query with ToArrayAsyncEx first, or walk it with 'await foreach' over ToAsync",
		category: "Ecng.Data.ORM",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "A foreach over a Database-backed IQueryable calls the synchronous GetEnumerator, which DefaultQueryable refuses with NotSupportedException. The query has to be read with ToArrayAsyncEx or walked with await foreach over ToAsync.");

	internal static readonly DiagnosticDescriptor AsyncEnumerableRule = new(
		id: "ECNGORM003",
		title: "ToAsyncEnumerable over a database query",
		messageFormat: "'{0}' wraps the synchronous enumerator of a database query, so the failure is only deferred to the first MoveNextAsync - use ToAsync, which hands back the query's own async enumerator",
		category: "Ecng.Data.ORM",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		description: "System.Linq.AsyncEnumerable.ToAsyncEnumerable adapts a synchronous IEnumerable, so over a Database-backed IQueryable it still reaches the synchronous GetEnumerator the ORM refuses - it only makes the code look asynchronous. QueryableAsyncExtensions.ToAsync returns the query itself, which implements IAsyncEnumerable.");

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

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule, ForEachRule, AsyncEnumerableRule);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
		context.RegisterOperationAction(AnalyzeLoop, OperationKind.Loop);
	}

	private static void AnalyzeInvocation(OperationAnalysisContext ctx)
	{
		var op = (IInvocationOperation)ctx.Operation;
		var method = op.TargetMethod;

		DiagnosticDescriptor rule;

		if (IsSyncTerminal(method))
			rule = Rule;
		else if (IsToAsyncEnumerable(method))
			rule = AsyncEnumerableRule;
		else
			return;

		// The source is the receiver — for an extension method it is the first argument.
		var source = op.Instance ?? (op.Arguments.Length > 0 ? op.Arguments[0].Value : null);

		if (source is null || !IsFromOrmQuery(source))
			return;

		if (IsInsideExpressionTree(op))
			return;

		ctx.ReportDiagnostic(Diagnostic.Create(rule, op.Syntax.GetLocation(), method.Name));
	}

	private static void AnalyzeLoop(OperationAnalysisContext ctx)
	{
		if (ctx.Operation is not IForEachLoopOperation loop)
			return;

		// `await foreach` is the same operation with this flag set, and over the query it is the correct form:
		// DefaultQueryable is an IAsyncEnumerable. Only the synchronous enumerator is what throws.
		if (loop.IsAsynchronous)
			return;

		var collection = Unwrap(loop.Collection);

		// A terminal has already read the query into memory, and it is reported where it stands - the loop then
		// walks an array, so a second report here would only say the same thing twice.
		if (collection is IInvocationOperation inv && IsSyncTerminal(inv.TargetMethod))
			return;

		if (!IsFromOrmQuery(collection))
			return;

		ctx.ReportDiagnostic(Diagnostic.Create(ForEachRule, collection.Syntax.GetLocation()));
	}

	private static IOperation Unwrap(IOperation op)
	{
		while (true)
		{
			switch (op)
			{
				case IConversionOperation conv:
					op = conv.Operand;
					break;

				case IParenthesizedOperation paren:
					op = paren.Operand;
					break;

				default:
					return op;
			}
		}
	}

	private static bool IsSyncTerminal(IMethodSymbol method)
	{
		var containing = method?.ContainingType;

		return containing is not null &&
			(containing.Name == "Enumerable" || containing.Name == "Queryable") &&
			containing.ContainingNamespace?.ToDisplayString() == "System.Linq" &&
			_syncTerminals.Contains(method.Name);
	}

	private static bool IsToAsyncEnumerable(IMethodSymbol method)
	{
		var containing = method?.ContainingType;

		return method?.Name == "ToAsyncEnumerable" &&
			containing?.Name == "AsyncEnumerable" &&
			containing.ContainingNamespace?.ToDisplayString() == "System.Linq";
	}

	// A terminal written inside a lambda that becomes an expression tree is not executed where it stands - it
	// is translated into the query and runs on the server. Reporting it would be wrong, and a rule that cries
	// wolf gets suppressed and then ignored.
	private static bool IsInsideExpressionTree(IOperation op)
	{
		// The body of a lambda is its own operation tree, so walking operation parents never reaches the
		// conversion that turns it into an expression tree. The syntax does reach it.
		var model = op.SemanticModel;

		if (model is null)
			return false;

		for (var node = op.Syntax.Parent; node is not null; node = node.Parent)
		{
			if (model.GetTypeInfo(node).ConvertedType is INamedTypeSymbol named &&
				named.Name == "Expression" &&
				named.ContainingNamespace?.ToDisplayString() == "System.Linq.Expressions")
				return true;
		}

		return false;
	}

	/// <summary>
	/// Walks the source chain of a terminal looking for evidence that the query originates
	/// from Ecng.Data.ORM: either a value typed <c>DefaultQueryable&lt;T&gt;</c> or a call to
	/// a <c>ToQueryable()</c> method that the ORM declares.
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
					if (IsOrmToQueryable(inv.TargetMethod))
						return true;

					op = inv.Instance ?? (inv.Arguments.Length > 0 ? inv.Arguments[0].Value : null);
					break;

				// `a?.B()` puts the rest of the chain under the conditional access, so the source is found by
				// stepping into it rather than by giving up here.
				case IConditionalAccessOperation conditional:
					op = conditional.WhenNotNull;
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

	// A consumer declares its own list type and overrides ToQueryable, so the method named at the call site
	// belongs to that assembly, not to the ORM. Following what it overrides is what finds the ORM underneath -
	// matching only the declaring assembly leaves the rule silent across an entire codebase.
	private static bool IsOrmToQueryable(IMethodSymbol method)
	{
		if (method?.Name != "ToQueryable")
			return false;

		for (var current = method; current is not null; current = current.OverriddenMethod)
		{
			if (IsFromOrmAssembly(current.ContainingAssembly))
				return true;
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
