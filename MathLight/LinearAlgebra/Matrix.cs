using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

// https://github.com/DanielBaumert/LinearAlgebra

namespace Ecng.MathLight.LinearAlgebra
{
    /// <summary>
    /// Implements simple Matrix functions needed for polynomial fitting
    /// </summary>
    public static class Matrix
    {
        /// <summary>
        /// Get the number of rows in a matrix
        /// </summary>
        /// <param name="self">2d double array representing a matrix</param>
        /// <returns>Number of rows in a matrix</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Rows(this double[,] self)
        {
            return self.GetLength(0);
        }

        /// <summary>
        /// Get the number of columns in a matrix
        /// </summary>
        /// <param name="self">2d double array representing a matrix</param>
        /// <returns>Number of columns in a matrix</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Cols(this double[,] self)
        {
            return self.GetLength(1);
        }

        /// <summary>
        /// Transpose a matrix.
        /// </summary>
        /// <param name="self">Matrix to be transposed</param>
        /// <returns>Transposed matrix</returns>
        public static double[,] Transpose(this double[,] self)
        {
            double[,] trans = new double[self.Cols(), self.Rows()];

            for (int row = 0; row < self.Rows(); row++)
            {
                for (int col = 0; col < self.Cols(); col++)
                {
                    trans[col, row] = self[row, col];
                }
            }

            return trans;
        }

        /// <summary>
        /// Calculate a dot product between two matrices.
        /// </summary>
        /// <param name="self">Matrix A</param>
        /// <param name="other">Matrix B</param>
        /// <returns>Dot product of A*B</returns>
        public static double[,] Product(this double[,] self, double[,] other)
        {
            if (self.Cols() != other.Rows())
            {
                throw new ArgumentException("Invalid matrix size");
            }

            double[,] prod = new double[self.Rows(), other.Cols()];

            for (int i = 0; i < self.Rows(); i++)
                for (int j = 0; j < other.Cols(); j++)
                    for (int k = 0; k < self.Cols(); k++)
                        prod[i, j] += self[i, k] * other[k, j];

            return prod;
        }

        /// <summary>
        /// Set the matrix so that it is an identity matrix.
        /// </summary>
        /// <param name="self">Matrix to be set to identity.</param>
        public static void Identity(this double[,] self)
        {
            if (self.Cols() != self.Rows())
            {
                throw new ArgumentException("Argument matrix is not square!");
            }
            Array.Clear(self, 0, self.Length);
            for (int i = 0; i < self.Rows(); i++)
            {
                self[i, i] = 1;
            }
        }

        /// <summary>
        /// Get the matrix column as IEnumerable.
        /// </summary>
        /// <param name="self">Matrix</param>
        /// <param name="col">Column to enumerate</param>
        /// <returns>Enumerated column from a given matrix.</returns>
        public static IEnumerable<double> GetColumn(this double[,] self, int col)
        {
            if (col < 0 || col >= self.Cols())
            {
                throw new ArgumentException("Specified column is out of range");
            }

            for (int row = 0; row < self.Rows(); row++)
            {
                yield return self[row, col];
            }
        }

        /// <summary>
        /// Get the matrix row as IEnumerable.
        /// </summary>
        /// <param name="self">Matrix</param>
        /// <param name="row">Row to enumerate</param>
        /// <returns>Enumerated row from a given matrix.</returns>
        public static IEnumerable<double> GetRow(this double[,] self, int row)
        {
            if (row < 0 || row >= self.Rows())
            {
                throw new ArgumentException("Specified row is out of range");
            }

            for (int col = 0; col < self.Cols(); col++)
            {
                yield return self[row, col];
            }
        }

        /// <param name="size">Number of rows/cols in a matrix</param>
        /// <returns>Identity matrix</returns>
        public static double[,] Identity(int size)
        {
            double[,] matrix = new double[size, size];
            for (int i = 0; i < size; i++)
            {
                matrix[i, i] = 1;
            }
            return matrix;
        }
    }
}
