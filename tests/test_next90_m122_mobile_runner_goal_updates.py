from __future__ import annotations

import importlib.util
import io
import tempfile
import unittest
from contextlib import redirect_stderr, redirect_stdout
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SCRIPT_PATH = ROOT / "scripts" / "verify_next90_m122_mobile_runner_goal_updates.py"


def load_module():
    spec = importlib.util.spec_from_file_location("verify_next90_m122_mobile_runner_goal_updates", SCRIPT_PATH)
    module = importlib.util.module_from_spec(spec)
    assert spec.loader is not None
    spec.loader.exec_module(module)
    return module


class VerifyNext90M122MobileRunnerGoalUpdatesTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.module = load_module()

    def test_verifier_passes_against_current_repo_state(self) -> None:
        stdout = io.StringIO()
        stderr = io.StringIO()
        with redirect_stdout(stdout), redirect_stderr(stderr):
            exit_code = self.module.main()

        self.assertEqual(exit_code, 0, stderr.getvalue())
        self.assertIn("m122_mobile_runner_goal_updates_verify_ok", stdout.getvalue())

    def test_verifier_fails_closed_when_proof_mentions_worker_only_context(self) -> None:
        original_proof = self.module.PROOF_DOC
        proof_text = original_proof.read_text(encoding="utf-8")

        with tempfile.TemporaryDirectory() as temp_dir:
            poisoned_proof = Path(temp_dir) / original_proof.name
            poisoned_proof.write_text(
                proof_text + "\nWorker-only note: TASK_LOCAL_TELEMETRY.generated.json must not become package evidence.\n",
                encoding="utf-8",
            )
            self.module.PROOF_DOC = poisoned_proof
            try:
                stdout = io.StringIO()
                stderr = io.StringIO()
                with redirect_stdout(stdout), redirect_stderr(stderr):
                    exit_code = self.module.main()
            finally:
                self.module.PROOF_DOC = original_proof

        self.assertEqual(exit_code, 1)
        self.assertIn("proof_doc: forbidden marker present: TASK_LOCAL_TELEMETRY.generated.json", stderr.getvalue())


if __name__ == "__main__":
    unittest.main()
