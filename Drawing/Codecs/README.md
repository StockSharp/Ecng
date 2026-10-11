# Internal managed codec sources

These sources are compiled into Ecng.Drawing. They do not add NuGet packages,
native libraries or runtime tool dependencies, and all imported types are internal.

| Source | Pinned revision | Included parts | License |
|---|---|---|---|
| [PeachImage](https://github.com/jhaygood86/PeachImage/tree/2339696d879fe82615a95389d87af46b2ffcefc3) | `2339696d879fe82615a95389d87af46b2ffcefc3` | VP8/VP8L/alpha decoding, AVIF container and AV1 still decoding, TIFF strip decoding | [MIT](PeachImage-LICENSE.txt) |
| [JpegLibrary](https://github.com/yigolden-oss/JpegLibrary/tree/767a2c381e49fbf9ca5acbc0d61b927a13165776) | `767a2c381e49fbf9ca5acbc0d61b927a13165776` | Arithmetic and lossless scan decoders, supporting JPEG primitives | [MIT](JpegLibrary-LICENSE.txt) |
| [ImageSharp IDCT](https://github.com/SixLabors/ImageSharp/blob/v1.0.0-beta7/src/ImageSharp/Formats/Jpeg/Components/FastFloatingPointDCT.cs) | `v1.0.0-beta7`, as included by JpegLibrary | `Jpeg/FastFloatingPointDCT.cs` | [Apache 2.0](ImageSharp-Apache-LICENSE.txt), [upstream notice](JpegLibrary-NOTICES.md) |

The license texts, upstream notice and this provenance record are included in
the Ecng.Drawing NuGet package under `licenses/`.

Adaptations made for Ecng:

- Internal namespaces and image/metadata adapters replace the upstream public APIs.
- .NET 6 uses scalar implementations where newer generic vector APIs are unavailable.
- Pixel, input and animation limits are applied by Ecng's wrappers and decoder allocations.
- TIFF rejects incomplete uncompressed, PackBits and LZW strip output.
- Lossless JPEG corrects first-row prediction, modular sample arithmetic and restart-row handling.
- AVIF fixes alpha item references and clips AV1 edge upsampling to the sample bit depth.

Ecng's own wrappers handle format dispatch, ICO, animation composition, output
selection and the literal lossless WebP writer. PNG and sequential/progressive
Huffman JPEG use Ecng's existing codecs, extended for row processing and scaled DCT.

Support is bounded: TIFF reads the first chunky strip-based page with uncompressed,
LZW or PackBits data; AVIF reads supported 8/10-bit still images. Unsupported
compression, AV1 features and animation are refused. Lossless JPEG restart
intervals must cover complete MCU rows. The WebP writer uses literal VP8L symbols;
it preserves RGBA and animation but does not offer an optimized compression ratio.

Frozen libjpeg-turbo, libwebp, libavif and GDI+ pixel references live in
[the Drawing test fixtures](../../Tests/Drawing/Fixtures/README.md). Running tests
does not require those external tools.
