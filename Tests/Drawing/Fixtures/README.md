These fixtures reproduce the Drawing entries added by configs commit `ff010f6`.
They are inputs and recorded references, not files produced by the decoder under test.
No external encoder, decoder, Python process or installed font is needed by the
`ImageDebtReproductionTests`.

All JPEG pictures are opaque black. The small ones are 32x24; `large-48mp.jpg`
is 8000x6000. They were generated from binary PPM/PGM using the official
[libjpeg-turbo 3.2.0 tools](https://github.com/libjpeg-turbo/libjpeg-turbo/releases/tag/3.2.0):

| File | cjpeg options |
|---|---|
| baseline.jpg | `-quality 100` |
| extended.jpg | baseline with SOF0 replaced by SOF1, still valid 8-bit sequential data |
| multiscan.jpg | `-quality 100 -scans sequential.scans`, containing `0;`, `1;`, `2;` |
| arithmetic.jpg | `-quality 100 -arithmetic` |
| lossless.jpg | `-lossless 1,0` |
| 12bit.jpg | `-quality 100 -precision 12` |
| 12bit-progressive.jpg | `-quality 100 -precision 12 -progressive`, independently tests precision beyond the SOF1 restriction |
| large-48mp.jpg | `-quality 90 -optimize`, grayscale PGM |

Every small JPEG was decoded with `djpeg -png` and its output pixels checked:
all RGB channels are zero. The SOF1 fixture also passed that independent decoder.
Changing a precision byte in an 8-bit JPEG was not used to fake a 12-bit fixture.

WebP files were generated with the official
[libwebp 1.6.0 utilities](https://developers.google.com/speed/webp/docs/precompiled).
The source PNGs were written by Windows GDI+, independently of Ecng:

| File | Content and encoder options |
|---|---|
| lossy.webp | Opaque black 32x24; `cwebp -q 100` (VP8) |
| lossless.webp | Opaque black 32x24; `cwebp -lossless -exact` (VP8L) |
| alpha.webp | Black 32x24, alpha 64; `cwebp -q 100 -exact` (VP8 with alpha) |
| animated.webp | Black, then white 32x24; `img2webp -loop 3 -lossless -d 70 black.png -d 110 white.png` |

`dwebp` decoded the static WebP fixtures back to the expected RGBA pixels.
`webpmux -info` verified the animated canvas, loop count, two frames and delays.
`black.tiff` is a 32x24 opaque black TIFF saved with Windows GDI+.

The ICO inputs and the 1-bit grayscale 48-megapixel PNG are assembled directly
in C# in the test class. ICO has one 32x24 frame: either RGBA PNG, or a 32-bit
DIB with a double-height header and a zero AND mask. The large PNG contains
6000 valid unfiltered scanlines of 1000 packed bytes (8000 black pixels) each.
The opt-in reference checks also decode ICO, TIFF, WebP and both large images
through the site's existing ImageSharp implementation.

`white-1x1.avif` is the upstream
[libavif `tests/data/white_1x1.avif`](https://github.com/AOMediaCodec/libavif/blob/b70519419573c958adc1bf94176ca0bf496e88e2/tests/data/white_1x1.avif) fixture.
Its SHA-256 is `ea4e43d1f07e4c00de16c13afa32376111bb306e51f08212cf4c1b6064df3667`.
See the adjacent upstream license. `white-1x1.png` is the output of official
libavif 1.4.2 `avifdec -d 8`; its pixel is RGBA `(253,253,253,255)`, rather than
the nominal white suggested by the filename. Its SHA-256 is
`1cbe48a8163ea335f2e70d0c93c4f82c24aca9e6a976b65ba9e3656a2150d3f9`.

The reproduction tests now pass on net6.0 and net10.0. Full-resolution pixel and
animation caps remain checked; large-image cases specifically exercise thumbnail
paths rather than lifting the full-resolution limit.

## Colored pixel references

`ImageAdditionalCodecGoldenTests` uses a 32×24 pattern with RGB channels
`(19*x+7*y)%256`, `(5*x+13*y)%256`, `(3*x+29*y)%256`. Alpha, where present, is
`((x+y)%5)*63`. Input PNG/PPM files were made independently with GDI+ or written
as raw samples. Adjacent expected PNGs were decoded by the named external tool,
then frozen in this directory; Ecng did not generate those expectations.

| Fixtures | Generator and reference decoder | Comparison |
|---|---|---|
| `color-lossless-1..7.jpg` | libjpeg-turbo 3.2.0 `cjpeg -rgb -lossless P,0`, `djpeg -png` | Exact RGBA |
| `color-lossless-point.jpg` | `cjpeg -rgb -lossless 7,2`, `djpeg -png` | Exact RGBA, including point transform |
| `color-lossless-restart.jpg` | `cjpeg -rgb -lossless 4,0 -restart 32B`, `djpeg -png` | Exact RGBA across restart rows |
| `color-arithmetic*.jpg` | `cjpeg -arithmetic -quality 92 -sample 1x1`, with/without `-progressive`; `djpeg -png` | At most 2 levels per channel |
| `color-12bit*.jpg` | `cjpeg -precision 12 -quality 92 -sample 1x1`, with/without `-progressive`; `djpeg -png` | At most 2 levels per channel |
| `color-lossless.webp` | libwebp 1.6.0 `cwebp -exact -lossless`, `dwebp` | Exact RGBA |
| `color-lossy.webp` | `cwebp -q 92 -alpha_q 100`, `dwebp` | At most 2 levels per channel |
| `color-avif-8.avif` | libavif 1.4.2 `avifenc -j 2 -s 8 -l -d 8`, `avifdec -j 2 -d 8` | Exact RGBA |
| `color-avif-10.avif` | `avifenc -j 2 -s 8 -l -d 10`, `avifdec -j 2 -d 8` | At most 1 level per channel |
| `color.tiff` | Windows GDI+ TIFF encoder and decoder | Exact RGBA |

The encoder test also records an Ecng lossless WebP artifact. A local check with
official `dwebp` accepted it and matched all 768 RGBA pixels exactly. The portable
native tools are only for creating/checking references; CI tests use frozen PNGs
and the separately sourced managed decoder.

Run the regressions and controls with a filter and timeout:

```sh
dotnet test Tests/Tests.csproj --no-build --no-restore -c Release -f net10.0 \
  --settings Tests/test.runsettings \
  --filter "FullyQualifiedName~ImageDebtReproductionTests" \
  --blame-hang-timeout 60s --blame-hang-dump-type none \
  -- RunConfiguration.TestSessionTimeout=240000
```

These cases also run on net6.0 and are included in the ordinary Drawing filter.
`ReadableHeader_DoesNotRemoveTheNeedToHandleMalformedPixelData`
is a positive control: a readable header still requires error handling when
actual pixel decoding fails. A Try-size API cannot replace that error handling.

`ImageDebtPerformanceTests` and `ImageDebtCorpusTests` are opt-in integration
checks on net10.0. They add no ImageSharp package reference. Set the runsettings
parameter `ImageSharpReferencePath` to an existing site build directory containing
`StockSharp.Web.Api.Data.dll` and its dependencies, from before the migration.
Keep the `<MSTest><Timeout>10000</Timeout></MSTest>` setting. Select the relevant
class with `--filter`, use the same hang/session timeouts, and run timing cases
alone with `MSTest.Parallelize.Workers=1`.

For corpus checks, set the process environment variable `ECNG_DRAWING_CORPUS_PATH`
to the administrative API export directory containing GIF/BMP files with their
correct extensions. Discovery creates a case for each file and each of the four
operations. If the export is absent or empty, the integration test fails with a
setup message; it does not claim that corpus coverage passed. The local smoke
run uses two generated GDI+ files and does not establish coverage of site files.

Timing compares the old site's transformations with Ecng on a generated gradient.
The old site writes resized JPEG inputs as PNG; Ecng preserves JPEG. Resize
filters and watermark defaults also differ. The reported medians are a workflow
comparison, not an isolated codec benchmark or a reproduction of the unpublished
491-file measurement. No wall-clock performance threshold is asserted.
