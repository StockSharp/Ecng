# Built-in font

`DejaVuSans.ttf` is a subset of DejaVu Sans 2.37 (Book). `Ecng.Drawing` embeds it and draws a watermark with it where neither the family asked for nor one of its usual stand-ins is installed.

The fonts are (c) Bitstream, the DejaVu changes are in the public domain, and the glyphs imported from Arev are (c) Tavmjong Bah. `DejaVuSans-LICENSE.txt` is the license file of the release, unchanged. It wants its notices in every copy of the font: they are in the name table of the file itself, the license file lies beside it here, and the NuGet package carries it in `licenses/`.

## What is in it

| Unicode range | Block |
|---|---|
| U+0020-007E, U+00A0-00FF | Basic Latin, Latin-1 Supplement |
| U+0100-024F, U+1E00-1EFF | Latin Extended-A, Latin Extended-B, Latin Extended Additional |
| U+0370-03FF | Greek and Coptic |
| U+0400-04FF | Cyrillic |
| U+2000-206F | General Punctuation |
| U+20A0-20BF | Currency Symbols |
| U+2100-214F | Letterlike Symbols |
| U+2190-21FF | Arrows |
| single characters | 14 mathematical operators and 12 geometric shapes of the WGL4 set, two stars, a check mark and a ballot X |

Of these ranges the file holds what DejaVu Sans has: 1520 characters in 1594 glyphs, 110 284 bytes. Hinting and the layout tables are left out, because the rasterizer of the library uses neither.

## How it was made

The source is `ttf/DejaVuSans.ttf` (SHA-256 `7da195a74c55bef988d0d48f9508bd5d849425c1770dba5d7bfc6ce9ed848954`) of `dejavu-fonts-ttf-2.37.zip` (SHA-256 `7576310b219e04159d35ff61dd4a4ec4cdba4f35c00e002a136f00e96a908b0a`) from <https://github.com/dejavu-fonts/dejavu-fonts/releases/tag/version_2_37>, cut down with fonttools 4.66.1:

```sh
python -m fontTools.subset DejaVuSans.ttf --output-file=DejaVuSans-subset.ttf --unicodes=U+0020-007E,U+00A0-00FF,U+0100-017F,U+0400-04FF,U+2000-206F,U+20A0-20BF,U+2100-214F,U+0180-024F,U+1E00-1EFF,U+0370-03FF,U+2190-21FF,U+2202,U+2206,U+220F,U+2211,U+2212,U+2215,U+2219,U+221A,U+221E,U+222B,U+2248,U+2260,U+2264,U+2265,U+25A0,U+25A1,U+25AA,U+25AB,U+25B2,U+25BA,U+25BC,U+25C4,U+25CA,U+25CB,U+25CF,U+25E6,U+2605,U+2606,U+2713,U+2717 --no-hinting --layout-features= --drop-tables+=GSUB,GPOS,GDEF,kern,MATH,FFTM,gasp --notdef-outline --name-IDs=* --no-recalc-timestamp
```

The command gives the same bytes every time: SHA-256 `e1b35496ddb70a17ff5649f3e78203345197cec46e4d3ac012ff669331bd903b`. The test `BuiltInFont_IsTheRecordedSubsetOfDejaVuSans` holds the file to that hash, so a font that is replaced has to be replaced there and here as well.
