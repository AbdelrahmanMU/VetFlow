# Decision Log Entry (format)

Routing (a decision lives in exactly one place):

- Reversing would take more than a day → ADR (`docs/templates/adr.md`).
- Spans more than one module (**Global**) → `docs/business/DECISION_LOG.md`.
- Confined to one module (**Module**) → that module's `decisions.md`.

Append rows in this format (decision text in Arabic per ADR-0002):

| Date | Decision — القرار | Why — السبب | Decided by |
|---|---|---|---|
| YYYY-MM-DD | <ما الذي تقرر> | <السبب في سطر واحد> | <owner / name> |
