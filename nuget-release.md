# Ecng NuGet releases

The GitHub Actions workflow at .github/workflows/nuget-release.yml is an
independent replacement for the private Web Publisher for Ecng only.

## Triggers

* Pull requests: offline tests, dry-run packing and metadata validation.
* Published GitHub releases: build, validate, push to nuget.org.
* Manual workflow_dispatch: preview by default; opt in to publish.

## Trusted Publishing (OIDC, no long-lived API key)

Ecng publishes through NuGet.org Trusted Publishing. No GitHub secret named
NUGET_API_KEY is needed: NuGet/login@v1 exchanges the publishing job's GitHub
OIDC token for a short-lived API key just before pushing packages.

In the NuGet.org account that owns the existing Ecng packages (StockSharp),
create a Trusted Publishing policy with:

* Provider: GitHub Actions
* NuGet.org policy owner / NuGet login user: StockSharp
* Repository owner: StockSharp
* Repository: Ecng
* Workflow filename: nuget-release.yml (filename only, not the full path)
* Environment: nuget
* Scope: push new package versions for Ecng.*; also enable new packages if
  you intend to publish previously nonexistent Ecng packages

The policy owner must have rights to all packages being published. The NuGet
package owner displayed for Ecng.Common is StockSharp.

In GitHub StockSharp/Ecng > Settings > Environments, create an environment
named nuget. Consider requiring manual approval for this environment before
production pushes. If setting deployment-branch/tag restrictions, account for
the fact that published GitHub releases run on refs/tags/... rather than master.

On a published GitHub release or an explicitly opted-in manual dispatch, the
publish job requests id-token: write; NuGet/login@v1 obtains a 1-hour key and
passes it to the existing publishing script. Tests, previews and PR runs do
not request a NuGet token or publish packages.

Official docs: https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing

## Cascade

The release planner reads the latest stable package and its nuspec on nuget.org,
the per-package source commit, common build files, and existing floating
dependency version checks. It selects source-changed packages and packages
whose published internal dependencies are obsolete, recursively adds downstream
ProjectReference consumers, and orders all selected packages dependencies-first.

Versions are calculated independently as the next patch release of each
affected package. A generated local Directory.Build.targets supplies every
referenced project's correct version to MSBuild, without changing csproj files.
A retry after a partially successful release only republishes the still-stale
dependents, not packages already pushed successfully.

## Metadata and symbols

For every previously published package, the live nuspec supplies the original
title, description, authors, owners, copyright, tags, URLs and repository
metadata. The package version, release notes, repository commit, and dependency
versions legitimately change. New packages use fallback StockSharp branding.

Every selected package is validated against its published metadata and its
internal dependency versions before publishing. The workflow also packages
Ecng.Common as a non-published metadata regression sample. Portable PDBs in
.snupkg files must be present and verifiable, and the matching .nupkg and
.snupkg are saved as GitHub Actions artifacts.

A deliberate change to metadata needs a reviewed change to the policy/test.
The aim is metadata parity, not byte-identical packages.

## Local preview

1. python3 -m unittest discover -s scripts/tests -p test_nuget_release.py -v
2. python3 scripts/nuget-release.py plan
3. dotnet restore Ecng.slnx
4. dotnet build Ecng.slnx -c Release --no-restore
5. python3 scripts/nuget-release.py pack
6. python3 scripts/nuget-release.py verify

Use the push command only for a deliberately approved production release.
If a publish fails halfway through, rerun it after fixing the cause: NuGet
packages are immutable, and the planner discovers stale downstream packages.
