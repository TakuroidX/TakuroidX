---
name: unity-feature
description: The standard steps for adding a new feature (component, system, UI logic) to a Unity project. Use for implementation requests like "add X" or "make Y".
---

# Implementing a Unity feature

1. **Understand**: find related code under `Assets/_Game/Scripts/` and read the existing conventions (namespaces, asmdef, event design).
2. **Design**: split the work into a pure C# logic class and a thin MonoBehaviour wrapper.
   - Logic → `Assets/_Game/Scripts/Runtime/` (no dependency on `UnityEngine` if you can manage it)
   - Editor-only code → `Assets/_Game/Scripts/Editor/`
3. **Test first**: write EditMode tests for the logic class in `Assets/_Game/Tests/EditMode/`.
4. **Implement**: write the code. Follow the C# conventions in CLAUDE.md (`[SerializeField] private`, no `?.` on UnityEngine.Object, no allocations in Update).
5. **Verify**: follow the `unity-test` skill for the compile check and tests.
6. **Report**: summarize these three points briefly:
   - The files you changed or added
   - Test results (the actual numbers)
   - **Manual work the user must do in the Unity Editor** (attaching components, setting references in the Inspector, adding scenes to Build Settings, and so on)

Don't do these:
- Create `.meta` files by hand (Unity generates them)
- Hand-edit large parts of `.unity` / `.prefab` files
- Add packages to `Packages/manifest.json` without asking the user
