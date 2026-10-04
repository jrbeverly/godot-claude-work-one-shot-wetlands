#!/usr/bin/env bash
# Render the hero shots defined in assets/world/layout.json to screenshots/*.png using a virtual
# display and Mesa's software Vulkan (lavapipe), so it works on a headless box.
# Usage: tools/screenshot.sh [shot1,shot2]  (no arg = all shots)
#   RES=1280x720 SETTLE=8 OUT=screenshots LAYOUT=res://assets/world/layout.json tools/screenshot.sh wide
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."
GODOT="${GODOT:-godot}"
RES="${RES:-1280x720}"
SETTLE="${SETTLE:-8}"
OUT="${OUT:-$PWD/screenshots}"
SHOTS="--shots"; [[ $# -ge 1 && -n "${1:-}" ]] && SHOTS="--shots=$1"
LAYOUT="${LAYOUT:-res://assets/world/layout.json}"
export XDG_RUNTIME_DIR="${XDG_RUNTIME_DIR:-/tmp/xdg}"; mkdir -p "$XDG_RUNTIME_DIR"
dotnet build --nologo -v q 2>&1 | tail -2
# Import any new/changed assets (textures, models) before running without the editor.
"$GODOT" --headless --path . --import >/dev/null 2>&1 || true
xvfb-run -a -s "-screen 0 ${RES}x24" "$GODOT" --path . --rendering-driver vulkan \
  --resolution "$RES" --fixed-fps 30 -- "$SHOTS" "--shot-dir=$OUT" "--settle=$SETTLE" "--layout=$LAYOUT" 2>&1 \
  | grep -vE 'ALSA lib|snd_|audio_driver_alsa|dummy driver|^\s*$' || true
ls -la "$OUT"/*.png 2>/dev/null | tail -20
