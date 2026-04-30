# Ecng NuGet republish guide

How to answer "this NuGet package was updated — what should I republish?".
**Don't guess, run the script.**

## Context

- This repo is a monorepo. Each top-level folder with `*.csproj` (except `Tests`)
  ships as `Ecng.<Folder>` on nuget.org.
- Inner projects reference each other via `<ProjectReference>`. At
  `nuget pack` time these become `<PackageReference>` entries with a
  pinned version in the published nuspec.
- Third-party versions in the repo are **floating** (`*`-masks in
  `common_versions.props` via `$(...Ver)` variables). Each pack resolves
  the mask to whatever is current on nuget.org at that moment; the
  already-published nuspec does not get rewritten retroactively.
- The republish question is therefore: which of our published packages
  carry an older third-party version in their nuspec than what the
  current floating mask would resolve to today.

## Main tool

`scripts/nuget-republish.py` (Python 3) does the online comparison
against `https://api.nuget.org/v3-flatcontainer`.

```bash
# Online stale-check (default) — the actual republish list right now.
# Hits nuget.org, takes 20–60 s.
python scripts/nuget-republish.py

# Offline modes:
python scripts/nuget-republish.py list-third-party      # third-party deps grouped per package
python scripts/nuget-republish.py deps <name>           # transitive ProjectReference consumers
python scripts/nuget-republish.py refs <project>        # what one project references
python scripts/nuget-republish.py all                   # every publishable Ecng package
```

## What the default mode does

- Parses every csproj.
- Skips `PrivateAssets=all` (compile-only refs don't reach the nuspec —
  e.g. `Microsoft.Windows.CsWin32`, `JetBrains.Annotations`).
- Pulls the latest published nuspec for each Ecng package via the
  flatcontainer API.
- Reads **all** `<dependency>` entries (including same id under several
  TFM groups, e.g. `Microsoft.Extensions.Logging.Abstractions` 10.0.6
  for net10 and 8.0.3 for net6).
- Resolves every floating mask in csproj against current nuget.org and
  picks the highest stable match.
- A package is stale when no published version of a dependency equals
  any of the currently-resolved masks.

## Don't substitute tables for the script's output

- If the user asks "what to republish" — run the script and return its
  output verbatim. Stale list is the source of truth.
- If nuget.org times out, run again. The script lists unreachable
  packages separately rather than reporting them as clean.
- An empty list is a valid normal state.

## Not published

- `Tests/` — unit-test project.
- `UnitTesting/` ships as `Ecng.UnitTesting`.
- `TestResults/`, `nupkgs/`, `_reviewResults/`, `reviewResults/` — scratch.
