namespace Ecng.Tests.Compilation;

using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Loader;

using Ecng.Compilation;
using Ecng.Compilation.FSharp;
using Ecng.Compilation.Python;
using Ecng.Compilation.Roslyn;
using Ecng.IO;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[TestClass]
public class CompilerTests : BaseTestClass
{
	private static readonly string _coreLibPath = typeof(object).Assembly.Location;
	private static readonly IFileSystem _fs = LocalFileSystem.Instance;

#pragma warning disable RS1001, RS2008
	private sealed class HiddenDiagnosticAnalyzer : DiagnosticAnalyzer
	{
		public const string DiagnosticId = "ECNGTEST001";

		private static readonly DiagnosticDescriptor _descriptor = new(
			DiagnosticId,
			"Hidden diagnostic",
			"Hidden diagnostic",
			"Tests",
			DiagnosticSeverity.Hidden,
			true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(_descriptor);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxTreeAction(static ctx =>
			{
				var root = ctx.Tree.GetRoot(ctx.CancellationToken);
				ctx.ReportDiagnostic(Diagnostic.Create(_descriptor, root.GetLocation()));
			});
		}
	}
#pragma warning restore RS1001, RS2008

	[TestMethod]
	public async Task Compile()
	{
		ICompiler compiler = new CSharpCompiler();
		var res = await compiler.Compile("test", "class Class1 {}",
		[
			_coreLibPath,
		], _fs, CancellationToken);
		if (res.HasErrors())
		{
			var errors = res.Errors.Select(e => $"{e.Type}: {e.Message}").ToArray();
			Fail($"Compilation errors:\n{errors.JoinNL()}");
		}
		res.GetAssembly(compiler.CreateContext()).AssertNotNull();
	}

	[TestMethod]
	public async Task CompileError()
	{
		ICompiler compiler = new CSharpCompiler();
		var res = await compiler.Compile("test", "class Class1 {",
		[
			_coreLibPath,
		], _fs, CancellationToken);
		res.GetAssembly(compiler.CreateContext()).AssertNull();
		res.HasErrors().AssertTrue();
	}

	[TestMethod]
	public Task CompileCancel()
	{
		var cts = new CancellationTokenSource();
		cts.Cancel();

		ICompiler compiler = new CSharpCompiler();
		return ThrowsExactlyAsync<OperationCanceledException>(()
			=> compiler.Compile("test", "class Class1 {",
			[
				_coreLibPath,
			], _fs, cts.Token));
	}

	[TestMethod]
	public void AssemblyLoadContextWrapper_Dispose_DefaultContextDoesNotThrow()
	{
		using var context = AssemblyLoadContext.Default.ToContext();
	}

	[TestMethod]
	public async Task Analyse_HiddenDiagnostic_ReturnsInfo()
	{
		ICompiler compiler = new CSharpCompiler();

		var errors = await compiler.Analyse(
			new HiddenDiagnosticAnalyzer(),
			[],
			"test",
			["public class Class1 { }"],
			[_coreLibPath.ToRef(_fs)],
			CancellationToken);

		errors.Any(e => e.Id == HiddenDiagnosticAnalyzer.DiagnosticId && e.Type == CompilationErrorTypes.Info).AssertTrue();
	}

	[TestMethod]
	public async Task CSharpCompileWithWarnings()
	{
		ICompiler compiler = new CSharpCompiler();
		// Unused variable 'x' should produce a warning
		var code = "class Class1 { void M() { int x = 1; } }";
		var res = await compiler.Compile("test", [code], [_coreLibPath], _fs, CancellationToken);
		if (res.HasErrors())
		{
			var errors = res.Errors.Select(e => $"{e.Type}: {e.Message}").ToArray();
			Fail($"Compilation errors:\n{errors.JoinNL()}");
		}
		res.GetAssembly(compiler.CreateContext()).AssertNotNull();
		res.Errors.Any(e => e.Type == CompilationErrorTypes.Warning).AssertTrue();
	}

	[TestMethod]
	public async Task CSharpCompileMultipleErrors()
	{
		ICompiler compiler = new CSharpCompiler();
		// Two errors: missing semicolon, and undefined variable
		var code = "class Class1 { void M() { int x = ; y = 2; } }";
		var res = await compiler.Compile("test", [code], [_coreLibPath], _fs, CancellationToken);
		res.GetAssembly(compiler.CreateContext()).AssertNull();
		res.HasErrors().AssertTrue();
		res.Errors.Count().AssertGreater(1);
	}

	[TestMethod]
	public async Task CSharpCompilerSimpleSuccess()
	{
		ICompiler compiler = new CSharpCompiler();
		var code = "public class Foo { public int Bar() => 42; }";
		var res = await compiler.Compile("test", [code], [_coreLibPath], _fs, CancellationToken);
		if (res.HasErrors())
		{
			var errors = res.Errors.Select(e => $"{e.Type}: {e.Message}").ToArray();
			Fail($"Compilation errors:\n{errors.JoinNL()}");
		}
		res.GetAssembly(compiler.CreateContext()).AssertNotNull();
	}

	[TestMethod]
	public async Task CSharpCompilerWarning()
	{
		ICompiler compiler = new CSharpCompiler();
		// Unused variable 'x' should produce a warning
		var code = "public class Foo { public void Bar() { int x = 1; } }";
		var res = await compiler.Compile("test", [code], [_coreLibPath], _fs, CancellationToken);
		if (res.HasErrors())
		{
			var errors = res.Errors.Select(e => $"{e.Type}: {e.Message}").ToArray();
			Fail($"Compilation errors:\n{errors.JoinNL()}");
		}
		res.GetAssembly(compiler.CreateContext()).AssertNotNull();
		res.Errors.Any(e => e.Type == CompilationErrorTypes.Warning).AssertTrue();
	}

	[TestMethod]
	public async Task PythonCompileSuccess()
	{
		ICompiler compiler = new PythonCompiler();
		var code = "def foo():\n    return 42";
		var res = await compiler.Compile("test", [code], [], CancellationToken);
		if (res.HasErrors())
		{
			var errors = res.Errors.Select(e => $"{e.Type}: {e.Message}").ToArray();
			Fail($"Compilation errors:\n{errors.JoinNL()}");
		}
		res.GetAssembly(compiler.CreateContext()).AssertNotNull();
	}

	[TestMethod]
	public async Task PythonCompileError()
	{
		ICompiler compiler = new PythonCompiler();
		// Syntax error: missing colon
		var code = "def foo()\n    return 42";
		var res = await compiler.Compile("test", [code], [], CancellationToken);
		res.GetAssembly(compiler.CreateContext()).AssertNull();
		res.HasErrors().AssertTrue();
	}

	[TestMethod]
	public async Task PythonCompileParallel_SharedEngine_ShouldFail()
	{
		// Regression test for PythonCompiler shared-engine concurrency: parallel compilations
		// on one PythonCompiler instance must all succeed. (Was: the shared IronPython
		// ScriptEngine was accessed without synchronization, so concurrent Compile calls
		// corrupted each other; Compile now serializes engine access, PythonCompiler.cs:91.)
		// Scripts that each define and use their own helper function
		var userScripts = Enumerable.Range(1, 20).Select(i => $@"
def helper_func_{i}(x):
    return x * 2

class Script{i}:
    def run(self):
        return helper_func_{i}({i})
").ToArray();

		// Single compiler instance (shared ScriptEngine)
		ICompiler compiler = new PythonCompiler();

		var errors = new System.Collections.Concurrent.ConcurrentBag<string>();
		var successCount = 0;

		// Compile all scripts in parallel
		await Task.WhenAll(userScripts.Select(async (script, idx) =>
		{
			try
			{
				var res = await compiler.Compile($"script{idx}", [script], [], CancellationToken);
				if (res.HasErrors())
				{
					foreach (var err in res.Errors)
						errors.Add($"Script{idx}: {err.Message}");
				}
				else
				{
					var asm = res.GetAssembly(compiler.CreateContext());
					if (asm != null)
						Interlocked.Increment(ref successCount);
					else
						errors.Add($"Script{idx}: Assembly is null");
				}
			}
			catch (Exception ex)
			{
				errors.Add($"Script{idx}: Exception - {ex.Message}");
			}
		}));

		// Report any failures for diagnostics; with serialized engine access there should be none.
		if (errors.Any())
		{
			Console.WriteLine($"Threading issues detected ({errors.Count} errors, {successCount} successes):");
			foreach (var err in errors.Take(10))
				Console.WriteLine($"  {err}");
		}

		// All parallel compilations must succeed.
		successCount.AssertEqual(userScripts.Length);
	}

	[TestMethod]
	public async Task PythonCompileParallel_SharedScope_ShouldFail()
	{
		// Regression test for PythonCompiler shared-engine concurrency: a utility script is
		// loaded first, then user scripts referencing its functions are compiled in parallel on
		// one shared engine; all must succeed. (Was: unsynchronized concurrent access to the
		// shared ScriptEngine corrupted compilations; Compile now serializes engine access,
		// PythonCompiler.cs:91.)
		var utilityScript = @"
def utility_add(a, b):
    return a + b

def utility_multiply(a, b):
    return a * b
";

		// User scripts that use the utility functions
		var userScripts = Enumerable.Range(1, 20).Select(i => $@"
class UserScript{i}:
    def calculate(self):
        return utility_add({i}, utility_multiply({i}, 2))
").ToArray();

		// Single compiler instance (shared ScriptEngine)
		ICompiler compiler = new PythonCompiler();

		var errors = new System.Collections.Concurrent.ConcurrentBag<string>();
		var successCount = 0;

		// First compile and load utility script
		var utilityRes = await compiler.Compile("utility", [utilityScript], [], CancellationToken);
		utilityRes.HasErrors().AssertFalse();

		// Now compile user scripts in parallel - they need to include utility code
		await Task.WhenAll(userScripts.Select(async (script, idx) =>
		{
			try
			{
				// Include utility script with each user script
				var fullScript = utilityScript + "\n" + script;
				var res = await compiler.Compile($"user{idx}", [fullScript], [], CancellationToken);
				if (res.HasErrors())
				{
					foreach (var err in res.Errors)
						errors.Add($"User{idx}: {err.Message}");
				}
				else
				{
					var asm = res.GetAssembly(compiler.CreateContext());
					if (asm != null)
						Interlocked.Increment(ref successCount);
					else
						errors.Add($"User{idx}: Assembly is null");
				}
			}
			catch (Exception ex)
			{
				errors.Add($"User{idx}: Exception - {ex.Message}");
			}
		}));

		if (errors.Any())
		{
			Console.WriteLine($"Threading issues detected ({errors.Count} errors, {successCount} successes):");
			foreach (var err in errors.Take(10))
				Console.WriteLine($"  {err}");
		}

		successCount.AssertEqual(userScripts.Length);
	}

	[TestMethod]
	public async Task PythonCompileParallel_SeparateEngines_ShouldSucceed()
	{
		// This test shows that separate engines work correctly
		var utilityScript = @"
def helper_func(x):
    return x * 2
";

		var userScripts = Enumerable.Range(1, 10).Select(i => $@"
{utilityScript}

class Script{i}:
    def run(self):
        return helper_func({i})
").ToArray();

		var successCount = 0;

		// Compile all scripts in parallel with SEPARATE compiler instances
		await Task.WhenAll(userScripts.Select(async (script, idx) =>
		{
			// Each task gets its own compiler (and ScriptEngine)
			ICompiler compiler = new PythonCompiler();
			var res = await compiler.Compile($"script{idx}", [script], [], CancellationToken);
			if (!res.HasErrors())
			{
				var asm = res.GetAssembly(compiler.CreateContext());
				if (asm != null)
					Interlocked.Increment(ref successCount);
			}
		}));

		successCount.AssertEqual(userScripts.Length);
	}

	[TestMethod]
	public async Task PythonCompileParallel_StressTest()
	{
		// Stress test with many parallel compilations
		const int scriptCount = 50;
		const int iterations = 3;

		ICompiler compiler = new PythonCompiler();

		for (var iter = 0; iter < iterations; iter++)
		{
			var scripts = Enumerable.Range(1, scriptCount).Select(i => $@"
def func_{iter}_{i}(x):
    result = x
    for j in range({i}):
        result = result + j
    return result

class Script_{iter}_{i}:
    value = {i}

    def process(self):
        return func_{iter}_{i}(self.value)
").ToArray();

			var errors = new System.Collections.Concurrent.ConcurrentBag<string>();
			var successCount = 0;

			await Task.WhenAll(scripts.Select(async (script, idx) =>
			{
				try
				{
					var res = await compiler.Compile($"stress_{iter}_{idx}", [script], [], CancellationToken);
					if (res.HasErrors())
					{
						foreach (var err in res.Errors)
							errors.Add($"Iter{iter}_Script{idx}: {err.Message}");
					}
					else
					{
						var ctx = compiler.CreateContext();
						var asm = res.GetAssembly(ctx);
						if (asm != null)
							Interlocked.Increment(ref successCount);
						else
							errors.Add($"Iter{iter}_Script{idx}: Assembly is null");
					}
				}
				catch (Exception ex)
				{
					errors.Add($"Iter{iter}_Script{idx}: {ex.GetType().Name} - {ex.Message}");
				}
			}));

			if (errors.Any())
			{
				Console.WriteLine($"Iteration {iter}: {errors.Count} errors, {successCount} successes");
				foreach (var err in errors.Take(5))
					Console.WriteLine($"  {err}");
			}

			successCount.AssertEqual(scriptCount);
		}
	}

	[TestMethod]
	public async Task PythonCompileParallel_WithFileImports_ShouldFail()
	{
		// Regression test for PythonCompiler shared-engine concurrency with file imports: scripts
		// that import from external .py files via a shared engine's search paths are compiled in
		// parallel and must all succeed. (Was: unsynchronized concurrent access to the shared
		// ScriptEngine let parallel module imports interfere; Compile now serializes engine access,
		// PythonCompiler.cs:91.)

		// Create temp directory for Python modules
		var tempDir = Path.Combine(Path.GetTempPath(), $"python_test_{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDir);

		try
		{
			// Write utility modules to temp directory
			await File.WriteAllTextAsync(Path.Combine(tempDir, "data_utils.py"), @"
def load_data(source, param):
    return [param * i for i in range(10)]

def process_data(data, factor):
    return [x * factor for x in data]
");

			await File.WriteAllTextAsync(Path.Combine(tempDir, "chart_utils.py"), @"
def draw_chart(panel, data, title):
    return f'Chart: {title} with {len(data)} points'

def create_series(name, values):
    return {'name': name, 'values': list(values)}
");

			await File.WriteAllTextAsync(Path.Combine(tempDir, "indicator_utils.py"), @"
def calculate_sma(data, period):
    if len(data) < period:
        return []
    return [sum(data[i:i+period])/period for i in range(len(data)-period+1)]

def calculate_ema(data, period):
    if not data:
        return []
    multiplier = 2 / (period + 1)
    ema = [data[0]]
    for price in data[1:]:
        ema.append((price - ema[-1]) * multiplier + ema[-1])
    return ema
");

			// Create engine with search paths (simulating real setup)
			var engine = IronPython.Hosting.Python.CreateEngine();
			var paths = engine.GetSearchPaths();
			paths.Add(tempDir);
			engine.SetSearchPaths(paths);

			// Single compiler with shared engine
			ICompiler compiler = new PythonCompiler(engine);

			// Scripts that import from the utility modules
			var userScripts = Enumerable.Range(1, 30).Select(i => $@"
from data_utils import load_data, process_data
from chart_utils import draw_chart, create_series
from indicator_utils import calculate_sma, calculate_ema

class AnalyticsScript_{i}:
    '''Analytics script {i}'''

    def run(self, panel, securities):
        results = []
        for sec in securities:
            data = load_data(sec, {i})
            processed = process_data(data, 2)
            sma = calculate_sma(processed, 3)
            ema = calculate_ema(processed, 3)
            series = create_series(f'SMA_{i}', sma)
            chart = draw_chart(panel, ema, f'Script{i}')
            results.append((series, chart))
        return results
").ToArray();

			var errors = new System.Collections.Concurrent.ConcurrentBag<string>();
			var successCount = 0;

			// Compile all scripts in parallel
			await Task.WhenAll(userScripts.Select(async (script, idx) =>
			{
				try
				{
					var res = await compiler.Compile($"analytics_{idx}", [script], [], CancellationToken);
					if (res.HasErrors())
					{
						foreach (var err in res.Errors)
							errors.Add($"Analytics{idx}: {err.Message}");
					}
					else
					{
						var ctx = compiler.CreateContext();
						var asm = res.GetAssembly(ctx);
						if (asm != null)
							Interlocked.Increment(ref successCount);
						else
							errors.Add($"Analytics{idx}: Assembly is null");
					}
				}
				catch (Exception ex)
				{
					errors.Add($"Analytics{idx}: {ex.GetType().Name} - {ex.Message}");
				}
			}));

			if (errors.Any())
			{
				Console.WriteLine($"File import threading issues ({errors.Count} errors, {successCount} successes):");
				foreach (var err in errors.Take(10))
					Console.WriteLine($"  {err}");
			}

			successCount.AssertEqual(userScripts.Length);
		}
		finally
		{
			// Cleanup
			try { Directory.Delete(tempDir, true); } catch { }
		}
	}

	[TestMethod]
	public async Task FSharpCompileSuccess()
	{
		// TODO: F# compiler is not available on non-Windows platforms.
		if (!OperatingSystemEx.IsWindows())
			return;

		ICompiler compiler = new FSharpCompiler();
		var code = "module Foo\nlet bar () = 42";
		var res = await compiler.Compile("test", [code], [_coreLibPath], _fs, CancellationToken);
		if (res.HasErrors())
		{
			var errors = res.Errors.Select(e => $"{e.Type}: {e.Message}").ToArray();
			Fail($"Compilation errors:\n{errors.JoinNL()}");
		}
		res.GetAssembly(compiler.CreateContext()).AssertNotNull();
	}

	[TestMethod]
	public async Task FSharpCompileError()
	{
		// TODO: F# compiler is not available on non-Windows platforms.
		if (!OperatingSystemEx.IsWindows())
			return;

		ICompiler compiler = new FSharpCompiler();
		// Syntax error: missing '='
		var code = "module Foo\nlet bar () 42";
		var res = await compiler.Compile("test", [code], [_coreLibPath], _fs, CancellationToken);
		res.GetAssembly(compiler.CreateContext()).AssertNull();
		res.HasErrors().AssertTrue();
	}

	[TestMethod]
	public void AssemblyReference_Location_WithFullPath_ShouldPreservePath()
	{
		// Regression test for AssemblyReference.Location: ensures a full path supplied via
		// FileName is preserved as-is. (Was: Path.GetFileName() stripped the directory and
		// returned just "MyLibrary.dll", AssemblyReference.cs:42.)
		var fullPath = Path.Combine(Path.GetTempPath(), "CustomLibs", "MyLibrary.dll");

		var asmRef = new AssemblyReference
		{
			FileName = fullPath
		};

		// Location must preserve the full path when it's provided.
		asmRef.Location.AssertEqual(fullPath);
	}

	[TestMethod]
	public void AssemblyReference_Location_WithRelativePath_ShouldPreservePath()
	{
		// Regression test for AssemblyReference.Location: ensures a relative path supplied via
		// FileName is preserved as-is. (Was: Location returned just "MyLibrary.dll",
		// AssemblyReference.cs:42.)
		var relativePath = Path.Combine("libs", "subdir", "MyLibrary.dll");

		var asmRef = new AssemblyReference
		{
			FileName = relativePath
		};

		// Location must preserve the relative path.
		asmRef.Location.AssertEqual(relativePath);
	}

	[TestMethod]
	public void AssemblyReference_Location_WithJustFileName_ShouldSearchRuntime()
	{
		// When FileName is just a filename (no path), Location should work as before:
		// look in RuntimePath first, otherwise return just the filename
		var fileName = "System.Runtime.dll";

		var asmRef = new AssemblyReference
		{
			FileName = fileName
		};

		// Since System.Runtime.dll exists in runtime path, it should find it there
		var location = asmRef.Location;
		location.IsEmpty().AssertFalse();
		Path.GetFileName(location).AssertEqual(fileName);
	}

	// The F# toolchain (FSharp.Compiler.Service) is only loadable on Windows in this repo,
	// mirroring the guard used by the existing FSharpCompileSuccess/FSharpCompileError tests.
	private static bool FSharpAvailable => OperatingSystemEx.IsWindows();

	/// <summary>
	/// Regression test for FSharpCompiler.Analyse: ensures source that parses cleanly but fails
	/// type checking (here an undefined identifier) reports at least one error, like
	/// RoslynCompiler.Analyse does. (Was: only parse-stage results.Diagnostics were collected and
	/// the type-check answer carrying semantic diagnostics was never inspected, so broken F# was
	/// treated as valid, FSharpCompiler.cs:167.)
	/// </summary>
	[TestMethod]
	[TestCategory("Integration")]
	public async Task Analyse_SemanticError_IsReported()
	{
		if (!FSharpAvailable)
		{
			Inconclusive("F# compiler is only available on Windows.");
			return;
		}

		ICompiler compiler = new FSharpCompiler();

		// Parses fine, but 'undefinedIdentifier' is not defined => a type-check (semantic) error.
		var code = "module Foo\nlet bar () = undefinedIdentifier 42";

		var errors = await compiler.Analyse(
			null,
			[],
			"test",
			[code],
			[_coreLibPath.ToRef(_fs)],
			CancellationToken);

		errors.HasErrors().AssertTrue();
	}

	/// <summary>
	/// Regression test for FSharpCompiler.Analyse: ensures valid F# that uses a type from a supplied
	/// reference analyses without errors. (Was: project options were built with an empty source list
	/// and an empty in-memory file system, so reference bodies were unreadable and sources were not
	/// registered, producing false "assembly not found" / "identifier not defined" diagnostics;
	/// sources and references are now registered in the file system, FSharpCompiler.cs:153.)
	/// </summary>
	[TestMethod]
	[TestCategory("Integration")]
	public async Task Analyse_ValidCodeUsingReference_HasNoFalseErrors()
	{
		if (!FSharpAvailable)
		{
			Inconclusive("F# compiler is only available on Windows.");
			return;
		}

		ICompiler compiler = new FSharpCompiler();

		// Uses System.String from the supplied core reference; must resolve cleanly.
		var code = "module Foo\nlet bar () : string = System.String.Empty";

		var errors = await compiler.Analyse(
			null,
			[],
			"test",
			[code],
			[_coreLibPath.ToRef(_fs)],
			CancellationToken);

		errors.HasErrors().AssertFalse();
	}

	/// <summary>
	/// Regression test for FSharpCompiler error reporting: ensures an error on the 3rd physical
	/// source line (0-based index 2) is reported with Line == 2, matching the 0-based Line the
	/// Roslyn implementation produces (the shared UI maps CompilationError.Line onto editor lines for
	/// every ICompiler). (Was: ToError passed the 1-based FSharpDiagnostic.StartLine straight through,
	/// so F# errors were highlighted one line below the actual location; ToError now subtracts one,
	/// FSharpCompiler.cs:225.)
	/// </summary>
	[TestMethod]
	[TestCategory("Integration")]
	public async Task Compile_ErrorLine_IsZeroBased()
	{
		if (!FSharpAvailable)
		{
			Inconclusive("F# compiler is only available on Windows.");
			return;
		}

		ICompiler compiler = new FSharpCompiler();

		// Line 0: module Foo
		// Line 1: let ok () = 1
		// Line 2: let bad () = undefinedIdentifier 42   <- the error lives here (0-based line 2).
		var code = "module Foo\nlet ok () = 1\nlet bad () = undefinedIdentifier 42";

		var res = await compiler.Compile("test", [code], [_coreLibPath], _fs, CancellationToken);

		res.HasErrors().AssertTrue();

		var error = res.Errors.ErrorsOnly().First();
		error.Line.AssertEqual(2);
	}

	/// <summary>
	/// Regression test for FSharpCompiler.Compile: ensures it tolerates duplicate reference names
	/// (realistic when merging default and user references) and returns a CompilationResult, like
	/// RoslynCompiler does. (Was: reference bodies were inserted via Dictionary.Add(), so a repeated
	/// file name threw ArgumentException "An item with the same key has already been added" and
	/// crashed the whole compilation; the reference map now uses TryAdd, FSharpCompiler.cs:119.)
	/// </summary>
	[TestMethod]
	[TestCategory("Integration")]
	public async Task Compile_DuplicateReferenceNames_DoesNotThrow()
	{
		if (!FSharpAvailable)
		{
			Inconclusive("F# compiler is only available on Windows.");
			return;
		}

		ICompiler compiler = new FSharpCompiler();

		var coreRef = _coreLibPath.ToRef(_fs);

		// Same reference name supplied twice (e.g. resolved from two places).
		IEnumerable<(string name, byte[] body)> refs = [coreRef, coreRef];

		var code = "module Foo\nlet bar () = 42";

		var res = await compiler.Compile("test", [code], refs, CancellationToken);

		res.AssertNotNull();
	}

	/// <summary>
	/// Regression test for FSharpCompiler reference caching: ensures recompiling against a reference
	/// of the same name but new content sees the new types. (Was: every reference was presented to
	/// FCS under a constant synthetic path Path.Combine(Path.GetTempPath(), r.name); the FCS
	/// IL-module-reader cache keys assembly metadata by (path, last-write-time), so a second Compile
	/// reusing the same reference name with a rebuilt body kept the stale metadata of the first body.
	/// The synthetic path is now content-addressed via the body hash, FSharpCompiler.cs:112.)
	/// </summary>
	[TestMethod]
	[TestCategory("Integration")]
	public async Task Compile_RebuiltReferenceSameName_UsesFreshMetadata()
	{
		if (!FSharpAvailable)
		{
			Inconclusive("F# compiler is only available on Windows.");
			return;
		}

		// Build two distinct assemblies that share the same simple name "MyLib" but expose
		// different public types: v1 has TypeA, v2 has TypeB. A single FSharpCompiler instance
		// (one FCS checker, one module-reader cache) then compiles against each in turn.
		var libV1 = await BuildReferenceAsync("namespace MyLib { public class TypeA { } }");
		var libV2 = await BuildReferenceAsync("namespace MyLib { public class TypeB { } }");

		const string refName = "MyLib.dll";
		var coreRef = _coreLibPath.ToRef(_fs);

		ICompiler compiler = new FSharpCompiler();

		// First compile resolves TypeA against the v1 body. This populates the FCS cache for
		// the synthetic reference path keyed by the (constant) write time.
		var resV1 = await compiler.Compile(
			"useA",
			["module UseA\nlet a = MyLib.TypeA()"],
			[coreRef, (refName, libV1)],
			CancellationToken);

		resV1.HasErrors().AssertFalse();

		// Second compile presents the SAME reference name but the rebuilt v2 body and uses the
		// newly added TypeB. The fresh metadata must be honoured.
		var resV2 = await compiler.Compile(
			"useB",
			["module UseB\nlet b = MyLib.TypeB()"],
			[coreRef, (refName, libV2)],
			CancellationToken);

		resV2.HasErrors().AssertFalse();
	}

	// Compiles a tiny C# library to raw assembly bytes used as an in-memory F# reference body.
	private async Task<byte[]> BuildReferenceAsync(string source)
	{
		ICompiler csharp = new CSharpCompiler();

		var res = await csharp.Compile("MyLib", source, [_coreLibPath], _fs, CancellationToken);

		if (res.HasErrors())
		{
			var errors = res.Errors.Select(e => $"{e.Type}: {e.Message}").ToArray();
			Fail($"Reference build failed:\n{errors.JoinNL()}");
		}

		var body = ((AssemblyCompilationResult)res).AssemblyBody;
		IsNotNull(body, "Reference build produced no assembly body.");
		return body;
	}

	// Compiles a single-class Python module and returns the reflected type produced by the
	// IronPython reflection layer (TypeImpl) for the requested class name. This is the exact
	// public path real consumers take: Compile -> GetAssembly -> GetTypes.
	private Type CompilePythonType(string code, string typeName)
	{
		ICompiler compiler = new PythonCompiler();

		var res = compiler.Compile("audit", [code], [], CancellationToken).Result;

		if (res.HasErrors())
		{
			var errors = res.Errors.Select(e => $"{e.Type}: {e.Message}").ToArray();
			Fail($"Compilation errors:\n{errors.JoinNL()}");
		}

		var asm = res.GetAssembly(compiler.CreateContext());
		IsNotNull(asm);

		var type = asm.GetTypes().FirstOrDefault(t => t.Name == typeName);
		IsNotNull(type, $"Type '{typeName}' not found in compiled assembly.");
		return type;
	}

	/// <summary>
	/// Regression test for PythonContext GetMethodImpl: ensures type.GetMethod("run") resolves the
	/// Python method by name without throwing. (Was: GetMethodImpl evaluated SequenceEqual(types)
	/// unconditionally, but Type.GetMethod(string) passes types == null, so it threw
	/// ArgumentNullException as soon as a name match was found; a "types is null" guard now
	/// short-circuits, PythonContext.cs:696.)
	/// </summary>
	[TestMethod]
	public void GetMethodByName_DoesNotThrowAndResolves()
	{
		var type = CompilePythonType("class C1:\n    def run(self):\n        return 42\n", "C1");

		// Standard reflection lookup by name only (no parameter types).
		var method = type.GetMethod("run");

		IsNotNull(method, "GetMethod(\"run\") must resolve the Python method.");
		AreEqual("run", method.Name);
	}

	/// <summary>
	/// Regression test for PythonContext MethodImpl.Invoke: ensures invoking a no-argument Python
	/// method with null parameters (MethodInfo.Invoke(obj, null) is the canonical parameterless call)
	/// returns its value. (Was: parameters were spread via [obj, .. parameters], throwing
	/// NullReferenceException when parameters was null; the spread now coalesces to an empty array,
	/// PythonContext.cs:269.)
	/// </summary>
	[TestMethod]
	public void Invoke_NullParameters_DoesNotThrow()
	{
		var type = CompilePythonType("class C2:\n    def get_value(self):\n        return 42\n", "C2");

		var instance = ((ITypeConstructor)type).CreateInstance([]);
		IsNotNull(instance);

		var method = type
			.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
			.First(m => m.Name == "get_value");

		// Canonical reflection call of a parameterless method: pass null for the arguments.
		var result = method.Invoke(instance, null);

		AreEqual(42, result.To<int>());
	}

	/// <summary>
	/// Regression test for PythonContext GetEvents: ensures a class exposing an ordinary add_item
	/// method (no remove_item) reflects its events without throwing, and the add_item method stays
	/// reachable through GetMethods. (Was: the Select yielded null for an add_-prefixed function with
	/// no matching remove_ pair, cached those nulls in _events, then dereferenced them via
	/// e.GetAddMethod(), so GetEvents/GetEvent/GetMembers threw NullReferenceException forever once the
	/// poisoned cache was built; nulls are now filtered before caching, PythonContext.cs:635.)
	/// </summary>
	[TestMethod]
	public void GetEvents_UnpairedAddMethod_DoesNotThrow()
	{
		var type = CompilePythonType("class C4:\n    def add_item(self, x):\n        return x\n", "C4");

		// Must not throw: the unpaired add_item is simply not an event.
		var events = type.GetEvents();
		IsNotNull(events);

		// The add_item function must remain reachable as a regular method.
		var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
		IsTrue(methods.Any(m => m.Name == "add_item"), "add_item must remain reachable as a method.");
	}

	/// <summary>
	/// Regression test for PythonContext GetInterfaces: ensures a Python class declaring
	/// class C(System.IDisposable) lists IDisposable in GetInterfaces(). (Was: GetInterfaces returned
	/// only _dotNetBaseType.GetInterfaces(), which walks the BaseType chain to System.Object, so
	/// interfaces implemented directly by the Python class were lost even though IsAssignableFrom
	/// reported them; it now also unions _underlyingType.GetInterfaces(), PythonContext.cs:558.)
	/// </summary>
	[TestMethod]
	public void GetInterfaces_DirectlyImplementedDotNetInterface_IsListed()
	{
		var code =
			"import System\n" +
			"class C6(System.IDisposable):\n" +
			"    def Dispose(self):\n" +
			"        pass\n";

		var type = CompilePythonType(code, "C6");

		var interfaces = type.GetInterfaces();

		IsTrue(interfaces.Contains(typeof(IDisposable)),
			"GetInterfaces() must include the directly implemented .NET interface.");
	}

	/// <summary>
	/// Regression test for PythonContext event name extraction: ensures only the leading "add_" prefix
	/// is removed, yielding event "pre_add_check" resolvable via GetEvent with its matching
	/// remove_pre_add_check pair. (Was: the name was extracted with Remove("add_", true), stripping
	/// every occurrence of "add_" anywhere in the name, so add_pre_add_check collapsed to "pre_check",
	/// the remove_pre_check lookup failed and the event was silently dropped; the name is now taken as
	/// the substring after the leading prefix, PythonContext.cs:627.)
	/// </summary>
	[TestMethod]
	public void EventName_StripsOnlyLeadingPrefix()
	{
		var code =
			"class C7:\n" +
			"    def add_pre_add_check(self, h):\n" +
			"        pass\n" +
			"    def remove_pre_add_check(self, h):\n" +
			"        pass\n";

		var type = CompilePythonType(code, "C7");

		var evt = type.GetEvent("pre_add_check");

		IsNotNull(evt, "Event name must keep the inner 'add_' and resolve as 'pre_add_check'.");
		AreEqual("pre_add_check", evt.Name);
	}

	/// <summary>
	/// Regression test for PythonContext typed GetCustomAttributes: ensures
	/// GetCustomAttributes(typeof(DisplayAttribute), false) returns the DisplayAttribute present in
	/// __dict__. (Was: the typeof(DisplayAttribute) branch returned nothing when display_name/__doc__
	/// were absent and never fell through to the __dict__ scan, so a real DisplayAttribute stored in
	/// the class __dict__ was unreachable via the typed overload even though the untyped overload
	/// returned it; the branch now only returns when it actually has a value, then falls through to the
	/// __dict__ scan, PythonContext.cs:89.)
	/// </summary>
	[TestMethod]
	public void TypedGetCustomAttributes_DisplayAttributeFromDict_IsFound()
	{
		// Reference the DisplayAttribute assembly by its simple name so IronPython reuses the
		// assembly already loaded in the host. Loading a fresh copy by path would give the attribute
		// a distinct Type identity, and attr.GetType().Is(typeof(DisplayAttribute)) would never match.
		var annotationsAsm = typeof(DisplayAttribute).Assembly.GetName().Name;

		var code =
			"import clr\n" +
			$"clr.AddReference('{annotationsAsm}')\n" +
			"from System.ComponentModel.DataAnnotations import DisplayAttribute\n" +
			"class C9:\n" +
			"    my_display = DisplayAttribute()\n";

		var type = CompilePythonType(code, "C9");

		// Sanity: the untyped overload sees the attribute living in __dict__.
		IsTrue(type.GetCustomAttributes(false).OfType<DisplayAttribute>().Any(),
			"Untyped GetCustomAttributes must see the DisplayAttribute in __dict__.");

		// The typed overload must find the same attribute.
		var typed = type.GetCustomAttributes(typeof(DisplayAttribute), false);

		IsTrue(typed.OfType<DisplayAttribute>().Any(),
			"Typed GetCustomAttributes(DisplayAttribute) must return the attribute from __dict__.");
	}

	/// <summary>
	/// Regression test for PythonContext TypeImpl.InvokeMember: ensures invoking a member with one
	/// argument forwards that argument as a scalar value. (Was: InvokeMember called
	/// pythonFunction.__call__(DefaultContext.Default, target, args); since __call__ is declared
	/// __call__(CodeContext, params object[] args), the args array was wrapped as a single positional
	/// argument instead of being spread, so the function received (target, args[]) rather than
	/// (target, arg0, arg1, ...); args are now spread via [target, .. args], PythonContext.cs:578.)
	/// </summary>
	[TestMethod]
	public void InvokeMember_SpreadsArgsNotAsSingleArray()
	{
		// A @staticmethod accessed off an instance target is resolved as a raw PythonFunction, which
		// drives the buggy InvokeMember branch. Its first parameter receives the target, the rest
		// receive the (correctly spread) call arguments.
		var code =
			"class C3:\n" +
			"    @staticmethod\n" +
			"    def grab(recv, value):\n" +
			"        return value\n";

		var type = CompilePythonType(code, "C3");

		var instance = ((ITypeConstructor)type).CreateInstance([]);
		IsNotNull(instance);

		// Pass exactly one argument. With correct spreading the Python 'value' parameter must be the
		// scalar string itself, not an object[] wrapping it.
		var result = type.InvokeMember("grab", BindingFlags.InvokeMethod, null, instance, ["only"], null, null, null);

		AreEqual("only", result);
	}
}
