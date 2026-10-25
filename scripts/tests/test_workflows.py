"""Regression tests for required release checks and SQL Server setup."""

import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest

import yaml


ROOT = Path(__file__).resolve().parents[2]


def workflow(name):
    return yaml.load((ROOT / ".github" / "workflows" / name).read_text(encoding="utf-8-sig"),
                     Loader=yaml.BaseLoader)


def dependencies(jobs, name):
    result = set()
    pending = [name]
    while pending:
        needs = jobs[pending.pop()].get("needs", [])
        if isinstance(needs, str):
            needs = [needs]
        for dependency in needs:
            if dependency not in result:
                result.add(dependency)
                pending.append(dependency)
    return result


class ReleaseWorkflowTests(unittest.TestCase):
    def setUp(self):
        self.release = workflow("release.yml")
        self.ci = workflow("dotnet.yml")

    def test_publication_requires_the_complete_ci_workflow(self):
        jobs = self.release["jobs"]
        self.assertIn("checks", dependencies(jobs, "publish"))
        self.assertIn("build-pack-verify", dependencies(jobs, "publish"))
        self.assertEqual(jobs["checks"]["uses"], "./.github/workflows/dotnet.yml")
        self.assertEqual(jobs["checks"]["secrets"], "inherit")
        self.assertIn("workflow_call", self.ci["on"])

    def test_failed_cancelled_or_skipped_checks_cannot_be_bypassed(self):
        jobs = self.release["jobs"]
        self.assertIn("checks", jobs)
        for name in ["publish", *dependencies(jobs, "publish")]:
            with self.subTest(job=name):
                self.assertNotEqual(jobs[name].get("continue-on-error", "false"), "true")
                condition = jobs[name].get("if", "")
                for override in ["always(", "failure(", "cancelled("]:
                    self.assertNotIn(override, condition)
        self.assertNotIn("if", jobs["checks"])

    def test_every_platform_and_service_check_is_required(self):
        jobs = self.ci["jobs"]
        self.assertEqual(set(jobs["build"]["strategy"]["matrix"]["os"]),
                         {"ubuntu-latest", "windows-latest", "macos-latest"})
        for name in ["build", "database", "azurite"]:
            with self.subTest(job=name):
                self.assertNotIn("if", jobs[name])
                self.assertNotEqual(jobs[name].get("continue-on-error", "false"), "true")
                for step in jobs[name]["steps"]:
                    self.assertNotEqual(step.get("continue-on-error", "false"), "true")
                    if step.get("uses", "").startswith("actions/checkout@"):
                        self.assertNotIn("ref", step.get("with", {}), "Checks must use the release's source commit.")

    def test_publication_refuses_a_commit_whose_tests_failed_in_another_run(self):
        publish = self.release["jobs"]["publish"]
        self.assertEqual(publish["permissions"].get("actions"), "read")
        steps = publish["steps"]
        gates = [i for i, step in enumerate(steps) if "nuget-release.py ci-gate" in step.get("run", "")]
        logins = [i for i, step in enumerate(steps) if step.get("uses", "").startswith("NuGet/login@")]
        self.assertEqual(len(gates), 1, "The publish job must run the CI gate once.")
        self.assertEqual(len(logins), 1)
        self.assertLess(gates[0], logins[0], "The gate must run before a NuGet key is obtained.")
        self.assertEqual(steps[gates[0]]["env"]["GITHUB_TOKEN"], "${{ github.token }}")
        self.assertNotEqual(steps[gates[0]].get("continue-on-error", "false"), "true")

    def test_workflow_changes_and_regression_tests_are_checked_in_pull_requests(self):
        paths = self.release["on"]["pull_request"]["paths"]
        self.assertIn(".github/workflows/dotnet.yml", paths)
        self.assertIn("scripts/tests/**", paths)
        commands = "\n".join(step.get("run", "") for step in self.release["jobs"]["planner-tests"]["steps"])
        self.assertIn('test_*.py', commands)
        self.assertIn("scripts/tests/requirements.txt", commands)


class DatabaseSetupTests(unittest.TestCase):
    def setUp(self):
        self.database = workflow("dotnet.yml")["jobs"]["database"]
        self.setup = next(step for step in self.database["steps"]
                          if "SQL Server" in step.get("name", ""))

    def test_setup_uses_the_tools_in_the_healthy_service_container(self):
        command = self.setup["run"]
        self.assertNotIn("apt-get", command)
        self.assertNotIn("curl", command)
        self.assertIn("docker exec", command)
        self.assertEqual(self.setup["env"]["SQLSERVER_CONTAINER"], "${{ job.services.sqlserver.id }}")
        health = self.database["services"]["sqlserver"]["options"]
        self.assertIn("/opt/mssql-tools18/bin/sqlcmd", health)
        self.assertNotIn("||", health)

    def run_setup(self, exit_code):
        self.assertTrue(self.setup["run"].lstrip().startswith("docker exec"),
                        "Only the container command may be executed by the shell tests.")
        bash = shutil.which("bash")
        self.assertIsNotNone(bash, "Workflow shell tests require bash.")
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            docker = directory / "docker"
            docker.write_text('#!/bin/bash\nprintf "%s\\n" "$@" > "$DOCKER_ARGS_FILE"\nexit "$DOCKER_TEST_EXIT"\n',
                              encoding="utf-8", newline="\n")
            docker.chmod(0o755)
            arguments = directory / "arguments.txt"
            env = os.environ.copy()
            env.update(PATH=str(directory) + os.pathsep + env["PATH"],
                       SQLSERVER_CONTAINER="sqlserver-test-container",
                       DOCKER_ARGS_FILE=arguments.as_posix(), DOCKER_TEST_EXIT=str(exit_code))
            result = subprocess.run([bash, "--noprofile", "--norc", "-eo", "pipefail", "-c", self.setup["run"]],
                                    env=env, capture_output=True, text=True, timeout=10)
            self.assertTrue(arguments.exists(), result.stderr)
            return result, arguments.read_text().splitlines()

    def test_sql_errors_fail_the_job(self):
        result, _ = self.run_setup(19)
        self.assertEqual(result.returncode, 19, result.stderr)

    def test_successful_setup_creates_the_database_and_enables_sql_error_exit_codes(self):
        result, arguments = self.run_setup(0)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(arguments[:3], ["exec", "sqlserver-test-container", "/opt/mssql-tools18/bin/sqlcmd"])
        self.assertIn("-b", arguments)
        self.assertEqual(arguments[arguments.index("-Q") + 1], "CREATE DATABASE ecng_test")


if __name__ == "__main__":
    unittest.main()
