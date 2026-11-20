#!/bin/sh
# Runs the solution's tests selected by an xUnit v3 trait filter, for scripts/check.conf.
#   usage: sh ECommerce/tests/run-tests.sh --filter-trait|--filter-not-trait "Category=Integration"
# Runner: Microsoft.Testing.Platform (global.json; plan 0001 A-1).
# Fails when dotnet test fails OR when the step executed zero tests: a filter that selects
# nothing must not pass silently (docs/testing.md, Two speeds rule 4; spec 0001 AC-1/AC-2).
# A project with no matching test exits 8 ("zero tests"); that per-project code is ignored and the
# zero guard below judges the whole step instead.
# resource hygiene: --no-build runs no MSBuild (the build step compiled everything); test
# processes exit with the run.
set -u
case "${1:-}" in
  --filter-trait|--filter-not-trait) [ $# -eq 2 ] || { echo "usage: run-tests.sh --filter-trait|--filter-not-trait \"<trait>\"" >&2; exit 2; } ;;
  *) echo "usage: run-tests.sh --filter-trait|--filter-not-trait \"<trait>\"" >&2; exit 2 ;;
esac
ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/../.." && pwd)
cd "$ROOT" || exit 2

out=$(dotnet test --solution UludagEnerji.slnx --no-build "$1" "$2" --ignore-exit-code 8 2>&1)
rc=$?
printf '%s\n' "$out"

# The run summary's "total: <n>" is summed over every test project of the step.
total=$(printf '%s\n' "$out" | sed -n '/Test run summary/,$ s/^ *total: *\([0-9][0-9]*\).*/\1/p' | tail -n 1)
total=${total:-0}

if [ "$rc" -ne 0 ]; then
  echo "run-tests: dotnet test failed (exit $rc) for $1 $2" >&2
  exit "$rc"
fi
if [ "$total" -eq 0 ]; then
  echo "run-tests: $1 $2 executed 0 tests — refusing to pass silently" >&2
  exit 1
fi
echo "run-tests: $total test(s) executed for $1 $2"
