# Ecng.Drawing

A lightweight, cross-platform drawing primitives library for .NET applications. Provides essential graphics utilities, color handling, brush abstractions, and UI layout helpers without heavy dependencies.

## Table of Contents

- [Installation](#installation)
- [Key Features](#key-features)
- [API Reference](#api-reference)
  - [Color Conversions](#color-conversions)
  - [Brushes](#brushes)
  - [Layout and Alignment](#layout-and-alignment)
  - [Drawing Styles](#drawing-styles)
- [Usage Examples](#usage-examples)
- [Target Frameworks](#target-frameworks)

## Installation

Add a reference to the `Ecng.Drawing` project or NuGet package in your .NET application.

```xml
<ProjectReference Include="path\to\Ecng.Drawing\Drawing.csproj" />
```

## Key Features

- **Color Utilities**: Convert between ARGB integers, HTML color strings, and System.Drawing.Color
- **Brush Abstractions**: Solid and gradient brush implementations for graphics rendering
- **Layout Primitives**: Thickness, alignment enums for UI layout
- **Drawing Styles**: Comprehensive set of chart and visualization styles
- **Cross-Platform**: Supports .NET Standard 2.0, .NET 6.0, and .NET 10.0
- **Lightweight**: Minimal dependencies, no heavy graphics frameworks required

## API Reference

### Color Conversions

The `DrawingExtensions` class provides extension methods for working with colors.

#### ToColor(int argb)

Converts an ARGB integer to a `Color` object.

```csharp
int argbValue = -16776961; // Blue color
Color color = argbValue.ToColor();
```

#### ToColor(string htmlColor)

Converts an HTML color string to a `Color` object. Supports multiple formats:
- `#RRGGBB` - 6-digit hex (e.g., `#FF5733`)
- `#RGB` - 3-digit hex shorthand (e.g., `#F53`)
- Named colors (e.g., `"Red"`, `"LightGrey"`)

```csharp
Color red = "#FF0000".ToColor();
Color blue = "#00F".ToColor();
Color gray = "LightGrey".ToColor(); // Special case handling
```

#### ToHtml(Color color)

Converts a `Color` object to its HTML string representation.

```csharp
Color color = Color.FromArgb(255, 87, 51);
string htmlColor = color.ToHtml(); // Returns "#FF5733"

Color semiTransparent = Color.FromArgb(128, 255, 87, 51);
string htmlWithAlpha = semiTransparent.ToHtml(); // Returns "#FF573380"
```

### Brushes

Abstract brush classes for painting operations.

#### SolidBrush

A brush that paints with a single, solid color.

```csharp
using Ecng.Drawing;
using System.Drawing;

// Create a solid red brush
var redBrush = new SolidBrush(Color.Red);
Color brushColor = redBrush.Color;

// Create from HTML color
var blueBrush = new SolidBrush("#0000FF".ToColor());
```

#### LinearGradientBrush

A brush that paints with a gradient between multiple colors.

```csharp
using Ecng.Drawing;
using System.Drawing;

// Method 1: Using color array and rectangle
var colors = new[] { Color.Red, Color.Yellow, Color.Blue };
var rectangle = new Rectangle(0, 0, 100, 100);
var gradientBrush = new LinearGradientBrush(colors, rectangle);

// Method 2: Using two points and two colors
var point1 = new Point(0, 0);
var point2 = new Point(100, 100);
var twoColorGradient = new LinearGradientBrush(
    point1,
    point2,
    Color.White,
    Color.Black
);

// Access gradient properties
Color[] gradientColors = gradientBrush.LinearColors;
Rectangle bounds = gradientBrush.Rectangle;
```

### Layout and Alignment

#### Thickness

Represents the thickness of a frame around a rectangle (padding or margin).

```csharp
using Ecng.Drawing;

// Create uniform thickness
var uniformThickness = new Thickness(10, 10, 10, 10);

// Create non-uniform thickness (left, top, right, bottom)
var customThickness = new Thickness(5, 10, 5, 20);

// Access individual values
double leftPadding = customThickness.Left;     // 5
double topPadding = customThickness.Top;       // 10
double rightPadding = customThickness.Right;   // 5
double bottomPadding = customThickness.Bottom; // 20

// Modify thickness values
customThickness.Left = 15;
customThickness.Top = 15;
```

#### HorizontalAlignment

Defines horizontal positioning within a layout container.

```csharp
using Ecng.Drawing;

// Available alignment options
HorizontalAlignment leftAlign = HorizontalAlignment.Left;
HorizontalAlignment centerAlign = HorizontalAlignment.Center;
HorizontalAlignment rightAlign = HorizontalAlignment.Right;
HorizontalAlignment stretchAlign = HorizontalAlignment.Stretch;

// Usage in UI layout
void PositionElement(HorizontalAlignment alignment)
{
    switch (alignment)
    {
        case HorizontalAlignment.Left:
            // Align element to left
            break;
        case HorizontalAlignment.Center:
            // Center element
            break;
        case HorizontalAlignment.Right:
            // Align element to right
            break;
        case HorizontalAlignment.Stretch:
            // Stretch element to fill width
            break;
    }
}
```

#### VerticalAlignment

Defines vertical positioning within a layout container.

```csharp
using Ecng.Drawing;

// Available alignment options
VerticalAlignment topAlign = VerticalAlignment.Top;
VerticalAlignment centerAlign = VerticalAlignment.Center;
VerticalAlignment bottomAlign = VerticalAlignment.Bottom;
VerticalAlignment stretchAlign = VerticalAlignment.Stretch;

// Usage in UI layout
void PositionElement(VerticalAlignment alignment)
{
    switch (alignment)
    {
        case VerticalAlignment.Top:
            // Align element to top
            break;
        case VerticalAlignment.Center:
            // Center element vertically
            break;
        case VerticalAlignment.Bottom:
            // Align element to bottom
            break;
        case VerticalAlignment.Stretch:
            // Stretch element to fill height
            break;
    }
}
```

### Drawing Styles

The `DrawStyles` enum defines various visualization and charting styles.

```csharp
using Ecng.Drawing;

// Available drawing styles
DrawStyles lineStyle = DrawStyles.Line;              // Standard line
DrawStyles noGapLine = DrawStyles.NoGapLine;         // Line without gaps
DrawStyles stepLine = DrawStyles.StepLine;           // Stepped line
DrawStyles band = DrawStyles.Band;                   // Band/area between values
DrawStyles bandOneValue = DrawStyles.BandOneValue;   // Single-value range
DrawStyles dot = DrawStyles.Dot;                     // Dot/scatter plot
DrawStyles histogram = DrawStyles.Histogram;         // Histogram bars
DrawStyles bubble = DrawStyles.Bubble;               // Bubble chart
DrawStyles stackedBar = DrawStyles.StackedBar;       // Stacked bar chart
DrawStyles dashedLine = DrawStyles.DashedLine;       // Dashed line
DrawStyles area = DrawStyles.Area;                   // Filled area

// Usage example
void ApplyChartStyle(DrawStyles style)
{
    switch (style)
    {
        case DrawStyles.Line:
            // Render as continuous line
            break;
        case DrawStyles.Histogram:
            // Render as vertical bars
            break;
        case DrawStyles.Bubble:
            // Render as sized bubbles
            break;
        // ... handle other styles
    }
}
```

## Usage Examples

### Example 1: Color Manipulation and Conversion

```csharp
using Ecng.Drawing;
using System.Drawing;

public class ColorExample
{
    public void DemonstrateColorConversion()
    {
        // Convert HTML colors
        Color red = "#FF0000".ToColor();
        Color blue = "#00F".ToColor();
        Color custom = "#A52A2A".ToColor();

        // Convert to HTML
        string redHtml = red.ToHtml();        // "#FF0000"
        string blueHtml = blue.ToHtml();      // "#0000FF"

        // Work with ARGB integers
        int argbValue = -65536; // Red
        Color fromArgb = argbValue.ToColor();

        // Handle transparency
        Color transparent = Color.FromArgb(128, 255, 0, 0);
        string htmlWithAlpha = transparent.ToHtml(); // "#FF000080"
    }
}
```

### Example 2: Creating Custom Brushes

```csharp
using Ecng.Drawing;
using System.Drawing;

public class BrushExample
{
    public Brush CreateBackgroundBrush(bool useGradient)
    {
        if (useGradient)
        {
            // Create a gradient from top to bottom
            var topColor = "#2C3E50".ToColor();
            var bottomColor = "#4CA1AF".ToColor();

            return new LinearGradientBrush(
                new Point(0, 0),
                new Point(0, 100),
                topColor,
                bottomColor
            );
        }
        else
        {
            // Create a solid brush
            return new SolidBrush("#34495E".ToColor());
        }
    }

    public Brush CreateMultiColorGradient()
    {
        // Create a rainbow gradient
        var colors = new[]
        {
            Color.Red,
            Color.Orange,
            Color.Yellow,
            Color.Green,
            Color.Blue,
            Color.Purple
        };

        var bounds = new Rectangle(0, 0, 200, 50);
        return new LinearGradientBrush(colors, bounds);
    }
}
```

### Example 3: UI Layout with Alignment and Thickness

```csharp
using Ecng.Drawing;

public class LayoutExample
{
    public class ElementLayout
    {
        public Thickness Margin { get; set; }
        public Thickness Padding { get; set; }
        public HorizontalAlignment HorizontalAlignment { get; set; }
        public VerticalAlignment VerticalAlignment { get; set; }
    }

    public ElementLayout CreateButtonLayout()
    {
        return new ElementLayout
        {
            Margin = new Thickness(10, 5, 10, 5),
            Padding = new Thickness(15, 8, 15, 8),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    public ElementLayout CreatePanelLayout()
    {
        return new ElementLayout
        {
            Margin = new Thickness(0, 0, 0, 0),
            Padding = new Thickness(20, 20, 20, 20),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
    }
}
```

### Example 4: Chart Rendering with Drawing Styles

```csharp
using Ecng.Drawing;
using System.Drawing;

public class ChartExample
{
    public class ChartSeries
    {
        public string Name { get; set; }
        public DrawStyles Style { get; set; }
        public Brush Brush { get; set; }
        public double[] Data { get; set; }
    }

    public ChartSeries CreatePriceSeries()
    {
        return new ChartSeries
        {
            Name = "Price",
            Style = DrawStyles.Line,
            Brush = new SolidBrush("#3498DB".ToColor()),
            Data = new[] { 100.0, 102.5, 101.8, 103.2, 105.0 }
        };
    }

    public ChartSeries CreateVolumeSeries()
    {
        return new ChartSeries
        {
            Name = "Volume",
            Style = DrawStyles.Histogram,
            Brush = new SolidBrush("#95A5A6".ToColor()),
            Data = new[] { 1000000, 1200000, 950000, 1100000, 1300000 }
        };
    }

    public ChartSeries CreateTrendBand()
    {
        var colors = new[]
        {
            Color.FromArgb(50, 52, 152, 219),  // Transparent blue
            Color.FromArgb(50, 46, 204, 113)   // Transparent green
        };

        return new ChartSeries
        {
            Name = "Trend Band",
            Style = DrawStyles.Band,
            Brush = new LinearGradientBrush(
                colors,
                new Rectangle(0, 0, 100, 100)
            ),
            Data = new[] { 95.0, 97.5, 98.0, 99.5, 100.0 }
        };
    }
}
```

### Example 5: Complete UI Component

```csharp
using Ecng.Drawing;
using System.Drawing;

public class CustomPanel
{
    public Thickness Margin { get; set; }
    public Thickness Padding { get; set; }
    public HorizontalAlignment HorizontalAlignment { get; set; }
    public VerticalAlignment VerticalAlignment { get; set; }
    public Brush Background { get; set; }

    public static CustomPanel CreateStyledPanel()
    {
        // Create a gradient background
        var gradient = new LinearGradientBrush(
            new Point(0, 0),
            new Point(0, 200),
            "#ECF0F1".ToColor(),
            "#BDC3C7".ToColor()
        );

        return new CustomPanel
        {
            Margin = new Thickness(10, 10, 10, 10),
            Padding = new Thickness(20, 20, 20, 20),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            Background = gradient
        };
    }

    public static CustomPanel CreateAccentPanel()
    {
        return new CustomPanel
        {
            Margin = new Thickness(5, 5, 5, 5),
            Padding = new Thickness(15, 10, 15, 10),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Background = new SolidBrush("#E74C3C".ToColor())
        };
    }
}
```

## Target Frameworks

This library supports the following target frameworks:

- **.NET Standard 2.0**: Maximum compatibility with .NET Framework, .NET Core, and Xamarin
- **.NET 6.0**: Modern .NET with long-term support
- **.NET 10.0**: Latest .NET features and performance improvements

## Platform-Specific Notes

### .NET Standard 2.0

On .NET Standard 2.0, the library includes custom implementations for HTML color conversion that handle:
- Standard hex color formats (`#RGB`, `#RRGGBB`)
- Transparency in hex format (`#RRGGBBAA`)
- Special case for `LightGrey` vs `LightGray` naming differences

### .NET 6.0+

On .NET 6.0 and later, the library leverages the built-in `ColorTranslator` class for improved performance and compatibility.

## Best Practices

1. **Color Conversions**: Use the extension methods for consistent color handling across different representations
2. **Brush Lifetime**: Create brushes as needed and reuse them when possible to avoid unnecessary allocations
3. **Layout Values**: Use `Thickness` for consistent spacing and padding throughout your UI
4. **Drawing Styles**: Choose appropriate styles for your data visualization needs
5. **Alignment**: Combine `HorizontalAlignment` and `VerticalAlignment` for precise element positioning

## License

Part of the StockSharp/Ecng library collection.
