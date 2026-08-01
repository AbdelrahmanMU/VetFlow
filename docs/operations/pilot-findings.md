# Pilot Findings Log — سجلّ ملاحظات التجربة الأولى

> Status: Live during the Pilot. **Owner ruling (2026-07-31, with the GO
> decision): every Pilot finding is recorded here under exactly one of three
> categories — Bug · Usability · Enhancement. No other categories.**
> Bugs are fixed through the normal gates (fixing is not a new feature).
> Enhancements are **not implemented during the Pilot** unless the owner rules
> them required to keep the system operational.

| # | Date | Category (Bug / Usability / Enhancement) | Screen / flow | What was observed (المشاهدة كما وقعت) | Raised by | Owner ruling / outcome |
|---|---|---|---|---|---|---|
| 1 | | | | | | |

## Finding report structure (owner ruling, 2026-08-01)

Every finding is written up in **exactly these fields, and nothing else** —
one finding per issue, with **related findings grouped into a single issue
rather than duplicated**:

| Field | Note |
|---|---|
| **ID** | one per issue |
| **Category** | exactly one: Bug · Usability · Enhancement |
| **Severity** | Critical · High · Medium · Low |
| **Steps to reproduce** | |
| **Expected behavior** | cited from the approved documentation where one applies |
| **Actual behavior** | |
| **Root cause** | **only if known** — never speculated |
| **Suggested fix** | **one paragraph only** |
| **Affected modules** | |
| **Regression risk** | |

**No implementation is proposed until the owner says «Fix this issue».** A
described observation is classified and then waited on. Working-mode rule:
`.claude/rules/workflow.md` §Pilot Observation Mode.

**Category guide (from the owner's ruling):**
- **Bug** — behavior contradicting an approved rule or criterion, or a crash/
  data fault. → Fix through the normal gates.
- **Usability** — the system is correct but the user hesitated, was slowed, or
  misread the screen. → Owner decides if and when to address.
- **Enhancement** — a capability or change beyond the approved scope. → Parked
  for the owner; implemented during the Pilot **only** if the owner rules it
  required for operational continuity.
