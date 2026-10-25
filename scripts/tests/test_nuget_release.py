"""Offline regression tests for the Ecng NuGet release planner.

Run: python -m unittest discover -s scripts/tests -p "test_nuget_release.py"
"""
import importlib.util
import tempfile
import unittest
import zipfile
from pathlib import Path
from unittest.mock import patch

SCRIPT = Path(__file__).resolve().parents[1] / "nuget-release.py"
spec = importlib.util.spec_from_file_location("ecng_nuget_release", SCRIPT)
release = importlib.util.module_from_spec(spec)
import sys
sys.modules[spec.name] = release
spec.loader.exec_module(release)


def sample(version, refs=(), metadata=None):
    return {
        "version": version,
        "metadata": metadata or {k: "" for k in release.METADATA},
        "repo_url": release.REPO,
        "repo_type": "git",
        "commit": "a" * 40,
        "dependencies": {"ecng." + r.lower(): ["[" + v + ", )"] for r, v in refs},
    }


class GraphTests(unittest.TestCase):
    def setUp(self):
        def p(name, *refs):
            return release.Project(name, name, frozenset(refs), "")
        self.graph = {
            "Common": p("Common"),
            "Collections": p("Collections", "Common"),
            "Serialization": p("Serialization", "Collections"),
            "Isolated": p("Isolated"),
        }

    def test_cascade_selects_transitive_consumers_not_unrelated_packages(self):
        self.assertEqual(release.closure({"Common"}, self.graph),
                         {"Common", "Collections", "Serialization"})

    def test_topological_publish_order(self):
        self.assertEqual(release.ordered({"Common", "Collections", "Serialization"}, self.graph),
                         ["Common", "Collections", "Serialization"])

    def test_inconsistent_nuspec_dependency_requires_republish_after_interrupted_run(self):
        published = {
            "Common": sample("1.0.275"),
            "Collections": sample("1.0.50", [("Common", "1.0.274")]),
            "Serialization": sample("1.0.8", [("Collections", "1.0.50")]),
            "Isolated": sample("1.0.9"),
        }
        with patch.object(release, "run", return_value="b" * 40):
            plan = release.create_plan(self.graph, published, set(), set())
        self.assertEqual(plan["order"], ["Collections", "Serialization"])
        self.assertEqual(plan["versions"]["Common"], "1.0.275")
        self.assertEqual(plan["versions"]["Collections"], "1.0.51")
        self.assertEqual(plan["versions"]["Serialization"], "1.0.9")
        self.assertNotIn("Isolated", plan["order"])

    def test_external_only_change_also_cascades(self):
        published = {
            "Common": sample("1.0.274"),
            "Collections": sample("1.0.50", [("Common", "1.0.274")]),
            "Serialization": sample("1.0.8", [("Collections", "1.0.50")]),
            "Isolated": sample("1.0.9"),
        }
        with patch.object(release, "run", return_value="c" * 40):
            plan = release.create_plan(self.graph, published, set(), {"Common"})
        self.assertEqual(plan["order"], ["Common", "Collections", "Serialization"])

    def test_no_change_means_no_publish(self):
        published = {
            "Common": sample("1.0.274"),
            "Collections": sample("1.0.50", [("Common", "1.0.274")]),
            "Serialization": sample("1.0.8", [("Collections", "1.0.50")]),
            "Isolated": sample("1.0.9"),
        }
        with patch.object(release, "run", return_value="a" * 40):
            plan = release.create_plan(self.graph, published, set(), set())
        self.assertEqual(plan["order"], [])

    def test_cycles_rejected(self):
        graph = {
            "A": release.Project("A", "A", frozenset({"B"}), ""),
            "B": release.Project("B", "B", frozenset({"A"}), ""),
        }
        with self.assertRaisesRegex(ValueError, "Cycle"):
            release.ordered(set(graph), graph)


class MetadataTests(unittest.TestCase):
    def test_nuspec_xml_parses_old_publisher_fields_and_multi_tfm_deps(self):
        xml = b"""<?xml version="1.0"?>
        <package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
          <metadata>
            <id>Ecng.Collections</id><version>1.0.50</version>
            <authors>StockSharp</authors><description>Collections package</description>
            <licenseUrl>https://stocksharp.com/en/products/eula/</licenseUrl>
            <iconUrl>https://stocksharp.com/icon.png</iconUrl>
            <repository type="git" url="https://github.com/StockSharp/Ecng" commit="abcdef"/>
            <dependencies>
              <group targetFramework="net6.0"><dependency id="Ecng.Common" version="[1.0.274, )"/></group>
              <group targetFramework="net10.0"><dependency id="Ecng.Common" version="[1.0.274, )"/></group>
            </dependencies>
          </metadata>
        </package>"""
        pkg = release.parse_nuspec(xml, "1.0.50")
        self.assertEqual(pkg["metadata"]["licenseUrl"], "https://stocksharp.com/en/products/eula/")
        self.assertEqual(pkg["repo_url"], release.REPO)
        self.assertEqual(pkg["dependencies"]["ecng.common"], ["[1.0.274, )"])
        self.assertTrue(release.range_has_floor("[1.0.274, )", "1.0.274"))
        self.assertFalse(release.range_has_floor("[1.0.273, )", "1.0.274"))

    def test_targets_preserve_baseline_metadata_and_override_version_and_symbols(self):
        with tempfile.TemporaryDirectory() as temporary:
            output = Path(temporary) / "Directory.Build.targets"
            baseline = sample("1.0.274", metadata={
                "title": "Ecng.Common",
                "description": "Core & common <helpers>",
                "authors": "StockSharp Platform LLC",
                "copyright": "Copyright StockSharp 2026",
                "licenseUrl": "https://stocksharp.com/en/products/eula/",
                "tags": "REST WebSockets JSON",
                "iconUrl": "https://stocksharp.com/logo.png",
                "projectUrl": "https://stocksharp.com",
            })
            plan = {
                "order": ["Common"],
                "versions": {"Common": "1.0.275"},
                "baseline": {"Common": baseline},
                "commit": "f" * 40,
            }
            release.write_targets(plan, output)
            xml = output.read_text()
            root = release.ET.fromstring(xml)
            props = root.find("PropertyGroup")
            values = {p.tag: p.text for p in props}
            self.assertEqual(values["PackageVersion"], "1.0.275")
            self.assertEqual(values["Description"], "Core & common <helpers>")
            self.assertEqual(values["PackageLicenseUrl"], "https://stocksharp.com/en/products/eula/")
            self.assertEqual(values["GenerateAssemblyCopyrightAttribute"], "false")
            self.assertEqual(values["Copyright"], "Copyright StockSharp 2026")
            self.assertEqual(values["IncludeSymbols"], "true")
            self.assertEqual(values["SymbolPackageFormat"], "snupkg")
            self.assertEqual(values["RepositoryCommit"], "f" * 40)

    def test_sdk_nuspec_retains_previous_owner_without_changing_package_files(self):
        with tempfile.TemporaryDirectory() as temporary:
            package = Path(temporary) / "Ecng.Common.1.0.275.nupkg"
            with zipfile.ZipFile(package, "w") as z:
                z.writestr("Ecng.Common.nuspec", """<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"><metadata><id>Ecng.Common</id><version>1.0.275</version><authors>Example</authors><description>Test</description></metadata></package>""")
                z.writestr("lib/net10.0/Ecng.Common.dll", b"unchanged test binary")
            release.preserve_legacy_owner(package, "StockSharp Platform LLC")
            with zipfile.ZipFile(package) as z:
                self.assertEqual(z.read("lib/net10.0/Ecng.Common.dll"), b"unchanged test binary")
                parsed = release.parse_nuspec(z.read("Ecng.Common.nuspec"), "1.0.275")
                self.assertEqual(parsed["metadata"]["owners"], "StockSharp Platform LLC")

    def test_roslyn_analyzer_symbol_layout_matches_analyzer_dll(self):
        with tempfile.TemporaryDirectory() as temporary:
            output = Path(temporary) / "Directory.Build.targets"
            plan = {
                "order": ["Analyzers"],
                "versions": {"Analyzers": "1.0.4"},
                "baseline": {"Analyzers": sample("1.0.3")},
                "commit": "d" * 40,
            }
            release.write_targets(plan, output)
            root = release.ET.parse(output).getroot()
            group = root.find("PropertyGroup")
            properties = {p.tag: p.text for p in group}
            self.assertEqual(properties["IncludeBuildOutput"], "true")
            self.assertEqual(properties["BuildOutputTargetFolder"], "analyzers/dotnet/cs")
            self.assertTrue(any(item.tag == "None" and "Remove" in item.attrib
                                for group in root.findall("ItemGroup") for item in group))

    def test_version_increment(self):
        self.assertEqual(release.next_version("1.0.274"), "1.0.275")
        self.assertEqual(release.next_version(None), "1.0.0")
        with self.assertRaises(ValueError):
            release.next_version("1.0.274-beta")


def workflow_run(number, path, status, conclusion=None):
    return {"id": number, "run_number": number, "name": path.rsplit("/", 1)[-1], "path": path,
            "status": status, "conclusion": conclusion, "html_url": f"https://github.test/runs/{number}"}


CI = ".github/workflows/dotnet.yml"


class CiGateTests(unittest.TestCase):
    def gate(self, *answers, wait_seconds=600):
        replies = iter(answers)
        pauses = []
        release.ci_gate(lambda: next(replies), pauses.append, wait_seconds=wait_seconds, poll_seconds=30)
        return pauses

    def test_a_failed_ci_run_of_the_commit_blocks_publication(self):
        with self.assertRaises(RuntimeError) as error:
            self.gate([workflow_run(747, CI, "completed", "failure")])
        self.assertIn("https://github.test/runs/747", str(error.exception))

    def test_a_timed_out_ci_run_blocks_publication(self):
        with self.assertRaises(RuntimeError):
            self.gate([workflow_run(5, CI, "completed", "timed_out")])

    def test_a_green_ci_run_lets_publication_through(self):
        self.assertEqual(self.gate([workflow_run(1, CI, "completed", "success")]), [])

    def test_other_workflows_and_cancelled_ci_runs_do_not_block(self):
        runs = [workflow_run(1, ".github/workflows/release.yml", "completed", "failure"),
                workflow_run(2, ".github/workflows/release.yml", "in_progress"),
                workflow_run(3, CI, "completed", "cancelled")]
        self.assertEqual(self.gate(runs), [])

    def test_an_unfinished_ci_run_is_waited_for_and_its_failure_blocks(self):
        with self.assertRaises(RuntimeError):
            self.gate([workflow_run(1, CI, "in_progress")], [workflow_run(1, CI, "completed", "failure")])

    def test_an_unfinished_ci_run_is_waited_for_and_its_success_lets_through(self):
        pauses = self.gate([workflow_run(1, CI, "queued")], [workflow_run(1, CI, "completed", "success")])
        self.assertEqual(pauses, [30])

    def test_a_ci_run_that_never_finishes_blocks_publication(self):
        unfinished = [workflow_run(1, CI, "in_progress")]
        with self.assertRaises(RuntimeError):
            self.gate(*[unfinished] * 4, wait_seconds=60)


if __name__ == "__main__":
    unittest.main()
