#!/usr/bin/env bash
# Build and run the project headlessly for a few frames (smoke test).
# Requires Godot 4.5.x .NET (mono) build: set GODOT=/path/to/Godot_..._mono_... or have `godot` on PATH.
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."
GODOT="${GODOT:-godot}"
dotnet build
"$GODOT" --headless --path . --quit-after "${1:-5}"
