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

The `ImageHelper` class reads, reduces, converts and watermarks pictures in managed .NET, without native codecs or third-party imaging NuGet dependencies. Selected internal decoder sources are included under MIT and Apache 2.0; see [source provenance and licenses](Codecs/README.md).

| Input | GetImageSize | ResizeImage | ConvertToPng | AddTextWatermark |
|---|---|---|---|---|
| PNG | Yes | PNG | PNG | PNG |
| APNG (animated PNG) | Yes | **APNG, all frames** | APNG unchanged | **APNG, all frames** |
| JPEG (sequential, progressive, arithmetic, lossless) | Yes | JPEG | PNG | JPEG |
| BMP (indexed, RGB, bitfields and RLE) | Yes | BMP | PNG | BMP |
| GIF87a / GIF89a | Yes | **animated GIF, all frames** | **APNG, all frames** | **animated GIF, all frames** |
| WebP (VP8, VP8L, alpha, animation) | Yes | **WebP, all frames** | **PNG/APNG, all frames** | **WebP, all frames** |
| ICO (PNG or DIB frames) | Yes | ICO | PNG | ICO |
| TIFF (supported first page) | Yes | PNG when modified | PNG | PNG when modified |
| AVIF (supported 8/10-bit still image) | Yes | PNG when modified | PNG | PNG when modified |

A picture is handled the way it is shown. A JPEG whose Exif orientation lays it on its side is turned upright when it is decoded, `GetImageSize` reports the size it is shown with, and a picture that is written again carries no orientation of its own; one that is returned as it came keeps the orientation it had.

`ResizeImage` never enlarges. With the default output format, a static picture that already fits is returned as a copy after reading its header, without decoding pixels; animation still validates every frame. This shortcut does not certify the pixel stream. `AddTextWatermark` puts its text - one line of 1024 characters at most - into the bottom right corner. The margin is a quarter of the smaller side of the picture at most, and the text is made smaller to fit between the margins, down to 6 pixels to the em. A text that has no room is not drawn. With the default output format, a picture nothing is drawn on is returned as it came. Reducing averages the whole source area of every pixel, with colors weighted by alpha.

`GetImageFormat` identifies a signature and returns `ImageFormats.Unknown` for unrecognized data. `TryGetImageSize` returns false for an unsupported or unreadable header. Neither guarantees that decoding will succeed. The `byte[]` and `ReadOnlySpan<byte>` overloads avoid allocating a pixel canvas.

`ResizeImage` and both font variants of `AddTextWatermark` also accept an explicit `ImageFormats` output: PNG, JPEG, BMP, GIF, WebP or ICO. An explicit format converts even an already-fitting picture or an opacity-zero watermark. TIFF/AVIF encoding is unsupported. Multiple animation frames require PNG (APNG), GIF or WebP output and are never silently discarded.

#### Limits

- Full-resolution decoding is limited to **25 megapixels** (`InvalidDataException`). A non-interlaced PNG can instead be reduced row by row; sequential Huffman JPEG can use DCT scales 1/2, 1/4 or 1/8. Tests cover 8000×6000 inputs reduced to thumbnails without a full RGBA canvas. Large Adam7 PNG, progressive/arithmetic/lossless JPEG and the other full-resolution decoders retain the cap. These limits are not caller-configurable.
- Header reading does not decode pixels. Container validation and input limits still apply; AVIF header parsing also enforces its decoder dimension cap.
- An animation is decoded up to **512 frames** and **100 megapixels** of canvas, all frames together. Frames are decoded one at a time, so a long animation does not hold all its canvases in memory at once.
- `GetGifLoopCount` and `GetAnimationLoopCount` read the file without decoding frames.
- Input that is damaged gives `InvalidDataException`; a format, or a feature of one, that is not supported gives `NotSupportedException`.

#### Animated GIF and APNG

GIF processing preserves **every displayed frame**, its delay and its looping, and composites GIF subrectangles using transparent indices and disposal modes 0–3. Interlaced GIF frames, global/local color tables, and LZW are supported. The canvas starts transparent and is restored to transparent: the background color of the logical screen is not used, the way browsers do not use it. A frame that reaches beyond the logical screen is clipped, bytes after the trailer are ignored, and pixel data may lack its end code.

- `GetGifFrames()` returns a list of `(byte[] png, int delayMilliseconds)` for all displayed GIF frames. Each `png` is a *full-canvas* composited RGBA8 PNG.
- `GetGifLoopCount()` returns the NETSCAPE loop count as the file states it: the number of repetitions after the first run (0 means infinite; -1 means no loop extension).
- `GetAnimationFrames()` and `GetAnimationLoopCount()` take GIF, **APNG** or **animated WebP**. `GetAnimationLoopCount()` returns the number of plays (0 means infinite): a GIF without a loop extension plays once, a GIF with a loop count of N plays N + 1 times.
- Resizing GIF/APNG resizes **every displayed frame**. Animation timings and looping remain unchanged: an APNG delay is kept as the fraction of a second the file states.
- Adding a watermark to GIF/APNG draws the text **on every frame**, retaining the animation.
- `ConvertToPng` on GIF yields **APNG**, not a frozen first frame. APNG carries the complete animation, timings and the same number of plays. Calling the other methods on that APNG is also animation-preserving.
- APNG decoding handles per-frame rectangles, SOURCE/OVER blending, and NONE/BACKGROUND/PREVIOUS disposal.

The palette of a GIF frame holds at most **256 indexed colors**, and its transparency is **binary**. When re-encoding GIF, a frame keeps its exact colors if it has no more than 256 of them (255 beside transparent pixels); a frame with more is reduced to that many by median cut, without dithering, and alpha below 128 is made transparent. When re-encoding GIF, unchanged pixels are not stored again: each frame normally uses the smallest rectangle of changed pixels, with a transparent index for unchanged pixels inside that rectangle and disposal method 1 (leave the underlying canvas in place). If the next frame needs formerly opaque pixels to become transparent, the preceding frame is written full-screen with disposal method 2 (restore transparent background). This also accounts for the last-to-first transition of a looping animation. Identical consecutive frames emit a single transparent pixel to preserve the frame delay. Every rectangle has its own color table and LZW compression. APNG uses full lossless RGBA8; converting GIF to APNG does not introduce new quantization.

#### BMP

The BMP decoder accepts OS/2 BITMAPCORE and Windows BITMAPINFO/V4/V5 DIB headers, top-down or bottom-up scanlines, indexed 1/4/8-bit color, RGB555/RGB565 and other nonoverlapping 16/32-bit bitfield masks, uncompressed 24/32-bit color, and BI_RLE4/BI_RLE8. Alpha is read where a mask declares it; the fourth byte of uncompressed 32-bit pixels is reserved and is not taken for alpha. BMP encoding for resizing and watermarking always emits a portable top-down **32-bit BGRA BITMAPV4HEADER** with explicit RGBA masks to retain alpha. Embedded JPEG/PNG compression inside BMP, RLE24, and exotic OS/2-only codecs are not supported.

#### JPEG and PNG

- PNG: all legal color types and bit depths including grayscale, indexed transparency, 8/16-bit samples, all five filter types, and Adam7 interlacing. Input APNG is recognized and handled as animated rather than accidentally discarding frames. Output is always RGBA8 and not interlaced. Its scanlines are packed twice - every row with the filter that leaves it the smallest values, which is the heuristic of libpng, and all rows unfiltered - and the shorter result is kept: filtering takes a sixth to two fifths off a photograph and most of a gradient, and often adds to a screenshot, whose rows repeat one another. Each frame of an APNG is packed that way on its own.
- JPEG decoding: 8/12-bit sequential (SOF0/SOF1), multi-scan and progressive Huffman data, arithmetic sequential/progressive data, and lossless predictors 1–7 with point transforms. Grayscale, YCbCr, RGB, Adobe CMYK/YCCK and restart markers are supported; lossless restart intervals must cover full MCU rows. Output is normalized to RGBA8.
- PNG bytes following a valid IEND are ignored. Chunk CRC, ordering, compressed scanline and animation checks remain in place.
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
byte[] pngPreview = bmp.ResizeImage(100, 100, ImageFormats.Png);
byte[] webpPreview = photo.ResizeImage(160, 160, ImageFormats.Webp);

if (photo.TryGetImageSize(out var dimensions))
    Console.WriteLine($"{photo.GetImageFormat()}: {dimensions}");

byte[] font = await File.ReadAllBytesAsync("brand.ttf");
byte[] branded = bmp.AddTextWatermark("StockSharp", font);   // A font of your own, as bytes
```

#### Testing

The Drawing tests run entirely on .NET, without Python, Pillow, external processes or native imaging dependencies. StbImageSharp is referenced only by the test project and independently decodes PNG pixels, BMP output, JPEG output and every frame of emitted GIF animations. PNG/BMP pixels are compared byte for byte; the JPEG cross-check permits a difference of at most two channel levels for inverse-DCT and color-conversion rounding. GIF frames are compared byte for byte, including their individual delays.

APNG chunks, sequence numbers, CRCs and timing are checked by the C# test reader. Each IDAT/fdAT frame payload is wrapped as a standalone PNG and decoded by StbImageSharp; composition, blending and disposal are checked against expected RGBA buffers constructed in C#. StbImageSharp is not used as an APNG animation decoder.

Checked-in independent BMP/GIF/JPEG fixtures and pixel buffers supplement programmatically constructed PNG color/depth/filter/Adam7, BMP palette/RLE/mask and interlaced GIF/LZW fixtures. Pixel oracles cover all 15 legal PNG color/depth pairs, JPEG baseline/progressive/CMYK/YCCK, BMP indexed and true-color samples, every composited GIF animation frame, APNG fdAT streams, frame delays, disposal/blending, and TrueType watermark alpha composition. JPEG files are also checked against recorded libjpeg-turbo pixels, and JPEG encoding against recorded size and fidelity at the same quality. These references do not require regenerating fixtures to run the tests. Invalid/truncated images, malformed animation chunks and malformed font data are checked with deterministic mutations; fonts are assembled in the tests so glyph rasterization is independent of installed fonts.

[Additional frozen fixtures](../Tests/Drawing/Fixtures/README.md) compare colored lossless/arithmetic/12-bit JPEG, WebP with alpha, TIFF and 8/10-bit AVIF against independent decoded PNGs. Lossless JPEG, lossless WebP, TIFF and 8-bit AVIF are exact RGBA comparisons; lossy JPEG/WebP allow two channel levels and 10-bit AVIF one level. Opt-in tests compare transformations with an existing pre-migration site build; the real site's GIF/BMP export remains a separate required input.

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
