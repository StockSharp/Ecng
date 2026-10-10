#!/usr/bin/env python3
"""Validate re-encoded GIF animations with independent Pillow GIF decoding.

The input and output GIFs are exported by BmpGifTddTests in CI. Pillow is
strictly a test-only dependency. Both animations must have identical visible
RGBA pixels, frame delays, frame count, loop metadata and canvas size.
"""
from pathlib import Path
import sys
from PIL import Image, ImageSequence


def render(file: Path):
    with Image.open(file) as gif:
        geometry = gif.size
        loops = gif.info.get("loop", -1)
        frames = []
        for frame in ImageSequence.Iterator(gif):
            rgba = bytearray(frame.convert("RGBA").tobytes())
            # GIF transparent palette RGB is not visible; normalize it.
            for at in range(0, len(rgba), 4):
                if rgba[at + 3] == 0:
                    rgba[at:at + 3] = b"\x00\x00\x00"
            frames.append((frame.info.get("duration", 0), bytes(rgba)))
        return geometry, loops, frames


def main(directory):
    folder = Path(directory)
    sources = sorted(folder.glob("*.original.gif"))
    if len(sources) < 2:
        raise AssertionError(f"Expected at least two GIF test animations in {folder}")

    for source in sources:
        output = folder / source.name.replace(".original.gif", ".delta.gif")
        if not output.is_file():
            raise AssertionError(f"Missing compressed GIF: {output}")
        a = render(source)
        b = render(output)
        if a[0] != b[0] or a[1] != b[1] or len(a[2]) != len(b[2]):
            raise AssertionError(f"GIF metadata mismatch in {source.name}: {a[:2]} vs {b[:2]}")

        for index, ((delay_a, pixels_a), (delay_b, pixels_b)) in enumerate(zip(a[2], b[2])):
            if delay_a != delay_b:
                raise AssertionError(
                    f"{source.name} frame {index}: delay {delay_a} != {delay_b}")
            if pixels_a != pixels_b:
                differing = next(i for i, (x, y) in enumerate(zip(pixels_a, pixels_b)) if x != y)
                raise AssertionError(
                    f"{source.name} frame {index}: RGBA channel {differing} differs "
                    f"at pixel {differing // 4}: {pixels_a[differing]} != {pixels_b[differing]}")
        print(f"PASS {source.name}: {len(a[2])} frames, "
              f"{source.stat().st_size:,} -> {output.stat().st_size:,} bytes, "
              f"{a[0][0]}x{a[0][1]}, loops={a[1]}")


if __name__ == "__main__":
    if len(sys.argv) != 2:
        sys.exit("usage: verify_gif_pillow.py <fixture_directory>")
    main(sys.argv[1])
