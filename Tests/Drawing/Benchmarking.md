# Generated image comparisons

No website export, account or network access is needed to run the generated corpus.
The generator produces color bands, gradients, fine lines, structured textures,
deterministic noise and soft alpha at 96x64 and 960x640. Each picture is independently
encoded as JPEG, BMP, GIF, lossless WebP and lossy WebP using ImageSharp; PNG
scanlines are assembled directly by the fixture helper.
The input is decoded by both ImageSharp and Ecng; lossless samples are exact RGBA
comparisons, with at most two levels per channel for JPEG and lossy WebP rounding.

The optional suite loads an existing ImageSharp assembly; it adds no NuGet or
production dependency. Set the runsettings parameter `ImageSharpReferencePath`
to a directory containing `SixLabors.ImageSharp.dll`. Its informational version
is recorded in every measurement. The reference runs with one processing thread;
its previous configuration is restored after each case.
Set `ImageBenchmarkOutputPath` to an absolute artifact directory to keep the
corpus, images and combined CSV together. Without it, the suite uses
`TestContext.ResultsDirectory/image-benchmark`.
Use a fresh output directory for each measurement run; the combined CSV appends
rows while individual case images and JSON files use stable names.

Run correctness before timing:

```sh
dotnet build Ecng.slnx --no-restore --configuration Release
dotnet test Tests/Tests.csproj --no-build --no-restore -c Release -f net10.0 \
  --settings path/to/measurement.runsettings \
  --filter "FullyQualifiedName~GeneratedCorpus_DecodedPixelsMatchIndependentDecoder" \
  --blame-hang-timeout 60s --blame-hang-dump-type none \
  -- RunConfiguration.TestSessionTimeout=240000 MSTest.Parallelize.Workers=1
```

Select `FullyQualifiedName~ImageCodecBenchmarkTests&FullyQualifiedName!~GeneratedCorpus`
for measurements and run it alone with one test worker. Set the measurement
runsettings' `MSTest.Timeout` to 30000 ms and the session timeout to 900000 ms.
Each case warms both implementations at least four times, up to 32 times, for
at least 250 ms when the warmup cap permits. Nine batches then alternate their
order. Each batch repeats up to 64 operations, calibrated to approximately 100 ms
for the faster implementation. Median and nearest-rank P90 are recorded; with nine samples, P90 is
the slowest sample. Allocations are the median managed bytes on the calling thread.
Timing covers decoding, transformation and encoding; fixture creation, file I/O,
pixel comparisons and metrics are outside the measured interval.

Matched output settings:

- JPEG qualities 30, 50, 70, 85, 90, 95 and 100, with 4:4:4 YCbCr and no metadata.
- PNG RGBA8; ImageSharp uses adaptive filtering and compression level 6.
- BMP RGBA32 with declared transparency.
- Lossless WebP output retains exact RGBA in both implementations, with compression enabled.
  Lossy original files are not used as a size ceiling for lossless output.
- Lossy WebP uses explicit VP8 qualities 30, 50, 70, 85, 90, 95 and 100 on six
  synthetic patterns (42 cases), preserving alpha. Compare the same mode and quality.
- PNG encoding and normalization call `ConvertToPng`, which decodes and encodes
  static input. `ResizeImage` without a size or format change would only copy it.
- Resize outputs have identical dimensions. Ecng streams sequential JPEG into a
  direct area average; ImageSharp uses Box, without gamma companding and with
  alpha premultiplication. These filters are not assumed to have identical samples.
- Animated GIF conversion measures decoding and APNG encoding of all 20 frames
  in both implementations. Every displayed frame is independently compared with
  the source; it is not compared with a first-frame-only conversion.

Encoding fidelity is measured against the original uncompressed RGBA pixels.
Resize fidelity is measured against an independent direct integral over each
output pixel's source rectangle, with alpha-weighted colors.

RGB MAE, RMSE and PSNR use colors premultiplied onto black; alpha MAE is separate.
Luminance SSIM uses weights 0.299/0.587/0.114, the valid 11x11 Gaussian window with
sigma 1.5, K1=0.01, K2=0.03 and range 255, following the
[authors' SSIM reference](https://www.cns.nyu.edu/~lcv/ssim/index.html).
Identical pixels have infinite PSNR. Analytical metric tests and JPEG quality
control tests run on both net6.0 and net10.0 without the external reference.
The independent resize integral is also checked against hand-calculated opaque,
fractional-coverage and transparent-color cases on both frameworks.

Results are saved in `ImageBenchmarkOutputPath` (or the default described above):
individual JSON measurements, a combined
`measurements.csv`, source images and both encoded outputs. Open the images
beside the metrics to inspect edge artifacts and compression choices.

Synthetic coverage is reproducible and includes difficult inputs; it does not
establish compatibility with every historical file of a particular site.
The `DrawingPerformance` category enforces a fixed median-time budget:
`Ecng <= ImageSharp * 1.25 + 0.25 ms`. A violation fails the test after saving
all measurements and output files. The category covers all 150 pipelines,
quality settings and animation conversions, plus 11 PNG/JPEG encoder cases
that exclude source decoding. ImageSharp receives an already decoded source
for those isolated encoders; both implementations serialize the same pixels.
The existing lossless, geometry and animation assertions run before the time check.

Run these tests alone in Release, with the reference assembly, one test worker
and explicit per-test/session timeouts:

```sh
dotnet test Tests/Tests.csproj --no-build --no-restore -c Release -f net10.0 \
  --settings path/to/measurement.runsettings \
  --filter "TestCategory=DrawingPerformance" \
  --blame-hang-timeout 60s --blame-hang-dump-type none \
  -- RunConfiguration.TestSessionTimeout=900000 MSTest.Parallelize.Workers=1
```

These checks detect the generated cases that exceed the budget on the current
machine; they do not establish performance for every possible image or machine.

`ImageCodecStageBenchmarkTests` isolates PNG decoding/encoding, JPEG encoding,
RGB-to-YCbCr conversion, DCT/quantization, JPEG decoding and RGBA area averaging.
It records hardware SIMD availability, warms each stage
32 times and reports the median of five samples. Use it to locate a cost; accepted
end-to-end comparisons remain the alternating, matched-output suite above.

`ImageCodecBaselineBenchmarkTests` loads a preserved previous Ecng.Drawing DLL
from the `EcngDrawingBaselinePath` runsettings parameter in a separate assembly
context. Four cases compare previous/current processing in the same process, with
32 alternating warmups and nine timed samples, and require identical independently
decoded output pixels. This separates Ecng changes from noise between benchmark runs.
Six more cases require identical JPEG bytes and decoded pixels at the entropy,
coefficient-cache and two-pass storage sizes, using color bands and dense noise.

`StreamingThumbnailTests` checks small destination budgets against independent
area averages and verifies an 8000x6000 progressive JPEG thumbnail allocates less
than 8 MiB on the calling thread. `ImageWorkingMemoryTests` checks refusal under
an explicit decoder working budget. `WebpWindowMemoryTests` compares 512x4096
VP8/VP8L thumbnails with libwebp PNG references under 128 KiB or 4 MiB budgets.
`AvifGridMemoryTests` checks 12/128 tile grids, alpha and 10-bit 4:2:0 color under
a 1 MiB budget, and rejects a budget too small for one tile. Individual AV1 tiles
retain reconstruction planes; the working-byte estimate does not measure total
process memory. `JpegDctParityTests` checks portable/vector DCT coefficients,
quantization ties, signs and overflow on both frameworks.
`JpegEncoderCacheTests` exercises entropy-symbol caching, coefficient caching
and the bounded two-pass path, independently decodes the result and verifies
that pooled-buffer reuse keeps output deterministic.
`PngCodecTests.SingleIdatDecode_AllocatesPixelsAndScanlineBuffersWithoutCopyingTheInput`
limits warmed managed allocations to the output RGBA pixels plus 128 KiB for a
512x384 noise image. Empty and split IDAT streams retain exact pixel checks.

For portable correctness, set `DOTNET_EnableHWIntrinsics=0` and
`COMPlus_EnableHWIntrinsic=0` in the test process environment. Run the relevant
Drawing correctness classes with `Tests/test.runsettings`, the
`TestCategory!=Integration` filter and one worker. Performance comparisons run
with the machine's normal hardware acceleration settings.
