# CLAUDE.md

This file gives Claude Code guidance for working in this Unity project.

## Project overview

- Engine: Unity (the version is `m_EditorVersion` in `ProjectSettings/ProjectVersion.txt`)
- Language: C# (Unity's C# version / .NET Standard 2.1 compatible)
- Game code: `Assets/_Game/` (everything else in `Assets/` is assumed to be third-party or Unity-generated)

## Directory layout

```
Assets/_Game/
  Scripts/Runtime/   … Game code (Game.Runtime.asmdef, namespace Game)
  Scripts/Editor/    … Editor extensions and build scripts (Game.Editor.asmdef, Editor only)
  Tests/EditMode/    … Pure logic tests (Game.Tests.EditMode.asmdef)
  Tests/PlayMode/    … Tests that need MonoBehaviours or frames (Game.Tests.PlayMode.asmdef)
  Scenes/ Prefabs/ Art/ Audio/ … Assets (created and edited in the Unity Editor)
scripts/             … Shell scripts for running Unity in batchmode
```

A new `.cs` file must go under the folder of the right asmdef. Code outside an asmdef ends up in `Assembly-CSharp` and can't be referenced from tests.

## Commands (run from the repository root)

| Purpose | Command |
|---|---|
| Compile check (fastest feedback) | `./scripts/unity-compile.sh` |
| EditMode tests | `./scripts/unity-test.sh editmode` |
| PlayMode tests | `./scripts/unity-test.sh playmode` |
| Build | `./scripts/unity-build.sh StandaloneWindows64` (etc.) |
| .meta consistency check | `./scripts/check-meta.sh` |

- The Unity executable is found from the `UNITY_PATH` environment variable, or from the Unity Hub default location plus `ProjectVersion.txt`.
- **Batchmode fails while the Unity Editor has the same project open** (project lock). If you see "another Unity instance is running", ask the user to close the Editor. Never delete the lock file on your own.
- Logs go to `Logs/claude/`. When something fails, read the log and look for `error CS` / `Exception`.

## Workflow after changing code

1. `./scripts/unity-compile.sh` → confirm zero compile errors
2. Add or update tests for the logic you changed → `./scripts/unity-test.sh editmode`
3. Run `playmode` too if MonoBehaviour behavior changed
4. At the end, list for the user **anything that needs manual work in the Editor** (placing things in scenes, Inspector setup, and so on)

If Unity can't run in the current environment (for example, the cloud), say plainly that you couldn't run compile or tests. Never claim "it passed" or "it works".

## Rules for Unity-specific files (important)

- **Don't create or edit `.meta` files by hand.** Unity generates them when it imports assets. The GUID inside is the reference key for scenes and prefabs.
- **Moving or renaming** an asset must move its `.meta` too: `git mv Foo.cs Bar.cs && git mv Foo.cs.meta Bar.cs.meta`. Renaming a MonoBehaviour class requires the file name to match the class name.
- **Don't hand-edit `.unity` / `.prefab` / `.asset` (YAML) files as a rule.** They're full of fileID/GUID references, and a mistake silently breaks them. If you have to, keep the change to a minimum and tell the user to verify it in the Editor.
- Don't read or edit `Library/`, `Temp/`, `Logs/`, `obj/`, `Builds/`, `UserSettings/` (generated files, huge).
- Renaming a `[SerializeField]` field loses the values already set in the Inspector. When renaming, add `[FormerlySerializedAs("oldName")]`.

## C# coding conventions

- Namespaces: `Game` (runtime), `Game.EditorTools` (editor), `Game.Tests` (tests). Don't use `Game.Editor` (it conflicts with `UnityEditor.Editor`).
- Inspector-exposed fields use `[SerializeField] private`, not `public` fields.
- Names: types and methods `PascalCase`, private fields `_camelCase`, serialized fields `camelCase`.
- **Separate logic from MonoBehaviour**: put game rules in plain C# classes (e.g. `HealthModel`) that you can test in EditMode, and keep the MonoBehaviour a thin wrapper (e.g. `Health`).
- Don't use `?.` / `??` / `is null` on `UnityEngine.Object` (they bypass Unity's overloaded `==` and miss destroyed objects). Use `if (obj != null)` or `if (obj)`.
- Avoid `GetComponent` / `Find*` / LINQ / string concatenation (allocations) in `Update`. Cache in `Awake`.
- `Debug.Log` in production code only where it's needed. Wrap in `#if UNITY_EDITOR || DEVELOPMENT_BUILD` if necessary.

## Git

- Commit messages follow Conventional Commits (`feat:`, `fix:`, `refactor:`, `test:`, `docs:`, `chore:`).
- Always commit a `.meta` together with its asset.
- Large binaries (images, audio, models) are managed with Git LFS (see `.gitattributes`).
