# ADR-0006: The Backend API Is a Product API

- **Status:** Proposed (decision approved by owner 2026-07-13, Topic 3; flips
  to Accepted once the owner reviews this write-up)
- **Date:** 2026-07-13

## Context

Angular is the only client at MVP, which tempts a backend-for-frontend design
whose endpoints mirror UI screens. The product vision, however, includes
future clients that must consume the same backend: mobile applications, a
customer portal, AI agents, and external integrations.

## Decision

The backend API is treated as a **product API from day one**. It is designed
so that future clients can consume it **without redesign**, including:

- Mobile applications
- Customer portal
- AI agents
- External integrations

## Alternatives Considered

- **UI-shaped backend (BFF-only):** fastest for the first Angular screens,
  but each future client forces either a redesign or a second API surface.
- **Full public-API program at MVP (developer portal, API keys, rate plans):**
  premature; "product API" here means design discipline, not a commercial
  API offering.

## Consequences

- API contracts model the business domain, not Angular screens; naming
  follows the English side of `docs/shared/GLOSSARY.md`.
- Versioning, consistent error contracts, and pagination/filtering
  conventions are designed in from the start (detailed during engineering
  documentation).
- The authentication strategy (open decision) must work for non-browser
  clients, not only the Angular app.
