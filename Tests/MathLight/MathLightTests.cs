namespace Ecng.Tests.MathLight;

using Ecng.MathLight;
using Ecng.MathLight.LinearAlgebra;

[TestClass]
public class MathLightTests
{
	[TestMethod]
	public void Normal_Evaluate_Basic()
	{
		Normal.Evaluate(0).AssertEqual(0);
		Normal.Evaluate(1, 2).AssertEqual(2);
		Normal.Evaluate(2, 1, 2, 3).AssertEqual(17); // 1 + 2*2 + 3*4 = 17
	}

	[TestMethod]
	public void Normal_Evaluate_EmptyOrNull()
	{
		Normal.Evaluate(5).AssertEqual(0);
		Assert.ThrowsExactly<ArgumentNullException>(() => Normal.Evaluate(1, null));
	}

	[TestMethod]
	public void Normal_CumulativeDistribution_Zero()
	{
		var cdf = Normal.CumulativeDistribution(0);
		var diff = Math.Abs(cdf - 0.5);
		(diff < 1e-6).AssertTrue($"diff={diff} should be <1e-6");
	}

	[TestMethod]
	public void Normal_CumulativeDistribution_PositiveNegative()
	{
		var cdf1 = Normal.CumulativeDistribution(1);
		var cdfm1 = Normal.CumulativeDistribution(-1);
		(cdf1 > 0.5 && cdf1 < 1).AssertTrue($"cdf1={cdf1} should be >0.5 and <1");
		(cdfm1 < 0.5 && cdfm1 > 0).AssertTrue($"cdfm1={cdfm1} should be <0.5 and >0");
	}

	[TestMethod]
	public void Normal_CumulativeDistribution_Limits()
	{
		Normal.CumulativeDistribution(double.PositiveInfinity).AssertEqual(1);
		Normal.CumulativeDistribution(double.NegativeInfinity).AssertEqual(0);
		var nanResult = Normal.CumulativeDistribution(double.NaN);
		(double.IsNaN(nanResult)).AssertTrue($"nanResult should be NaN, but was {nanResult}");
	}

	[TestMethod]
	public void Normal_CumulativeDistribution_Symmetry()
	{
		var x = 1.23;
		var cdf = Normal.CumulativeDistribution(x);
		var cdfNeg = Normal.CumulativeDistribution(-x);
		var symDiff = Math.Abs((cdf + cdfNeg) - 1);
		(symDiff < 1e-10).AssertTrue($"symDiff={symDiff} should be <1e-10");
	}

	[TestMethod]
	public void Matrix_AllMethods_BasicAndLarge()
	{
		// Basic
		double[,] m = new double[3, 2];
		m.Rows().AssertEqual(3);
		m.Cols().AssertEqual(2);
		var t = m.Transpose();
		t.Rows().AssertEqual(2);
		t.Cols().AssertEqual(3);

		// Identity
		var id = new double[5, 5];
		id.Identity();
		for (int i = 0; i < 5; i++)
			for (int j = 0; j < 5; j++)
				id[i, j].AssertEqual(i == j ? 1 : 0);

		// Product
		double[,] a = new double[,] { { 1, 2 }, { 3, 4 } };
		double[,] b = new double[,] { { 2, 0 }, { 1, 2 } };
		var p = a.Product(b);
		p[0, 0].AssertEqual(4);
		p[0, 1].AssertEqual(4);
		p[1, 0].AssertEqual(10);
		p[1, 1].AssertEqual(8);

		// GetRow/GetColumn
		var row = a.GetRow(1).ToArray();
		row.AssertEqual([3.0, 4.0]);
		var col = a.GetColumn(0).ToArray();
		col.AssertEqual([1.0, 3.0]);

		// Large matrix
		int n = 50;
		var big = new double[n, n];
		for (int i = 0; i < n; i++)
			for (int j = 0; j < n; j++)
				big[i, j] = i == j ? 2 : 1;
		var bigT = big.Transpose();
		bigT.Rows().AssertEqual(n);
		bigT.Cols().AssertEqual(n);
	}

	[TestMethod]
	public void Matrix_EdgeCases_And_Exceptions()
	{
		// Несовместимые размеры для Product
		double[,] a = new double[2, 3];
		double[,] b = new double[4, 2];
		Assert.ThrowsExactly<ArgumentException>(() => a.Product(b));

		// Identity на не квадратной матрице
		var m = new double[2, 3];
		Assert.ThrowsExactly<ArgumentException>(m.Identity);

		// GetRow/GetColumn вне диапазона
		var mat = new double[2, 2];
		Assert.ThrowsExactly<ArgumentException>(() => mat.GetRow(-1).ToArray());
		Assert.ThrowsExactly<ArgumentException>(() => mat.GetColumn(2).ToArray());
	}

	[TestMethod]
	public void LUDecomposition_SingularMatrix_Throws()
	{
		// Сингулярная матрица (нулевая строка)
		double[,] m = { { 1, 2 }, { 0, 0 } };
		Assert.ThrowsExactly<ArgumentException>(() => new LUDecomposition(m));
	}

	[TestMethod]
	public void LUDecomposition_NonSquare_Throws()
	{
		double[,] m = new double[2, 3];
		Assert.ThrowsExactly<ArgumentException>(() => new LUDecomposition(m));
	}

	[TestMethod]
	public void PolyFit_Constant_And_Linear()
	{
		// Константа
		var x = Enumerable.Range(0, 10).Select(i => (double)i).ToArray();
		var y = Enumerable.Repeat(5.0, 10).ToArray();
		var pf = new PolyFit(x, y, 0);
		var coeff0Diff = (pf.Coeff[0] - 5).Abs();
		(coeff0Diff < 1e-10).AssertTrue($"coeff0Diff={coeff0Diff} should be <1e-10");

		// Линейная функция
		var y2 = x.Select(v => 2 * v + 1).ToArray();
		var pf2 = new PolyFit(x, y2, 1);
		var pf2Coeff0Diff = (pf2.Coeff[0] - 1).Abs();
		(pf2Coeff0Diff < 1e-10).AssertTrue($"pf2Coeff0Diff={pf2Coeff0Diff} should be <1e-10");
		var pf2Coeff1Diff = (pf2.Coeff[1] - 2).Abs();
		(pf2Coeff1Diff < 1e-10).AssertTrue($"pf2Coeff1Diff={pf2Coeff1Diff} should be <1e-10");
	}

	[TestMethod]
	public void PolyFit_Cubic_And_Noisy()
	{
		// Кубическая функция
		var x = Enumerable.Range(-5, 11).Select(i => (double)i).ToArray();
		var y = x.Select(v => 1 - 2 * v + 0.5 * v * v - 0.1 * v * v * v).ToArray();
		var pf = new PolyFit(x, y, 3);
		var c0Diff = (pf.Coeff[0] - 1).Abs();
		(c0Diff < 1e-8).AssertTrue($"c0Diff={c0Diff} should be <1e-8");
		var c1Diff = (pf.Coeff[1] + 2).Abs();
		(c1Diff < 1e-8).AssertTrue($"c1Diff={c1Diff} should be <1e-8");
		var c2Diff = (pf.Coeff[2] - 0.5).Abs();
		(c2Diff < 1e-8).AssertTrue($"c2Diff={c2Diff} should be <1e-8");
		var c3Diff = (pf.Coeff[3] + 0.1).Abs();
		(c3Diff < 1e-8).AssertTrue($"c3Diff={c3Diff} should be <1e-8");

		// Шумовые данные (аппроксимация тренда)
		var rnd = new Random(42);
		var x2 = Enumerable.Range(0, 100).Select(i => (double)i).ToArray();
		var y2 = x2.Select(v => 3 * v - 7 + rnd.NextDouble() * 0.1).ToArray();
		var pf2 = new PolyFit(x2, y2, 1);
		var pf2C1Diff = (pf2.Coeff[1] - 3).Abs();
		(pf2C1Diff < 0.01).AssertTrue($"pf2C1Diff={pf2C1Diff} should be <0.01");
		var pf2C0Diff = (pf2.Coeff[0] + 7).Abs();
		(pf2C0Diff < 0.1).AssertTrue($"pf2C0Diff={pf2C0Diff} should be <0.1");
	}

	[TestMethod]
	public void Normal_Evaluate_LongCoeffs_And_Zeros()
	{
		var coeffs = Enumerable.Range(0, 20).Select(i => (double)i).ToArray();
		Normal.Evaluate(0, coeffs).AssertEqual(0);
		Normal.Evaluate(1, coeffs).AssertEqual(coeffs.Sum());
		Normal.Evaluate(2, new double[10]).AssertEqual(0);
	}

	[TestMethod]
	public void Normal_CumulativeDistribution_Extremes()
	{
		Normal.CumulativeDistribution(100).AssertEqual(1);
		Normal.CumulativeDistribution(-100).AssertEqual(0);
		var cdf10 = Normal.CumulativeDistribution(10);
		(cdf10 > 0.9999).AssertTrue($"cdf10={cdf10} should be >0.9999");
		var cdfMinus10 = Normal.CumulativeDistribution(-10);
		(cdfMinus10 < 0.0001).AssertTrue($"cdfMinus10={cdfMinus10} should be <0.0001");
	}

	[TestMethod]
	public void Normal_CumulativeDistribution_Monotonicity()
	{
		var z = Enumerable.Range(-100, 201).Select(i => i / 10.0).ToArray();
		var cdf = z.Select(Normal.CumulativeDistribution).ToArray();
		for (int i = 1; i < cdf.Length; i++)
			(cdf[i] >= cdf[i - 1]).AssertTrue($"cdf[{i}]={cdf[i]} should be >= cdf[{i - 1}]={cdf[i - 1]}");
	}
}