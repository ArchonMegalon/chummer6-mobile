from __future__ import annotations

import importlib.util
import io
import json
import tempfile
import unittest
from contextlib import redirect_stderr, redirect_stdout
from pathlib import Path

import yaml

ROOT = Path(__file__).resolve().parents[1]
SCRIPT_PATH = ROOT / "scripts" / "verify_next90_m121_mobile_live_combat_confidence.py"


def load_module():
    spec = importlib.util.spec_from_file_location("verify_next90_m121_mobile_live_combat_confidence", SCRIPT_PATH)
    module = importlib.util.module_from_spec(spec)
    assert spec.loader is not None
    spec.loader.exec_module(module)
    return module


class VerifyNext90M121MobileLiveCombatConfidenceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.module = load_module()

    def test_verifier_passes_against_current_repo_state(self) -> None:
        stdout = io.StringIO()
        stderr = io.StringIO()
        with redirect_stdout(stdout), redirect_stderr(stderr):
            exit_code = self.module.main()

        self.assertEqual(exit_code, 0, stderr.getvalue())
        self.assertIn("m121_mobile_live_combat_confidence_verify_ok", stdout.getvalue())

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

    def test_verifier_fails_closed_when_queue_row_claims_completion(self) -> None:
        original_design_queue = self.module.DESIGN_QUEUE
        queue_payload = yaml.safe_load(original_design_queue.read_text(encoding="utf-8"))
        for row in queue_payload["items"]:
            if row.get("package_id") == self.module.PACKAGE_ID:
                row["status"] = "complete"
                row["landed_commit"] = "deadbeef"
                break

        with tempfile.TemporaryDirectory() as temp_dir:
            queue_override = Path(temp_dir) / original_design_queue.name
            queue_override.write_text(yaml.safe_dump(queue_payload, sort_keys=False), encoding="utf-8")
            self.module.DESIGN_QUEUE = queue_override
            try:
                stdout = io.StringIO()
                stderr = io.StringIO()
                with redirect_stdout(stdout), redirect_stderr(stderr):
                    exit_code = self.module.main()
            finally:
                self.module.DESIGN_QUEUE = original_design_queue

        self.assertEqual(exit_code, 1)
        self.assertIn("design queue: block drifted from the canonical M121 implementation-only shape", stderr.getvalue())
        self.assertIn("design queue: forbidden marker present: landed_commit:", stderr.getvalue())

    def test_explicit_fleet_input_must_exist(self) -> None:
        with tempfile.TemporaryDirectory() as tmp, redirect_stdout(io.StringIO()), redirect_stderr(io.StringIO()) as stderr:
            code = self.module.main(Path(tmp) / "missing.yaml")
        self.assertEqual(code, 1)
        self.assertIn("missing file", stderr.getvalue())

    def test_explicit_fleet_queue_drift_fails(self) -> None:
        queue_payload = yaml.safe_load(self.module.DESIGN_QUEUE.read_text(encoding="utf-8"))
        row = next(item for item in queue_payload["items"] if item.get("package_id") == self.module.PACKAGE_ID)
        row["status"] = "complete"
        with tempfile.TemporaryDirectory() as tmp:
            queue = Path(tmp) / "queue.yaml"
            queue.write_text(yaml.safe_dump(queue_payload, sort_keys=False), encoding="utf-8")
            with redirect_stdout(io.StringIO()), redirect_stderr(io.StringIO()) as stderr:
                code = self.module.main(queue)
        self.assertEqual(code, 1)
        self.assertIn("fleet queue: block drifted", stderr.getvalue())
        self.assertIn("queue mirror parity: block drifted", stderr.getvalue())

    def test_recorded_checkout_provenance_cannot_be_empty(self) -> None:
        original = self.module.GENERATED_PROOF
        payload = json.loads(original.read_text(encoding="utf-8"))
        receipt = next(item for item in payload["package_receipts"] if item["package_id"] == self.module.PACKAGE_ID)
        receipt["checkout_root"] = ""
        with tempfile.TemporaryDirectory() as tmp:
            proof = Path(tmp) / "proof.json"
            proof.write_text(json.dumps(payload), encoding="utf-8")
            self.module.GENERATED_PROOF = proof
            try:
                with redirect_stdout(io.StringIO()), redirect_stderr(io.StringIO()) as stderr:
                    code = self.module.main()
            finally:
                self.module.GENERATED_PROOF = original
        self.assertEqual(code, 1)
        self.assertIn("invalid checkout_root provenance", stderr.getvalue())

    def test_verifier_fails_closed_when_generated_proof_duplicates_m121_receipt(self) -> None:
        original_generated_proof = self.module.GENERATED_PROOF
        payload = json.loads(original_generated_proof.read_text(encoding="utf-8"))
        duplicate_receipt = next(
            receipt for receipt in payload["package_receipts"] if receipt["package_id"] == self.module.PACKAGE_ID
        ).copy()
        duplicate_receipt["proof_receipt"] = "docs/duplicate-next90-m121-mobile-live-combat-confidence.proof.md"
        payload["package_receipts"].append(duplicate_receipt)

        with tempfile.TemporaryDirectory() as temp_dir:
            generated_override = Path(temp_dir) / original_generated_proof.name
            generated_override.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")
            self.module.GENERATED_PROOF = generated_override
            try:
                stdout = io.StringIO()
                stderr = io.StringIO()
                with redirect_stdout(stdout), redirect_stderr(stderr):
                    exit_code = self.module.main()
            finally:
                self.module.GENERATED_PROOF = original_generated_proof

        self.assertEqual(exit_code, 1)
        self.assertIn(
            f"generated_proof_payload: expected exactly one package receipt with package_id={self.module.PACKAGE_ID!r}, found 2",
            stderr.getvalue(),
        )
        self.assertIn(
            "generated_proof_payload: expected surface 'add_player_table_cards_between:mobile' on exactly one package receipt, found 2",
            stderr.getvalue(),
        )


if __name__ == "__main__":
    unittest.main()
