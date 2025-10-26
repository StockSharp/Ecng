namespace Ecng.Tests.Compilation;

using Ecng.Compilation;
using Ecng.Compilation.Expressions;
using Ecng.Compilation.Roslyn;

using Nito.AsyncEx;

[TestClass]
public class ExpressionTests
{
	private static readonly ICompiler _compiler = new CSharpCompiler();
	private static readonly ICompilerContext _context = _compiler.CreateContext();

	private const string _cacheDir = "asm_cache";

	private static ExpressionFormula<decimal> Compile(string expression, ICompilerCache cache = default)
		=> AsyncContext.Run(() => _compiler.Compile<decimal>(_context, expression, cache));

	[ClassInitialize]
	public static void Init(TestContext _)
	{
		_cacheDir.SafeDeleteDir();
	}

	[TestMethod]
	public void Cache()
	{
		var cache = new InMemoryCompilerCache(TimeSpan.MaxValue);
		cache.Init();

		var formula = Compile("RI@FORTS - SBER@TQBR", cache);
		formula.Calculate([6, 4]).AssertEqual(2);

		cache.Count.AssertEqual(1);

		formula = Compile("RI@FORTS - SBER@TQBR", cache);
		formula.Calculate([6, 5]).AssertEqual(1);

		cache.Count.AssertEqual(1);

		cache.Clear();
		cache.Count.AssertEqual(0);
	}

	[TestMethod]
	public void FileCache()
	{
		var cache = new FileCompilerCache(_cacheDir, TimeSpan.MaxValue);
		cache.Init();

		Directory.GetFiles(_cacheDir).Length.AssertEqual(0);

		var formula = Compile("RI@FORTS - SBER@TQBR", cache);
		formula.Calculate([6, 4]).AssertEqual(2);

		cache.Count.AssertEqual(1);
		Directory.GetFiles(_cacheDir).Length.AssertEqual(1);

		formula = Compile("RI@FORTS - SBER@TQBR", cache);
		formula.Calculate([6, 5]).AssertEqual(1);

		cache.Count.AssertEqual(1);
		Directory.GetFiles(_cacheDir).Length.AssertEqual(1);

		cache.Clear();
		cache.Count.AssertEqual(0);

		Directory.Exists(_cacheDir).AssertFalse();
	}

	[TestMethod]
	public void Parse()
	{
		var formula = Compile("RI@FORTS - SBER@TQBR");
		formula.Calculate([6, 4]).AssertEqual(2);
	}

	[TestMethod]
	public void Parse2()
	{
		var formula = Compile("[RI@FORTS] * [SBER@TQBR]");
		formula.Calculate([6, 4]).AssertEqual(24);
	}

	[TestMethod]
	public void Parse3()
	{
		var formula = Compile("RI@FORTS+[SBER@TQBR]");
		formula.Calculate([6, 4]).AssertEqual(10);

		formula = Compile("RI@FORTS + [SBER@TQBR]");
		formula.Calculate([6, 4]).AssertEqual(10);

		formula = Compile("RI@FORTS+ [SBER@TQBR]");
		formula.Calculate([6, 4]).AssertEqual(10);

		formula = Compile("RI@FORTS +[SBER@TQBR]");
		formula.Calculate([6, 4]).AssertEqual(10);

		formula = Compile("POW (RI@FORTS +[SBER@TQBR],2)");
		formula.Calculate([6, 4]).AssertEqual(100);

		formula = Compile("POW(RI@FORTS +SBER@TQBR,   2)");
		formula.Calculate([6, 4]).AssertEqual(100);

		formula = Compile("POW (RI@FORTS+SBER@TQBR,2)");
		formula.Calculate([6, 4]).AssertEqual(100);

		formula = Compile("POW   (RI@FORTS*SBER@TQBR,2)");
		formula.Calculate([6, 4]).AssertEqual(576);
	}

	[TestMethod]
	public void Parse4()
	{
		var formula = Compile("1*RI@FORTS + [@SBER@TQBR]");
		formula.Calculate([6, 4]).AssertEqual(10);

		formula = Compile("-RI@FORTS +[@SBER@TQBR]");
		formula.Calculate([6, 4]).AssertEqual(-2);
	}

	[TestMethod]
	public void Parse5()
	{
		var formula = Compile("[*RI@FORTS] + @SBER@TQBR");
		formula.Calculate([6, 4]).AssertEqual(10);
	}

	[TestMethod]
	public void Parse6()
	{
		var formula = Compile("[SPFB.SBRF@FORTS] + SBER@TQBR");
		formula.Calculate([6, 4]).AssertEqual(10);
	}

	[TestMethod]
	public void Parse7()
	{
		var formula = Compile("SPFB.SBRF@FORTS + SBER@TQBR");
		formula.Calculate([6, 4]).AssertEqual(10);
	}

	[TestMethod]
	public void Parse8()
	{
		var formula = Compile("SPFB.SBRF@FORTS + [.SBER@TQBR]");
		formula.Calculate([6, 4]).AssertEqual(10);
	}

	[TestMethod]
	public void Parse9()
	{
		var formula = Compile("SPFB.SBRF@FORTS + [.SBER@TQBR]");
		formula.Calculate([6, 4]).AssertEqual(10);
	}

	[TestMethod]
	public void Parse10()
	{
		var formula = Compile("#SPFB.SBRF@FORTS + [#.SBER@TQBR]");
		formula.Calculate([6, 4]).AssertEqual(10);
	}

	[TestMethod]
	public void Parse11()
	{
		var formula = Compile("#SPFB.SBRF@FORTS + #.SBER@TQBR");
		formula.Calculate([6, 4]).AssertEqual(10);
	}

	[TestMethod]
	public void Parse12()
	{
		var formula = Compile("#SPFB.SBRF@FORTS * 0.1m - #SPFB.SBRF@FORTS");
		formula.Calculate([6]).AssertEqual(-5.4m);
	}

	[TestMethod]
	public void ParseCurrency()
	{
		var formula = Compile("RI@FORTS / [AUD/CAD@DUKAS]");
		formula.Calculate([6, 4]).AssertEqual(1.5m);
	}

	[TestMethod]
	public void ParseCurrency2()
	{
		var formula = Compile("RI@FORTS / [AUD-CAD@DUKAS]");
		formula.Calculate([6, 4]).AssertEqual(1.5m);
	}

	[TestMethod]
	public void ParseAbs()
	{
		var formula = Compile("RI@FORTS - Abs(SBER@TQBR)");
		formula.Calculate([6, -4]).AssertEqual(2);
	}

	[TestMethod]
	public void ParseAbs2()
	{
		var formula = Compile("POW(abs@FORTS,  abs(SBER@TQBR))");
		formula.Calculate([2, -4]).AssertEqual(16);
	}

	[TestMethod]
	public void ParsePow()
	{
		var formula = Compile("POW(RI@FORTS,  Abs(SBER@TQBR))");
		formula.Calculate([2, -4]).AssertEqual(16);
	}

	[TestMethod]
	public void ParsePow2()
	{
		var formula = Compile("POW(RI@FORTS,  SBER@TQBR)");
		formula.Calculate([2, -4]).AssertEqual(0.0625m);
	}

	[TestMethod]
	public void ParsePow3()
	{
		var formula = Compile("POW(RI@FORTS*SBER@TQBR,1.0m/3)");
		formula.Calculate([2, 4.5m]).AssertEqual(2.0800838230519m);
	}

	[TestMethod]
	public void ParsePow4()
	{
		var formula = Compile("POW([RI@FORTS]*[SBER@TQBR],1m/3)");
		formula.Calculate([2, 4.5m]).AssertEqual(2.0800838230519m);
	}

	[TestMethod]
	public void ParsePow5()
	{
		var formula = Compile("POW(RI@FORTS*[SBER@TQBR],1m/3)");
		formula.Calculate([2, 4.5m]).AssertEqual(2.0800838230519m);
	}

	[TestMethod]
	public void ParsePow6()
	{
		var formula = Compile("pow(RI@FORTS*[SBER@TQBR],1m/3)");
		formula.Calculate([2, 4.5m]).AssertEqual(2.0800838230519m);
	}

	[TestMethod]
	public void ParsePow7()
	{
		var formula = Compile("Pow(RI@FORTS*[SBER@TQBR],1m/3)");
		formula.Calculate([2, 4.5m]).AssertEqual(2.0800838230519m);
	}

	[TestMethod]
	public void ParsePow9()
	{
		var formula = Compile("   Pow(RI@FORTS*[SBER@TQBR],1m/3) ");
		formula.Calculate([2, 4.5m]).AssertEqual(2.0800838230519m);
	}

	[TestMethod]
	public void ParsePow8()
	{
		var formula = Compile("Pow(RI@FORTS*[RI@FORTS],1m/RI@FORTS)");
		formula.Calculate([2]).AssertEqual(2);
	}

	[TestMethod]
	public void ParseFormula1()
	{
		var formula = Compile("x + y");
		formula.Calculate([2, -4]).AssertEqual(-2);
	}

	[TestMethod]
	public void ParseFormula2()
	{
		var formula = Compile("x * y");
		formula.Calculate([2, -4]).AssertEqual(-8);
	}

	[TestMethod]
	public void ParseFormula3()
	{
		var formula = Compile("x*y");
		formula.Calculate([2, -4]).AssertEqual(-8);
	}

	[TestMethod]
	public void ParseFormula4()
	{
		var formula = Compile("pow(x,abs(y))");
		formula.Calculate([2, -4]).AssertEqual(16);
	}

	[TestMethod]
	public void ParseFormula5()
	{
		var formula = Compile("(x+y)*X/3");
		formula.Calculate([2, 3]).AssertEqual(3.3333333333333333333333333333m);
	}

	[TestMethod]
	public void ParseFormula6()
	{
		var formula = Compile("(x+y)*5.2m/3");
		formula.Calculate([2, 3]).AssertEqual(8.666666666666666666666666667m);
	}

	[TestMethod]
	public void ParseFormula7()
	{
		var formula = Compile("((x+y)*5.2m/3) + abs(X)");
		formula.Calculate([2, 3]).AssertEqual(10.666666666666666666666666667m);
	}

	[TestMethod]
	public void ParseFormula8()
	{
		var formula = Compile("SBER@TQBR + 12334545");
		formula.Calculate([2, 12334545]).AssertEqual(2 + 12334545);
	}

	[TestMethod]
	public void ParseFormula9()
	{
		var formula = Compile("    x*   y");
		formula.Calculate([2, -4]).AssertEqual(-8);
	}

	private static Task<ExpressionFormula<bool>> CompileBool(string expression)
		=> _compiler.Compile<bool>(_context, expression);

	[TestMethod]
	public async Task Bool()
	{
		var formula = await CompileBool("C > O");
		formula.Calculate([6, 4]).AssertTrue();
		formula.Calculate([4, 6]).AssertFalse();
	}

	[TestMethod]
	public async Task BoolComples()
	{
		var formula = await CompileBool("(C > O) && (B < C)");
		formula.Calculate([6, 4, 3]).AssertTrue();
		formula.Calculate([4, 6, 8]).AssertFalse();
	}

	[TestMethod]
	public async Task BoolComples2()
	{
		var formula = await CompileBool("([C] > O) && ([B] < C)");
		formula.Calculate([6, 4, 3]).AssertTrue();
		formula.Calculate([4, 6, 8]).AssertFalse();
	}

	[TestMethod]
	public async Task BoolMath()
	{
		var formula = await CompileBool("pow(C,2) > sin(O)");
		formula.Calculate([6, 4]).AssertTrue();
		formula.Calculate([0.1m, 360m]).AssertFalse();
	}

	[TestMethod]
	public async Task BoolMath2()
	{
		var formula = await CompileBool("pow([C],2) > sin(O)");
		formula.Calculate([6, 4]).AssertTrue();
		formula.Calculate([0.1m, 360m]).AssertFalse();
	}

	[TestMethod]
	public async Task BoolMath3()
	{
		var formula = await CompileBool("    Pow(   [C],2) > sin(    O)  ");
		formula.Calculate([6, 4]).AssertTrue();
		formula.Calculate([0.1m, 360m]).AssertFalse();
	}

	[TestMethod]
	public async Task BoolMath4()
	{
		var formula = await CompileBool("    Pow(   (C*O)/O,2) > sin(    O)  ");
		formula.Calculate([6, 4]).AssertTrue();
		formula.Calculate([0.1m, 360m]).AssertFalse();
	}

	[TestMethod]
	public async Task BoolMath5()
	{
		var formula = await CompileBool("    Pow(   C*(O/O),2) > sin(    O)  ");
		formula.Calculate([6, 4]).AssertTrue();
		formula.Calculate([0.1m, 360m]).AssertFalse();
	}

	[TestMethod]
	public void Round()
	{
		var formula = Compile(" ROUND(a, b) ");
		formula.Calculate([6, 4]).AssertEqual(6);
		formula.Calculate([0.01m, 2]).AssertEqual(0.01m);
		formula.Calculate([0.011m, 2]).AssertEqual(0.01m);
	}
}