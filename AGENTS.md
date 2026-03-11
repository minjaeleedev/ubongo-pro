# Repository Guidelines

## Build, Test, and Development Commands
- Open project in Unity Hub: `open -a "Unity Hub"`
- Open in VS Code: `code .`
- Regenerate C# solution files from Unity: `Assets > Open C# Project`
- Open project directly with Unity CLI (macOS example): `"/Applications/Unity/Hub/Editor/6000.3.6f1/Unity.app/Contents/MacOS/Unity" -projectPath "$(pwd)"`
- Run Edit Mode tests with Unity CLI: `"/Applications/Unity/Hub/Editor/6000.3.6f1/Unity.app/Contents/MacOS/Unity" -batchmode -projectPath "$(pwd)" -runTests -testPlatform editmode -assemblyNames Ubongo.Tests.EditMode -testResults "$(pwd)/Logs/editmode.xml" -logFile Logs/editmode-run.log`
- Run Play Mode tests (when tests exist): `Unity -batchmode -projectPath "$(pwd)" -runTests -testPlatform PlayMode -logFile Logs/playmode-tests.log -quit`
- When running Unity batchmode tests from Codex or any sandboxed agent, request sandbox-outside execution up front. Unity writes to licensing/state databases during test startup, and sandboxed runs fail with errors like `readonly database` or `unable to open database file`.

## Coding Style & Naming Conventions
- Follow `.editorconfig`: UTF-8, LF, 4-space indentation for `*.cs`; 2 spaces for `*.json` and `*.asmdef`.
- Use K&R/C# standard readability defaults in this repo: braces on new lines, explicit method bodies preferred.
- Naming: `PascalCase` for classes/public members, `camelCase` for private fields, interfaces prefixed with `I`.
- Match file names to primary class names (example: `GameManager.cs` -> `GameManager`).
- Keep Unity-specific references serialized when needed (`[SerializeField] private ...`).

## Testing Guidelines
- Automated test assemblies are committed; run relevant EditMode/PlayMode tests for changed scope.
- Do not use `make test` in this repo. Use the Unity batchmode commands above and inspect `Logs/editmode-run.log`, `Logs/playmode-tests.log`, and `Logs/editmode.xml` for failures.
- For non-trivial logic, add Unity Test Framework tests under `Assets/Tests/EditMode` or `Assets/Tests/PlayMode`.
- Test naming: `FeatureNameTests` classes and `Method_State_ExpectedResult` methods.
- Include a short manual test checklist in PRs (scene, input flow, win/fail conditions).
- Test through public contracts only (public methods/properties/events and externally observable behavior).
- Do not call private methods or read/write private fields from tests (including reflection like `BindingFlags.NonPublic`, `GetField`, `GetMethod`).
- Do not inject serialized private fields from tests to force state; use scene/prefab wiring or public setup APIs.
- If a behavior is not testable via public API, prefer adding/refining explicit production seams (public interface, adapter, or testable composition root) instead of reflection-based tests.

## Runtime Wiring Rules
- Scene-owned runtime policy is strict: required runtime components must exist in the scene and must not be auto-created implicitly at runtime.
- Composition root is the single wiring entry point. Runtime dependencies should be connected explicitly there, not resolved ad hoc inside leaf components.
- Startup fail-fast is intentional. Missing or duplicated required dependencies should stop initialization early with clear errors.
- When multiple dependency-related errors cascade, treat them as one upstream composition failure and fix the root wiring first.
- Do not hide composition failures by adding fallback global lookups or defensive null branches in consumers.
- Keep wiring contracts deterministic: one source of truth for runtime graph construction, one clear ownership path for each dependency.
- Any scene/prefab change that affects runtime graph composition must include a startup smoke check in validation steps.

## Commit & Pull Request Guidelines
- Commit style in history is mostly conventional (`feat:`, `fix:`, `docs:`); use that format consistently.
- Keep commits scoped (logic, UI, and project settings separated when possible).
- Unity rule: commit `.meta` files with related asset changes.
- PRs should include: summary, linked issue/spec section, validation steps, and screenshots/GIFs for UI or scene-visible changes.
