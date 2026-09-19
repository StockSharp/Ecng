#if NET10_0_OR_GREATER

namespace Ecng.Tests.Analyzers;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

using Ecng.Data;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Verifies <see cref="SyncQueryAnalyzer"/> flags the three synchronous reads of a query that comes from the
/// ORM - a LINQ terminal (ECNGORM001), a <c>foreach</c> (ECNGORM002) and <c>ToAsyncEnumerable</c>
/// (ECNGORM003) - and stays silent on everything else.
///
/// Two shapes decide whether it is any use in practice. Consumers derive their own list type and override
/// <c>ToQueryable</c>, so the method the call site names belongs to their assembly, not to the ORM - looking
/// only at the declaring assembly makes the rule silent across a whole codebase. And a query reached through
/// <c>?.</c> puts a conditional access between the terminal and its source, which the walk has to step over
/// rather than give up on.
///
/// A third shape decides it as much: the query is routinely put in a local before it is read, and a rule
/// that only sees the direct form misses most real code. Following the local is only safe while the
/// declaration is what decides the value, so the probes pin both halves of that.
///
/// The silences matter as much: <c>await foreach</c> over <c>ToAsync</c> reaches the analyzer as the same
/// loop operation as a plain <c>foreach</c>, and a loop over an already-read array sits on top of a source
/// chain that still leads back to the ORM.
/// </summary>
[TestClass]
public class SyncQueryAnalyzerTests : BaseTestClass
{
	private const string _diagId = "ECNGORM001";
	private const string _foreachId = "ECNGORM002";
	private const string _toAsyncEnumerableId = "ECNGORM003";

	// Stands in for the ORM. What matters is the assembly name, which is what the analyzer recognises.
	private const string _ormSource = """
		using System.Collections.Generic;
		using System.Linq;
		using System.Threading;
		using System.Threading.Tasks;

		namespace Ecng.Serialization
		{
			public class DefaultQueryable<T>
			{
			}

			public static class QueryableAsyncExtensions
			{
				public static IAsyncEnumerable<T> ToAsync<T>(this IQueryable<T> q) => null;
				public static ValueTask<T[]> ToArrayAsyncEx<T>(this IQueryable<T> q, CancellationToken token) => default;
			}
		}

		namespace Ecng.Data
		{
			public class RelationManyList<T>
			{
				public virtual IQueryable<T> ToQueryable() => null;
			}
		}
		""";

	// Building the reference set out of whatever happens to be loaded makes the probe depend on the order the
	// suite runs in: a missing System.Linq.Queryable leaves the probe uncompilable, the analyzer then has
	// nothing to look at, and the test reads as "rule broken" when it is the harness that is.
	private static MetadataReference[] BaseReferences()
		=> [.. new[]
			{
				typeof(object),
				typeof(IEnumerable<>),
				typeof(IAsyncEnumerable<>),
				typeof(Enumerable),
				typeof(IQueryable),
				typeof(Queryable),
				typeof(AsyncEnumerable),
				typeof(CancellationToken),
				typeof(ValueTask<>),
				typeof(TaskAsyncEnumerableExtensions),
				typeof(System.Linq.Expressions.Expression),
			}
			.Select(t => t.Assembly)
			.Concat(AppDomain.CurrentDomain.GetAssemblies())
			// The real ORM is loaded in this process, and the stub below carries its name: importing both is
			// what made the probe fail, silently, whenever the suite happened to have loaded it first.
			.Where(a => !a.IsDynamic && !a.Location.IsEmpty() && a.GetName().Name != "Ecng.Data.ORM")
			.Select(a => a.Location)
			.Distinct()
			.Select(l => (MetadataReference)MetadataReference.CreateFromFile(l))];

	private static void EnsureCompiles(CSharpCompilation compilation)
	{
		var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();

		if (errors.Length > 0)
			throw new InvalidOperationException("the probe does not compile: " + errors.Select(e => e.ToString()).JoinN());
	}

	private static MetadataReference BuildOrmReference()
	{
		var refs = BaseReferences();

		var orm = CSharpCompilation.Create(
			"Ecng.Data.ORM",
			[CSharpSyntaxTree.ParseText(_ormSource)],
			refs,
			new(OutputKind.DynamicallyLinkedLibrary));

		var stream = new MemoryStream();
		var emit = orm.Emit(stream);

		if (!emit.Success)
			throw new InvalidOperationException(emit.Diagnostics.Select(d => d.ToString()).JoinN());

		stream.Position = 0;
		return MetadataReference.CreateFromStream(stream);
	}

	private async Task<Diagnostic[]> AnalyzeAsync(string code)
	{
		var refs = BaseReferences().Concat([BuildOrmReference()]).ToArray();

		var compilation = CSharpCompilation.Create(
			"SyncQueryProbe",
			[CSharpSyntaxTree.ParseText(code)],
			refs,
			new(OutputKind.DynamicallyLinkedLibrary));

		EnsureCompiles(compilation);

		var withAnalyzers = compilation.WithAnalyzers(
			ImmutableArray.Create<DiagnosticAnalyzer>(new SyncQueryAnalyzer()));

		var diags = await withAnalyzers.GetAnalyzerDiagnosticsAsync(CancellationToken);

		// Every rule of this analyzer is kept, not just the one a test is about: a probe that expects
		// silence has to fail when any of the three starts firing on it.
		return [.. diags.Where(d => d.Id.StartsWith("ECNGORM", StringComparison.Ordinal))];
	}

	private const string _consumer = """
		using System.Collections.Generic;
		using System.Linq;
		using System.Threading;
		using System.Threading.Tasks;

		using Ecng.Serialization;

		public class Item
		{
			public bool Deleted { get; set; }
		}

		// What every consumer does: derive and override.
		public class ItemList : Ecng.Data.RelationManyList<Item>
		{
			public override IQueryable<Item> ToQueryable() => null;
		}

		public class Holder
		{
			public ItemList List { get; set; }
		}
		""";

	private Task<Diagnostic[]> ProbeAsync(string body)
		=> AnalyzeAsync(_consumer + "\n\npublic class Probe\n{\n" + body + "\n}\n");

	[TestMethod]
	public async Task ATerminalOnAnOverriddenToQueryableIsFlagged()
	{
		var diags = await ProbeAsync("	public object M(ItemList list) => list.ToQueryable().ToArray();");

		AreEqual(1, diags.Length, "a sync terminal on a list that overrides ToQueryable was not flagged");
	}

	[TestMethod]
	public async Task ATerminalReachedThroughConditionalAccessIsFlagged()
	{
		var diags = await ProbeAsync("	public object M(Holder h) => h?.List?.ToQueryable().ToArray();");

		AreEqual(1, diags.Length, "a sync terminal reached through ?. was not flagged");
	}

	[TestMethod]
	public async Task EveryOtherSyncTerminalIsFlaggedToo()
	{
		var diags = await ProbeAsync("""
				public object A(ItemList list) => list.ToQueryable().First();
				public object B(ItemList list) => list.ToQueryable().Count();
				public object C(ItemList list) => list.ToQueryable().Any();
			""");

		AreEqual(3, diags.Length);
	}

	/// <summary>A composed query is still the ORM's: the walk has to see through the operators.</summary>
	[TestMethod]
	public async Task AComposedQueryIsFlagged()
	{
		var diags = await ProbeAsync("	public object M(ItemList list) => list.ToQueryable().Where(i => !i.Deleted).ToArray();");

		AreEqual(1, diags.Length);
	}

	/// <summary>Inside a Where the terminal becomes part of the query and runs on the server.</summary>
	[TestMethod]
	public async Task ATerminalInsideAnExpressionTreeIsNotFlagged()
	{
		var diags = await ProbeAsync("""
				public object M(ItemList a, ItemList b)
				{
					return a.ToQueryable().Where(i => !b.ToQueryable().Any(x => x.Deleted)).ToArray();
				}
			""");

		AreEqual(1, diags.Length, $"got {diags.Length}: {diags.Select(d => d.GetMessage()).JoinComma()}");
	}

	[TestMethod]
	public async Task AnArrayIsNotFlagged()
	{
		var diags = await ProbeAsync("	public object M(Item[] items) => items.ToArray();");

		AreEqual(0, diags.Length, "a plain array terminal was flagged");
	}

	[TestMethod]
	public async Task AnAsyncTerminalIsNotFlagged()
	{
		var diags = await ProbeAsync("	public object M(ItemList list) => list.ToQueryable();");

		AreEqual(0, diags.Length, "reading the query without a terminal was flagged");
	}

	/// <summary>A plain foreach calls the synchronous GetEnumerator, which the ORM refuses at runtime.</summary>
	[TestMethod]
	public async Task AForeachOverAnOrmQueryIsFlagged()
	{
		var diags = await ProbeAsync("""
				public void M(ItemList list)
				{
					foreach (var i in list.ToQueryable().Where(x => !x.Deleted))
					{
					}
				}
			""");

		AreEqual(1, diags.Length, $"got {diags.Length}: {diags.Select(d => d.GetMessage()).JoinComma()}");
		AreEqual(_foreachId, diags[0].Id);
	}

	/// <summary>An await foreach over ToAsync is the recommended form and must stay silent.</summary>
	[TestMethod]
	public async Task AnAwaitForeachOverToAsyncIsNotFlagged()
	{
		var diags = await ProbeAsync("""
				public async Task M(ItemList list)
				{
					await foreach (var i in list.ToQueryable().ToAsync())
					{
					}
				}
			""");

		AreEqual(0, diags.Length, $"got {diags.Length}: {diags.Select(d => d.GetMessage()).JoinComma()}");
	}

	[TestMethod]
	public async Task AnAwaitForeachOverToAsyncWithCancellationIsNotFlagged()
	{
		var diags = await ProbeAsync("""
				public async Task M(ItemList list, CancellationToken token)
				{
					await foreach (var i in list.ToQueryable().ToAsync().WithCancellation(token))
					{
					}
				}
			""");

		AreEqual(0, diags.Length, $"got {diags.Length}: {diags.Select(d => d.GetMessage()).JoinComma()}");
	}

	/// <summary>The other recommended form: the query is already an array by the time the loop runs.</summary>
	[TestMethod]
	public async Task AForeachOverAMaterializedQueryIsNotFlagged()
	{
		var diags = await ProbeAsync("""
				public async Task M(ItemList list, CancellationToken token)
				{
					foreach (var i in await list.ToQueryable().ToArrayAsyncEx(token))
					{
					}
				}
			""");

		AreEqual(0, diags.Length, $"got {diags.Length}: {diags.Select(d => d.GetMessage()).JoinComma()}");
	}

	/// <summary>The sync terminal is the problem here; the loop walks an array and must not add a second report.</summary>
	[TestMethod]
	public async Task AForeachOverASyncTerminalIsReportedOnceOnTheTerminal()
	{
		var diags = await ProbeAsync("""
				public void M(ItemList list)
				{
					foreach (var i in list.ToQueryable().ToArray())
					{
					}
				}
			""");

		AreEqual(1, diags.Length, $"got {diags.Length}: {diags.Select(d => d.GetMessage()).JoinComma()}");
		AreEqual(_diagId, diags[0].Id);
	}

	[TestMethod]
	public async Task AForeachOverAPlainListIsNotFlagged()
	{
		var diags = await ProbeAsync("""
				public void M(List<Item> items)
				{
					foreach (var i in items)
					{
					}
				}
			""");

		AreEqual(0, diags.Length, "a foreach over an ordinary list was flagged");
	}

	/// <summary>ToAsyncEnumerable wraps the synchronous enumerator, so it only defers the same failure.</summary>
	[TestMethod]
	public async Task ToAsyncEnumerableOverAnOrmQueryIsFlagged()
	{
		var diags = await ProbeAsync("	public object M(ItemList list) => list.ToQueryable().Where(x => !x.Deleted).ToAsyncEnumerable();");

		AreEqual(1, diags.Length, $"got {diags.Length}: {diags.Select(d => d.GetMessage()).JoinComma()}");
		AreEqual(_toAsyncEnumerableId, diags[0].Id);
	}

	[TestMethod]
	public async Task ToAsyncEnumerableOverAnArrayIsNotFlagged()
	{
		var diags = await ProbeAsync("	public object M(Item[] items) => items.ToAsyncEnumerable();");

		AreEqual(0, diags.Length, "ToAsyncEnumerable over an ordinary array was flagged");
	}

	/// <summary>Putting the query in a local first is the usual shape, and all three rules have to see through it.</summary>
	[TestMethod]
	public async Task ATerminalOnALocalHoldingAnOrmQueryIsFlagged()
	{
		var diags = await ProbeAsync("""
				public object M(ItemList list)
				{
					var q = list.ToQueryable().Where(x => !x.Deleted);
					return q.ToArray();
				}
			""");

		AreEqual(1, diags.Length, $"got {diags.Length}: {diags.Select(d => d.GetMessage()).JoinComma()}");
		AreEqual(_diagId, diags[0].Id);
	}

	[TestMethod]
	public async Task AForeachOverALocalHoldingAnOrmQueryIsFlagged()
	{
		var diags = await ProbeAsync("""
				public void M(ItemList list)
				{
					var q = list.ToQueryable().Where(x => !x.Deleted);

					foreach (var i in q)
					{
					}
				}
			""");

		AreEqual(1, diags.Length, $"got {diags.Length}: {diags.Select(d => d.GetMessage()).JoinComma()}");
		AreEqual(_foreachId, diags[0].Id);
	}

	[TestMethod]
	public async Task ToAsyncEnumerableOverALocalHoldingAnOrmQueryIsFlagged()
	{
		var diags = await ProbeAsync("""
				public object M(ItemList list)
				{
					var q = list.ToQueryable().Where(x => !x.Deleted);
					return q.ToAsyncEnumerable();
				}
			""");

		AreEqual(1, diags.Length, $"got {diags.Length}: {diags.Select(d => d.GetMessage()).JoinComma()}");
		AreEqual(_toAsyncEnumerableId, diags[0].Id);
	}

	/// <summary>A local still has to be read for what it holds, not assumed to hold a query.</summary>
	[TestMethod]
	public async Task AForeachOverALocalHoldingAPlainListIsNotFlagged()
	{
		var diags = await ProbeAsync("""
				public void M(List<Item> source)
				{
					var items = source;

					foreach (var i in items)
					{
					}
				}
			""");

		AreEqual(0, diags.Length, "a foreach over a local holding an ordinary list was flagged");
	}

	/// <summary>Two assignments mean the value at the terminal is not decided by the declaration.</summary>
	[TestMethod]
	public async Task ALocalAssignedInTwoBranchesIsNotFlagged()
	{
		var diags = await ProbeAsync("""
				public object M(ItemList list, Item[] items, bool flag)
				{
					IQueryable<Item> q;

					if (flag)
						q = list.ToQueryable();
					else
						q = items.AsQueryable();

					return q.ToArray();
				}
			""");

		AreEqual(0, diags.Length, $"got {diags.Length}: {diags.Select(d => d.GetMessage()).JoinComma()}");
	}

	/// <summary>The declaration says ORM, a later assignment says otherwise - so the declaration does not settle it.</summary>
	[TestMethod]
	public async Task ALocalReassignedAfterItsDeclarationIsNotFlagged()
	{
		var diags = await ProbeAsync("""
				public object M(ItemList list, Item[] items, bool flag)
				{
					var q = list.ToQueryable();

					if (flag)
						q = items.AsQueryable();

					return q.ToArray();
				}
			""");

		AreEqual(0, diags.Length, $"got {diags.Length}: {diags.Select(d => d.GetMessage()).JoinComma()}");
	}

	/// <summary>Where a parameter came from is decided by the caller, which is outside this method.</summary>
	[TestMethod]
	public async Task AQueryableParameterIsNotFlagged()
	{
		var diags = await ProbeAsync("	public object M(IQueryable<Item> q) => q.ToArray();");

		AreEqual(0, diags.Length, "a terminal on an IQueryable parameter was flagged");
	}
}

#endif
