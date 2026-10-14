"""Offline regression tests for the Ecng NuGet release planner.

Run: python -m unittest discover -s scripts/tests -p "test_nuget_release.py"
"""
import importlib.util
import tempfile
import unittest
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
            self.assertEqual(values["IncludeSymbols"], "true")
            self.assertEqual(values["SymbolPackageFormat"], "snupkg")
            self.assertEqual(values["RepositoryCommit"], "f" * 40)

    def test_version_increment(self):
        self.assertEqual(release.next_version("1.0.274"), "1.0.275")
        self.assertEqual(release.next_version(None), "1.0.0")
        with self.assertRaises(ValueError):
            release.next_version("1.0.274-beta")


if __name__ == "__main__":
    unittest.main()
