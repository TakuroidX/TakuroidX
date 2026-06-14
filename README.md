# Hi, I'm Takuro 👋

Solo developer in Tokyo, exploring **LLM-augmented autonomous systems** through hands-on building.

I work as an **AI Director** — directing Claude on architecture and implementation, applying my own domain judgment, vision, and 15 months of trial-and-error. This is a personal experiment in modern AI-augmented engineering.

---

## 🚀 What I'm working on

### [bitflyer-mcp](https://github.com/TakuroidX/bitflyer-mcp) — open source
A read-only **MCP server** for bitFlyer public market data (ticker, order book,
executions, exchange health) — letting Claude and other MCP clients query the
exchange in natural language.
**Safe by design:** public API only, no API key, no account access. MIT licensed.

### llm-self-evolve *(private — in development)*
A framework for **LLM-powered autonomous code improvement** with multi-layer safety.
Claude observes a host system, identifies failure patterns, generates code
(multi-file, 3-layer), validates statistically, and creates PRs for human review.
Built with Claude over 15 months, tested in a 24/7 simulation environment.

> *The loop closes itself. The human only approves merges.*

### Cryptocurrency Trading Bot *(private)*
The reference implementation domain for `llm-self-evolve`.
24/7 **simulation** mode (not live trading with real funds).
XGBoost + technical-analysis fusion with multi-layer filtering.
**Trading performance is ~breakeven, not profitable** — this is a learning project,
and being honest about that is part of the point.

### survivor-brain-pi *(private)*
Non-invasive head-impact monitor for combat sports. Hardware × ML × athlete safety.

---

## 🛠️ Tech I use daily

```
Languages:      Python, TypeScript
Frameworks:     FastAPI, Vue 3, Vite, MCP (Model Context Protocol)
LLM:            Anthropic Claude SDK
ML:             XGBoost, TensorFlow
Infrastructure: macOS + cron + git worktree
Testing:        pytest
```

---

## 🎯 Currently exploring

- **AI Director workflow**: letting Claude propose architecture and implementation,
  while I provide domain judgment, boundaries, and vision
- **Autonomous improvement loops**: observation → analysis → proposal →
  validation → implementation → monitoring
- **Anti-overfitting discipline**: building the verification harness that tells a
  *real* result from a *comforting illusion* — and not shipping changes that fail it
- **Honest evaluation**: what's actually novel vs what's standard — essential for
  not over-claiming in this AI hype cycle

---

## 📫 Get in touch

- 💼 Open to opportunities (Tokyo / remote, AI-augmented engineering roles)

---

<sub>"Don't try to write the perfect strategy. Build a system that learns to improve itself."</sub>

<sub>This profile was written together with Claude.</sub>
