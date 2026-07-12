# ADR-0002: Documentation Language (Hybrid)

- **Status:** Accepted
- **Date:** 2026-07-12

## Context

The product UI is Arabic-first and the owner reads and approves all business
content. Development is AI-assisted with Claude Code, where English technical
documentation is cheaper in tokens, more precise, and better supported by
tooling. The originally proposed ADR-0002 (all engineering docs in English)
was replaced by the owner with this hybrid decision before it was ever
accepted; no documentation content existed yet, so the ADR number is reused
rather than superseded.

## Decision

Hybrid documentation, split by audience:

- **Engineering documentation → English.** Architecture docs and ADRs
  (`docs/architecture/`), shared UI standards (`docs/ui/`), templates,
  `.claude/` rules/commands/playbooks, scripts, `README.md`, `STATUS.md`,
  `CHANGELOG.md`.
- **Business/Product documentation → Arabic.** Domain and product knowledge
  (`docs/business/`), cross-module business docs in `docs/shared/` (vision,
  personas, events, roadmap and release scope), and the business content of
  module docs (`overview`, `requirements`, `business-rules`, `workflow`,
  `ui`, `acceptance`, `decisions`, `test-scenarios`).
- **Engineering scaffolding stays in English/Latin script** even inside
  Arabic documents: file and folder names, doc statuses
  (`Draft/Review/Approved`), stable IDs (`REQ-…`, `BR-…`, `ADR-NNNN`), table
  scaffolding, and cross-reference links — these are parsed by tooling and
  conventions.
- `docs/shared/GLOSSARY.md` remains the canonical EN ↔ AR bridge: every domain term
  has one approved Arabic form and one English form used in code and
  engineering docs.

## Alternatives Considered

- All documentation in English (original proposal): cheapest for AI tooling,
  but the owner — who must read and approve every business document — would
  review in a foreign language. Rejected by the owner.
- All documentation in Arabic: weaker AI precision and tooling support for
  architecture/engineering content.
- Fully bilingual documentation: doubles maintenance and drifts.

## Consequences

The owner approves business content in Arabic; engineers and AI tooling work
in English where precision matters. The glossary becomes load-bearing: it is
the only sanctioned mapping between Arabic domain terms and the English
identifiers used in code and engineering docs. Module documentation templates
keep English scaffolding; their content is authored in Arabic.
