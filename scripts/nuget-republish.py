#!/usr/bin/env python3
"""
nuget-republish — figure out which Ecng NuGet packages need a republish.

Modes:
  (default)        Online check. For every Ecng package on nuget.org
                   compare the dependency versions baked into its latest
                   published nuspec against the latest version of each
                   dependency that is still allowed by the floating mask
                   in our current csproj. Print only the Ecng packages
                   whose nuspec is out of sync.

  --list-third-party  Offline scan. For every distinct third-party
                       NuGet package referenced by our csprojs, print
                       its floating mask plus the Ecng packages that
                       directly reference it.

  --deps NAME       Offline scan. Transitive ProjectReference consumers
                       of NAME (third-party id or Ecng folder name).

  --refs PROJECT    Offline. Project's ProjectReference + PackageReference.

  --all             Offline. Every publishable Ecng project (everything
                       except Tests).

Networking is best-effort — packages that the script can't resolve are
reported as "?" rather than masking the answer. Run a second time if
nuget.org is flaky.
"""

from __future__ import annotations

import argparse
import json
import re
import sys
import urllib.error
import urllib.request
import xml.etree.ElementTree as ET
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
NON_PUBLISHED = {"Tests"}
NOISE_PATTERNS = (
    re.compile(r"^Microsoft\.NET\.Test"),
    re.compile(r"^MSTest"),
    re.compile(r"^coverlet"),
    re.compile(r"^GitVersion"),
    re.compile(r"^System\."),
    re.compile(r"^Microsoft\.SourceLink"),
)
PACKAGE_REF_RE = re.compile(
    # Match both self-closing and element-bodied PackageReference forms.
    # group(1)=Include, group(2)=Version, group(3)=inner body (may be empty).
    r'<PackageReference\s+Include="([^"]+)"\s*[^>]*?Version="([^"]+)"'
    r'(?:\s*/>|\s*>(.*?)</PackageReference>)',
    re.DOTALL,
)
PROJECT_REF_RE = re.compile(r'<ProjectReference\s+Include="([^"]+)"')
PRIVATE_ASSETS_ALL_RE = re.compile(
    r'<PrivateAssets>\s*all\s*</PrivateAssets>', re.IGNORECASE
)
ECNG_OWNER = "Ecng"
NUGET_API = "https://api.nuget.org/v3-flatcontainer"


def is_third_party(name: str) -> bool:
    if name.startswith("Ecng."):
        return False
    return not any(p.match(name) for p in NOISE_PATTERNS)


def all_projects() -> list[str]:
    out = []
    for d in sorted(ROOT.iterdir()):
        if not d.is_dir():
            continue
        proj = d / f"{d.name}.csproj"
        if not proj.is_file():
            continue
        if d.name in NON_PUBLISHED:
            continue
        out.append(d.name)
    return out


def csproj_text(project: str) -> str:
    return (ROOT / project / f"{project}.csproj").read_text(encoding="utf-8")


def package_refs(project: str) -> list[tuple[str, str]]:
    """Return (id, version) pairs for PackageReferences that actually
    end up in the published nuspec. Entries with PrivateAssets=all (or
    Attribute form) are compile-only and are excluded."""
    out = []
    text = csproj_text(project)
    for inc, ver, body in PACKAGE_REF_RE.findall(text):
        # Inline attribute form: PrivateAssets="all" on the element itself.
        # Re-scan the original PackageReference opening tag for safety.
        opening = re.search(
            rf'<PackageReference\s+[^>]*Include="{re.escape(inc)}"[^>]*>',
            text,
        )
        opening_text = opening.group(0) if opening else ""
        if re.search(r'PrivateAssets\s*=\s*"all"', opening_text, re.IGNORECASE):
            continue
        if body and PRIVATE_ASSETS_ALL_RE.search(body):
            continue
        out.append((inc, ver))
    return out


def project_refs(project: str) -> list[str]:
    out = []
    for ref in PROJECT_REF_RE.findall(csproj_text(project)):
        norm = ref.replace("\\", "/").rstrip("/")
        name = Path(norm).stem
        out.append(name)
    return out


def load_versions_props() -> dict[str, str]:
    f = ROOT / "common_versions.props"
    if not f.is_file():
        return {}
    text = f.read_text(encoding="utf-8")
    return {
        prop: ver
        for prop, ver in re.findall(r"<([A-Za-z0-9_]+Ver)>([^<]+)</\1>", text)
    }


def resolve_version_token(token: str, versions: dict[str, str]) -> str:
    """Resolve a Version="$(SomeVer)" token to the literal floating mask."""
    m = re.fullmatch(r"\$\(([A-Za-z0-9_]+)\)", token)
    if not m:
        return token
    return versions.get(m.group(1), token)


def http_get_json(url: str) -> dict | None:
    try:
        with urllib.request.urlopen(url, timeout=15) as resp:
            return json.loads(resp.read().decode("utf-8"))
    except (urllib.error.URLError, json.JSONDecodeError):
        return None


def http_get_text(url: str) -> str | None:
    try:
        with urllib.request.urlopen(url, timeout=15) as resp:
            return resp.read().decode("utf-8")
    except urllib.error.URLError:
        return None


def latest_versions(package_id: str) -> list[str] | None:
    data = http_get_json(f"{NUGET_API}/{package_id.lower()}/index.json")
    if not data:
        return None
    return list(data.get("versions") or [])


def is_stable(v: str) -> bool:
    return "-" not in v


def resolve_mask(mask: str, versions: list[str]) -> str | None:
    """Pick the highest version on nuget that satisfies the floating mask."""
    if not versions:
        return None
    if "*" not in mask:
        return mask
    pattern = re.escape(mask).replace(r"\*", r".*")
    rx = re.compile(f"^{pattern}$")
    matches = [v for v in versions if rx.match(v) and is_stable(v)]
    if not matches:
        matches = [v for v in versions if rx.match(v)]
    return matches[-1] if matches else None


def latest_published_nuspec(package_id: str) -> ET.Element | None:
    versions = latest_versions(package_id)
    if not versions:
        return None
    stable = [v for v in versions if is_stable(v)]
    chosen = (stable or versions)[-1]
    pkg_l = package_id.lower()
    nuspec = http_get_text(f"{NUGET_API}/{pkg_l}/{chosen}/{pkg_l}.nuspec")
    if not nuspec:
        return None
    try:
        return ET.fromstring(nuspec)
    except ET.ParseError:
        return None


def nuspec_dependencies(nuspec: ET.Element) -> dict[str, set[str]]:
    """Return {id: {version, ...}} from a nuspec.

    A nuspec can contain per-TFM dependency groups, each with its own version
    of the same dep id (e.g. net6 -> 8.0.3, net10 -> 10.0.6 for
    Microsoft.Extensions.Logging.Abstractions). Use a set to keep all of them.
    """
    out: dict[str, set[str]] = {}
    for dep in nuspec.findall(".//{*}dependency"):
        pkg = dep.attrib.get("id")
        ver = dep.attrib.get("version", "")
        if pkg and ver:
            out.setdefault(pkg, set()).add(
                ver.lstrip("[").rstrip("]").split(",")[0].strip()
            )
    return out


# --------------------------- modes ----------------------------------------


def mode_default() -> int:
    """Online: print only Ecng packages whose published nuspec lags behind
    what the current floating masks resolve to on nuget.org.

    Algorithm:
      1. For every Ecng project with third-party PackageReference, collect
         the set of floating masks per (project, dep_id).
      2. Pull each third-party's full version list from nuget.org once;
         resolve each mask to the highest version that satisfies it now.
      3. For every Ecng project pull its latest published nuspec; collect
         the set of versions referenced per dep_id (across TFM groups).
      4. A project is stale if for any of its deps the published versions
         do not include any mask-resolution that the current csproj would
         produce. That captures either: the dep moved forward and no mask
         resolves to the published version anymore, or the mask in csproj
         was tightened/loosened since the last publish.
    """
    versions_props = load_versions_props()

    # plan: project -> {dep_id -> set of floating masks}
    plan: dict[str, dict[str, set[str]]] = {}
    for proj in all_projects():
        for pkg, ver in package_refs(proj):
            if not is_third_party(pkg):
                continue
            mask = resolve_version_token(ver, versions_props)
            plan.setdefault(proj, {}).setdefault(pkg, set()).add(mask)

    if not plan:
        print("No Ecng packages with third-party PackageReference found.")
        return 0

    # Cache nuget version lists for every distinct third-party id.
    third_parties = sorted({dep for refs in plan.values() for dep in refs})
    versions_cache: dict[str, list[str] | None] = {}

    def fetch_versions(pkg: str) -> tuple[str, list[str] | None]:
        return pkg, latest_versions(pkg)

    with ThreadPoolExecutor(max_workers=8) as pool:
        for pkg, vs in pool.map(fetch_versions, third_parties):
            versions_cache[pkg] = vs

    def resolve(pkg: str, mask: str) -> str | None:
        return resolve_mask(mask, versions_cache.get(pkg) or [])

    # Pull every Ecng nuspec.
    def fetch_nuspec(name: str) -> tuple[str, ET.Element | None]:
        return name, latest_published_nuspec(f"Ecng.{name}")

    nuspec_cache: dict[str, ET.Element | None] = {}
    with ThreadPoolExecutor(max_workers=8) as pool:
        for name, nuspec in pool.map(fetch_nuspec, plan.keys()):
            nuspec_cache[name] = nuspec

    # Diff per project.
    print("# Stale Ecng packages on nuget.org (their published nuspec is")
    print("# out of sync with what current csproj masks resolve to today).")
    print("# Format: Ecng.<name>: <dep_id> nuspec={...} expected={...}")
    print()

    stale_lines: list[str] = []
    stale_packages: set[str] = set()
    unknown_packages: list[str] = []

    for proj in sorted(plan.keys()):
        nuspec = nuspec_cache.get(proj)
        if nuspec is None:
            unknown_packages.append(proj)
            continue
        published = nuspec_dependencies(nuspec)
        for dep_id, masks in plan[proj].items():
            published_versions = published.get(dep_id, set())
            if not published_versions:
                # Dep is in csproj but not in published nuspec: project not
                # yet published with this dep, mark as stale.
                stale_packages.add(proj)
                stale_lines.append(
                    f"Ecng.{proj}: {dep_id} nuspec=(missing) "
                    f"expected={{{', '.join(sorted(masks))}}}"
                )
                continue
            current_resolutions = {
                resolve(dep_id, m) for m in masks
            } - {None}
            # Stale if no published version equals any current resolution.
            if not (published_versions & current_resolutions):
                stale_packages.add(proj)
                stale_lines.append(
                    f"Ecng.{proj}: {dep_id} "
                    f"nuspec={{{', '.join(sorted(published_versions))}}} "
                    f"expected={{{', '.join(sorted(v for v in current_resolutions if v))}}}"
                )

    if stale_lines:
        for line in stale_lines:
            print(line)
        print()
    else:
        print("(every published nuspec is in sync with current floating masks)")
        print()

    if unknown_packages:
        print("# Could not fetch nuspec for (network/never published?):")
        for p in unknown_packages:
            print(f"  Ecng.{p}")
        print()

    print(f"# Republish list ({len(stale_packages)} packages):")
    if stale_packages:
        for proj in sorted(stale_packages):
            print(f"Ecng.{proj}")
    else:
        print("(none)")
    return 0


def mode_list_third_party() -> int:
    versions_props = load_versions_props()

    grouped: dict[str, dict[str, set[str]]] = {}
    for proj in all_projects():
        for pkg, ver in package_refs(proj):
            if not is_third_party(pkg):
                continue
            mask = resolve_version_token(ver, versions_props)
            grouped.setdefault(pkg, {}).setdefault(mask, set()).add(f"Ecng.{proj}")

    for pkg in sorted(grouped):
        for mask in sorted(grouped[pkg]):
            print(f"{pkg} ({mask})")
            for consumer in sorted(grouped[pkg][mask]):
                print(f"  {consumer}")
            print()
    return 0


def mode_deps(target: str) -> int:
    consumers: set[str] = set()
    for proj in all_projects():
        text = csproj_text(proj)
        if (
            re.search(rf'PackageReference\s+Include="{re.escape(target)}"', text)
            or target in project_refs(proj)
        ):
            consumers.add(proj)

    progress = True
    while progress:
        progress = False
        for proj in all_projects():
            if proj in consumers:
                continue
            for r in project_refs(proj):
                if r in consumers:
                    consumers.add(proj)
                    progress = True
                    break

    for proj in sorted(consumers):
        print(f"Ecng.{proj}")
    return 0


def mode_refs(project: str) -> int:
    if not (ROOT / project / f"{project}.csproj").is_file():
        print(f"Project not found: {project}", file=sys.stderr)
        return 1
    print("# ProjectReference (Ecng dependencies):")
    for r in project_refs(project):
        print(f"  Ecng.{r}")
    print()
    print("# PackageReference (third-party):")
    versions_props = load_versions_props()
    for pkg, ver in package_refs(project):
        if not is_third_party(pkg):
            continue
        mask = resolve_version_token(ver, versions_props)
        print(f"  {pkg} ({mask})")
    return 0


def mode_all() -> int:
    for proj in all_projects():
        print(f"Ecng.{proj}")
    return 0


def main() -> int:
    p = argparse.ArgumentParser(
        description="Figure out which Ecng NuGet packages need a republish.",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=__doc__,
    )
    sub = p.add_subparsers(dest="cmd")

    sub.add_parser("list-third-party", aliases=["list"], help="offline list of direct third-party deps grouped per package")
    deps = sub.add_parser("deps", help="transitive ProjectReference consumers of NAME")
    deps.add_argument("name")
    refs = sub.add_parser("refs", help="ProjectReference + PackageReference of PROJECT")
    refs.add_argument("project")
    sub.add_parser("all", help="every publishable Ecng project")

    args = p.parse_args()
    if args.cmd in (None, "default"):
        return mode_default()
    if args.cmd in ("list-third-party", "list"):
        return mode_list_third_party()
    if args.cmd == "deps":
        return mode_deps(args.name)
    if args.cmd == "refs":
        return mode_refs(args.project)
    if args.cmd == "all":
        return mode_all()
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
