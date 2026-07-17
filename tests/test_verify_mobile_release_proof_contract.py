from __future__ import annotations

import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
VERIFY_SCRIPT = ROOT / "scripts" / "release" / "verify_mobile_release_proof.sh"


class VerifyMobileReleaseProofContractTests(unittest.TestCase):
    def test_live_release_blocker_validation_ignores_only_the_self_referential_release_wrapper(self) -> None:
        text = VERIFY_SCRIPT.read_text(encoding="utf-8")

        self.assertIn(
            'self_referential_release_wrapper_ids = {"release_truth:release_ready"}',
            text,
        )
        self.assertIn(
            'require(isinstance(live_release_blockers.get("generated_at"), str) and live_release_blockers.get("generated_at").strip(), "live release blockers generated_at missing")',
            text,
        )
        self.assertIn(
            '"release_boundary canonical_release_blockers substantive root_blocker_ids drifted from live receipt"',
            text,
        )
        self.assertIn("snapshot_substantive_ids == live_substantive_ids", text)
        self.assertNotIn('blocker_id.startswith("release_truth:")', text)
        self.assertNotIn("blocker_id.startswith(self_referential_release_wrapper_ids)", text)
        self.assertNotIn(
            'require(canonical_release_blockers.get("generated_at") == live_release_blockers.get("generated_at"), "release_boundary canonical_release_blockers generated_at drifted from live receipt")',
            text,
        )


if __name__ == "__main__":
    unittest.main()
