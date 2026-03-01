This repository is being refactored from Lubelogger into PawLogger using a controlled phased domain migration.

Non-negotiable rules:

* Do not do a full rewrite.
* Preserve the existing architecture, frameworks, dependencies, routing, and project structure unless absolutely required.
* Reuse existing CRUD patterns, services, database access, validation, and shared UI components wherever possible.
* Only implement the phase explicitly requested.
* Do not jump ahead to later phases.
* Do not perform unrelated refactors, formatting churn, or speculative cleanups.
* Keep the project building after each phase.
* Prefer additive and safe database/schema changes.
* Do not delete legacy vehicle-specific logic until the replacement pet-health logic is implemented and verified.
* Avoid blind global renames; use deliberate domain mapping.
* At the end of each requested phase, summarize:

  * what changed
  * what was intentionally left untouched
  * what legacy vehicle-specific items remain
  * any migration/build concerns
