# ADR-0010: Provider-Independent Authentication Architecture

- **Status:** **Accepted** <!-- owner ruling 2026-08-06 -->
  *(Was: "Proposed — decision approved by owner 2026-07-13, Topic 3 review;
  flips to Accepted once the owner reviews this write-up." **That condition was
  met on 2026-08-06**, when the owner reviewed and accepted this write-up. The
  original wording is preserved rather than overwritten, per the amendment
  convention.)*
- **Date:** 2026-07-13 · **Accepted:** 2026-08-06

## Context

The authentication recommendation (ASP.NET Core Identity for the MVP) was
**partially accepted** by the owner. Identity is permitted as the MVP
implementation, but the owner mandates that the application must never be
coupled to it: the product API (ADR-0006) will outlive any particular
identity technology, and future deployments may require enterprise or
standards-based providers.

## Decision

1. The system **may use ASP.NET Core Identity in the MVP**.
2. **Authentication must be abstracted behind an interface.** The
   application is never coupled directly to Identity.
3. The **authentication provider is an interchangeable infrastructure
   implementation**. The architecture must allow future replacement by:
   - OpenIddict
   - Keycloak
   - Azure AD (Entra ID)
   - any future identity provider

   **without affecting the Application layer.**
4. This architectural independence is **mandatory**.

## Alternatives Considered

- **Direct use of ASP.NET Core Identity types throughout the application:**
  the framework default and the least code, but couples every consumer to one
  provider and makes replacement a redesign. Rejected by the owner.
- **External identity provider from day one (Keycloak, Azure AD):** avoids
  the coupling differently, but adds operational or subscription cost that a
  two-user MVP does not justify.

## Consequences

- Identity (user store, password hashing, sign-in) lives only in the
  Infrastructure layer behind the authentication abstraction; the Application
  layer sees provider-neutral concepts (current user, authentication result,
  permissions).
- Replacing the provider is an Infrastructure swap plus configuration — no
  Application-layer change.
- **Still open (not ruled):** the API token mechanism (JWT access +
  refresh-token rotation vs alternatives) and the permission-based
  authorization model were recommended but not explicitly approved; they
  will be specified during engineering documentation for owner review.

  **AMENDED 2026-08-02 (owner ruling, ADR-0022 §7) — the token mechanism is
  now specified, exactly as this clause anticipated.** The Pilot ships **one
  JWT access token and no refresh-token rotation**, carrying user, tenant,
  branch and role. This clause is superseded **for the token mechanism only**;
  **the permission-based authorization model remains unruled** — the Pilot
  uses the closed two-role set of BD-PRD-003 carried on the membership
  (BR-ORG-006), not a permission system. Implementation choices recorded under
  this ADR's §2 abstraction mandate: `PasswordHasher<T>` from ASP.NET Core
  Identity behind the authentication abstraction, with
  `Microsoft.AspNetCore.Authentication.JwtBearer` for token validation
  (DEC-IDN-004). Module documentation: `docs/modules/identity/`.
- Presupposes a layered backend (Application vs Infrastructure); the backend
  layering itself is not yet recorded in any ADR — flagged as a pending
  architecture decision.
