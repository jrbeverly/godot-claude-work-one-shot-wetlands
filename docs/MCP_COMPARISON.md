# Godot MCP Candidates: Comparison

Analysis only. Based on static inspection of the pinned shallow checkouts under
`references/godot/mcp/`. Nothing was installed or run; "declared" means stated in a README or manifest, not tested.
Each checkout is a single commit, so there is no commit history to judge activity from.

Paths below are relative to `references/godot/mcp/<repo>/`.

## Candidates

| Short name | Checkout | Head commit (date) | Shape |
|---|---|---|---|
| mkdevkit | `mkdevkit-godot-mcp` | 2026-06-09, "Create FUNDING.yml" | Node.js stdio MCP server + WebSocket :6505 + editor plugin with 3 runtime autoloads |
| slangwald | `slangwald-godot-mcp` | 2026-02-12, "Add crossword puzzle example..." | Python (uv) stdio MCP server + TCP :9500 editor plugin + TCP :9501 game autoload |
| claudot | `claudot-pub` | 2026-09-20, merge of PR #10 | Editor plugin with chat panel + Python bridge daemon + standalone Python MCP server (HTTP :7778 to the editor) |

## Per-criterion findings

| Criterion | mkdevkit | slangwald | claudot |
|---|---|---|---|
| Licence | MIT (`LICENSE`, (c) 2026 mkdevkit; `server/package.json` "MIT") | MIT (`LICENSE`, (c) 2025 slangwald; `mcp/pyproject.toml`) | MIT (`LICENSE`, (c) 2026 Ryan Rinkel). README disclaims affiliation with Anthropic |
| Declared Godot version | "Godot 4.4+" (`README.md` Requirements) | "Godot 4.6" only (`README.md` badge and prerequisites; `project.godot` features "4.6"). Nothing says older versions work | "Godot 4.2+" (`README.md` Prerequisites) |
| Godot .NET / Mono | Not found / unclear. No mention in README or source. All tooling is GDScript | Declared: "standard or mono edition" (`README.md`, `CONTRIBUTING.md`). Only the engine build is mentioned. C# script support is not claimed. The tools take inline GDScript (`attach_script`) | Not stated as supported. Source says C# scripts are unsupported: `get_node_script` is "GDScript source only, not C#" (`addons/claudot/claudot_plugin.gd:452`), and `scene_tools.gd:858` rejects non-GDScript scripts |
| Claude Code compat | Declared. Claude Code is the first client listed; project-root `.mcp.json` with `node server/build/index.js` (`README.md`, `.mcp.json.example`) | Declared and primary. Built for Claude Code. `claude mcp add godot-mcp -- uv run --directory .../mcp python godot_mcp_server.py`. Ships a `CLAUDE.md` | Declared and primary. The plugin auto-generates `.mcp.json`, `.claude/CLAUDE.md` and tool permissions (`claudot_plugin.gd` calls `_generate_mcp_json()`). It also has its own chat panel, Codex and API-key providers |
| Editor scene-tree inspection | Yes: `get_scene_tree`, `get_scene_file_content`, `get_node_properties`, `get_filesystem_tree` | Yes: `get_scene_tree`, `get_node_properties`, `get_signals`, `get_editor_state` | Yes: `get_scene_state` (max_depth), `get_editor_context`, `get_node_property`, `get_node_script` |
| Node create / modify | Yes, broad: `add_node`, `delete_node`, `duplicate_node`, `move_node`, `update_property`, `rename_node`, `batch_add_nodes`, `batch_set_property`, `connect_signal`, plus script, resource, physics, tilemap, animation and similar tools. Goes through UndoRedo (README claim) | Yes: `create_node`, `modify_node`, `delete_node`, `set_resource`, `attach_script`, `set_script`, `connect_signal`, `instantiate_scene`, `undo`/`redo`, `save_scene`, `open_scene` | Yes: `create_node`, `set_node_property`, `delete_node`, `reparent_node`; all undoable per README. No script create or attach tool in the list. Files are edited by Claude Code itself |
| Project execution | Yes: `play_scene`, `stop_scene`, `export_project` | Yes: `run_project`, `run_scene`, `stop_project` | Yes: `run_scene` (optional scene_path), `stop_scene`, `run_tests` (GUT) |
| Debugger / runtime error collection | Partial / unclear. `get_editor_errors`, `get_output_log`, `get_game_errors`-style tools are not listed. `get_editor_errors` filters an in-memory `_output_buffer` for ERROR/WARNING lines (`addons/godot_mcp/commands/editor_commands.gd:31`). `append_output()` is defined there, but I found no caller in `addons/`, so how the buffer gets filled is unclear | Weak. `get_output` reads `user://logs/godot.log` (`addons/mcp_bridge/plugin.gd:508`), so it covers prints and errors only as far as the log file holds them. No structured debugger capture | Strongest. `EditorDebuggerPlugin` capture (`addons/claudot/debugger/debugger_capture_plugin.gd`) keeps output and error buffers. Tools: `get_debugger_output`, `get_debugger_errors` |
| Runtime scene-tree inspection | Yes: `get_game_scene_tree`, `get_game_node_properties`, `set_game_node_property`, `execute_game_script`, `find_nodes_by_script`, `watch_signals`, `wait_for_node` (via `MCPRuntimeBridge`) | Yes: `get_runtime_tree` (game autoload :9501, `mcp_bridge_game.gd`) | Not found. None of the 20 listed tools inspect the running game's tree |
| Input simulation | Yes, extensive: `simulate_key`, `simulate_mouse_click`, `simulate_mouse_move`, `simulate_action`, `simulate_sequence`, record/replay, `click_button_by_text` | Minimal: `click(x, y)` only, which builds `InputEventMouseButton` (`mcp_bridge_game.gd:110`). No key or action input | Not found. Only `request_user_input`, which asks the developer a question and is not game input |
| Screenshot capture | Yes: `get_editor_screenshot`, `get_game_screenshot`, `compare_screenshots`, `capture_frames`. Transport is `user://` file IPC | Yes: `screenshot` returns a base64 PNG from the running game viewport only. No editor screenshot | Yes: `capture_screenshot` with viewport_type 2d_editor, 3d_editor or game |
| Tool count | 173 declared (`README.md`; `server/src/tool-manifest.ts`) | 24 (21 editor, 3 game) per `README.md` and `CLAUDE.md` | 20 declared (`README.md`; 20 `@mcp.tool` hits in `bridge/godot_mcp_server.py`) |
| Setup complexity | Medium. Copy `addons/godot_mcp/`, enable the plugin (it injects 3 autoloads), then `npm install && npm run build` in `server/` (no prebuilt `build/` in the checkout). Write `.mcp.json` with an absolute path. Deps: Node 18+, `@modelcontextprotocol/sdk`, `ws`, `zod` (`server/package.json`). Port 6505 via `GODOT_MCP_PORT` | Low-medium. Copy `addons/mcp_bridge/` plus `mcp_bridge_game.gd`, enable the plugin, add the autoload manually. `claude mcp add ... uv run`. Deps: Python 3.10+, uv, `mcp[cli]`. Ports 9500/9501 hard-coded in docs; `mcp_ports.cfg` shows a configurable-port mechanism | Low for install (copy `addons/claudot/`, enable, restart; no manual MCP registration). Heavy underneath: Python 3.10+ or uv, plus `claude-agent-sdk`, `anyio`, `httpx` and `fastmcp>=3.0.0` (`bridge/requirements.txt`). Auto-writes `.mcp.json`, `.claude/CLAUDE.md` and permissions into the project. Includes a chat UI and multi-provider code we do not need |
| Maintenance signals | Recent (head 2026-06-09), README in English and Chinese, `.github/FUNDING.yml`. Plugin and server both at version 0.1.0. No tests, no CHANGELOG, no CONTRIBUTING found. Head commit is a funding file, so the last code change is not visible from this shallow clone | Quiet (head 2026-02-12), changelog to 0.2.0 (2026-02-10), roadmap listed but unfinished, CONTRIBUTING and CLAUDE.md present. Version 0.1.0 in manifests. No tests found | Most recent (head 2026-09-20, merged community PR #10, so outside contributors exist). Plugin version "3.1.0-beta". Smoke tests present (`bridge/test_*_smoke.py`, `validate_mcp_tools.py`). Fast-moving: README names several model IDs, which will age |

## Notes and risks per candidate

- **mkdevkit**: Widest coverage (173 tools) and the only candidate with both runtime tree and real input simulation. Costs: a large tool surface that burns context in the client, a build step, and a broad plugin that injects autoloads. The README claims go well beyond what I could confirm from reading, for example UndoRedo on every edit. Error collection looks the least certain part (see table).
- **slangwald**: Small and readable (one Python server, one editor plugin, one autoload). Little that would break across Godot versions, but it is explicitly built for 4.6 and thin on input and debugger. Single-commit history and the last commit date suggest it is a demo-grade project.
- **claudot**: Best fit for the error-feedback loop and the cleanest install, and the most recently updated. It is a full chat product, not just an MCP bridge. Weakest on runtime inspection and input, and it pulls the most dependencies. The standalone MCP server (`godot_mcp_server.py`) depends on the editor plugin's HTTP bridge, so the plugin must run in the editor either way.
- None of the three mentions C# script manipulation, so for a .NET project all of them cover scene and node work only, not C# source. Verify per the list below.

## Recommendation: test mkdevkit first

Why:
1. It covers the most of what an agent needs in a game-development loop: editor tree, node edits, run, runtime tree, input simulation and screenshots, all in one place. The other two each lack one of runtime tree or input, so they would force a second tool.
2. It declares Godot 4.4+, a lower floor than slangwald's 4.6-only claim, and Claude Code is a first-class listed client.
3. Recent head commit (June 2026) and a clear, modular layout (`addons/godot_mcp/commands/*`) that is easy to audit or trim.
4. Setup is moderate and fully scriptable: Node and npm only, no Python stack.

Counterpoint: if the first test shows the debugger/error path is dead (see below) or the 173-tool surface is too noisy, test claudot next (best error capture) before slangwald.

## What to verify when testing mkdevkit (in a throwaway copy, not the main project)

1. Build: `npm install && npm run build` in `server/` succeeds on the target Node version, and `build/index.js` exists.
2. Connection: Godot plugin connects to the server on 6505 after the editor opens; reconnect works if the server starts later. Check what firewall or port conflicts occur.
3. Godot version: confirm which Godot version the project uses (and whether it is the .NET build) and that the plugin loads without parse errors on it.
4. Tool list size: how many of the 173 tools Claude Code actually sees, and whether context cost is acceptable. Check whether `server/src/tools.ts` allows trimming.
5. Editor tree: `get_scene_tree` on a real scene; `add_node` and `update_property` on it, then confirm Ctrl+Z undoes them.
6. Run and runtime: `play_scene`, then `get_game_scene_tree`, `get_game_node_properties`. Confirm the 3 injected autoloads do not break the game or export, and that they are removed when the plugin is disabled.
7. Input and screenshots: `simulate_action`/`simulate_key` and `simulate_mouse_click` change game state; `get_game_screenshot` returns a usable image; `get_editor_screenshot` works when the editor is not focused.
8. Errors (the main doubt): deliberately introduce a GDScript parse error and a runtime error, and check whether `get_editor_errors` and `get_output_log` report them. If not, note that `append_output()` has no caller (`commands/editor_commands.gd:25`) and compare with claudot's `get_debugger_errors`.
9. .NET: confirm behaviour in a Godot .NET project, including whether C# scripts and `execute_editor_script` / `execute_game_script` work. Treat these as unknown until tested.
10. Safety: `execute_editor_script`, `execute_game_script`, `export_project` and `set_project_setting` are powerful. Decide on a permission allowlist in Claude Code before enabling the server in the real project.
