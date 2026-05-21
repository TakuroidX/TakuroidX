# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this repository is

This is a **GitHub profile repository** (`TakuroidX/TakuroidX`). GitHub renders
the root `README.md` directly on the profile page at https://github.com/TakuroidX.

There is no application code, build system, test suite, linter, or CI here. The
sole deliverable is `README.md`. Edits to it are the work — when it looks right
as Markdown, it ships.

## Working in this repo

- The only file under version control is `README.md`. Changes are pure content
  edits; there is nothing to build, run, or test.
- Preview rendering as GitHub-flavored Markdown. GitHub strips/sanitizes some
  HTML, so prefer Markdown over raw HTML where possible (the existing file uses
  `<sub>` for fine print, which GitHub does allow).
- Commit messages follow Conventional Commits (`docs:`, `feat:`), as seen in the
  history.

## Content conventions (important)

The defining convention of this profile is **honesty over hype** — this is
deliberate, not incidental, and is reflected in both the commit history (e.g.
"honest profile — 'AI Director' framing, no production claim") and the README
body. When editing, preserve these explicit qualifications rather than smoothing
them into stronger claims:

- The trading bot runs in **24/7 simulation mode, not live trading**, and is
  **"not yet profitable — a learning project."**
- The author's role is framed as **"AI Director"** (directing Claude on
  architecture/implementation while providing domain judgment), explicitly
  **not** as having shipped a production system.
- The README calls out "honest evaluation: what's actually novel vs what's
  standard" as a goal. Do not introduce marketing language or unverifiable
  superlatives.

The README links to related projects (`llm-self-evolve`, `survivor-brain-pi`,
and a private trading bot) and lists the author's daily tech stack. Keep links
and project descriptions consistent with how they are described upstream.
