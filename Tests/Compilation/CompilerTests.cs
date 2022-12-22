namespace Ecng.Tests.Compilation
{
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
			var res = compiler.Compile(new(), "test", "class Class1 {}", new string[]
			{
				//"C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\6.0.12\\mscorlib.dll",
				//"C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\6.0.12\\netstandard.dll",
				//"C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\6.0.12\\System.dll",
				//"C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\6.0.12\\System.Core.dll",
				"C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\6.0.12\\System.Private.CoreLib.dll",
			});
			res.Assembly.AssertNotNull();
		}
	}
}