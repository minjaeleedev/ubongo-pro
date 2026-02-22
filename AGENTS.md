# Repository Guidelines

## Project Structure & Module Organization
This repository is a Unity 6 project (`ProjectVersion.txt`: `6000.3.6f1`).

- Runtime gameplay code: `Assets/Scripts` organized by domain (`Core`, `GameBoard`, `Pieces`, `Input`, `Managers`, `Systems`, `UI`).
- Editor-only tooling: `Assets/Editor` (`Ubongo.Editor.asmdef`).
- Scenes and content: `Assets/Scenes`, `Assets/Prefabs`, `Assets/Materials`, `Assets/Models`, `Assets/Input`.
- Package dependencies: `Packages/manifest.json`.
- Product/design specs: `docs/requirements`.

Keep script files aligned with assembly and namespace boundaries (`Ubongo` for runtime, `Ubongo.Editor` for editor tools).

## Build, Test, and Development Commands
- Open project in Unity Hub: `open -a "Unity Hub"`
- Open in VS Code: `code .`
- Regenerate C# solution files from Unity: `Assets > Open C# Project`
- Open project directly with Unity CLI (macOS example): `"/Applications/Unity/Hub/Editor/6000.3.6f1/Unity.app/Contents/MacOS/Unity" -projectPath "$(pwd)"`
- Run Play Mode tests (when tests exist): `Unity -batchmode -projectPath "$(pwd)" -runTests -testPlatform PlayMode -logFile Logs/playmode-tests.log -quit`

## Coding Style & Naming Conventions
- Follow `.editorconfig`: UTF-8, LF, 4-space indentation for `*.cs`; 2 spaces for `*.json` and `*.asmdef`.
- Use K&R/C# standard readability defaults in this repo: braces on new lines, explicit method bodies preferred.
- Naming: `PascalCase` for classes/public members, `camelCase` for private fields, interfaces prefixed with `I`.
- Match file names to primary class names (example: `GameManager.cs` -> `GameManager`).
- Keep Unity-specific references serialized when needed (`[SerializeField] private ...`).

## Testing Guidelines
- No dedicated automated test assemblies are currently committed; validate changes in Unity Play Mode before opening a PR.
- For non-trivial logic, add Unity Test Framework tests under `Assets/Tests/EditMode` or `Assets/Tests/PlayMode`.
- Test naming: `FeatureNameTests` classes and `Method_State_ExpectedResult` methods.
- Include a short manual test checklist in PRs (scene, input flow, win/fail conditions).

## Commit & Pull Request Guidelines
- Commit style in history is mostly conventional (`feat:`, `fix:`, `docs:`); use that format consistently.
- Keep commits scoped (logic, UI, and project settings separated when possible).
- Unity rule: commit `.meta` files with related asset changes.
- PRs should include: summary, linked issue/spec section, validation steps, and screenshots/GIFs for UI or scene-visible changes.
