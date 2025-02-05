namespace Ecng.Tests.Compilation
{
	using System.Diagnostics;
	using System.Threading;

	using Ecng.Compilation;
	using Ecng.Compilation.Roslyn;

	[TestClass]
	public class CompilerTests
	{
		private static readonly string _coreLibPath = typeof(object).Assembly.Location;

		[TestMethod]
		public async Task Compile()
		{
			ICompiler compiler = new CSharpCompiler();
			var res = await compiler.Compile("test", "class Class1 {}",
			[
				_coreLibPath,
			]);
			res.GetAssembly(compiler.CreateContext()).AssertNotNull();
			res.HasErrors().AssertFalse();
		}

		[TestMethod]
		public async Task CompileError()
		{
			ICompiler compiler = new CSharpCompiler();
			var res = await compiler.Compile("test", "class Class1 {",
			[
				_coreLibPath,
			]);
			res.GetAssembly(compiler.CreateContext()).AssertNull();
			res.HasErrors().AssertTrue();
		}

		[TestMethod]
		[ExpectedException(typeof(OperationCanceledException))]
		public async Task CompileCancel()
		{
			var cts = new CancellationTokenSource();
			cts.Cancel();
			ICompiler compiler = new CSharpCompiler();
			var res = await compiler.Compile("test", "class Class1 {",
			[
				_coreLibPath,
			], cts.Token);
			res.GetAssembly(compiler.CreateContext()).AssertNull();
			res.HasErrors().AssertTrue();
		}

		[TestMethod]
		public async Task BannedSymbols()
		{
			var testCode = @"using System.Diagnostics;

class Class1
{
	public void Method()
	{
		Process.GetCurrentProcess().Kill();
	}
}";

			var refs = new HashSet<string>(
			[
				_coreLibPath,
				typeof(Process).Assembly.Location,
				typeof(System.ComponentModel.Component).Assembly.Location,
				"System.Runtime.dll".ToFullRuntimePath(),
			], StringComparer.InvariantCultureIgnoreCase);

			ICompiler compiler = new CSharpCompiler();
			var (analyzer, settings) = @"T:System.Diagnostics.Process;Don't use Process".ToBannedSymbolsAnalyzer();
			var res = await compiler.Analyse(analyzer, [settings], "test", [testCode], refs.Select(r => r.ToRef()));

			res.Length.AssertEqual(1);
			res[0].Message.AssertEqual("The symbol 'Process' is banned in this project: Don't use Process");
		}
	}
}