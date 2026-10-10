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
  - [PNG Header](#png-header)
  - [Image Processing](#image-processing)
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
- **PNG Header**: Tell a PNG picture by its signature and read its size without decoding it
- **Image Processing**: Dimensions, downscaling, PNG conversion, and text watermarks
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

### PNG Header

The `PngHelper` class reads what a PNG picture states about itself in its header, without decoding the picture.
The size sits in the first 24 bytes, so the beginning of a picture is enough.

```csharp
using System.Drawing;
using Ecng.Drawing;

byte[] picture = await http.GetByteArrayAsync(url);

// By the signature every PNG picture starts with
bool isPng = picture.IsPng();

// Throws InvalidDataException if the data does not begin with a PNG header
Size size = picture.GetPngSize();

// The same without an exception
if (picture.TryGetPngSize(out var read))
    Console.WriteLine($"{read.Width}x{read.Height}");
```

Each of the three takes a `byte[]` or a `ReadOnlySpan<byte>`.

## Image Processing (fully managed C#)

The image pipeline is pure managed .NET and requires no third-party imaging packages, GDI+, SkiaSharp, P/Invoke, or native graphics codecs. Tested on Windows, Linux and macOS.

| Input | GetImageSize | ResizeImage | ConvertToPng | AddTextWatermark |
|---|---|---|---|---|
| PNG | Yes | PNG | PNG | PNG |
| APNG (animated PNG) | Yes | **APNG, all frames** | APNG unchanged | **APNG, all frames** |
| JPEG (8-bit baseline / progressive, CMYK, YCCK) | Yes | JPEG | PNG | PNG |
| BMP (indexed, RGB, bitfields and RLE) | Yes | BMP | PNG | BMP |
| GIF87a / GIF89a | Yes | **animated GIF, all frames** | **APNG, all frames** | **animated GIF, all frames** |

### Animated GIF and APNG

GIF processing preserves **every displayed frame**, its delay and the NETSCAPE looping setting (0 means infinite), and composites GIF subrectangles using transparent indices and disposal modes 0–3. Interlaced GIF frames, global/local color tables, and LZW are supported.

- `GetGifFrames()` returns a list of `(byte[] png, int delayMilliseconds)` for all displayed GIF frames. Each `png` is a *full-canvas* composited RGBA8 PNG.
- `GetGifLoopCount()` returns the GIF NETSCAPE repetition count (0 means infinite; -1 means no loop extension).
- `GetAnimationFrames()` and `GetAnimationLoopCount()` also work with **APNG** input.
- Resizing GIF/APNG resizes **every displayed frame**. Animation timings and looping remain unchanged.
- Adding a watermark to GIF/APNG draws the text **on every frame**, retaining the animation.
- `ConvertToPng` on GIF yields **APNG**, not a frozen first frame. APNG carries the complete animation, timings and loop metadata. Calling the other methods on that APNG is also animation-preserving.
- APNG decoding handles per-frame rectangles, SOURCE/OVER blending, and NONE/BACKGROUND/PREVIOUS disposal.

GIF's native palette stores at most **256 indexed colors per frame** and **binary** transparency. When re-encoding GIF, exact colors are preserved if a frame has no more than 255 different nontransparent RGB colors; more complex pictures are quantized to an RGB 3:3:2 palette, and alpha below 128 is made transparent. GIF's LZW encoder favors simple, deterministic interoperable output over maximum compression efficiency. APNG uses full lossless RGBA8; converting GIF to APNG does not introduce new quantization.

### BMP

The BMP decoder accepts OS/2 BITMAPCORE and Windows BITMAPINFO/V4/V5 DIB headers, top-down or bottom-up scanlines, indexed 1/4/8-bit color, RGB555/RGB565 and other nonoverlapping 16/32-bit bitfield masks, uncompressed 24/32-bit color, and BI_RLE4/BI_RLE8. BMP encoding for resizing and watermarking always emits a portable top-down **32-bit BGRA BITMAPV4HEADER** with explicit RGBA masks to retain alpha. Embedded JPEG/PNG compression inside BMP, RLE24, and exotic OS/2-only codecs are not supported.

### JPEG and PNG

- PNG: all legal color types and bit depths including grayscale, indexed transparency, 8/16-bit samples, all five filter types, and Adam7 interlacing. Input APNG is recognized and handled as animated rather than accidentally discarding frames.
- JPEG: 8-bit baseline and progressive Huffman encoding; grayscale, RGB/YCrCb, Adobe CMYK/YCCK and restart markers. Arithmetic-coded, lossless and 12-bit JPEG are not supported.
- JPEG downscaling returns managed baseline JPEG with fixed quantization/Huffman tables. There is no output quality setting yet.
- PNG processing preserves alpha using premultiplied color interpolation, and watermarks use managed TrueType rendering.
- Verdana itself is not distributed. The watermark renderer prefers installed Verdana, falls back to a system TrueType font or accepts the exact `.ttf` through `fontFilePath`.

```csharp
using Ecng.Drawing;

byte[] animatedGif = await File.ReadAllBytesAsync("animation.gif");
var size = animatedGif.GetImageSize();
var frames = animatedGif.GetGifFrames();
int loops = animatedGif.GetGifLoopCount(); // 0: infinite, -1: unspecified

byte[] smallerGif = animatedGif.ResizeImage(320, 240);      // Still animated GIF
byte[] markedGif = smallerGif.AddTextWatermark("StockSharp"); // Every frame
byte[] animatedPng = animatedGif.ConvertToPng();             // All frames in APNG
byte[] smallerApng = animatedPng.ResizeImage(320, 240);      // Still animated APNG

byte[] bmp = await File.ReadAllBytesAsync("picture.bmp");
byte[] markedBmp = bmp.AddTextWatermark("StockSharp");       // BMP output
```

### Testing and coverage

Dedicated [Drawing coverage CI](https://github.com/StockSharp/Ecng/actions/workflows/drawing-coverage.yml) instruments `Ecng.Drawing` with Coverlet, reports line/branch coverage for each codec, and uploads the Cobertura XML. It fails below **90% line / 85% branch coverage**.

The tests use independent Pillow-created BMP/GIF fixtures plus programmatically constructed BMP palette/RLE/mask and interlaced GIF/LZW fixtures. Pixel oracles cover all 15 legal PNG color/depth pairs, JPEG baseline/progressive/CMYK/YCCK, BMP indexed and true-color samples, every composited GIF animation frame, APNG fdAT streams, frame delays, disposal/blending, and TrueType watermark alpha composition. Invalid/truncated images, GIF, PNG, JPEG, BMP, APNG and malformed font data are checked with deterministic mutations.

As with any custom image parser, broad regression tests and passing coverage thresholds are not substitutes for a security audit or a comprehensive external fuzzing campaign. Memory and total frame budgets limit pathological inputs.

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
