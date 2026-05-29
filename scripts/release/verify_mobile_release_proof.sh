#!/usr/bin/env bash
set -euo pipefail
python3 - <<'PY'
import json
from pathlib import Path
path = Path('/docker/chummercomplete/chummer-play/.codex-studio/published/MOBILE_LOCAL_RELEASE_PROOF.generated.json')
payload = json.loads(path.read_text())
if str(payload.get('status')).lower() not in {'pass','passed','ready'}:
    raise SystemExit(f"mobile release proof status is not pass: {payload.get('status')}")
print('mobile release proof ok')
PY
