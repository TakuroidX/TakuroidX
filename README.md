Python tools and small experiments, built with AI.

## Projects

**[evolve-lab](https://github.com/TakuroidX/evolve-lab)** (v1.0.0)
A small, deterministic case study of a self-improving loop fooling itself, and the selection gates
that caught it: held-out checks, time-split checks, and a look-ahead-leak demo. Pure Python, no
dependencies.

**[bitflyer-mcp](https://github.com/TakuroidX/bitflyer-mcp)**
A read-only MCP server for bitFlyer's public market data: prices, order books, recent trades,
and exchange status. It needs no API key and cannot place orders.

**[Punchy](https://punchy.fit/)**
A boxing training web app. Free to use.

## Notes

Trading experiments, all in simulation, did not find a profitable edge. They taught me to decide
how a test will be judged before looking at the result, and to watch for leaked future data,
costs larger than the signal, and choosing on the same data used for judging.

Currently testing whether bitFlyer–Coincheck price differences survive trading costs.

Open to work.
