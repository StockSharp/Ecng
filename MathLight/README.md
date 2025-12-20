# Ecng.MathLight

A lightweight mathematics and linear algebra library for .NET providing essential statistical functions, matrix operations, and polynomial fitting without heavy dependencies.

## Purpose

Ecng.MathLight is designed for applications that need basic mathematical operations, linear algebra, and statistical functions without the overhead of comprehensive math libraries. It's particularly useful for financial calculations, data analysis, and scientific computing where a minimal footprint is desired.

## Key Features

- Normal distribution calculations (cumulative distribution function)
- Matrix operations (transpose, multiplication, row/column extraction)
- LU decomposition for solving systems of linear equations
- Polynomial fitting and evaluation
- No external dependencies beyond .NET standard libraries
- Optimized for performance with aggressive inlining

## Installation

Add a reference to the `Ecng.MathLight` package in your project.

## Normal Distribution

The `Normal` class provides methods for working with the standard normal distribution.

### Cumulative Distribution Function (CDF)

Calculate the probability that a standard normal random variable is less than or equal to a given value.

```csharp
using Ecng.MathLight;

// Calculate P(X <= x) for standard normal distribution (mean=0, stddev=1)
double x = 1.96;
double probability = Normal.CumulativeDistribution(x);
Console.WriteLine($"P(X <= {x}) = {probability:F6}"); // 0.975002

// Common use cases
double p_0 = Normal.CumulativeDistribution(0);     // 0.5 (50th percentile)
double p_1 = Normal.CumulativeDistribution(1);     // 0.841345 (84.13%)
double p_minus1 = Normal.CumulativeDistribution(-1); // 0.158655 (15.87%)

// Calculate probability in a range: P(a < X < b)
double a = -1.0;
double b = 1.0;
double prob_range = Normal.CumulativeDistribution(b) - Normal.CumulativeDistribution(a);
Console.WriteLine($"P({a} < X < {b}) = {prob_range:F4}"); // 0.6827 (68.27%)
```

### Polynomial Evaluation

The `Evaluate` method can be used for evaluating polynomials, though it's primarily used internally.

```csharp
using Ecng.MathLight;

// Evaluate polynomial: 1 + 2x + 3x^2 at x = 2
// Result = 1 + 2*2 + 3*2^2 = 1 + 4 + 12 = 17
double[] coefficients = { 1, 2, 3 }; // constant term first
double x = 2.0;
double result = Normal.Evaluate(x, coefficients);
Console.WriteLine($"Polynomial value at x={x}: {result}"); // 17
```

### Statistical Applications

```csharp
using Ecng.MathLight;

// Calculate z-score probability for investment returns
public double CalculateReturnProbability(double expectedReturn, double stdDev, double threshold)
{
    // Standardize the threshold
    double z = (threshold - expectedReturn) / stdDev;

    // Calculate probability
    return Normal.CumulativeDistribution(z);
}

// Example: Stock with 10% expected return and 15% standard deviation
// What's the probability of returns below 0%?
double probLoss = CalculateReturnProbability(0.10, 0.15, 0.0);
Console.WriteLine($"Probability of loss: {probLoss:P2}"); // ~25.25%

// Calculate confidence intervals
public (double lower, double upper) GetConfidenceInterval(
    double mean, double stdDev, double confidence)
{
    // For 95% confidence, we need 2.5% and 97.5% percentiles
    // Using inverse: z-score for 95% is approximately 1.96
    double z = 1.96; // for 95% confidence

    if (confidence == 0.99)
        z = 2.576;
    else if (confidence == 0.90)
        z = 1.645;

    return (mean - z * stdDev, mean + z * stdDev);
}

var interval = GetConfidenceInterval(100, 15, 0.95);
Console.WriteLine($"95% CI: [{interval.lower:F2}, {interval.upper:F2}]");
```

## Matrix Operations

The `Matrix` class provides extension methods for 2D double arrays to perform common linear algebra operations.

### Creating and Inspecting Matrices

```csharp
using Ecng.MathLight.LinearAlgebra;

// Create a matrix
double[,] matrix = new double[3, 3]
{
    { 1, 2, 3 },
    { 4, 5, 6 },
    { 7, 8, 9 }
};

// Get dimensions
int rows = matrix.Rows();    // 3
int cols = matrix.Cols();    // 3

Console.WriteLine($"Matrix is {rows}x{cols}");
```

### Matrix Transpose

```csharp
using Ecng.MathLight.LinearAlgebra;

double[,] matrix = new double[2, 3]
{
    { 1, 2, 3 },
    { 4, 5, 6 }
};

// Transpose the matrix
double[,] transposed = matrix.Transpose();
// Result: 3x2 matrix
// { 1, 4 }
// { 2, 5 }
// { 3, 6 }

Console.WriteLine($"Original: {matrix.Rows()}x{matrix.Cols()}");
Console.WriteLine($"Transposed: {transposed.Rows()}x{transposed.Cols()}");
```

### Matrix Multiplication

```csharp
using Ecng.MathLight.LinearAlgebra;

double[,] matrixA = new double[2, 3]
{
    { 1, 2, 3 },
    { 4, 5, 6 }
};

double[,] matrixB = new double[3, 2]
{
    { 7, 8 },
    { 9, 10 },
    { 11, 12 }
};

// Multiply matrices: A * B
double[,] product = matrixA.Product(matrixB);
// Result: 2x2 matrix
// { 58,  64 }
// { 139, 154 }

Console.WriteLine($"Product matrix: {product.Rows()}x{product.Cols()}");
```

### Identity Matrix

```csharp
using Ecng.MathLight.LinearAlgebra;

// Create an identity matrix
double[,] identity = Matrix.Identity(3);
// Result:
// { 1, 0, 0 }
// { 0, 1, 0 }
// { 0, 0, 1 }

// Set an existing matrix to identity
double[,] matrix = new double[4, 4];
matrix.Identity();
// Now matrix is a 4x4 identity matrix
```

### Accessing Rows and Columns

```csharp
using Ecng.MathLight.LinearAlgebra;
using System.Linq;

double[,] matrix = new double[3, 3]
{
    { 1, 2, 3 },
    { 4, 5, 6 },
    { 7, 8, 9 }
};

// Get a column as IEnumerable
var column1 = matrix.GetColumn(1).ToArray(); // { 2, 5, 8 }

// Get a row as IEnumerable
var row2 = matrix.GetRow(2).ToArray(); // { 7, 8, 9 }

// Calculate column sum
double columnSum = matrix.GetColumn(0).Sum(); // 1 + 4 + 7 = 12

// Calculate row average
double rowAverage = matrix.GetRow(1).Average(); // (4 + 5 + 6) / 3 = 5
```

## LU Decomposition

The `LUDecomposition` class performs LU decomposition and solves systems of linear equations.

### Basic LU Decomposition

```csharp
using Ecng.MathLight.LinearAlgebra;

// Create a square matrix
double[,] matrix = new double[3, 3]
{
    { 2, 1, 1 },
    { 4, -6, 0 },
    { -2, 7, 2 }
};

// Perform LU decomposition
var lu = new LUDecomposition(matrix);

// Access L and U matrices
double[,] L = lu.L; // Lower triangular matrix
double[,] U = lu.U; // Upper triangular matrix

Console.WriteLine($"L matrix: {L.Rows()}x{L.Cols()}");
Console.WriteLine($"U matrix: {U.Rows()}x{U.Cols()}");
```

### Solving Linear Equations (Ax = b)

```csharp
using Ecng.MathLight.LinearAlgebra;

// Solve system: Ax = b
// 2x + y + z = 5
// 4x - 6y = -2
// -2x + 7y + 2z = 9

double[,] A = new double[3, 3]
{
    { 2, 1, 1 },
    { 4, -6, 0 },
    { -2, 7, 2 }
};

double[] b = { 5, -2, 9 };

var lu = new LUDecomposition(A);
double[] solution = lu.Solve(b);

Console.WriteLine($"x = {solution[0]:F4}");
Console.WriteLine($"y = {solution[1]:F4}");
Console.WriteLine($"z = {solution[2]:F4}");
```

### Solving Multiple Systems

```csharp
using Ecng.MathLight.LinearAlgebra;

// Solve multiple systems with the same coefficient matrix
double[,] A = new double[3, 3]
{
    { 2, 1, 1 },
    { 4, -6, 0 },
    { -2, 7, 2 }
};

// Multiple right-hand sides (matrix form)
double[,] B = new double[3, 2]
{
    { 5, 1 },
    { -2, 2 },
    { 9, 3 }
};

var lu = new LUDecomposition(A);
double[,] solutions = lu.Solve(B);

// Each column in solutions corresponds to one system
for (int col = 0; col < solutions.Cols(); col++)
{
    Console.WriteLine($"Solution {col + 1}:");
    for (int row = 0; row < solutions.Rows(); row++)
    {
        Console.WriteLine($"  x[{row}] = {solutions[row, col]:F4}");
    }
}
```

## Polynomial Fitting

The `PolyFit` class performs least-squares polynomial regression.

### Basic Polynomial Fitting

```csharp
using Ecng.MathLight.LinearAlgebra;

// Data points
double[] x = { 0, 1, 2, 3, 4 };
double[] y = { 1, 3, 7, 13, 21 };

// Fit a 2nd-degree polynomial (quadratic)
var polyFit = new PolyFit(x, y, order: 2);

// Get coefficients: y = c0 + c1*x + c2*x^2
double[] coefficients = polyFit.Coeff;
Console.WriteLine($"y = {coefficients[0]:F4} + {coefficients[1]:F4}*x + {coefficients[2]:F4}*x^2");
```

### Evaluating the Fitted Polynomial

```csharp
using Ecng.MathLight.LinearAlgebra;

// Fit polynomial to data
double[] x = { 0, 1, 2, 3, 4 };
double[] y = { 1, 3, 7, 13, 21 };
var polyFit = new PolyFit(x, y, order: 2);

// Predict values at new points
double[] newX = { 0.5, 1.5, 2.5, 3.5 };
double[] predictedY = polyFit.Fit(newX);

for (int i = 0; i < newX.Length; i++)
{
    Console.WriteLine($"f({newX[i]}) = {predictedY[i]:F4}");
}

// Evaluate at a single point
double singleX = 2.7;
double singleY = polyFit.Fit(new[] { singleX })[0];
Console.WriteLine($"f({singleX}) = {singleY:F4}");
```

### Real-World Example: Trend Analysis

```csharp
using Ecng.MathLight.LinearAlgebra;
using System;
using System.Linq;

public class TrendAnalyzer
{
    public (double[] coefficients, double[] fittedValues) FitTrend(
        double[] timePoints, double[] values, int degree)
    {
        // Fit polynomial to the data
        var polyFit = new PolyFit(timePoints, values, degree);

        // Get fitted values for all time points
        double[] fittedValues = polyFit.Fit(timePoints);

        return (polyFit.Coeff, fittedValues);
    }

    public double Forecast(double[] coefficients, double futureTime)
    {
        double result = 0;
        double xPower = 1;

        foreach (double coeff in coefficients)
        {
            result += coeff * xPower;
            xPower *= futureTime;
        }

        return result;
    }
}

// Usage: Analyze price trend
var analyzer = new TrendAnalyzer();

// Historical prices over 10 days
double[] days = Enumerable.Range(0, 10).Select(x => (double)x).ToArray();
double[] prices = { 100, 102, 105, 103, 107, 110, 108, 112, 115, 113 };

// Fit linear trend
var (coeffs, fitted) = analyzer.FitTrend(days, prices, degree: 1);
Console.WriteLine($"Trend: y = {coeffs[0]:F2} + {coeffs[1]:F2}*x");

// Forecast next 3 days
for (int i = 10; i < 13; i++)
{
    double forecast = analyzer.Forecast(coeffs, i);
    Console.WriteLine($"Day {i}: ${forecast:F2}");
}

// Calculate R-squared (goodness of fit)
double mean = prices.Average();
double ssTotal = prices.Sum(y => Math.Pow(y - mean, 2));
double ssResidual = prices.Zip(fitted, (actual, fit) =>
    Math.Pow(actual - fit, 2)).Sum();
double rSquared = 1 - (ssResidual / ssTotal);
Console.WriteLine($"R² = {rSquared:F4}");
```

### Polynomial Fitting for Different Orders

```csharp
using Ecng.MathLight.LinearAlgebra;

double[] x = { 0, 1, 2, 3, 4, 5 };
double[] y = { 1, 2.5, 5, 8.5, 13, 18.5 };

// Linear fit (1st order)
var linear = new PolyFit(x, y, order: 1);
Console.WriteLine($"Linear: y = {linear.Coeff[0]:F2} + {linear.Coeff[1]:F2}*x");

// Quadratic fit (2nd order)
var quadratic = new PolyFit(x, y, order: 2);
Console.WriteLine($"Quadratic: y = {quadratic.Coeff[0]:F2} + {quadratic.Coeff[1]:F2}*x + {quadratic.Coeff[2]:F2}*x²");

// Cubic fit (3rd order)
var cubic = new PolyFit(x, y, order: 3);
Console.WriteLine($"Cubic: y = {cubic.Coeff[0]:F2} + {cubic.Coeff[1]:F2}*x + {cubic.Coeff[2]:F2}*x² + {cubic.Coeff[3]:F2}*x³");
```

## Advanced Examples

### Portfolio Risk Calculation

```csharp
using Ecng.MathLight;

public class PortfolioRisk
{
    public double CalculateVaR(double portfolioValue, double expectedReturn,
        double volatility, double confidenceLevel)
    {
        // Value at Risk using normal distribution
        // VaR = Portfolio Value * (Expected Return - z * Volatility)

        // For 95% confidence, we need 5th percentile (lower tail)
        // z-score for 5% is approximately -1.645

        double z = -1.645; // for 95% confidence
        if (confidenceLevel == 0.99)
            z = -2.326;
        else if (confidenceLevel == 0.90)
            z = -1.282;

        double var = portfolioValue * (expectedReturn + z * volatility);

        return -var; // Return positive VaR
    }
}

// Usage
var riskCalc = new PortfolioRisk();
double var95 = riskCalc.CalculateVaR(
    portfolioValue: 1_000_000,
    expectedReturn: 0.08,
    volatility: 0.15,
    confidenceLevel: 0.95
);

Console.WriteLine($"95% VaR: ${var95:N2}");
```

### Curve Fitting for Interest Rates

```csharp
using Ecng.MathLight.LinearAlgebra;

public class YieldCurve
{
    private PolyFit _curveFit;

    public void Fit(double[] maturities, double[] yields, int degree = 3)
    {
        _curveFit = new PolyFit(maturities, yields, degree);
    }

    public double GetYield(double maturity)
    {
        return _curveFit.Fit(new[] { maturity })[0];
    }

    public double[] GetForwardCurve(double[] forwardMaturities)
    {
        return _curveFit.Fit(forwardMaturities);
    }
}

// Usage: Fit a yield curve
var yieldCurve = new YieldCurve();

double[] maturities = { 0.25, 0.5, 1, 2, 5, 10, 30 }; // Years
double[] yields = { 0.02, 0.025, 0.03, 0.035, 0.04, 0.042, 0.045 }; // Rates

yieldCurve.Fit(maturities, yields, degree: 3);

// Interpolate yields for arbitrary maturities
Console.WriteLine($"3-year yield: {yieldCurve.GetYield(3):P2}");
Console.WriteLine($"7-year yield: {yieldCurve.GetYield(7):P2}");
```

## Error Handling

```csharp
using Ecng.MathLight.LinearAlgebra;
using System;

try
{
    // Matrix must be square for LU decomposition
    double[,] nonSquare = new double[3, 2];
    var lu = new LUDecomposition(nonSquare); // Throws ArgumentException
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Error: {ex.Message}"); // "Matrix is not square"
}

try
{
    // Matrix must be non-singular
    double[,] singular = new double[2, 2]
    {
        { 1, 2 },
        { 2, 4 } // Second row is multiple of first
    };
    var lu = new LUDecomposition(singular); // Throws ArgumentException
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Error: {ex.Message}"); // "Matrix is singular"
}

try
{
    // Arrays must have same length for polynomial fitting
    double[] x = { 1, 2, 3 };
    double[] y = { 1, 2 }; // Different length
    var fit = new PolyFit(x, y, 2); // Throws ArgumentException
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
```

## Performance Considerations

- Matrix operations use `AggressiveInlining` for better performance
- LU decomposition uses efficient row swapping with `Buffer.BlockCopy`
- Normal distribution calculations use optimized polynomial approximations
- All operations work directly with `double[,]` arrays for minimal overhead

## Platform Support

- .NET Standard 2.0+
- .NET 6.0+
- .NET 10.0+

## Dependencies

No external dependencies beyond .NET standard libraries.

## Attribution

Linear algebra components are based on [LinearAlgebra by DanielBaumert](https://github.com/DanielBaumert/LinearAlgebra).
Normal distribution implementation is based on MathNet.Numerics algorithms.

## See Also

- For more comprehensive math operations, consider Math.NET Numerics
- For advanced statistics, consider Math.NET or Accord.NET
