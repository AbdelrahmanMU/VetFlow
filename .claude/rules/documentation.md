# Rule: Writing Documentation

- One module = one folder under `docs/modules/` with the standard file set
  defined by `docs/modules/_TEMPLATE/`: `overview.md`, `requirements.md`,
  `business-rules.md`, `workflow.md`, `ui.md`, `acceptance.md`,
  `decisions.md`, `test-scenarios.md`. Create new modules with `/new-module`.
- Hybrid language (per ADR-0002): engineering docs in English;
  business/product content — `docs/business/`, `docs/shared/`, and module-doc
  content — in Arabic. File names, doc statuses, and stable IDs
  (`REQ-`/`BR-`/ADR) stay in English. Canonical Arabic terms in
  `docs/shared/GLOSSARY.md`; user-facing Arabic copy in each module's `ui.md`.
- Shared folders (`docs/architecture/`, `docs/ui/`, `docs/business/`,
  `docs/shared/`) hold only what genuinely spans modules. If it can live in
  one module, it must.
- Link instead of duplicating. Every module gets a row in
  `docs/modules/_INDEX.md`; every ADR a row in `decisions/_INDEX.md`.
