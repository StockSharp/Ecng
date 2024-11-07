namespace Ecng.Tests.Compilation
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;

	using Ecng.Compilation;
	using Ecng.Compilation.Roslyn;
	using Ecng.UnitTesting;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class CompilerTests
	{
		private static readonly string _coreLibPath = typeof(object).Assembly.Location;

		[TestMethod]
		public void Compile()
		{
			ICompiler compiler = new RoslynCompiler();
			var res = compiler.Compile("test", "class Class1 {}",
			[
				_coreLibPath,
			]);
			res.Assembly.AssertNotNull();
			res.HasErrors().AssertFalse();
		}

		[TestMethod]
		public void CompileError()
		{
			ICompiler compiler = new RoslynCompiler();
			var res = compiler.Compile("test", "class Class1 {",
			[
				_coreLibPath,
			]);
			res.Assembly.AssertNull();
			res.HasErrors().AssertTrue();
		}

		[TestMethod]
		[ExpectedException(typeof(OperationCanceledException))]
		public void CompileCancel()
		{
			var cts = new CancellationTokenSource();
			cts.Cancel();
			ICompiler compiler = new RoslynCompiler();
			var res = compiler.Compile("test", "class Class1 {",
			[
				_coreLibPath,
			], cts.Token);
			res.Assembly.AssertNull();
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

			ICompiler compiler = new RoslynCompiler();
			var (analyzer, settings) = @"T:System.Diagnostics.Process;Don't use Process".ToBannedSymbolsAnalyzer();
			var res = await compiler.Analyse(analyzer, [settings], "test", [testCode], refs.Select(r => r.ToRef()));

			res.Length.AssertEqual(1);
			res[0].Message.AssertEqual("The symbol 'Process' is banned in this project: Don't use Process");
		}
	}
}