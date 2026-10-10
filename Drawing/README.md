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

### Image Processing

The `ImageHelper` class reads, reduces, converts and watermarks pictures. It is pure managed .NET: no third-party imaging packages, GDI+, SkiaSharp, P/Invoke or native codecs. Tested on Windows, Linux and macOS.

| Input | GetImageSize | ResizeImage | ConvertToPng | AddTextWatermark |
|---|---|---|---|---|
| PNG | Yes | PNG | PNG | PNG |
| APNG (animated PNG) | Yes | **APNG, all frames** | APNG unchanged | **APNG, all frames** |
| JPEG (8-bit baseline / progressive; gray, YCbCr, RGB, CMYK, YCCK) | Yes | JPEG | PNG | JPEG |
| BMP (indexed, RGB, bitfields and RLE) | Yes | BMP | PNG | BMP |
| GIF87a / GIF89a | Yes | **animated GIF, all frames** | **APNG, all frames** | **animated GIF, all frames** |

A picture is handled the way it is shown. A JPEG whose Exif orientation lays it on its side is turned upright when it is decoded, `GetImageSize` reports the size it is shown with, and a picture that is written again carries no orientation of its own; one that is returned as it came keeps the orientation it had.

`ResizeImage` never enlarges and returns a picture that already fits as it came. `AddTextWatermark` puts its text - one line of 1024 characters at most - into the bottom right corner. The margin is a quarter of the smaller side of the picture at most, and the text is made smaller to fit between the margins, down to 6 pixels to the em. What is fitted is the line as it is drawn: a mark that reaches beyond the advance of its letter, or above and below what the font gives as its height, stays between the margins too. A text that has no room even then is not drawn. A picture nothing is drawn on - for want of room, with an opacity of 0, or because the text would lay no opacity on any pixel, being too small to reach a point a pixel is sampled at or too faint for the part of a pixel it covers - is returned as it came. A text that is laid on and cannot be told from what is under it, white on white, still has the picture written again. Reducing averages the whole source area of every pixel, with colors weighted by alpha, so thin lines keep their share of brightness and transparent pixels lend no color to their neighbors.

#### Limits

- A picture of more than **25 megapixels** is not decoded (`InvalidDataException`). `GetImageSize` reads a header and has no such limit.
- An animation is decoded up to **512 frames** and **100 megapixels** of canvas, all frames together. Frames are decoded one at a time, so a long animation does not hold all its canvases in memory at once.
- `GetGifLoopCount` and `GetAnimationLoopCount` read the file without decoding frames.
- Input that is damaged gives `InvalidDataException`; a format, or a feature of one, that is not supported gives `NotSupportedException`.

#### Animated GIF and APNG

GIF processing preserves **every displayed frame**, its delay and its looping, and composites GIF subrectangles using transparent indices and disposal modes 0–3. Interlaced GIF frames, global/local color tables, and LZW are supported. The canvas starts transparent and is restored to transparent: the background color of the logical screen is not used, the way browsers do not use it. A frame that reaches beyond the logical screen is clipped, bytes after the trailer are ignored, and pixel data may lack its end code.

- `GetGifFrames()` returns a list of `(byte[] png, int delayMilliseconds)` for all displayed GIF frames. Each `png` is a *full-canvas* composited RGBA8 PNG.
- `GetGifLoopCount()` returns the NETSCAPE loop count as the file states it: the number of repetitions after the first run (0 means infinite; -1 means no loop extension).
- `GetAnimationFrames()` and `GetAnimationLoopCount()` take a GIF or an **APNG**. `GetAnimationLoopCount()` returns the number of plays (0 means infinite): a GIF without a loop extension plays once, a GIF with a loop count of N plays N + 1 times.
- Resizing GIF/APNG resizes **every displayed frame**. Animation timings and looping remain unchanged: an APNG delay is kept as the fraction of a second the file states.
- Adding a watermark to GIF/APNG draws the text **on every frame**, retaining the animation.
- `ConvertToPng` on GIF yields **APNG**, not a frozen first frame. APNG carries the complete animation, timings and the same number of plays. Calling the other methods on that APNG is also animation-preserving.
- APNG decoding handles per-frame rectangles, SOURCE/OVER blending, and NONE/BACKGROUND/PREVIOUS disposal.

The palette of a GIF frame holds at most **256 indexed colors**, and its transparency is **binary**. When re-encoding GIF, a frame keeps its exact colors if it has no more than 256 of them (255 beside transparent pixels); a frame with more is reduced to that many by median cut, without dithering, and alpha below 128 is made transparent. Every frame is written as the whole canvas with a color table of its own and LZW compression, so a GIF whose frames were stored as small differences comes out larger than it went in. APNG uses full lossless RGBA8; converting GIF to APNG does not introduce new quantization.

#### BMP

The BMP decoder accepts OS/2 BITMAPCORE and Windows BITMAPINFO/V4/V5 DIB headers, top-down or bottom-up scanlines, indexed 1/4/8-bit color, RGB555/RGB565 and other nonoverlapping 16/32-bit bitfield masks, uncompressed 24/32-bit color, and BI_RLE4/BI_RLE8. Alpha is read where a mask declares it; the fourth byte of uncompressed 32-bit pixels is reserved and is not taken for alpha. BMP encoding for resizing and watermarking always emits a portable top-down **32-bit BGRA BITMAPV4HEADER** with explicit RGBA masks to retain alpha. Embedded JPEG/PNG compression inside BMP, RLE24, and exotic OS/2-only codecs are not supported.

#### JPEG and PNG

- PNG: all legal color types and bit depths including grayscale, indexed transparency, 8/16-bit samples, all five filter types, and Adam7 interlacing. Input APNG is recognized and handled as animated rather than accidentally discarding frames. Output is always RGBA8 and not interlaced. Its scanlines are packed twice - every row with the filter that leaves it the smallest values, which is the heuristic of libpng, and all rows unfiltered - and the shorter result is kept: filtering takes a sixth to two fifths off a photograph and most of a gradient, and often adds to a screenshot, whose rows repeat one another. Each frame of an APNG is packed that way on its own.
- JPEG: 8-bit baseline and progressive Huffman encoding; grayscale, YCbCr, RGB, Adobe CMYK/YCCK and restart markers. Arithmetic-coded, lossless, 12-bit, extended sequential (SOF1) and multi-scan baseline JPEG are not supported.
- A JPEG that is reduced or watermarked comes back as a baseline JPEG without chroma subsampling: a second, lossy generation of the picture. How lossy is set by `jpegQuality`, from 1 to 100 on the scale of libjpeg - the quantization tables of the standard, scaled - and is `ImageHelper.DefaultJpegQuality`, 90, unless another is asked for. The Huffman tables are built for the picture, which makes a file about as large as libjpeg writes it at the same quality with optimized tables and no subsampling.
- The ICC profile of a JPEG or a PNG stays with the picture that is made of it - reduced, watermarked or converted to PNG - if it is a profile for RGB of 4 MB at most. A JPEG keeps it in its APP2 segments, a PNG made of a JPEG gets an iCCP chunk, and a PNG, animated or not, keeps the chunks that say what its colors are - iCCP or sRGB, gAMA, cHRM - as they were. A profile for CMYK or for gray is left behind: what is written is RGB whatever was read, and such a profile no longer describes it. Exif and other metadata are not carried over.

#### Watermark font

- The text is drawn from TrueType outlines (`glyf`) with a managed rasterizer: one line, left to right, without kerning or shaping. Letters that touch or lie over one another make one shape of one opacity. A character the font lacks is drawn as the missing-glyph box of the font; a text none of whose characters has an outline in the font is refused (`ArgumentException`).
- `fontFamily` is the family name as fonts state it - "Times New Roman", "DejaVu Sans" - or the name of a font file without its extension. Verdana is the default and is not distributed with the library. If the family is not installed, the first installed of Verdana, DejaVu Sans, Arial, Liberation Sans, Noto Sans, FreeSans and Helvetica is taken, and failing those the built-in font: a machine without a single font, such as a container, draws the text all the same.
- The built-in font is a subset of DejaVu Sans 2.37: the Latin, Greek and Cyrillic letters, punctuation, currency signs, arrows and a few symbols. Asking for "DejaVu Sans" - or for "DejaVuSans", as its file is named - gives the installed font or, where there is none, this subset: the same letters on every machine. Its license, that of Bitstream Vera, is in [`Fonts/DejaVuSans-LICENSE.txt`](Fonts/DejaVuSans-LICENSE.txt) and in the `licenses` folder of the package; [`Fonts/README.md`](Fonts/README.md) tells how the subset was made.
- `fontFilePath` names a `.ttf` file to use instead of an installed font. The overload with `byte[] font` takes the file itself, for a font an application keeps in its resources.
- The installed fonts are listed once per process.

```csharp
using Ecng.Drawing;

byte[] animatedGif = await File.ReadAllBytesAsync("animation.gif");
var size = animatedGif.GetImageSize();
var frames = animatedGif.GetGifFrames();
int loops = animatedGif.GetGifLoopCount();       // 0: infinite, -1: unspecified
int plays = animatedGif.GetAnimationLoopCount(); // 0: infinite, otherwise the number of plays

byte[] smallerGif = animatedGif.ResizeImage(320, 240);      // Still animated GIF
byte[] markedGif = smallerGif.AddTextWatermark("StockSharp"); // Every frame
byte[] animatedPng = animatedGif.ConvertToPng();             // All frames in APNG
byte[] smallerApng = animatedPng.ResizeImage(320, 240);      // Still animated APNG

byte[] photo = await File.ReadAllBytesAsync("photo.jpg");
byte[] preview = photo.ResizeImage(1280, 1280);              // JPEG at the default quality of 90
byte[] thumbnail = photo.ResizeImage(160, 160, jpegQuality: 75);

byte[] bmp = await File.ReadAllBytesAsync("picture.bmp");
byte[] markedBmp = bmp.AddTextWatermark("StockSharp");       // BMP output

byte[] font = await File.ReadAllBytesAsync("brand.ttf");
byte[] branded = bmp.AddTextWatermark("StockSharp", font);   // A font of your own, as bytes
```

#### Testing

The tests use independent Pillow-created BMP/GIF fixtures plus programmatically constructed BMP palette/RLE/mask and interlaced GIF/LZW fixtures. Pixel oracles cover all 15 legal PNG color/depth pairs, JPEG baseline/progressive/CMYK/YCCK, BMP indexed and true-color samples, every composited GIF animation frame, APNG fdAT streams, frame delays, disposal/blending, and TrueType watermark alpha composition. Invalid/truncated images, GIF, PNG, JPEG, BMP, APNG and malformed font data are checked with deterministic mutations.

JPEG files are checked against the pixels libjpeg-turbo gives for them, and what the JPEG encoder writes against the size and the fidelity of what libjpeg-turbo writes of the same pixels at the same quality. What the GIF and APNG encoders write is read back by decoders that belong to the tests, and fonts are assembled in the tests as well, so the glyph rasterizer does not depend on the fonts of the machine.

As with any custom image parser, regression tests are not a substitute for a security audit or a comprehensive external fuzzing campaign.

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
