from __future__ import annotations

import importlib.util
import unittest
from pathlib import Path

SCRIPT = Path(__file__).resolve().parents[1] / "scripts/verify_next90_m122_mobile_runner_goal_updates.py"


class M122ProofHygieneTests(unittest.TestCase):
    def test_worker_only_context_is_rejected_without_external_inputs(self) -> None:
        spec = importlib.util.spec_from_file_location("m122_proof_hygiene", SCRIPT)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        proof = module.PROOF_DOC.read_text(encoding="utf-8")
        self.assertEqual(module.require_clean_markers("proof_doc", proof, module.FORBIDDEN_PROOF_MARKERS), [])
        failures = module.require_clean_markers("proof_doc", proof + "\nTASK_LOCAL_TELEMETRY.generated.json", module.FORBIDDEN_PROOF_MARKERS)
        self.assertIn("proof_doc: forbidden marker present: TASK_LOCAL_TELEMETRY.generated.json", failures)


if __name__ == "__main__":
    unittest.main()
