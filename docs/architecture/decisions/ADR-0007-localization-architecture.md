# ADR-0007: Localization-Ready Architecture, Arabic-First MVP

- **Status:** Proposed (decision approved by owner 2026-07-13, Topic 3; flips
  to Accepted once the owner reviews this write-up)
- **Date:** 2026-07-13

## Context

The MVP serves one Egyptian clinic in Arabic. Retro-fitting localization into
a system that hardcodes language, currency, calendar, or text direction is a
redesign-scale effort, and the product vision allows future tenants with
different regional needs.

## Decision

- The **MVP ships in Arabic** (single UI language).
- The architecture is **localization-ready from the beginning**: adding
  languages later must **not require redesign** (no hardcoded user-facing
  strings; locale-sensitive formatting isolated behind services; RTL is the
  first-class direction but layout must not assume it).

MVP regional defaults:

- Currency: **Egyptian Pound (EGP)**
- Calendar: **Gregorian**
- Numerals: **Western (0-9)**

Future **tenant-specific localization** (currency, calendar, numerals,
language) must remain possible.

## Alternatives Considered

- **Hardcode Arabic/EGP everywhere:** fastest MVP, redesign-scale cost the
  day a second locale or tenant appears. Rejected.
- **Full multi-language MVP:** doubles content work (translations, testing)
  with no MVP user who needs it. Rejected as premature.

## Consequences

- All user-facing strings live in localization resources from the first
  screen, even with only an Arabic resource present.
- Money, dates, and numbers are stored culture-neutral and formatted at the
  edge per the active locale defaults.
- The UI/UX architecture discovery (see STATUS.md agenda) must cover RTL
  architecture explicitly; the design system (ADR-0009) treats direction as
  a token-level concern.
