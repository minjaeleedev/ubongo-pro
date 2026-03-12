# CLAUDE.md — Ubongo Pro

## Build & Test

There is no `make test`. Use Unity CLI batchmode:

```bash
# Edit Mode tests
"/Applications/Unity/Hub/Editor/6000.3.6f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -projectPath "$(pwd)" -runTests -testPlatform editmode \
  -assemblyNames Ubongo.Tests.EditMode \
  -testResults "$(pwd)/Logs/editmode.xml" -logFile Logs/editmode-run.log

# Play Mode tests
"/Applications/Unity/Hub/Editor/6000.3.6f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -projectPath "$(pwd)" -runTests -testPlatform PlayMode \
  -logFile Logs/playmode-tests.log -quit
```

Sandboxed agents (Codex etc.) need sandbox-outside execution — Unity writes to licensing/state databases at startup.

## Architecture

DDD-inspired layered architecture enforced by assembly definitions. Dependencies flow **inward only**:

```
Ubongo.Domain            (no references — pure domain logic)
  ^
Ubongo.Application       (references: Domain)
  ^
Ubongo.Infrastructure    (references: Domain, Application)
  ^
Ubongo.Presentation      (references: Domain, Application, Infrastructure + Unity UI/Input)
  ^
Ubongo.Bootstrap         (references: all four above — composition root lives here)

Ubongo.Editor            (Editor-only tooling)

Ubongo.Tests.EditMode  \
Ubongo.Tests.PlayMode  / (reference all production assemblies)
```

**Never add an inward→outward reference** (e.g. Domain must never reference Application or Infrastructure).

### Composition Root

`GameCompositionRoot` (`Assets/Scripts/Application/Bootstrap/GameCompositionRoot.cs`):
- Runs at `[DefaultExecutionOrder(-1000)]`, `[ExecuteAlways]`
- Singleton — destroys duplicates
- Startup sequence: resolve scene components → construct GameBoard → wire runtime dependencies → initialize GameManager
- Fail-fast: throws `InvalidOperationException` if any required component is missing or duplicated
- Scene-owned policy: all required MonoBehaviours must exist in the scene, never auto-created at runtime

## Key Conventions

- Follow `.editorconfig` (UTF-8, LF, 4-space indent for C#, 2-space for JSON/asmdef)
- Test naming: `Method_State_ExpectedResult` in `FeatureNameTests` classes
- No reflection in tests — test through public contracts only; if not testable, add a production seam
- Commit `.meta` files alongside asset changes
- Conventional commits: `feat:`, `fix:`, `refactor:`, `test:`, `docs:`

## Unity-Specific

- **Unity version**: 6000.3.6f1
- **Single scene**: `Assets/Scenes/MainScene.unity`
- **Custom physics layers**: Board = 8, Piece = 9
- **Camera sync rule**: camera parameters live in three files that must stay in sync:
  1. `GameManager.cs` (SetupCamera)
  2. `CameraSetupTool.cs` (editor tool)
  3. `MainScene.unity` (scene YAML)

