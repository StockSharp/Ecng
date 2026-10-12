# Internal managed codec sources

These sources are compiled into Ecng.Drawing. They do not add NuGet packages,
native libraries or runtime tool dependencies, and all imported types are internal.

| Source | Pinned revision | Included parts | License |
|---|---|---|---|
| [PeachImage](https://github.com/jhaygood86/PeachImage/tree/2339696d879fe82615a95389d87af46b2ffcefc3) | `2339696d879fe82615a95389d87af46b2ffcefc3` | VP8/VP8L/alpha decoding, VP8 encoding, AVIF container and AV1 still decoding, TIFF strip decoding | [MIT](PeachImage-LICENSE.txt) |
| [JpegLibrary](https://github.com/yigolden-oss/JpegLibrary/tree/767a2c381e49fbf9ca5acbc0d61b927a13165776) | `767a2c381e49fbf9ca5acbc0d61b927a13165776` | Arithmetic and lossless scan decoders, supporting JPEG primitives | [MIT](JpegLibrary-LICENSE.txt) |
| [ImageSharp DCT](https://github.com/SixLabors/ImageSharp/blob/v1.0.0-beta7/src/ImageSharp/Formats/Jpeg/Components/FastFloatingPointDCT.cs) | `v1.0.0-beta7`, as included by JpegLibrary | Forward and inverse transforms in `Jpeg/FastFloatingPointDCT.cs` and the eight-column AVX adaptation in `Jpeg/JpegVectorDct.cs` | [Apache 2.0](ImageSharp-Apache-LICENSE.txt), [upstream notice](JpegLibrary-NOTICES.md) |

The license texts, upstream notice and this provenance record are included in
the Ecng.Drawing NuGet package under `licenses/`.

Adaptations made for Ecng:

- Internal namespaces and image/metadata adapters replace the upstream public APIs.
- .NET 6 uses scalar implementations where newer generic vector APIs are unavailable.
- Pixel, input, animation and estimated working-memory limits are applied by Ecng's wrappers and decoder allocations.
- TIFF rejects incomplete uncompressed, PackBits and LZW strip output.
- TIFF strip decompression and JPEG thumbnail scans retain bounded rows instead of full source pixels or coefficients.
- VP8 thumbnails retain two macroblock bands, keeping prediction borders separate from filtered pixels; chroma interpolation waits for the next band.
- VP8L thumbnail transforms and compressed alpha process rows over a bounded backward-reference history. Full decoding shares the same entropy-command reader.
- AVIF grid thumbnails release each color/alpha tile pair and its filter metadata before the next pair, with a rolling destination accumulator. Individual AV1 tiles remain protected by the working-memory guard.
- JPEG forward/inverse AVX DCT and double-precision quantization match the portable kernels bit-for-bit. Buffered short Huffman codes preserve marker and restart checks.
- VP8 encoding accepts explicit quality; lossless compressed alpha and animation containers are supplied by Ecng.
- Lossless JPEG corrects first-row prediction, modular sample arithmetic and restart-row handling.
- AVIF fixes alpha item references and clips AV1 edge upsampling to the sample bit depth.

Ecng's own wrappers handle format dispatch, ICO, animation composition, output
selection and a lossless WebP writer with palette/predictor transforms, color caching,
LZ77 references and optimized Huffman codes. PNG and sequential/progressive
Huffman JPEG use Ecng's existing codecs, extended for row processing and direct area thumbnails.

Support is bounded: TIFF reads the first chunky strip-based page with uncompressed,
LZW or PackBits data; AVIF reads supported 8/10-bit still images. Unsupported
compression, AV1 features and animation are refused. Lossless JPEG restart
intervals must cover complete MCU rows. Lossless VP8L retains RGBA exactly; lossy
VP8 changes colors according to quality while keeping alpha exact. Both retain animation.
JPEG encoding reuses at most 8 MiB of cached entropy symbols or quantized
coefficients; larger images use two transforms while keeping the same quality and optimized Huffman coding.
A shared 256 KiB coefficient table avoids repeated amplitude classification.
Cached entropy symbols are written directly into the output stream buffer.
Single-IDAT PNG decoding reads the validated compressed bytes from the input
without copying them. Area thumbnails reuse contributions from identical rows
and initialize each destination row from its first contribution.

Frozen libjpeg-turbo, libwebp, libavif and GDI+ pixel references live in
[the Drawing test fixtures](../../Tests/Drawing/Fixtures/README.md). Running tests
does not require those external tools.
