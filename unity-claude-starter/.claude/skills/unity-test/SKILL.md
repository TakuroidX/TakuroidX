---
name: unity-test
description: Run the Unity compile check and EditMode/PlayMode tests in batchmode, analyze any failures, and fix them. Use for "run the tests", "check it compiles", or verifying after a change.
---

# Unity tests and compile check

1. Run `./scripts/unity-compile.sh`.
   - If you see `error CS`, open the file and line from the message and fix it. Rerun until errors are zero.
   - If you see "another Unity instance is running", ask the user to close the Unity Editor and stop there.
2. Run `./scripts/unity-test.sh editmode`.
   - On failure, read `<test-case ... result="Failed">` `<message>` / `<stack-trace>` in `Logs/claude/test-editmode-results.xml`.
   - First decide whether the implementation or the test is wrong. Only change a test's expected value when the spec has changed.
3. If the change touches MonoBehaviour, scene, or frame-dependent behavior, also run `./scripts/unity-test.sh playmode`.
4. When reporting, give the numbers from the scripts (total/passed/failed) as they are. If a script could not run (no Unity), say so plainly.

To run a single test: `./scripts/unity-test.sh editmode "Game.Tests.ClassName.MethodName"`
