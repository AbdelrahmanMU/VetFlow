# ADR-0009: Own Design System + Adaptive UI

- **Status:** Proposed (decision approved by owner 2026-07-13, Topic 3; flips
  to Accepted once the owner reviews this write-up)
- **Date:** 2026-07-13
- **Amended:** 2026-07-13 — design-system scope expanded per owner's Topic 3
  review (decision 5); component-library independence recorded in ADR-0012.

## Context

VetFlow's UI must have the visual quality of a premium commercial SaaS
product. It must never look like an AI-generated dashboard, a generic admin
template, or a component-library showcase. The two MVP users work long
sessions doing fast data entry, on desktop first but with tablet/phone use
plausible.

## Decision

1. **VetFlow builds its own design system** above whichever component library
   is selected (the library — PrimeNG or otherwise — is only a component
   foundation, never the visual identity; see ADR-0012). The **design system
   is a first-class architectural asset** and covers:

   - Design tokens
   - Typography
   - Color system
   - Elevation
   - Motion
   - Icons
   - Spacing
   - Responsive rules
   - Adaptive UI rules
   - Accessibility standards
   - RTL standards
   - Component standards
   - Component states
   - Validation standards
   - Empty states
   - Error states
   - Loading states
   - Skeleton states
   - Table standards
   - Form standards
   - Dialog standards
   - Notification standards
   - Dashboard standards
   - Page templates
2. The interface follows an **Adaptive UI** approach rather than simple
   responsive design: different devices get different layouts whenever that
   improves usability.
   - **Desktop:** permanent sidebar, information-dense layouts,
     keyboard-first workflows.
   - **Tablet:** optimized layouts, larger touch targets.
   - **Mobile:** mobile-first navigation, simplified layouts, one-hand
     operation where appropriate.
3. Design principles: premium commercial appearance; Arabic-first
   experience; clean enterprise UI; low visual noise; comfortable for long
   working sessions; fast data entry; keyboard-first on desktop;
   touch-friendly on tablet/phone; consistent component behavior; purposeful
   animations only; accessibility considered from the beginning.

## Alternatives Considered

- **Use a component library's default theme:** fastest, but produces exactly
  the generic-template look this decision forbids.
- **Fully custom components (no library):** maximum control, unsustainable
  build-and-maintain cost for a solo-owner team.
- **Responsive-only design:** one layout stretched across devices; conflicts
  with keyboard-first desktop density and one-hand mobile use.

## Consequences

- A dedicated **UI/UX Architecture discovery topic** precedes engineering
  documentation (owner-requested): design-system philosophy, layout and
  navigation architecture, adaptive UI strategy, component standards, tables,
  forms, keyboard shortcuts, accessibility, RTL architecture, theme
  architecture, design tokens.
- The UI component library is a *foundation under* this design system and an
  implementation detail hidden behind the VetFlow UI Kit (ADR-0012) —
  themability/unstyled support and RTL quality remain primary criteria for
  the internal library.
- Shared UI standards produced by that discovery live in `docs/ui/`.
