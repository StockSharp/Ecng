namespace Ecng.Analyzers;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

/// <summary>
/// Reports calls to the helpers that pump asynchronous work to completion on the calling
/// thread — <c>AsyncHelper.Run</c> and Nito's <c>AsyncContext.Run</c>. Each of them parks
/// the calling thread for the entire operation, so on a server they consume a thread-pool
/// thread per in-flight request; under load the pool is exhausted and every request times
/// out until the process is restarted. The fix is always to make the caller asynchronous
/// and await the work rather than bridge to it.
/// </summary>
// The Roslyn analyzer surface (DiagnosticAnalyzer base, ImmutableArray, AnalysisContext)
// is not CLS-compliant, and the assembly is [CLSCompliant(true)]; an analyzer must stay
// public to be discovered, so opt this type out of CLS compliance.
[CLSCompliant(false)]
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SyncOverAsyncAnalyzer : DiagnosticAnalyzer
{
	/// <summary>
	/// Identifier of the diagnostic this analyzer reports.
	/// </summary>
	public const string DiagnosticId = "ECNGASYNC001";

	internal static readonly DiagnosticDescriptor Rule = new(
		id: DiagnosticId,
		title: "Blocking sync-over-async bridge",
		messageFormat: "'{0}' blocks the calling thread until the asynchronous work completes - make the caller async and await the work instead",
		category: "Ecng.Async",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description:
			"AsyncHelper.Run and AsyncContext.Run start an asynchronous operation and then block until it finishes, " +
			"holding the calling thread for the whole round-trip. On a server that costs one thread-pool thread per " +
			"in-flight call, so under parallel load the pool is starved and unrelated requests begin to time out - a " +
			"state only a restart clears. Propagate async instead: make the caller return Task/ValueTask and await. " +
			"Calls inside a member or type already marked Obsolete are not reported, so a blocking wrapper can be " +
			"deprecated and kept during its removal period without suppressing anything. Lower the severity in " +
			".editorconfig for code that genuinely cannot be made asynchronous, such as a Main entry point or a " +
			"synchronous framework callback.");

	/// <summary>
	/// Bridges that block the caller, keyed by the fully qualified declaring type.
	/// Add an entry here to ban another one.
	/// </summary>
	private static readonly Dictionary<string, string[]> _blockingBridges = new()
	{
		["Ecng.Common.AsyncHelper"] = ["Run"],
		["Nito.AsyncEx.AsyncContext"] = ["Run"],
	};

	/// <inheritdoc />
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	/// <inheritdoc />
	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterOperationAction(Analyze, OperationKind.Invocation);
	}

	private static void Analyze(OperationAnalysisContext context)
	{
		var invocation = (IInvocationOperation)context.Operation;
		var method = invocation.TargetMethod;

		// Match on the declaring type's full name rather than on an assembly reference, so the
		// analyzer needs no dependency on the code it judges and works in any consumer solution.
		var declaringType = method.ContainingType?.ToDisplayString();

		if (declaringType is null || !_blockingBridges.TryGetValue(declaringType, out var methodNames))
			return;

		if (!methodNames.Contains(method.Name))
			return;

		// A blocking wrapper that is itself already deprecated is not a new problem: the
		// warning belongs at ITS call sites, which the Obsolete attribute already produces.
		// Flagging its body too would leave no way to keep the wrapper around during the
		// deprecation period except suppressing the diagnostic. This mirrors the compiler,
		// which does not report obsolete usage from inside an obsolete member either.
		if (IsInsideObsolete(context.ContainingSymbol))
			return;

		context.ReportDiagnostic(Diagnostic.Create(
			Rule,
			invocation.Syntax.GetLocation(),
			$"{declaringType}.{method.Name}"));
	}

	/// <summary>
	/// Walks out from <paramref name="symbol"/> through its containing members and types,
	/// looking for <see cref="ObsoleteAttribute"/>. The whole chain matters: the call may sit
	/// in an accessor of an obsolete property, or anywhere inside an obsolete type.
	/// </summary>
	private static bool IsInsideObsolete(ISymbol symbol)
	{
		for (var current = symbol; current is not null && current.Kind != SymbolKind.Namespace; current = current.ContainingSymbol)
		{
			foreach (var attribute in current.GetAttributes())
			{
				if (attribute.AttributeClass?.ToDisplayString() == "System.ObsoleteAttribute")
					return true;
			}
		}

		return false;
	}
}
