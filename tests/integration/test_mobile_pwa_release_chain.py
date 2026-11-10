"""External release-controller contract. Run directly with both checkout paths."""
from __future__ import annotations

import argparse
import unittest
from pathlib import Path


class MobilePwaReleaseChainIntegrationTests(unittest.TestCase):
    def test_release_chain_forwards_to_the_mobile_budget_gate(self) -> None:
        root_release = (
            RELEASE_ROOT / "scripts" / "release" / "verify_chummer6_release_ready.sh"
        ).read_text(encoding="utf-8")
        release_materializer = (
            RUN_SERVICES_ROOT
            / "scripts"
            / "materialize_release_ready_receipt.py"
        ).read_text(encoding="utf-8")

        self.assertIn(
            'MATERIALIZER = RUN_SERVICES_ROOT / "scripts" / "materialize_release_ready_receipt.py"',
            root_release,
        )
        self.assertIn(
            'configured_run_services_root = os.environ.get("CHUMMER_RUN_SERVICES_ROOT", "").strip()',
            root_release,
        )
        self.assertIn('"--run-authoritative-controller"', root_release)
        self.assertIn("os.execve(", root_release)
        self.assertIn(
            'spec("verify_mobile_release_proof", f"{bash} {root}/chummer-play/scripts/release/verify_mobile_release_proof.sh"',
            release_materializer,
        )

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--release-root", required=True, type=Path)
    parser.add_argument("--run-services-root", required=True, type=Path)
    args = parser.parse_args()
    RELEASE_ROOT = args.release_root.resolve()
    RUN_SERVICES_ROOT = args.run_services_root.resolve()
    unittest.main(argv=[__file__])
