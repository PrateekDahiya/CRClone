#!/usr/bin/env bash
set -euo pipefail

echo "=== CRClone Test Runner ==="

echo "[1/3] Server unit tests"
(cd server && npm run test:unit)

echo "[2/3] Server integration tests (mocked, no live DB required)"
(cd server && npm run test:integration)

echo "[3/3] Lint + typecheck"
(cd server && npm run lint)
(cd server && npm run typecheck)

echo "Unity tests require Unity Editor:"
echo "  Unity -batchmode -runTests -projectPath . -testPlatform EditMode -testResults results.xml"
echo "Load test (requires k6 + running server):"
echo "  k6 run server/tests/load/k6-load-test.js"
echo "E2E (requires staging env):"
echo "  npx playwright test"

echo "All runnable checks complete."
