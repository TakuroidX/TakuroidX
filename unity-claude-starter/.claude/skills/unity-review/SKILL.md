---
name: unity-review
description: Review changed C# code (git diff) for Unity-specific pitfalls. Use for "review this" or pre-commit checks.
---

# Unity code review

Look at the changes with `git diff` (or `git diff main...HEAD`) and check these points. Report only problems you actually found, with `file:line`.

## Correctness
- `?.` / `??` / `is null` used on a `UnityEngine.Object` (misses destroyed objects)
- A `[SerializeField]` field was renamed without `[FormerlySerializedAs]`
- MonoBehaviour class name and file name don't match
- Order dependencies between `Awake` / `OnEnable` / `Start` (touching another component's uninitialized state)
- Events subscribed in `OnEnable` are not unsubscribed in `OnDisable` / `OnDestroy`
- A coroutine keeps running after its object is destroyed
- An asset was moved or deleted but its `.meta` was left behind (`./scripts/check-meta.sh`)

## Performance
- `GetComponent` / `Find*` / `Camera.main` (older versions) / LINQ / string concatenation / `new` in `Update` / `FixedUpdate`
- Leftover `Debug.Log` in a hot path
- `Instantiate` / `Destroy` every frame where pooling is warranted

## Structure
- Game logic buried in a MonoBehaviour and untestable
- A `UnityEditor` reference outside an Editor asmdef (breaks the build)
- A reference that breaks asmdef dependency direction (Runtime → Editor, etc.)
