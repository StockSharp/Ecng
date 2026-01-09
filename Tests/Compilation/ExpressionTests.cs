namespace Ecng.Tests.Compilation;

using Ecng.IO;
using Ecng.Compilation;
using Ecng.Compilation.Expressions;
using Ecng.Compilation.Roslyn;

using Nito.AsyncEx;

[TestClass]
public class ExpressionTests : BaseTestClass
{
	private static readonly ICompiler _compiler = new CSharpCompiler();
	private static readonly ICompilerContext _context = _compiler.CreateContext();

	private static readonly IFileSystem _fileSystem = LocalFileSystem.Instance;
	private const string _cacheDir = "asm_cache";

	private static ExpressionFormula<decimal> Compile(string expression, ICompilerCache cache = default)
		=> AsyncContext.Run(() => _compiler.Compile<decimal>(_context, _fileSystem, expression, cache));

	[ClassInitialize]
	public static void Init(TestContext _)
	{
		_fileSystem.SafeDeleteDir(_cacheDir);
	}

	[TestMethod]
	public async Task Cache()
	{
		var cache = new CompilerCache(new MemoryFileSystem(), "compiler-cache", TimeSpan.MaxValue);
		await cache.InitAsync(CancellationToken);

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
	public async Task FileCache()
	{
		var fs = _fileSystem;
		var cache = new CompilerCache(fs, _cacheDir, TimeSpan.MaxValue);
		await cache.InitAsync(CancellationToken);

		fs.EnumerateFiles(_cacheDir).Count().AssertEqual(0);

		var formula = Compile("RI@FORTS - SBER@TQBR", cache);
		formula.Calculate([6, 4]).AssertEqual(2);

		cache.Count.AssertEqual(1);
		fs.EnumerateFiles(_cacheDir).Count().AssertEqual(1);

		formula = Compile("RI@FORTS - SBER@TQBR", cache);
		formula.Calculate([6, 5]).AssertEqual(1);

		cache.Count.AssertEqual(1);
		fs.EnumerateFiles(_cacheDir).Count().AssertEqual(1);

		cache.Clear();
		cache.Count.AssertEqual(0);

		fs.DirectoryExists(_cacheDir).AssertFalse();
	}

	[TestMethod]
	public void Calculate_Subtraction()
	{
		var formula = Compile("RI@FORTS - SBER@TQBR");
		formula.Calculate([6, 4]).AssertEqual(2);
	}

	[TestMethod]
	public void Calculate_Multiplication()
	{
		var formula = Compile("[RI@FORTS] * [SBER@TQBR]");
		formula.Calculate([6, 4]).AssertEqual(24);
	}

	[TestMethod]
	public void Calculate_Addition_VariousSyntax()
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
	public void Calculate_UnaryNegation()
	{
		var formula = Compile("1*RI@FORTS + [@SBER@TQBR]");
		formula.Calculate([6, 4]).AssertEqual(10);

		formula = Compile("-RI@FORTS +[@SBER@TQBR]");
		formula.Calculate([6, 4]).AssertEqual(-2);
	}

	[TestMethod]
	public void Calculate_SpecialPrefixes_Asterisk()
	{
		var formula = Compile("[*RI@FORTS] + @SBER@TQBR");
		formula.Calculate([6, 4]).AssertEqual(10);
	}

	[TestMethod]
	public void Calculate_DotInIdentifier_Bracketed()
	{
		var formula = Compile("[SPFB.SBRF@FORTS] + SBER@TQBR");
		formula.Calculate([6, 4]).AssertEqual(10);
	}

	[TestMethod]
	public void Calculate_DotInIdentifier_Unbracketed()
	{
		var formula = Compile("SPFB.SBRF@FORTS + SBER@TQBR");
		formula.Calculate([6, 4]).AssertEqual(10);
	}

	[TestMethod]
	public void Calculate_DotPrefix_InBracket()
	{
		var formula = Compile("SPFB.SBRF@FORTS + [.SBER@TQBR]");
		formula.Calculate([6, 4]).AssertEqual(10);
	}

	[TestMethod]
	public void Calculate_DotPrefix_Variant()
	{
		var formula = Compile("SPFB.SBRF@FORTS + [.SBER@TQBR]");
		formula.Calculate([6, 4]).AssertEqual(10);
	}

	[TestMethod]
	public void Calculate_HashPrefix_Bracketed()
	{
		var formula = Compile("#SPFB.SBRF@FORTS + [#.SBER@TQBR]");
		formula.Calculate([6, 4]).AssertEqual(10);
	}

	[TestMethod]
	public void Calculate_HashPrefix_Unbracketed()
	{
		var formula = Compile("#SPFB.SBRF@FORTS + #.SBER@TQBR");
		formula.Calculate([6, 4]).AssertEqual(10);
	}

	[TestMethod]
	public void Calculate_DecimalLiteral_SingleVariable()
	{
		var formula = Compile("#SPFB.SBRF@FORTS * 0.1m - #SPFB.SBRF@FORTS");
		formula.Calculate([6]).AssertEqual(-5.4m);
	}

	[TestMethod]
	public void Calculate_CurrencyPair_SlashSeparator()
	{
		var formula = Compile("RI@FORTS / [AUD/CAD@DUKAS]");
		formula.Calculate([6, 4]).AssertEqual(1.5m);
	}

	[TestMethod]
	public void Calculate_CurrencyPair_DashSeparator()
	{
		var formula = Compile("RI@FORTS / [AUD-CAD@DUKAS]");
		formula.Calculate([6, 4]).AssertEqual(1.5m);
	}

	[TestMethod]
	public void Calculate_AbsFunction()
	{
		var formula = Compile("RI@FORTS - Abs(SBER@TQBR)");
		formula.Calculate([6, -4]).AssertEqual(2);
	}

	[TestMethod]
	public void Calculate_AbsAsIdentifierPart()
	{
		var formula = Compile("POW(abs@FORTS,  abs(SBER@TQBR))");
		formula.Calculate([2, -4]).AssertEqual(16);
	}

	[TestMethod]
	public void Calculate_PowWithAbs()
	{
		var formula = Compile("POW(RI@FORTS,  Abs(SBER@TQBR))");
		formula.Calculate([2, -4]).AssertEqual(16);
	}

	[TestMethod]
	public void Calculate_PowWithNegativeExponent()
	{
		var formula = Compile("POW(RI@FORTS,  SBER@TQBR)");
		formula.Calculate([2, -4]).AssertEqual(0.0625m);
	}

	[TestMethod]
	public void Calculate_PowWithFractionalExponent()
	{
		var formula = Compile("POW(RI@FORTS*SBER@TQBR,1.0m/3)");
		formula.Calculate([2, 4.5m]).AssertEqual(2.0800838230519m);
	}

	[TestMethod]
	public void Calculate_PowWithBrackets_Variant1()
	{
		var formula = Compile("POW([RI@FORTS]*[SBER@TQBR],1m/3)");
		formula.Calculate([2, 4.5m]).AssertEqual(2.0800838230519m);
	}

	[TestMethod]
	public void Calculate_PowWithBrackets_Variant2()
	{
		var formula = Compile("POW(RI@FORTS*[SBER@TQBR],1m/3)");
		formula.Calculate([2, 4.5m]).AssertEqual(2.0800838230519m);
	}

	[TestMethod]
	public void Calculate_Pow_LowerCase()
	{
		var formula = Compile("pow(RI@FORTS*[SBER@TQBR],1m/3)");
		formula.Calculate([2, 4.5m]).AssertEqual(2.0800838230519m);
	}

	[TestMethod]
	public void Calculate_Pow_MixedCase()
	{
		var formula = Compile("Pow(RI@FORTS*[SBER@TQBR],1m/3)");
		formula.Calculate([2, 4.5m]).AssertEqual(2.0800838230519m);
	}

	[TestMethod]
	public void Calculate_Pow_WithWhitespace()
	{
		var formula = Compile("   Pow(RI@FORTS*[SBER@TQBR],1m/3) ");
		formula.Calculate([2, 4.5m]).AssertEqual(2.0800838230519m);
	}

	[TestMethod]
	public void Calculate_Pow_SameVariableMultipleTimes()
	{
		var formula = Compile("Pow(RI@FORTS*[RI@FORTS],1m/RI@FORTS)");
		formula.Calculate([2]).AssertEqual(2);
	}

	[TestMethod]
	public void Calculate_SimpleVariables_Addition()
	{
		var formula = Compile("x + y");
		formula.Calculate([2, -4]).AssertEqual(-2);
	}

	[TestMethod]
	public void Calculate_SimpleVariables_Multiplication()
	{
		var formula = Compile("x * y");
		formula.Calculate([2, -4]).AssertEqual(-8);
	}

	[TestMethod]
	public void Calculate_SimpleVariables_MultiplicationNoSpaces()
	{
		var formula = Compile("x*y");
		formula.Calculate([2, -4]).AssertEqual(-8);
	}

	[TestMethod]
	public void Calculate_SimpleVariables_PowAndAbs()
	{
		var formula = Compile("pow(x,abs(y))");
		formula.Calculate([2, -4]).AssertEqual(16);
	}

	[TestMethod]
	public void Calculate_SimpleVariables_ComplexExpression()
	{
		var formula = Compile("(x+y)*X/3");
		formula.Calculate([2, 3]).AssertEqual(3.3333333333333333333333333333m);
	}

	[TestMethod]
	public void Calculate_SimpleVariables_DecimalMultiplier()
	{
		var formula = Compile("(x+y)*5.2m/3");
		formula.Calculate([2, 3]).AssertEqual(8.666666666666666666666666667m);
	}

	[TestMethod]
	public void Calculate_SimpleVariables_NestedParentheses()
	{
		var formula = Compile("((x+y)*5.2m/3) + abs(X)");
		formula.Calculate([2, 3]).AssertEqual(10.666666666666666666666666667m);
	}

	[TestMethod]
	public void Calculate_NumericLiteral_Addition()
	{
		var formula = Compile("SBER@TQBR + 12334545");
		formula.Calculate([2, 12334545]).AssertEqual(2 + 12334545);
	}

	[TestMethod]
	public void Calculate_SimpleVariables_IrregularWhitespace()
	{
		var formula = Compile("    x*   y");
		formula.Calculate([2, -4]).AssertEqual(-8);
	}

	private static Task<ExpressionFormula<bool>> CompileBool(string expression)
		=> _compiler.Compile<bool>(_context, _fileSystem, expression);

	[TestMethod]
	public async Task Bool()
	{
		var formula = await CompileBool("C > O");
		formula.Calculate([6, 4]).AssertTrue();
		formula.Calculate([4, 6]).AssertFalse();
	}

	[TestMethod]
	public async Task BoolComplex()
	{
		var formula = await CompileBool("(C > O) && (B < C)");
		formula.Calculate([6, 4, 3]).AssertTrue();
		formula.Calculate([4, 6, 8]).AssertFalse();
	}

	[TestMethod]
	public async Task BoolComplex2()
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

	[TestMethod]
	public async Task BoolLessThan()
	{
		var formula = await CompileBool("C < O");
		formula.Calculate([4, 6]).AssertTrue();
		formula.Calculate([6, 4]).AssertFalse();
		formula.Calculate([5, 5]).AssertFalse();
	}

	[TestMethod]
	public async Task BoolGreaterOrEqual()
	{
		var formula = await CompileBool("C >= O");
		formula.Calculate([6, 4]).AssertTrue();
		formula.Calculate([5, 5]).AssertTrue();
		formula.Calculate([4, 6]).AssertFalse();
	}

	[TestMethod]
	public async Task BoolLessOrEqual()
	{
		var formula = await CompileBool("C <= O");
		formula.Calculate([4, 6]).AssertTrue();
		formula.Calculate([5, 5]).AssertTrue();
		formula.Calculate([6, 4]).AssertFalse();
	}

	[TestMethod]
	public async Task BoolEqual()
	{
		var formula = await CompileBool("C == O");
		formula.Calculate([5, 5]).AssertTrue();
		formula.Calculate([4, 6]).AssertFalse();
	}

	[TestMethod]
	public async Task BoolNotEqual()
	{
		var formula = await CompileBool("C != O");
		formula.Calculate([4, 6]).AssertTrue();
		formula.Calculate([5, 5]).AssertFalse();
	}

	[TestMethod]
	public async Task BoolOr()
	{
		var formula = await CompileBool("(C > O) || (B > O)");
		formula.Calculate([6, 4, 3]).AssertTrue();
		formula.Calculate([3, 4, 6]).AssertTrue();
		formula.Calculate([3, 4, 2]).AssertFalse();
	}

	[TestMethod]
	public async Task BoolNot()
	{
		var formula = await CompileBool("!(C > O)");
		formula.Calculate([4, 6]).AssertTrue();
		formula.Calculate([6, 4]).AssertFalse();
	}

	[DataTestMethod]
	[DataRow("x * 2", 10, 20, DisplayName = "Integer literal")]
	[DataRow("x * 0.5", 10, 5, DisplayName = "Decimal without suffix")]
	[DataRow("x * 0.5m", 10, 5, DisplayName = "Decimal with suffix")]
	public void DecimalMultiply(string expression, int input, int expected)
	{
		var formula = Compile(expression);
		formula.Calculate([(decimal)input]).AssertEqual((decimal)expected);
	}

	[DataTestMethod]
	[DataRow("x + 10", 5, 15, DisplayName = "Integer addition")]
	[DataRow("x + 10.5", 5, 15.5, DisplayName = "Decimal addition without suffix")]
	[DataRow("x + 10.5m", 5, 15.5, DisplayName = "Decimal addition with suffix")]
	public void DecimalAddition(string expression, int input, double expected)
	{
		var formula = Compile(expression);
		formula.Calculate([(decimal)input]).AssertEqual((decimal)expected);
	}

	[DataTestMethod]
	[DataRow("abs(x - 10)", 7, 3, DisplayName = "abs with integer")]
	[DataRow("abs(x - 10.5)", 7, 3.5, DisplayName = "abs with decimal")]
	[DataRow("floor(x + 0.7)", 10, 10, DisplayName = "floor with decimal")]
	[DataRow("ceiling(x + 0.3)", 10, 11, DisplayName = "ceiling with decimal")]
	[DataRow("round(x * 0.333, 2)", 10, 3.33, DisplayName = "round with decimal")]
	public void DecimalFunctions(string expression, int input, double expected)
	{
		var formula = Compile(expression);
		formula.Calculate([(decimal)input]).AssertEqual((decimal)expected);
	}

	[DataTestMethod]
	[DataRow("pow(x, 2)", 3, 9, DisplayName = "pow with integer exponent")]
	[DataRow("pow(x, 2.0)", 3, 9, DisplayName = "pow with decimal exponent")]
	[DataRow("sqrt(x)", 16, 4, DisplayName = "sqrt")]
	[DataRow("pow(x, 0.5)", 16, 4, DisplayName = "pow as sqrt")]
	public void DecimalPowSqrt(string expression, int input, int expected)
	{
		var formula = Compile(expression);
		formula.Calculate([(decimal)input]).AssertEqual((decimal)expected);
	}

	[TestMethod]
	public void DecimalComplexFormula()
	{
		// abs(x - 10.5) + round(y * 0.25, 2) + max(z, 1.0)
		var formula = Compile("abs(x - 10.5) + round(y * 0.25, 2) + max(z, 1.0)");
		// abs(10 - 10.5) + round(8 * 0.25, 2) + max(0.5, 1.0) = 0.5 + 2 + 1 = 3.5
		formula.Calculate([10m, 8m, 0.5m]).AssertEqual(3.5m);
	}

	[TestMethod]
	public void DecimalTrigonometry()
	{
		// sin(0) + cos(0) = 0 + 1 = 1
		var formula = Compile("sin(x * 3.14159) + cos(y * 0.5)");
		formula.Calculate([0m, 0m]).AssertEqual(1m);
	}

	[TestMethod]
	public void DecimalMultipleVariables()
	{
		// x * 0.5 + y * 0.25 + z * 0.125 = 5 + 5 + 5 = 15
		var formula = Compile("x * 0.5 + y * 0.25 + z * 0.125");
		formula.Calculate([10m, 20m, 40m]).AssertEqual(15m);
	}
}
