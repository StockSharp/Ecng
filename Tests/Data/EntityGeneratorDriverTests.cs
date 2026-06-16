#if NET10_0_OR_GREATER

namespace Ecng.Tests.Data;

using System.Collections.Immutable;

using Ecng.Data;          // EntityGenerator
using Ecng.Serialization; // forces the Ecng.Data.ORM assembly (IdentityAttribute, ...) to be referenced

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

/// <summary>
/// Tests that drive <see cref="EntityGenerator"/> in isolation against in-memory source.
/// These cover defects that cannot be reproduced from inside the Tests project itself,
/// because the faulty input would break the Tests build (duplicate generator hint name,
/// invalid generated C#) or crash the compiler host (unbounded recursion). Running the
/// generator against a throw-away in-memory compilation keeps the bad input contained.
/// </summary>
[TestClass]
public class EntityGeneratorDriverTests : BaseTestClass
{
	private const string _failureId = "ECNGGEN001";

	// A minimal IDbPersistable base with an [Identity] so derived partial classes are
	// recognised as entities by the generator.
	private const string _preamble = @"
using System.Threading;
using System.Threading.Tasks;
using Ecng.Serialization;

public abstract partial class DriverBaseEntity : IDbPersistable
{
	[Identity]
	public long Id { get; set; }

	object IDbPersistable.GetIdentity() => Id;
	void IDbPersistable.SetIdentity(object id) => Id = (long)id;
	public virtual void Save(SettingsStorage storage) { }
	public virtual ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken ct) => default;
	public virtual void InitLists(IStorage db) { }
}
";

	private static (ImmutableArray<Diagnostic> diagnostics, GeneratorDriverRunResult result) Run(string source)
	{
		var tree = CSharpSyntaxTree.ParseText(_preamble + source);

		var refs = AppDomain.CurrentDomain.GetAssemblies()
			.Where(a => !a.IsDynamic && !a.Location.IsEmpty())
			.Select(a => (MetadataReference)MetadataReference.CreateFromFile(a.Location))
			.ToArray();

		var compilation = CSharpCompilation.Create(
			"GenDriverInMemory",
			[tree],
			refs,
			new(OutputKind.DynamicallyLinkedLibrary));

		var driver = CSharpGeneratorDriver.Create(new EntityGenerator());

		var updated = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

		return (diagnostics, updated.GetRunResult());
	}

	private static bool Failed(ImmutableArray<Diagnostic> diagnostics)
		=> diagnostics.Any(d => d.Id == _failureId);

	private static string[] HintNames(GeneratorDriverRunResult result)
		=> [.. result.Results.SelectMany(r => r.GeneratedSources).Select(s => s.HintName)];

	private static string AllGeneratedText(GeneratorDriverRunResult result)
		=> result.Results.SelectMany(r => r.GeneratedSources).Select(s => s.SourceText.ToString()).JoinN();

	/// <summary>
	/// Finding #1: EmitSchemaInitializer runs once per namespace but always uses the constant
	/// hint name "SchemaInitializer.cs"; a second namespace makes AddSource throw, which the
	/// generator turns into an ECNGGEN001 failure that aborts all output. Two entities in two
	/// namespaces must generate cleanly with distinct schema-initializer hint names.
	/// (Was: Data.Entities.Generator\EntityGenerator.cs:795 — constant "SchemaInitializer.cs".)
	/// </summary>
	[TestMethod]
	public void Driver_TwoNamespaces_NoDuplicateSchemaInitializerHint()
	{
		var (diagnostics, result) = Run(@"
namespace DriverNs1 { public partial class DriverAlpha : DriverBaseEntity { public string A { get; set; } } }
namespace DriverNs2 { public partial class DriverBeta : DriverBaseEntity { public string B { get; set; } } }
");

		Failed(diagnostics).AssertFalse(
			$"Generator failed (ECNGGEN001): {diagnostics.Where(d => d.Id == _failureId).Select(d => d.GetMessage()).JoinN()}");

		HintNames(result).Count(h => h.Contains("SchemaInitializer")).AssertEqual(2,
			"Each namespace must get its own schema-initializer source with a unique hint name");
	}

	/// <summary>
	/// Finding #9: EmitSource derives the hint name from the simple type name only, so two
	/// equally named entity classes in different namespaces collide on
	/// "{Name}_DbPersistable.cs" and AddSource throws (ECNGGEN001), aborting generation. Both
	/// must generate with distinct hint names.
	/// (Was: Data.Entities.Generator\EntityGenerator.cs:772 — entityType.Name only.)
	/// </summary>
	[TestMethod]
	public void Driver_SameClassNameDifferentNamespaces_NoHintCollision()
	{
		var (diagnostics, result) = Run(@"
namespace DriverNsA { public partial class DriverDup : DriverBaseEntity { public string A { get; set; } } }
namespace DriverNsB { public partial class DriverDup : DriverBaseEntity { public string B { get; set; } } }
");

		Failed(diagnostics).AssertFalse(
			$"Generator failed (ECNGGEN001): {diagnostics.Where(d => d.Id == _failureId).Select(d => d.GetMessage()).JoinN()}");

		HintNames(result).Count(h => h.Contains("DbPersistable")).AssertEqual(2,
			"Same-named entities in different namespaces must produce two distinct DbPersistable sources");
	}

	/// <summary>
	/// Finding #8: property filters never exclude IPropertySymbol.IsIndexer, whose Roslyn name is
	/// "this[]"; a flattenable inner type carrying a writable indexer makes the generator emit
	/// member access containing "this[]" — invalid C# that breaks the consumer build. Generated
	/// source must never contain "this[".
	/// (Was: Data.Entities.Generator\EntityGenerator.cs:850 — no !p.IsIndexer filter.)
	/// </summary>
	[TestMethod]
	public void Driver_InnerTypeWithIndexer_DoesNotEmitInvalidThisAccess()
	{
		var (diagnostics, result) = Run(@"
namespace DriverIdx
{
	public class DriverHasIndexer
	{
		public string Name { get; set; }
		public string this[int i] { get => null; set { } }
	}

	public partial class DriverIndexerEntity : DriverBaseEntity
	{
		public DriverHasIndexer Data { get; set; }
	}
}
");

		Failed(diagnostics).AssertFalse(
			$"Generator failed (ECNGGEN001): {diagnostics.Where(d => d.Id == _failureId).Select(d => d.GetMessage()).JoinN()}");

		AllGeneratedText(result).Contains("this[").AssertFalse(
			"Generated source must not reference an indexer member ('this[]') — it is invalid C#");
	}

	/// <summary>
	/// Finding #2: CanFlattenInnerType recurses into nested complex types without a visited set,
	/// so a self-referencing property type recurses forever and the StackOverflowException kills
	/// the compiler/analyzer host (it is not catchable by the generator's try/catch).
	/// Ignored because, until the visited-set guard is added, simply running this would crash the
	/// test host; it documents the expected behaviour (generate without crashing).
	/// (Was: Data.Entities.Generator\EntityGenerator.cs:1030 — no visiting/visited guard.)
	/// </summary>
	[TestMethod]
	public void Driver_SelfReferencingInnerType_DoesNotStackOverflow()
	{
		var (diagnostics, _) = Run(@"
namespace DriverRec
{
	public class DriverNode
	{
		public string Name { get; set; }
		public DriverNode Self { get; set; }
	}

	public partial class DriverRecursiveEntity : DriverBaseEntity
	{
		public DriverNode Data { get; set; }
	}
}
");

		Failed(diagnostics).AssertFalse("Generator must handle self-referencing inner types without crashing");
	}
}

#endif
