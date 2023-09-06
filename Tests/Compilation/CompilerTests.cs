namespace Ecng.Tests.Compilation
{
	using System;
	using System.Threading;

	using Ecng.Compilation;
	using Ecng.Compilation.Roslyn;
	using Ecng.UnitTesting;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class CompilerTests
	{
		[TestMethod]
		public void Compile()
		{
			ICompiler compiler = new RoslynCompiler(CompilationLanguages.CSharp);
			var res = compiler.Compile("test", "class Class1 {}", new string[]
			{
				"System.Private.CoreLib.dll".ToFullRuntimePath(),
			});
			res.Assembly.AssertNotNull();
			res.HasErrors().AssertFalse();
		}

		[TestMethod]
		public void CompileError()
		{
			ICompiler compiler = new RoslynCompiler(CompilationLanguages.CSharp);
			var res = compiler.Compile("test", "class Class1 {", new string[]
			{
				"System.Private.CoreLib.dll".ToFullRuntimePath(),
			});
			res.Assembly.AssertNull();
			res.HasErrors().AssertTrue();
		}

		[TestMethod]
		[ExpectedException(typeof(OperationCanceledException))]
		public void CompileCancel()
		{
			var cts = new CancellationTokenSource();
			cts.Cancel();
			ICompiler compiler = new RoslynCompiler(CompilationLanguages.CSharp);
			var res = compiler.Compile("test", "class Class1 {", new string[]
			{
				"System.Private.CoreLib.dll".ToFullRuntimePath(),
			}, cts.Token);
			res.Assembly.AssertNull();
			res.HasErrors().AssertTrue();
		}
	}
}