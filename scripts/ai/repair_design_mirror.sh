#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

CHUMMER6_MOBILE_REPO_ROOT="${repo_root}" \
python3 /docker/chummercomplete/chummer-design/scripts/ai/publish_local_mirrors.py \
  --repo chummer6-mobile

python3 "${repo_root}/scripts/ai/verify_design_mirror.py"
