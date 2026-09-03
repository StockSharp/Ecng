#if NET10_0_OR_GREATER

namespace Ecng.Tests.Analyzers;

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

using Ecng.Data;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Verifies <see cref="SyncQueryAnalyzer"/> (ECNGORM001) flags a synchronous LINQ terminal on a query that
/// comes from the ORM, and stays silent on everything else.
///
/// Two shapes decide whether it is any use in practice. Consumers derive their own list type and override
/// <c>ToQueryable</c>, so the method the call site names belongs to their assembly, not to the ORM - looking
/// only at the declaring assembly makes the rule silent across a whole codebase. And a query reached through
/// <c>?.</c> puts a conditional access between the terminal and its source, which the walk has to step over
/// rather than give up on.
/// </summary>
[TestClass]
public class SyncQueryAnalyzerTests : BaseTestClass
{
	private const string _diagId = "ECNGORM001";

	// Stands in for the ORM. What matters is the assembly name, which is what the analyzer recognises.
	private const string _ormSource = """
		using System.Linq;

		namespace Ecng.Serialization
		{
			public class DefaultQueryable<T>
			{
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

	private static MetadataReference BuildOrmReference()
	{
		var refs = AppDomain.CurrentDomain
			.GetAssemblies()
			.Where(a => !a.IsDynamic && !a.Location.IsEmpty())
			.Select(a => (MetadataReference)MetadataReference.CreateFromFile(a.Location))
			.ToArray();

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
		var refs = AppDomain.CurrentDomain
			.GetAssemblies()
			.Where(a => !a.IsDynamic && !a.Location.IsEmpty())
			.Select(a => (MetadataReference)MetadataReference.CreateFromFile(a.Location))
			.Concat([BuildOrmReference()])
			.ToArray();

		var compilation = CSharpCompilation.Create(
			"SyncQueryProbe",
			[CSharpSyntaxTree.ParseText(code)],
			refs,
			new(OutputKind.DynamicallyLinkedLibrary));

		var withAnalyzers = compilation.WithAnalyzers(
			ImmutableArray.Create<DiagnosticAnalyzer>(new SyncQueryAnalyzer()));

		var diags = await withAnalyzers.GetAnalyzerDiagnosticsAsync(CancellationToken);

		return [.. diags.Where(d => d.Id == _diagId)];
	}

	private const string _consumer = """
		using System.Linq;

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
					var query = a.ToQueryable().Where(i => !b.ToQueryable().Any(x => x.Deleted));
					return query.ToArray();
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
}

#endif
