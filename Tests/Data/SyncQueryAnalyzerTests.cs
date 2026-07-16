#if NET10_0_OR_GREATER

namespace Ecng.Tests.Data;

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

using Ecng.Data;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Verifies <see cref="SyncQueryAnalyzer"/> (ECNGORM001) flags synchronous LINQ terminals on
/// an Ecng.Data.ORM query and stays silent on ordinary LINQ.
/// </summary>
[TestClass]
public class SyncQueryAnalyzerTests : BaseTestClass
{
	private const string _diagId = "ECNGORM001";

	private async Task<Diagnostic[]> AnalyzeAsync(string code)
	{
		var refs = AppDomain.CurrentDomain
			.GetAssemblies()
			.Where(a => !a.IsDynamic && !a.Location.IsEmpty())
			.Select(a => (MetadataReference)MetadataReference.CreateFromFile(a.Location))
			.ToArray();

		var compilation = CSharpCompilation.Create(
			"SyncQueryAnalyzerProbe",
			[CSharpSyntaxTree.ParseText(code)],
			refs,
			new(OutputKind.DynamicallyLinkedLibrary));

		var withAnalyzers = compilation.WithAnalyzers(
			ImmutableArray.Create<DiagnosticAnalyzer>(new SyncQueryAnalyzer()));

		var diags = await withAnalyzers.GetAnalyzerDiagnosticsAsync(CancellationToken);
		return [.. diags];
	}

	[TestMethod]
	public async Task SyncTerminalOnOrmQuery_IsFlagged()
	{
		var diags = await AnalyzeAsync("""
			using System.Linq;
			using Ecng.Serialization;
			static class C { static void M(DefaultQueryable<int> q) { _ = q.First(); _ = q.ToList(); _ = q.Any(); } }
			""");

		var orm = diags.Count(d => d.Id == _diagId);
		(orm >= 3).AssertTrue($"expected >= 3 {_diagId}, got {orm}: {string.Join(",", diags.Select(d => d.Id))}");
	}

	[TestMethod]
	public async Task SyncTerminalOnPlainLinq_NotFlagged()
	{
		var diags = await AnalyzeAsync("""
			using System.Linq;
			using System.Collections.Generic;
			static class C { static void M(List<int> q) { _ = q.First(); _ = q.ToList(); _ = q.Any(); } }
			""");

		diags.Any(d => d.Id == _diagId).AssertFalse($"unexpected {_diagId} on plain LINQ");
	}

	[TestMethod]
	public async Task AsyncTerminal_NotFlagged()
	{
		// AnyAsyncEx / ToArrayAsyncEx are the recommended terminals — must not be flagged.
		var diags = await AnalyzeAsync("""
			using System.Threading;
			using Ecng.Serialization;
			static class C { static void M(DefaultQueryable<int> q) { _ = q.AnyAsyncEx(CancellationToken.None); } }
			""");

		diags.Any(d => d.Id == _diagId).AssertFalse($"unexpected {_diagId} on async terminal");
	}
}

#endif
