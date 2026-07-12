---
description: Scaffold the documentation folder for a new business module
argument-hint: <module-name-kebab-case>
---

Create the documentation set for module `$ARGUMENTS`:

1. Run `scripts/new-module.ps1 -Name $ARGUMENTS`.
2. Fill the `overview.md` header with the module's Arabic name — ask the owner
   if unknown. Leave all docs in `Draft` status.
3. Add a row for the module to `docs/modules/_INDEX.md`.
4. Remind the owner: documentation requires approval before any implementation.
