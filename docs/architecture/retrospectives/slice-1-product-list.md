# Retrospective — Slice 1: Catalog → Product List (S1)

> Status: Record. Engineering evidence only — no governance redesign (Governance
> Change Policy). Scope: the first vertical slice (`db0a671`), its hardening pass
> (R1–R3), and the Slice-2 boundary check. Ledger: `../TECH_DEBT_LEDGER.md`.

**Outcome:** read-only Product List shipped per DEC-CAT-025 (.NET 10 query-side
CQRS, EF Core + PostgreSQL, RFC 9457, Angular 21 over the VetFlow UI Kit),
**105/105 tests**, verified live, committed and owner-reviewed. Hardening R1–R3
landed in the same commit; all gates re-verified green this session (build 0/0,
format clean, 105/105, ESLint + Stylelint clean; R1 test proven to *bite*).

**1. What worked well.** Documentation-First traced every field/filter/sort to an
approved ID — the review's `spec-conformance` and `doc-sync` dimensions found
**zero** issues. Architecture tests catch real drift (30 tests; R1 proven to bite
via probe). Testcontainers integration tests exercise real PostgreSQL at 18 s. The
ADR-0012 library-independence seam kept the Angular-version pin (F1) out of features.

**2. What slowed development.** *Bootstrap tax (F2)* — the first slice carries the
solution, UI Kit, theme, Docker, and test harness on top of the feature. *State
drift* — the session-start git snapshot was stale; STATUS/memory had to be
reconciled against source (the "repository is the only truth" rule caught that
Phase 1 was already done). *Hardening rework* — R1–R3 were avoidable had the
forbidden-library test (F5) existed from the start.

**3. Foundation friction.** F1 — ADR-0005 "latest stable Angular" vs. PrimeNG one
major behind (shipped on 21). F3 — Stylelint is blind to Angular inline component
styles; only `.scss` is checked.

**4. Governance friction.** F4 — standards name CI enforcement, but no CI platform
exists (equivalents run as arch tests / locally). F5 (*resolved*) — STD-BE-020/028
claimed a forbidden-library test that did not exist; fixed as R1. *Directive drift*
— the hardening directive named R1–R3 as pending though they had landed in
`db0a671`; verifying against the repository prevented redundant rework.

**5. Architecture friction.** F6/R5 — STD-BE-004 enforced by an enumerated entity
allowlist that omits real entities (no breach, under-covers). R4 — `CorsOptions`
binds ad-hoc, bypassing `AddOptions/ValidateOnStart`. Both open (ledger TD-005/004).

**6. Change before Slice 2.** (a) Resolve the Slice-2 doc gaps — internal-code
format, audit-log infrastructure, duplicate strictness (owner-ruled this session:
DEC-CAT-026/027/028/029). (b) Namespace-scope the architecture rules (closes R5).
(c) Decide the CI platform (F4). (d) Low-cost: fold `CorsOptions` into the options
pipeline (R4); close the inline-style lint gap (F3).

**Net:** the foundation held — the frictions are documentation gaps and
enforcement-coverage edges, not design flaws. No redesign recommended.
