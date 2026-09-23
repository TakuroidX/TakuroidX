# Unity × Claude Code Starter

A starter kit that lets Claude Code **write code, compile, run tests, and fix problems autonomously** in a Unity project.

## What's included

| Path | Role |
|---|---|
| `CLAUDE.md` | Project rules for Claude (asmdef layout, `.meta` handling, C# conventions, verification steps) |
| `.claude/settings.json` | Permissions: tests and git commands allowed, reads of `Library/` and edits of `.meta` denied |
| `.claude/skills/` | `/unity-test`, `/unity-feature`, `/unity-review` skills |
| `scripts/` | Batchmode scripts: compile, test, build, and `.meta` checks |
| `Assets/_Game/` | Runtime / Editor / EditMode tests / PlayMode tests split by asmdef, plus sample code |
| `.gitignore` `.gitattributes` `.editorconfig` | Git settings for Unity (YAML merge, LFS) |
| `.github/workflows/unity-ci.yml` | `.meta` check + GameCI test job (optional) |

## Setup

1. Create a new project in Unity Hub (2021.3 or later; tested only at the code level)
2. Copy this kit into it:
   ```bash
   ./install.sh /path/to/MyUnityProject          # with sample code
   ./install.sh /path/to/MyUnityProject --no-sample
   ```
3. Open the project in Unity once (this generates the `.meta` files and installs the Test Framework)
4. In Unity: **Edit > Project Settings > Editor**
   - Version Control: `Visible Meta Files`
   - Asset Serialization: `Force Text`
5. If you use Git LFS, run `git lfs install`
6. Close the Editor, start `claude`, and try this:
   ```
   /unity-test
   ```

If Unity isn't found automatically, set the path:
```bash
export UNITY_PATH="/Applications/Unity/Hub/Editor/6000.0.xxf1/Unity.app/Contents/MacOS/Unity"
```

## Development loop

```
You: "Add a feature where the player dodges while invincible"
  ↓ Claude follows /unity-feature
  pure C# logic + EditMode tests → implementation → unity-compile.sh → unity-test.sh
  ↓
Claude: the changed files, test results, and a list of "manual work in the Editor"
```

**Split the work**: Claude handles what can be finished in text (C# logic, tests, editor extensions, build scripts). Scene layout, Inspector settings, and visual tuning are faster to do by hand in the Editor.

## Limitations and caveats

- **Batchmode can't run while the Unity Editor has the same project open.** Close the Editor before letting Claude run tests. (Or clone a second copy of the project for Claude.)
- Unity can't run in cloud environments (such as Claude Code on the web). There Claude can only write code; compiling and testing happen locally or in CI.
- If you want Claude to operate the Editor directly (scenes, GameObjects, the console), consider the Unity MCP servers published by the community. This kit doesn't include one; install it following each project's docs.

## Optional: merging with UnityYAMLMerge

```bash
git config merge.unityyamlmerge.name "Unity SmartMerge"
git config merge.unityyamlmerge.driver '"<Unity install path>/Tools/UnityYAMLMerge" merge -p %O %B %A %A'
```
