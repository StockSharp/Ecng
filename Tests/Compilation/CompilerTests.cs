namespace Ecng.Tests.Compilation;

using Ecng.Compilation;
using Ecng.Compilation.FSharp;
using Ecng.Compilation.Python;
using Ecng.Compilation.Roslyn;
using Ecng.IO;

[TestClass]
public class CompilerTests : BaseTestClass
{
	private static readonly string _coreLibPath = typeof(object).Assembly.Location;
	private static readonly IFileSystem _fs = LocalFileSystem.Instance;

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
		// This test reproduces threading issues with shared ScriptEngine
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

		// If there are any errors, the test exposes the threading issue
		if (errors.Any())
		{
			Console.WriteLine($"Threading issues detected ({errors.Count} errors, {successCount} successes):");
			foreach (var err in errors.Take(10))
				Console.WriteLine($"  {err}");
		}

		// This assertion will likely fail due to threading issues
		successCount.AssertEqual(userScripts.Length);
	}

	[TestMethod]
	public async Task PythonCompileParallel_SharedScope_ShouldFail()
	{
		// This test simulates the real scenario: utility script loaded first,
		// then user scripts executed in parallel that reference utility functions
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
		// This test reproduces the real issue: scripts that import from external .py files
		// When compiled in parallel, module imports can interfere with each other

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
}