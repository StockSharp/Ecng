#!/usr/bin/env python3
"""Plan, pack and validate independent but cascading Ecng NuGet releases.

No third-party Python dependencies. Never publishes unless 'push' is invoked.
The published .nuspec is the metadata baseline for each existing package.
"""
from __future__ import annotations

import argparse
import heapq
import json
import os
import re
import subprocess
import sys
import urllib.error
import urllib.request
import xml.etree.ElementTree as ET
import zipfile
from dataclasses import dataclass
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
FEED = "https://api.nuget.org/v3-flatcontainer"
METADATA = {
    "title": "Title",
    "description": "Description",
    "authors": "Authors",
    "owners": "Owners",
    "copyright": "Copyright",
    "tags": "PackageTags",
    "projectUrl": "PackageProjectUrl",
    "iconUrl": "PackageIconUrl",
    "licenseUrl": "PackageLicenseUrl",
}
REPO = "https://github.com/StockSharp/Ecng"
# Common (non-package) build inputs. For individual floating versions, see
# changed_version_properties below: that does NOT republish unrelated packages.
GLOBAL_FILES = ("common.props",)
PACKAGE_NAME = re.compile(r"^[A-Za-z0-9][A-Za-z0-9._-]*$")
VERSION = re.compile(r"^(\d+)\.(\d+)\.(\d+)(?:\.\d+)?$")


def run(*args: str) -> str:
    return subprocess.check_output(args, cwd=ROOT, text=True, stderr=subprocess.PIPE).strip()


def api(path: str) -> bytes | None:
    try:
        req = urllib.request.Request(FEED + "/" + path, headers={"User-Agent": "EcngNuGetRelease/1.0"})
        with urllib.request.urlopen(req, timeout=25) as response:
            return response.read()
    except urllib.error.HTTPError as error:
        if error.code == 404:
            return None
        raise


def stable_version(version: str) -> tuple[int, ...] | None:
    match = VERSION.fullmatch(version)
    return tuple(map(int, match.groups())) if match else None


def next_version(version: str | None) -> str:
    if version is None:
        return "1.0.0"
    parts = stable_version(version)
    if parts is None:
        raise ValueError(f"Unsupported stable version: {version}")
    return f"{parts[0]}.{parts[1]}.{parts[2] + 1}"


def latest(package: str) -> dict | None:
    package_lower = package.lower()
    data = api(f"{package_lower}/index.json")
    if data is None:
        return None
    versions = json.loads(data)["versions"]
    candidates = [v for v in versions if stable_version(v) is not None]
    if not candidates:
        raise ValueError(f"{package}: no stable SemVer 1.0 version found")
    version = max(candidates, key=stable_version)
    nuspec = api(f"{package_lower}/{version}/{package_lower}.nuspec")
    if nuspec is None:
        raise RuntimeError(f"Published package has no nuspec: {package} {version}")
    return parse_nuspec(nuspec, version)


def parse_nuspec(data: bytes, version: str) -> dict:
    root = ET.fromstring(data)
    meta = root.find("{*}metadata")
    if meta is None:
        raise ValueError("Missing nuspec metadata")

    def value(name: str) -> str:
        el = meta.find("{*}" + name)
        return (el.text or "").strip() if el is not None else ""

    repo = meta.find("{*}repository")
    deps = {}
    for group in meta.findall(".//{*}dependencies/{*}group"):
        # Keep every TFM: the same id can have different lower bounds.
        for dep in group.findall("{*}dependency"):
            deps.setdefault(dep.attrib["id"].lower(), set()).add(dep.attrib.get("version", ""))
    for dep in meta.findall("./{*}dependencies/{*}dependency"):
        deps.setdefault(dep.attrib["id"].lower(), set()).add(dep.attrib.get("version", ""))
    return {
        "id": value("id"),
        "version": value("version") or version,
        "metadata": {name: value(name) for name in METADATA},
        "readme": value("readme"),
        "repo_url": repo.attrib.get("url", "") if repo is not None else "",
        "repo_type": repo.attrib.get("type", "") if repo is not None else "",
        "commit": repo.attrib.get("commit", "") if repo is not None else "",
        "dependencies": {k: sorted(v) for k, v in deps.items()},
    }


@dataclass(frozen=True)
class Project:
    name: str
    folder: str
    refs: frozenset[str]
    text: str

    @property
    def package(self) -> str:
        return f"Ecng.{self.name}"


def projects(root: Path = ROOT) -> dict[str, Project]:
    found = {}
    for proj in sorted(root.glob("*/*.csproj")):
        folder = proj.parent.name
        if folder == "Tests" or proj.stem != folder:
            continue
        content = proj.read_text(encoding="utf-8-sig")
        refs = set()
        # XML rather than a filename regex handles attribute order and conditional
        # ProjectReference (all TFMs participate in the dependency closure).
        for node in ET.fromstring(content).iter("ProjectReference"):
            if not node.get("Include"):
                continue
            ref = Path(node.get("Include").replace("\\", "/")).stem
            refs.add(ref)
        found[folder] = Project(folder, folder, frozenset(refs), content)

    for project in found.values():
        missing = project.refs - found.keys()
        if missing:
            raise ValueError(f"{project.name}: unresolved local projects {sorted(missing)}")
    return found


def closure(changed: set[str], all_projects: dict[str, Project]) -> set[str]:
    result = set(changed)
    while True:
        more = {p.name for p in all_projects.values() if p.refs & result}
        new = result | more
        if new == result:
            return result
        result = new


def ordered(selected: set[str], all_projects: dict[str, Project]) -> list[str]:
    indegree = {p: len(all_projects[p].refs & selected) for p in selected}
    outgoing = {p: set() for p in selected}
    for name in selected:
        for dep in all_projects[name].refs & selected:
            outgoing[dep].add(name)
    ready = [name for name, count in indegree.items() if not count]
    heapq.heapify(ready)
    order = []
    while ready:
        name = heapq.heappop(ready)
        order.append(name)
        for dependent in sorted(outgoing[name]):
            indegree[dependent] -= 1
            if not indegree[dependent]:
                heapq.heappush(ready, dependent)
    if len(order) != len(selected):
        raise ValueError("Cycle in Ecng ProjectReference graph")
    return order


def changed_paths(commit: str, folder: str) -> bool:
    # Missing history must not silently classify a package as clean.
    run("git", "cat-file", "-e", commit + "^{commit}")
    return bool(run("git", "diff", "--name-only", commit, "HEAD", "--", folder, *GLOBAL_FILES))


def changed_version_properties(commit: str, project: Project) -> bool:
    try:
        previous = run("git", "show", commit + ":common_versions.props")
    except subprocess.CalledProcessError:
        return True
    current = (ROOT / "common_versions.props").read_text(encoding="utf-8-sig")
    rx = re.compile(r"<([A-Za-z_]\w*)>([^<]+)</\1>")
    prev = dict(rx.findall(previous))
    now = dict(rx.findall(current))
    for prop in set(prev) | set(now):
        if prev.get(prop) != now.get(prop) and f"$({prop})" in project.text:
            return True
    return False


def has_newer_internal_dep(snapshot: dict | None, refs: frozenset[str], published: dict[str, dict | None]) -> bool:
    if snapshot is None:
        return True
    for ref in refs:
        expected = published[ref]["version"] if published[ref] else None
        ranges = snapshot["dependencies"].get(("Ecng." + ref).lower(), [])
        # Compile-time analyzer references may not be declared as NuGet dependencies.
        # Adding a PackageReference changes the csproj and is detected by Git;
        # absence from an old nuspec must not trigger endless republishing.
        if ranges and (not expected or any(not range_has_floor(r, expected) for r in ranges)):
            return True
    return False


def range_has_floor(spec: str, expected: str) -> bool:
    # NuGet encodes the lower-bound as [x,), [x], or x.
    match = re.match(r"^\s*\[?\s*(\d+\.\d+\.\d+)", spec)
    return match is not None and match.group(1) == expected


def stale_third_party() -> set[str]:
    # Use the repository's existing check for floating third-party dependencies.
    result = run(sys.executable, "scripts/nuget-republish.py")
    if "# Could not fetch nuspec" in result:
        raise RuntimeError("Third-party check could not inspect all NuGet packages:\n" + result)
    if "# Republish list" not in result:
        raise RuntimeError("Unrecognized nuget-republish.py output:\n" + result)
    section = result.split("# Republish list", 1)[1]
    return {p.removeprefix("Ecng.") for p in re.findall(r"^Ecng\.([A-Za-z0-9_.]+)$", section, re.M)}


def create_plan(all_projects: dict[str, Project], published: dict[str, dict | None],
                source_changed: set[str], external_changed: set[str]) -> dict:
    direct = set(source_changed) | set(external_changed)
    for project in all_projects.values():
        if has_newer_internal_dep(published[project.name], project.refs, published):
            direct.add(project.name)
    selected = closure(direct, all_projects)
    order = ordered(selected, all_projects)
    versions = {name: next_version(published[name]["version"]) if name in selected
                else published[name]["version"] for name in all_projects
                if name in selected or published[name] is not None}
    # All referenced projects must have a version even when they aren't selected.
    for name in order:
        for ref in all_projects[name].refs:
            if ref not in versions:
                raise RuntimeError(f"{name}: unpublished dependency {ref} was not planned")
    return {"order": order, "direct": sorted(direct), "versions": versions,
            "baseline": {name: published[name] for name in all_projects},
            "sample": "Common", "repo": REPO, "commit": run("git", "rev-parse", "HEAD")}


def xml_escape(value: str) -> str:
    return (value.replace("&", "&amp;").replace("<", "&lt;")
            .replace(">", "&gt;").replace('"', "&quot;").replace("'", "&apos;"))


def write_targets(plan: dict, path: Path) -> None:
    lines = ["<Project>"]
    for name, version in sorted(plan["versions"].items()):
        # Only versions planned for publishing and existing referenced projects
        # get set; other assembly versions remain the repository defaults.
        lines.append(f'  <PropertyGroup Condition="\'$(MSBuildProjectName)\' == \'{xml_escape(name)}\'">')
        lines.append(f"    <Version>{xml_escape(version)}</Version>")
        lines.append(f"    <PackageVersion>{xml_escape(version)}</PackageVersion>")
        if name in plan["order"] or name == plan.get("sample"):
            previous = plan["baseline"][name]
            meta = previous["metadata"] if previous is not None else {}
            if previous is None:
                meta = {"title": "Ecng." + name, "description": "Ecng system framework",
                        "authors": "StockSharp Platform LLC https://stocksharp.com",
                        "owners": "StockSharp Platform LLC",
                        "projectUrl": "https://stocksharp.com",
                        "licenseUrl": "https://stocksharp.com/en/products/eula/"}
            for nuspec_name, prop in METADATA.items():
                if meta.get(nuspec_name):
                    lines.append(f"    <{prop}>{xml_escape(meta[nuspec_name])}</{prop}>")
            # common.props already writes AssemblyCopyrightAttribute manually;
            # passing Copyright to SDK pack must not create a duplicate attribute.
            if meta.get("copyright"):
                lines.append("    <GenerateAssemblyCopyrightAttribute>false</GenerateAssemblyCopyrightAttribute>")
            repo_url = previous["repo_url"] if previous and previous["repo_url"] else REPO
            repo_type = previous["repo_type"] if previous and previous["repo_type"] else "git"
            lines += [
                f"    <RepositoryUrl>{xml_escape(repo_url)}</RepositoryUrl>",
                f"    <RepositoryType>{xml_escape(repo_type)}</RepositoryType>",
                f"    <RepositoryCommit>{xml_escape(plan['commit'])}</RepositoryCommit>",
                "    <PublishRepositoryUrl>true</PublishRepositoryUrl>",
                "    <IncludeSymbols>true</IncludeSymbols>",
                "    <SymbolPackageFormat>snupkg</SymbolPackageFormat>",
                "    <DebugType>portable</DebugType>",
                "    <DebugSymbols>true</DebugSymbols>",
                "    <Deterministic>true</Deterministic>",
                "    <EmbedUntrackedSources>true</EmbedUntrackedSources>",
                f"    <PackageReleaseNotes>Built from {xml_escape(plan['commit'])}</PackageReleaseNotes>",
            ]
            # Roslyn components manually pack DLLs into analyzers/dotnet/cs
            # with IncludeBuildOutput=false. The SDK then has NO build-output
            # symbols to put in the .snupkg (NU5017). Pack the managed output
            # in the *same* analyzer path, removing the old duplicate None item.
            project_file = (ROOT / name / f"{name}.csproj").read_text(encoding="utf-8-sig")
            is_analyzer_package = (
                "<IncludeBuildOutput>false</IncludeBuildOutput>" in project_file
                and 'PackagePath="analyzers/dotnet/cs"' in project_file
            )
            if is_analyzer_package:
                lines.append("    <IncludeBuildOutput>true</IncludeBuildOutput>")
                lines.append("    <BuildOutputTargetFolder>analyzers/dotnet/cs</BuildOutputTargetFolder>")
            if (ROOT / name / "README.md").is_file():
                lines.append("    <PackageReadmeFile>README.md</PackageReadmeFile>")
        lines.append("  </PropertyGroup>")
        if name in plan["order"] or name == plan.get("sample"):
            project_file = (ROOT / name / f"{name}.csproj").read_text(encoding="utf-8-sig")
            is_analyzer_package = (
                "<IncludeBuildOutput>false</IncludeBuildOutput>" in project_file
                and 'PackagePath="analyzers/dotnet/cs"' in project_file
            )
            has_readme = (ROOT / name / "README.md").is_file()
            if has_readme or is_analyzer_package:
                lines.append(f'  <ItemGroup Condition="\'$(MSBuildProjectName)\' == \'{xml_escape(name)}\'">')
                if has_readme:
                    lines.append('    <None Include="$(MSBuildProjectDirectory)/README.md" Pack="true" PackagePath="/" />')
                if is_analyzer_package:
                    lines.append(r'    <None Remove="bin\$(Configuration)\netstandard2.0\$(AssemblyName).dll" />')
                lines.append("  </ItemGroup>")
    lines.append("</Project>")
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def make_plan(args: argparse.Namespace) -> None:
    all_projects = projects()
    published = {name: latest(p.package) for name, p in all_projects.items()}
    direct = set()
    for name, project in all_projects.items():
        baseline = published[name]
        if baseline is None or not baseline["commit"]:
            direct.add(name)
            continue
        if changed_paths(baseline["commit"], project.folder) or changed_version_properties(baseline["commit"], project):
            direct.add(name)
    third_party = stale_third_party() if not args.no_third_party_check else set()
    plan = create_plan(all_projects, published, direct, third_party)
    Path(args.output).write_text(json.dumps(plan, indent=2) + "\n", encoding="utf-8")
    write_targets(plan, Path(args.targets))
    print(f"Direct: {', '.join(plan['direct']) or '(none)'}")
    print(f"Release order: {', '.join(plan['order']) or '(nothing to publish)'}")
    for name in plan["order"]:
        print(f"  Ecng.{name} {plan['versions'][name]}")


def preserve_legacy_owner(package: Path, owner: str) -> None:
    """SDK pack cannot emit the deprecated nuspec owners field.

    Old Ecng packages include it, so retain metadata parity without replacing
    NuGet.org account ownership (which is controlled independently).
    """
    if not owner:
        return
    replacement = package.with_name(package.name + ".tmp")
    try:
        with zipfile.ZipFile(package, "r") as src, zipfile.ZipFile(replacement, "w") as dest:
            if any(n.lower().endswith(".signature.p7s") for n in src.namelist()):
                raise ValueError(f"Refusing to modify signed package: {package}")
            for entry in src.infolist():
                data = src.read(entry.filename)
                if entry.filename.lower().endswith(".nuspec"):
                    root = ET.fromstring(data)
                    metadata = root.find("{*}metadata")
                    if metadata is None:
                        raise ValueError("Package nuspec has no metadata")
                    authors = metadata.find("{*}authors")
                    if authors is None:
                        raise ValueError("Package nuspec has no authors")
                    old = metadata.find("{*}owners")
                    ns = metadata.tag.split("}")[0].lstrip("{") if "}" in metadata.tag else ""
                    if ns:
                        ET.register_namespace("", ns)
                    if old is None:
                        old = ET.Element(f"{{{ns}}}owners" if ns else "owners")
                        metadata.insert(list(metadata).index(authors) + 1, old)
                    old.text = owner
                    data = ET.tostring(root, encoding="utf-8", xml_declaration=True)
                dest.writestr(entry, data)
        os.replace(replacement, package)
    finally:
        if replacement.exists():
            replacement.unlink()


def pack(args: argparse.Namespace) -> None:
    plan = json.loads(Path(args.plan).read_text(encoding="utf-8"))
    dest = Path(args.directory)
    dest.mkdir(parents=True, exist_ok=True)
    targets = list(dict.fromkeys([*plan["order"], plan.get("sample", "Common")]))
    for name in targets:
        cmd = ["dotnet", "pack", f"{name}/{name}.csproj", "-c", "Release",
               "--no-build", "--no-restore", "-o", str(dest.resolve())]
        print("+", " ".join(cmd), flush=True)
        subprocess.run(cmd, cwd=ROOT, check=True)
        previous = plan["baseline"][name]
        if previous:
            preserve_legacy_owner(dest / f"Ecng.{name}.{plan['versions'][name]}.nupkg",
                                  previous["metadata"].get("owners", ""))


def read_archived_nuspec(path: Path) -> dict:
    with zipfile.ZipFile(path) as z:
        specs = [n for n in z.namelist() if n.lower().endswith(".nuspec")]
        if len(specs) != 1:
            raise AssertionError(f"{path}: expected one nuspec, got {specs}")
        return parse_nuspec(z.read(specs[0]), "")


def verify(args: argparse.Namespace) -> None:
    plan = json.loads(Path(args.plan).read_text(encoding="utf-8"))
    out = Path(args.directory)
    failures = []
    targets = list(dict.fromkeys([*plan["order"], plan.get("sample", "Common")]))
    all_projects = projects()
    for name in targets:
        pkg_id = "Ecng." + name
        ver = plan["versions"][name]
        package = out / f"{pkg_id}.{ver}.nupkg"
        symbols = out / f"{pkg_id}.{ver}.snupkg"
        try:
            if not package.is_file() or not symbols.is_file():
                raise AssertionError(f"{pkg_id}: missing .nupkg or .snupkg")
            spec = read_archived_nuspec(package)
            if spec["id"].lower() != pkg_id.lower() or spec["version"] != ver:
                raise AssertionError(f"{pkg_id}: packaged nuspec identity {spec['id']} {spec['version']} != {ver}")
            if spec["repo_url"].rstrip("/") != (plan["baseline"][name]["repo_url"] if plan["baseline"][name] and plan["baseline"][name]["repo_url"] else REPO).rstrip("/"):
                raise AssertionError(f"{pkg_id}: repository URL changed")
            if spec["commit"] != plan["commit"]:
                raise AssertionError(f"{pkg_id}: repository commit is not checkout SHA")
            previous = plan["baseline"][name]
            if previous:
                for field, expected in previous["metadata"].items():
                    actual = spec["metadata"][field]
                    if expected and actual != expected:
                        raise AssertionError(f"{pkg_id}: metadata {field} changed: {expected!r} -> {actual!r}")
            for ref in all_projects[name].refs:
                expected = plan["versions"][ref]
                ranges = spec["dependencies"].get(("Ecng." + ref).lower(), [])
                if not ranges or not all(range_has_floor(v, expected) for v in ranges):
                    raise AssertionError(f"{pkg_id}: dependency Ecng.{ref} expected {expected}, got {ranges}")
            with zipfile.ZipFile(symbols) as z:
                pdblist = [n for n in z.namelist() if n.lower().endswith(".pdb")]
                if not pdblist:
                    raise AssertionError(f"{pkg_id}: .snupkg contains no PDB")
                for pdb in pdblist:
                    if not z.read(pdb).startswith(b"BSJB"):
                        raise AssertionError(f"{pkg_id}: not a portable PDB: {pdb}")
            with zipfile.ZipFile(package) as z:
                if previous and previous.get("readme") and not any(n.lower() == "readme.md" for n in z.namelist()):
                    raise AssertionError(f"{pkg_id}: README missing")
            print(f"OK {pkg_id} {ver}: baseline metadata, dependencies, portable debug symbols")
        except Exception as exc:
            failures.append(str(exc))
    if failures:
        raise AssertionError("\n".join(failures))


def push(args: argparse.Namespace) -> None:
    import os
    key = os.environ.get("NUGET_API_KEY")
    if not key:
        raise RuntimeError("NUGET_API_KEY secret is missing; no packages pushed")
    plan = json.loads(Path(args.plan).read_text(encoding="utf-8"))
    verify(args)
    for name in plan["order"]:
        package = Path(args.directory) / f"Ecng.{name}.{plan['versions'][name]}.nupkg"
        cmd = ["dotnet", "nuget", "push", str(package), "--source",
               "https://api.nuget.org/v3/index.json", "--api-key", key, "--skip-duplicate"]
        # Never echo arguments because they contain the API key.
        print(f"Publishing {package.name} (and its .snupkg)", flush=True)
        subprocess.run(cmd, cwd=ROOT, check=True)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest="command", required=True)
    p = sub.add_parser("plan")
    p.add_argument("--output", default="nuget-release-plan.json")
    p.add_argument("--targets", default="Directory.Build.targets")
    p.add_argument("--no-third-party-check", action="store_true", help="Development only")
    p.set_defaults(func=make_plan)
    for command, action in (("pack", pack), ("verify", verify), ("push", push)):
        p = sub.add_parser(command)
        p.add_argument("--plan", default="nuget-release-plan.json")
        p.add_argument("--directory", default="nuget-release-artifacts")
        p.set_defaults(func=action)
    args = parser.parse_args()
    args.func(args)


if __name__ == "__main__":
    main()
