#!/usr/bin/env bash
# Fetch the pinned reference library described in references/references.yaml.
# Usage: tools/fetch-references.sh [--list] [id ...]
# Each reference is a shallow, detached checkout of the pinned commit (not a submodule).
# Re-running is idempotent: up-to-date checkouts are skipped, stale ones are re-pinned.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
MANIFEST="$ROOT/references/references.yaml"

# Emit "id|path|url|commit" per manifest entry.
parse() {
  awk '
    function flush() { if (id != "") print id "|" path "|" url "|" commit; id = path = url = commit = "" }
    /^  - id:/ { flush(); id = $3; next }
    /^    path:/   { path = $2; next }
    /^    url:/    { url = $2; next }
    /^    commit:/ { commit = $2; next }
    END { flush() }
  ' "$MANIFEST"
}

if [[ "${1:-}" == "--list" ]]; then
  parse | tr '|' '\t'
  exit 0
fi

want=("$@")
wanted() {
  [[ ${#want[@]} -eq 0 ]] && return 0
  local w; for w in "${want[@]}"; do [[ "$w" == "$1" ]] && return 0; done
  return 1
}

fail=0
while IFS='|' read -r id path url commit; do
  wanted "$id" || continue
  dest="$ROOT/$path"
  if [[ -f "$dest/SNAPSHOT.json" ]]; then
    echo "snapshot $id (committed source; archive URL and omitted files in SNAPSHOT.json)"
    continue
  fi
  # Safety net: never let `git add -A` pick up a clone as an embedded repo.
  if [[ -d "$ROOT/.git/info" ]] && ! grep -qxF "/$path/" "$ROOT/.git/info/exclude" 2>/dev/null; then
    echo "/$path/" >> "$ROOT/.git/info/exclude"
  fi
  if [[ -d "$dest/.git" ]] && [[ "$(git -C "$dest" rev-parse HEAD 2>/dev/null)" == "$commit" ]]; then
    echo "ok       $id @ ${commit:0:10}"
    continue
  fi
  echo "fetching $id @ ${commit:0:10}"
  rm -rf "$dest"; mkdir -p "$dest"
  tries=0
  until {
    git -C "$dest" init -q &&
    git -C "$dest" remote add origin "$url" 2>/dev/null || true
    GIT_LFS_SKIP_SMUDGE=1 git -C "$dest" -c advice.detachedHead=false fetch -q --depth 1 origin "$commit" &&
    GIT_LFS_SKIP_SMUDGE=1 git -C "$dest" -c advice.detachedHead=false checkout -q FETCH_HEAD
  }; do
    tries=$((tries + 1))
    if [[ $tries -ge 4 ]]; then echo "FAILED   $id" >&2; fail=1; break; fi
    sleep $((2 ** tries)); rm -rf "$dest"; mkdir -p "$dest"
  done
done < <(parse)

exit $fail
