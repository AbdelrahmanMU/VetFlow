# ADR-0012: VetFlow UI Kit — UI Library Independence

- **Status:** Proposed (decision approved by owner 2026-07-13, Topic 3
  review; flips to Accepted once the owner reviews this write-up)
- **Date:** 2026-07-13

## Context

The component-library recommendation proposed PrimeNG as the foundation under
the VetFlow design system (ADR-0009). The owner's ruling goes further:
**PrimeNG is not part of the application's public architecture at all** — it
is an implementation detail, and a new architectural principle makes UI
libraries interchangeable.

## Decision

1. **New architectural principle — UI libraries are implementation
   details.** The application depends only on the **VetFlow UI Kit**.
2. **No feature module may use PrimeNG components (or any future UI
   library) directly.**
3. Every reusable component is wrapped inside the VetFlow UI Kit, e.g.:
   `<VfButton>`, `<VfInput>`, `<VfTable>`, `<VfDialog>`, `<VfCard>`,
   `<VfGrid>`, `<VfSelect>`, etc.
4. Internally these wrappers **may use PrimeNG today**; the internal
   implementation may change in the future **without affecting feature
   modules**.
5. This architectural independence is **mandatory**.

## Alternatives Considered

- **Feature modules use the component library directly:** the ecosystem
  default and less initial code, but every feature becomes coupled to one
  vendor's API; replacing the library is a rewrite of every screen. Rejected
  by the owner.
- **Fully custom components (no underlying library):** independence without
  wrappers, at an unsustainable build-and-maintain cost for a solo-owner
  team. Rejected previously (ADR-0009).

## Consequences

- The UI Kit is where the design system (ADR-0009) is enforced: tokens,
  states, RTL behavior, and component standards are implemented once, in the
  wrappers.
- Wrapping has a real cost — every adopted component needs a `Vf*` wrapper
  with a deliberately owned API; the wrapper API surface is defined during
  the UI/UX Architecture discovery.
- PrimeNG becomes a dependency of the UI Kit package only; feature code
  imports only `Vf*` components (enforceable by lint rules later).
- The UI-independence principle is recorded in
  `docs/architecture/principles.md`.
