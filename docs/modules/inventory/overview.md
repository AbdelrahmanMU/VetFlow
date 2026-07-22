# Inventory — Overview

> Status: Placeholder — pending documentation phase. Do not implement from this file.

> **Write Kernel carve-out (2026-07-22):** a minimal **Inventory Write Kernel** supporting **Purchase
> Receiving only** is specified in [`write-kernel.md`](write-kernel.md) (Draft DoR). It is **not** the
> Inventory module — the module proper (stock levels, movements, projection, batch management) **remains
> pending and undesigned**.

Scope note (owner decision, 2026-07-12): the original Inventory domain was split into three modules — **Inventory** (this module: stock levels and movements), **Batch** (`../batch/`), and **Monitoring** (`../monitoring/`). Exact boundaries between the three will be defined during the documentation phase.
