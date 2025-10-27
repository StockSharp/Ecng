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
		(Math.Abs(cdf - 0.5) < 1e-6).AssertTrue();
	}

	[TestMethod]
	public void Normal_CumulativeDistribution_PositiveNegative()
	{
		var cdf1 = Normal.CumulativeDistribution(1);
		var cdfm1 = Normal.CumulativeDistribution(-1);
		(cdf1 > 0.5 && cdf1 < 1).AssertTrue();
		(cdfm1 < 0.5 && cdfm1 > 0).AssertTrue();
	}

	[TestMethod]
	public void Normal_CumulativeDistribution_Limits()
	{
		Normal.CumulativeDistribution(double.PositiveInfinity).AssertEqual(1);
		Normal.CumulativeDistribution(double.NegativeInfinity).AssertEqual(0);
		(double.IsNaN(Normal.CumulativeDistribution(double.NaN))).AssertTrue();
	}

	[TestMethod]
	public void Normal_CumulativeDistribution_Symmetry()
	{
		var x = 1.23;
		var cdf = Normal.CumulativeDistribution(x);
		var cdfNeg = Normal.CumulativeDistribution(-x);
		(Math.Abs((cdf + cdfNeg) - 1) < 1e-10).AssertTrue();
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
		((pf.Coeff[0] - 5).Abs() < 1e-10).AssertTrue();

		// Линейная функция
		var y2 = x.Select(v => 2 * v + 1).ToArray();
		var pf2 = new PolyFit(x, y2, 1);
		((pf2.Coeff[0] - 1).Abs() < 1e-10).AssertTrue();
		((pf2.Coeff[1] - 2).Abs() < 1e-10).AssertTrue();
	}

	[TestMethod]
	public void PolyFit_Cubic_And_Noisy()
	{
		// Кубическая функция
		var x = Enumerable.Range(-5, 11).Select(i => (double)i).ToArray();
		var y = x.Select(v => 1 - 2 * v + 0.5 * v * v - 0.1 * v * v * v).ToArray();
		var pf = new PolyFit(x, y, 3);
		((pf.Coeff[0] - 1).Abs() < 1e-8).AssertTrue();
		((pf.Coeff[1] + 2).Abs() < 1e-8).AssertTrue();
		((pf.Coeff[2] - 0.5).Abs() < 1e-8).AssertTrue();
		((pf.Coeff[3] + 0.1).Abs() < 1e-8).AssertTrue();

		// Шумовые данные (аппроксимация тренда)
		var rnd = new Random(42);
		var x2 = Enumerable.Range(0, 100).Select(i => (double)i).ToArray();
		var y2 = x2.Select(v => 3 * v - 7 + rnd.NextDouble() * 0.1).ToArray();
		var pf2 = new PolyFit(x2, y2, 1);
		((pf2.Coeff[1] - 3).Abs() < 0.01).AssertTrue();
		((pf2.Coeff[0] + 7).Abs() < 0.1).AssertTrue();
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
		(Normal.CumulativeDistribution(10) > 0.9999).AssertTrue();
		(Normal.CumulativeDistribution(-10) < 0.0001).AssertTrue();
	}

	[TestMethod]
	public void Normal_CumulativeDistribution_Monotonicity()
	{
		var z = Enumerable.Range(-100, 201).Select(i => i / 10.0).ToArray();
		var cdf = z.Select(Normal.CumulativeDistribution).ToArray();
		for (int i = 1; i < cdf.Length; i++)
			(cdf[i] >= cdf[i - 1]).AssertTrue();
	}
}