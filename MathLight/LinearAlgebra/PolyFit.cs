using System;
using System.Linq;

// https://github.com/DanielBaumert/LinearAlgebra

namespace Ecng.MathLight.LinearAlgebra
{
	/// <summary>
	/// Implements simple Matrix functions needed for polynomial fitting
	/// </summary>
	public class PolyFit
	{
		/// <summary>
		/// Coefficients of a polynomial starting at the constant coefficient 
		/// and ending with the coefficient of power to the chosen order.
		/// </summary>
		public double[] Coeff { get; private set; }

		/// <summary>
		/// Finds the coefficients of a polynomial p(x) of degree n that fits the data, 
		/// p(x(i)) to y(i), in a least squares sense. The result p is a row vector of 
		/// length n+1 containing the polynomial coefficients in incremental powers.
		/// </summary>
		/// <param name="x">x axis values</param>
		/// <param name="y">y axis values</param>
		/// <param name="order">polynomial order including the constant</param>
		public PolyFit(double[] x, double[] y, int order)
		{
			// increment the order to match matlab way
			double[,] matrixX = new double[x.Length, ++order];
			double[,] matrixY = new double[x.Length, 1];

			if (x.Length != y.Length)
			{
				throw new ArgumentException("x and y array lengths do not match!");
			}

			// copy y matrix
			for (int i = 0; i < y.Length; i++)
			{
				matrixY[i, 0] = y[i];
			}

			// create the X matrix
			for (int row = 0; row < x.Length; row++)
			{
				double nVal = 1.0;
				for (int col = 0; col < order; col++)
				{
					matrixX[row, col] = nVal;
					nVal *= x[row];
				}
			}

			var matrixXt = matrixX.Transpose();
			var matrixXtX = matrixXt.Product(matrixX);
			var matrixXtY = matrixXt.Product(matrixY);

			var lu = new LUDecomposition(matrixXtX);
			Coeff = lu.Solve(matrixXtY).GetColumn(0).ToArray();
		}

		/// <summary>
		/// Calculates the value of a polynomial of degree n evaluated at x. The input argument 
		/// pCoeff is a vector of length n+1 whose elements are the coefficients in incremental 
		/// powers of the polynomial to be evaluated.
		/// </summary>
		/// <param name="x">Array of x values</param>
		/// <returns>Array of fitted y values</returns>
		public double[] Fit(double[] x)
		{
			double[] y = new double[x.Length];
			int pos = 0;

			foreach (double xval in x)
			{
				double xcoeff = 1;
				foreach (double coeffval in Coeff)
				{
					// multiply current x by a coefficient
					y[pos] += coeffval * xcoeff;
					// power up the X
					xcoeff *= xval;
				}
				pos++;
			}

			return y;
		}
	}
}