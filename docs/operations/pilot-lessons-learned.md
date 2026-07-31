# Pilot Lessons Learned (D8 — PRS WS6, added by owner request 2026-07-31)

> Short by design. What the Pilot Readiness phase taught — gaps found, what
> worked, and what the Pilot itself should keep in mind. Findings, not rules:
> anything here that should bind later work needs the owner to promote it.

## What the phase found that inspection had missed

1. **"It runs in dev" said nothing about deployability.** The frontend had no
   deployment vehicle at all — discovered only because WS1 stated the facts
   before proposing work. The fix (same-origin static hosting inside the API
   image) removed a whole class of operational surface: no second server, no
   CORS, one URL.
2. **A backup is fiction until a restore has been watched.** The first backup
   "succeeded", passed its size check, and was not a valid archive (PowerShell
   text-encodes binary redirection). Two more restore defects (role ownership,
   path resolution) fell out of the same rehearsal. **The procedure is now
   trusted because it failed first, in rehearsal, not in an emergency.**
3. **The `????` Arabic corruption was never an application fault.** The
   deployed stack round-trips Arabic byte-identically; corruption reproduces
   only when Arabic passes through a Windows console command line. Development
   habit going forward: file-based bodies for any Arabic test data. The users'
   path (browser) is unaffected.
4. **Verification data ≠ pilot data.** ADR-0020's "intentionally entered for
   business use" wording carried the whole phase: smoke and rehearsal data
   lived freely in the pilot stack because the definition, not a convention,
   says what counts as real.

## What worked and is worth keeping

- **Executing the runbooks as the verification** — every document in
  `docs/operations/` describes something that was actually run, not something
  intended. The three script defects were found *by the runbook's own steps*.
- **The PRS as single source of truth** — six owner rulings landed in one
  review and unblocked every workstream at once; no mid-phase relitigation.
- **Sequential workstreams with evidence per step** kept the Go/No-Go report
  assemblable in minutes — it is links, not claims.

## For the Pilot itself

- The daily backup is scheduled logic plus a human glance: **the OK line, once
  a day**. If it is missing, that is the day to act.
- The clinic workstation differs from the dev machine only by the runbook's
  prerequisites; the first deployment there should re-run the runbook's verify
  section once (closes PRS-RSK-06).
- UAT will produce capability requests (discount? printing? cash?). The
  defect-log routes them to the owner's «لازمة للتشغيل الناجح» judgment —
  the phase's scope discipline should outlive the phase.
