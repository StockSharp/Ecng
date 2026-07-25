#if NET10_0_OR_GREATER

namespace Ecng.Tests.Analyzers;

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

using Ecng.Analyzers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Verifies <see cref="SyncOverAsyncAnalyzer"/> (ECNGASYNC001) flags the blocking
/// sync-over-async bridges and stays silent on everything else.
/// </summary>
[TestClass]
public class SyncOverAsyncAnalyzerTests : BaseTestClass
{
	private const string _diagId = SyncOverAsyncAnalyzer.DiagnosticId;

	// The analyzer matches on fully qualified type names, so stubs standing in for the real
	// helpers are enough — the test needs no reference to Ecng.Common or Nito.AsyncEx.
	private const string _stubs = """
		namespace Ecng.Common
		{
			public static class AsyncHelper
			{
				public static T Run<T>(System.Func<System.Threading.Tasks.Task<T>> func) => default;
			}
		}

		namespace Nito.AsyncEx
		{
			public static class AsyncContext
			{
				public static T Run<T>(System.Func<System.Threading.Tasks.Task<T>> func) => default;
			}
		}

		namespace Other
		{
			public static class MyHelper
			{
				public static T Run<T>(System.Func<System.Threading.Tasks.Task<T>> func) => default;
			}
		}
		""";

	private async Task<Diagnostic[]> AnalyzeAsync(string code)
	{
		var refs = AppDomain.CurrentDomain
			.GetAssemblies()
			.Where(a => !a.IsDynamic && !a.Location.IsEmpty())
			.Select(a => (MetadataReference)MetadataReference.CreateFromFile(a.Location))
			.ToArray();

		var compilation = CSharpCompilation.Create(
			"SyncOverAsyncProbe",
			[CSharpSyntaxTree.ParseText(_stubs), CSharpSyntaxTree.ParseText(code)],
			refs,
			new(OutputKind.DynamicallyLinkedLibrary));

		var withAnalyzers = compilation.WithAnalyzers(
			ImmutableArray.Create<DiagnosticAnalyzer>(new SyncOverAsyncAnalyzer()));

		var diags = await withAnalyzers.GetAnalyzerDiagnosticsAsync(CancellationToken);
		return [.. diags];
	}

	[TestMethod]
	public async Task AsyncHelperRun_IsFlagged()
	{
		var diags = await AnalyzeAsync("""
			using System.Threading.Tasks;
			using Ecng.Common;
			static class C { static int M() => AsyncHelper.Run(() => Task.FromResult(1)); }
			""");

		var hits = diags.Where(d => d.Id == _diagId).ToArray();

		hits.Length.AssertEqual(1, $"expected one {_diagId}, got: {string.Join(",", diags.Select(d => d.Id))}");
		hits[0].GetMessage().Contains("Ecng.Common.AsyncHelper.Run").AssertTrue(
			$"the message must name the offending call, got: {hits[0].GetMessage()}");
	}

	[TestMethod]
	public async Task AsyncContextRun_IsFlagged()
	{
		var diags = await AnalyzeAsync("""
			using System.Threading.Tasks;
			using Nito.AsyncEx;
			static class C { static int M() => AsyncContext.Run(() => Task.FromResult(1)); }
			""");

		diags.Count(d => d.Id == _diagId).AssertEqual(1, $"expected one {_diagId} for AsyncContext.Run");
	}

	/// <summary>
	/// The user asked for these to stop the build, not merely to be noticed.
	/// </summary>
	[TestMethod]
	public async Task Diagnostic_IsAnError()
	{
		var diags = await AnalyzeAsync("""
			using System.Threading.Tasks;
			using Ecng.Common;
			static class C { static int M() => AsyncHelper.Run(() => Task.FromResult(1)); }
			""");

		var hit = diags.First(d => d.Id == _diagId);

		hit.Severity.AssertEqual(DiagnosticSeverity.Error, $"{_diagId} must be an error by default");
	}

	/// <summary>
	/// Matching is by declaring type, so an unrelated method that merely happens to be
	/// called Run must not be reported.
	/// </summary>
	[TestMethod]
	public async Task UnrelatedRunMethod_IsNotFlagged()
	{
		var diags = await AnalyzeAsync("""
			using System.Threading.Tasks;
			using Other;
			static class C { static int M() => MyHelper.Run(() => Task.FromResult(1)); }
			""");

		diags.Any(d => d.Id == _diagId).AssertFalse($"unexpected {_diagId} on an unrelated Run method");
	}

	/// <summary>
	/// A blocking wrapper that is already deprecated is not a new problem — the warning belongs
	/// at its call sites, which [Obsolete] already produces. Reporting its body too would leave
	/// no way to keep it during the removal period except suppressing the diagnostic.
	/// </summary>
	[TestMethod]
	public async Task CallInsideObsoleteMember_IsNotFlagged()
	{
		var diags = await AnalyzeAsync("""
			using System;
			using System.Threading.Tasks;
			using Ecng.Common;

			static class C
			{
				[Obsolete("Blocking sync-over-async wrapper. Use MAsync instead.")]
				public static int M() => AsyncHelper.Run(() => Task.FromResult(1));
			}
			""");

		diags.Any(d => d.Id == _diagId).AssertFalse(
			$"unexpected {_diagId} inside an Obsolete member");
	}

	[TestMethod]
	public async Task CallInsideObsoleteType_IsNotFlagged()
	{
		var diags = await AnalyzeAsync("""
			using System;
			using System.Threading.Tasks;
			using Ecng.Common;

			[Obsolete("Legacy blocking facade.")]
			static class C
			{
				public static int M() => AsyncHelper.Run(() => Task.FromResult(1));
			}
			""");

		diags.Any(d => d.Id == _diagId).AssertFalse(
			$"unexpected {_diagId} inside an Obsolete type");
	}

	/// <summary>
	/// The exemption must not leak: a plain member sitting next to a deprecated one is
	/// still reported.
	/// </summary>
	[TestMethod]
	public async Task CallOutsideObsoleteMember_IsStillFlagged()
	{
		var diags = await AnalyzeAsync("""
			using System;
			using System.Threading.Tasks;
			using Ecng.Common;

			static class C
			{
				[Obsolete("Blocking sync-over-async wrapper. Use MAsync instead.")]
				public static int Old() => AsyncHelper.Run(() => Task.FromResult(1));

				public static int New() => AsyncHelper.Run(() => Task.FromResult(2));
			}
			""");

		diags.Count(d => d.Id == _diagId).AssertEqual(
			1, $"only the non-deprecated call must be reported, got: {diags.Length}");
	}

	[TestMethod]
	public async Task AwaitedCall_IsNotFlagged()
	{
		var diags = await AnalyzeAsync("""
			using System.Threading.Tasks;
			static class C { static async Task<int> M() => await Task.FromResult(1); }
			""");

		diags.Any(d => d.Id == _diagId).AssertFalse($"unexpected {_diagId} on properly awaited code");
	}
}

#endif
