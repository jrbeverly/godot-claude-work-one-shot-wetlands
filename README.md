# godot-warcraft-implementation

Minimal Godot 4.5 .NET (C#) project plus a pinned local reference library, bootstrapped for later AI-assisted environment work. No game or world content yet.

## Requirements
- [Godot 4.5.x **.NET/Mono** build](https://godotengine.org/download) (4.5+ is needed by the Terrain3D candidate)
- .NET SDK 8

## Build & run
```sh
dotnet build                                   # builds GodotWarcraft.sln/.csproj
godot --path . --editor                        # open in the editor (use the .NET build)
godot --path .                                 # run scenes/Main.tscn
GODOT=/path/to/godot tools/run.sh              # build + headless smoke test (prints "Main ready")
```

## Reference library
```sh
tools/fetch-references.sh        # one command; pinned in references/references.yaml (~2.6 GB)
```
Then read `references/README.md` (index: what, why, licence, key files). Licences: `THIRD_PARTY.md`.

## Layout
`scenes/` `src/` (C#) `assets/` `addons/` `tools/` `references/` `docs/` · agent guidance: `CLAUDE.md`, `AGENTS.md`
Docs: `docs/MCP_COMPARISON.md`, `docs/WORLD_EDITOR_REFERENCE.md`, `docs/CLASSIC_TERRAIN_NOTES.md`
