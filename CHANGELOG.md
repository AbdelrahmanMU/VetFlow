# Changelog

Format: [Keep a Changelog](https://keepachangelog.com). Entries are added when releases are cut.

## [Unreleased]

- Sprint 0: repository foundation created (structure per ADR-0001, Accepted
  with owner revisions).
- ADR-0002 replaced with hybrid documentation (engineering EN / business AR).
- Modules: Inventory split into Inventory, Batch, Monitoring; Products renamed
  to Catalog (Pricing is a Catalog capability); Settings added.
- `docs/modules/_TEMPLATE/` standard eight-file module doc set.
- `docs/shared/` for cross-module business docs (vision, glossary, personas,
  events with initial event list, roadmap, reference split into
  external/regulations/competitors/assets).
- `.claude/playbooks/` (discovery, new-feature, review, bug-fix, refactor,
  release); decision logs split into Global and Module.
