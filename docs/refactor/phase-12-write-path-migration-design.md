# Phase 12 - Write-Path Migration Design / Spec

## 1. Executive summary

This document defines a non-destructive write-path migration blueprint from legacy vehicle-era write contracts to profile-era v2 write contracts for already shadowed families.

Scope constraints for this phase:
- Design only; no runtime code, schema, route, controller, JS, model, or test implementation changes.
- Legacy write behavior (`/Vehicle/*`, `/api/vehicle/*`, `VehicleId`/`vehicleId`) remains authoritative during transition.
- v2 write rollout must be additive, flag-gated, observable, and rollback-safe.

Recommended strategy:
- Use **Option B (shared write handler with dual contract acceptance)** as the target architecture, introduced incrementally per family.
- Keep storage authoritative on existing `VehicleId` semantics through migration; avoid persistence dual-write in this phase family set.
- Preserve existing webhook/event names and payload keys during write migration; add event contract evolution later as a separate staged effort.

Safest first write family: `notes`.
Highest-risk family to defer: `healthrecords` (and linked medical families) because of multi-record side effects and cross-family coupling.

---

## 2. Current write-side legacy surfaces

### 2.1 API write endpoints (legacy `/api/vehicle/*`)

Families with existing API writes (`add`/`update`/`delete`):
- `servicerecords`
- `gasrecords`
- `reminders`
- `odometerrecords` (plus `recalculate`)
- `planrecords`
- `taxrecords`
- `repairrecords`
- `notes`
- `upgraderecords`
- `equipmentrecords`
- `supplyrecords`

Families currently read-only in API (no legacy API write endpoints):
- `healthrecords`
- `vetvisitrecords`
- `vaccinationrecords`
- `medicationrecords`
- `licensingrecords`
- `petexpenserecords`

Observation:
- Existing v2 profile routes are read-shadow only.
- No current `/api/v2/profiles/*` write endpoints for the covered families.

### 2.2 MVC/form write endpoints (`/Vehicle/*`)

All covered families currently write through MVC endpoints using `Save*ToVehicleId` and `Delete*ById` patterns:
- `SaveServiceRecordToVehicleId`, `DeleteServiceRecordById`
- `SaveGasRecordToVehicleId`, `DeleteGasRecordById`
- `SaveReminderRecordToVehicleId`, `DeleteReminderRecordById`, `PushbackRecurringReminderRecord`
- `SaveOdometerRecordToVehicleId`, `DeleteOdometerRecordById`, `ForceRecalculateDistanceByVehicleId`
- `SavePlanRecordToVehicleId`, `DeletePlanRecordById` (+ template/progress operations)
- `SaveTaxRecordToVehicleId`, `DeleteTaxRecordById`
- `SaveCollisionRecordToVehicleId`, `DeleteCollisionRecordById`
- `SaveNoteToVehicleId`, `DeleteNoteById`
- `SaveUpgradeRecordToVehicleId`, `DeleteUpgradeRecordById`
- `SaveEquipmentRecordToVehicleId`, `DeleteEquipmentRecordById`
- `SaveSupplyRecordToVehicleId`, `DeleteSupplyRecordById`
- `SaveHealthRecordToVehicleId`, `DeleteHealthRecordById`
- `SaveVetVisitRecordToVehicleId`, `DeleteVetVisitRecordById`
- `SaveVaccinationRecordToVehicleId`, `DeleteVaccinationRecordById`
- `SaveMedicationRecordToVehicleId`, `DeleteMedicationRecordById`
- `SaveLicensingRecordToVehicleId`, `DeleteLicensingRecordById`
- `SavePetExpenseRecordToVehicleId`, `DeletePetExpenseRecordById`

### 2.3 Import/export dependencies (write-side)

Write-coupled import/export surfaces:
- `/Vehicle/ImportToVehicleIdFromCsv` writes imported records by `vehicleId` and `ImportMode`.
- `/Vehicle/ExportFromVehicleToCsv` writes export artifacts based on `vehicleId`-scoped data.
- `Models/Shared/ImportModel.cs` export DTOs use legacy key names (`VehicleId`, `DueOdometer`, `FuelConsumed`, etc.).

### 2.4 Backup/restore implications

- `FileHelper.MakeBackup()` archives DB + assets/config without contract-version metadata.
- `FileHelper.RestoreBackup()` restores DB/assets with legacy-path tolerance, but no write-contract version negotiation.
- Restore behavior assumes storage compatibility and must continue tolerating mixed-origin records.

### 2.5 Webhook/event emissions on writes

- Write paths emit per-record events (`*.add`, `*.update`, `*.delete`, plus `.api` variants).
- Payload and hub grouping remain vehicle-keyed (`VehicleId`, `data.vehicleId`, SignalR group `vehicleId_{id}`).
- Existing ecosystem compatibility depends on current event names and payload fields.

### 2.6 Auth/ownership/security checks on writes

Current enforcement pattern:
- API writes: `APIKeyFilter` + `CollaboratorFilter` + explicit `_userLogic.UserCanEditVehicle(...)` checks.
- MVC writes: explicit `_userLogic.UserCanEditVehicle(...)` checks.
- Permissions vary by action (`Edit`, `Delete`, `View`) and must remain invariant across legacy/v2 routes.

### 2.7 Validation/model binding surfaces

- API add/update validation is per-family imperative validation in controller methods.
- Query/body alias parsing is centralized in `QueryParamFilter` for `vehicleId` alias support and conflict rejection.
- Inputs/DTOs remain `VehicleId`-named across input models and export models.

### 2.8 Browser/query/client assumptions affecting writes

- Browser write callers post to `/Vehicle/Save*ToVehicleId` endpoints.
- Client identifier authority is `GetVehicleId().vehicleId` and mirrored URL aliases (`animalId` <-> `vehicleId`).
- Realtime subscriptions use `vehicleId_{id}` SignalR group naming.
- Session/local state uses legacy `vehicleId`-keyed storage names in multiple flows.

---

## 3. Target end state for write contracts

Target contract posture (post migration window):
- Legacy v1 writes continue to function for backward compatibility (`/api/vehicle/*`, `/Vehicle/*`).
- v2 profile write routes exist for covered families under `/api/v2/profiles/<family>/(add|update|delete)`.
- v2 writes accept `petProfileId` canonical naming, with bounded legacy alias acceptance.
- Authorization and side effects are identical between v1 and v2 for equivalent operations.
- Event/backup/import compatibility remains stable until explicitly versioned contract transitions are launched.

Non-goals for this phase sequence:
- No persistence key rename/cutover.
- No webhook event-name swap.
- No browser storage namespace migration.

---

## 4. Family-by-family write migration matrix

Legend:
- Shared handler: legacy + v2 call same business path.
- Dual-accept parsing: accept both `vehicleId` and `petProfileId` during transition.
- Dual-write: writing multiple persistence identifiers/records for same logical write (not recommended here).

| Family | Legacy write endpoints now | Current payload shape | Future v2 write route/path | Future v2 payload naming | Shared handler? | Dual-accept safe? | Dual-write needed? | Rollback notes | Test requirements |
|---|---|---|---|---|---|---|---|---|---|
| servicerecords | API + MVC | `GenericRecordExportModel` + `vehicleId` | `/api/v2/profiles/servicerecords/add|update|delete` | `petProfileId` alias to legacy id | Yes | Yes | No | disable v2 route flag | v1/v2 parity, auth parity, side-effect parity (odometer auto-insert) |
| gasrecords | API + MVC | `GasRecordExportModel` + `vehicleId` | `/api/v2/profiles/gasrecords/add|update|delete` | `petProfileId` | Yes | Yes | No | disable v2 route flag | validation parity, auth parity, event parity |
| reminders | API + MVC | `ReminderExportModel` + metric rules | `/api/v2/profiles/reminders/add|update|delete` | `petProfileId` | Yes | Yes | No | disable v2 route flag | metric validation parity, conflict-id rejection, event parity |
| odometerrecords | API + MVC | `OdometerRecordExportModel`, optional `autoIncludeEquipment` | `/api/v2/profiles/odometerrecords/add|update|delete|recalculate` | `petProfileId` | Yes | Yes (strict) | No | disable v2 route flag | equipment-id validation parity, recalc parity, auth parity |
| planrecords | API + MVC | `PlanRecordExportModel` enums + constraints | `/api/v2/profiles/planrecords/add|update|delete` | `petProfileId` | Yes | Yes | No | disable v2 route flag | enum validation parity, supply requisition restore parity |
| taxrecords | API + MVC | `TaxRecordExportModel` | `/api/v2/profiles/taxrecords/add|update|delete` | `petProfileId` | Yes | Yes | No | disable v2 route flag | recurring tax update side-effect parity |
| repairrecords | API + MVC (`collision` model) | `GenericRecordExportModel` | `/api/v2/profiles/repairrecords/add|update|delete` | `petProfileId` | Yes | Yes | No | disable v2 route flag | attachment/odometer side-effect parity |
| notes | API + MVC | `NoteRecordExportModel` / `Note` | `/api/v2/profiles/notes/add|update|delete` | `petProfileId` | Yes | Yes | No | disable v2 route flag | baseline canary family; auth/event parity |
| upgraderecords | API + MVC | `GenericRecordExportModel` | `/api/v2/profiles/upgraderecords/add|update|delete` | `petProfileId` | Yes | Yes | No | disable v2 route flag | parity with repair/service generic contract |
| equipmentrecords | API + MVC | `EquipmentRecordExportModel` | `/api/v2/profiles/equipmentrecords/add|update|delete` | `petProfileId` | Yes | Yes | No | disable v2 route flag | equipment state parity + auth parity |
| supplyrecords | API + MVC | `SupplyRecordExportModel` | `/api/v2/profiles/supplyrecords/add|update|delete` | `petProfileId` | Yes | Yes (special vehicleId=0 shop supply behavior) | No | disable v2 route flag | shop-supply access parity, permission parity |
| healthrecords | MVC only | `HealthRecordInput` | Deferred; eventually `/api/v2/profiles/healthrecords/add|update|delete` if API write parity introduced | `petProfileId` | Yes, later | Conditional (needs linked-reminder guardrails) | No | keep MVC-only path | linked reminder sync parity, delete cascade parity |
| vetvisitrecords | MVC only | `VetVisitRecordInput` | Deferred; later v2 API writes only after health parity | `petProfileId` | Yes, later | Conditional | No | keep MVC-only path | linked health record creation/deletion parity |
| vaccinationrecords | MVC only | `VaccinationRecordInput` | Deferred | `petProfileId` | Yes, later | Conditional | No | keep MVC-only path | linked health sync/cascade parity |
| medicationrecords | MVC only | `MedicationRecordInput` | Deferred | `petProfileId` | Yes, later | Conditional | No | keep MVC-only path | linked health sync/cascade parity |
| licensingrecords | MVC only | `LicensingRecordInput` | Deferred | `petProfileId` | Yes, later | Conditional | No | keep MVC-only path | linked health sync/cascade parity |
| petexpenserecords | MVC only | `PetExpenseRecordInput` | Deferred | `petProfileId` | Yes, later | Conditional | No | keep MVC-only path | health-link selection parity |

Recommendation for deferred families:
- Do not introduce first-wave API v2 writes where no legacy API write contract exists yet.
- First stabilize v2 writes for families already having v1 API write contracts.

---

## 5. Identifier authority and conflict rules

### 5.1 Transition authority

- **Internal authoritative identifier remains `vehicleId`/`VehicleId`** during transition.
- `petProfileId` is accepted as an alias at route/query/body boundaries where explicitly enabled.

### 5.2 When both ids are supplied

- If both are present and equal: proceed.
- If both are present and conflict: reject with `400` and stable error text.

### 5.3 Precedence and safe parsing

Recommended precedence for write requests:
1. Route/query id resolution (`vehicleId` + `petProfileId`) via shared resolver.
2. If route/query unresolved and endpoint permits body id fallback, parse body id aliases.
3. If resolved route/query id conflicts with body id field, reject `400`.

### 5.4 Update/delete authority

- For update/delete by record id, authoritative profile ownership is derived from existing record lookup (`existingRecord.VehicleId`), not mutable body id fields.
- Ignore non-authoritative body profile ids after conflict check.

### 5.5 Ambiguous input behavior

Safe behavior for ambiguity:
- Do not guess.
- Return explicit `400` with alias-conflict message.
- Emit observability marker/log for conflict for rollback diagnostics.

---

## 6. Auth/security invariants

Required invariants:
- Same authentication requirements for legacy and v2 writes.
- Same permission gates (`Edit`, `Delete`, `View`) for equivalent operations.
- Same ownership decision source (`UserCanEditVehicle` / collaborator filters) regardless of route version.
- Same unauthorized outcomes/status patterns for equivalent failure conditions.

Privilege-drift tests required before enabling any family:
- Legacy/v2 allow matrix by role: root, direct collaborator, household collaborator (with/without permission), unauthorized user, API key with insufficient scope.
- Conflict-id rejection cannot be bypassed by supplying alternative id in body.
- Update/delete cannot escalate by changing body profile id.

High-risk failure modes to guard:
- Route-version-dependent permission behavior.
- Alias parsing that overrides validated route id silently.
- Shop-supply (`vehicleId=0`) behavior divergence in `supplyrecords`.

---

## 7. Webhook/event strategy

Current state:
- Event names and payloads remain vehicle-era (`*.add|update|delete`, `VehicleId`, `data.vehicleId`, `vehicleId_{id}` hub groups).

Recommended transition sequence:
1. During write-route migration, keep emitting current event names/payloads only.
2. Add event-contract versioning in a later dedicated phase.
3. Introduce profile-era event naming only after consumers can negotiate versions.

Dual emission guidance:
- **Do not dual-emit by default** in early write migration; risk of duplicate downstream processing is high.
- If dual emission is ever introduced, require explicit endpoint-level flagging, idempotency guidance, and consumer readiness sign-off.

---

## 8. Import/export/backup/restore implications

Import/export:
- Existing import/export contracts remain legacy-named and write-coupled to `vehicleId`.
- v2 write-route introduction alone must not alter CSV/API export schemas.
- Any payload renaming in export/import requires explicit schema versioning and parser compatibility mode.

Backup/restore:
- Backup artifacts currently have no write-contract version metadata.
- Restore must continue to accept mixed data origins (legacy/v2 route-created records) as long as persistence schema is unchanged.
- Before any persisted write semantic changes, add backup manifest metadata (format/version/feature flags) in a later phase.

---

## 9. Telemetry / feature flag / rollback plan

### 9.1 Feature flags (recommended boundaries)

- `WriteV2RoutesEnabled` (global hard gate)
- `WriteV2AliasParsingEnabled` (global id alias acceptance)
- `WriteV2Family_<family>` (per-family enablement)
- `WriteV2StrictIdConflictReject` (must be on by default)
- `WriteV2CanaryUsers` or `WriteV2CanaryApiKeys` (targeted rollout)

### 9.2 Observability required

Per request, log/metric dimensions:
- contract route (`legacy-v1` vs `v2-profiles`)
- family + operation (`add|update|delete|recalculate`)
- id source (`vehicleId`, `petProfileId`, conflict)
- auth outcome + permission type
- validation failure category
- downstream side effect success/failure (event publish, linked record sync)

### 9.3 Canary path

- Start with `notes` family, API writes only.
- Canary to root/internal API keys first.
- Expand to one production cohort after parity + error-rate gates pass.

### 9.4 Rollback triggers

Immediate rollback (disable per-family v2 flag) if any occur:
- auth-deny/allow drift from baseline
- conflict-id bypass or ambiguity bug
- event volume anomaly indicating duplicate/missed emissions
- sustained increase in 4xx/5xx above defined threshold

### 9.5 Release gates

Gate to broaden rollout only when all are true:
- contract tests green (legacy unchanged + v2 parity)
- auth parity tests green
- no unresolved canary incidents over defined soak period
- rollback drill validated for the family

---

## 10. Recommended staged implementation order

### Phase 13 (next): harness + contract matrix + flags

- Add write-route contract tests (route templates + verb + accepts alias fields).
- Add authenticated write integration test matrix for one seed family (`notes`).
- Add feature flags and telemetry plumbing (disabled by default).
- Add id-conflict tests for write requests (query/body/route combinations).

### Phase 14: first additive v2 write shadowing (low risk)

- Implement v2 write route wrappers for `notes`, then `equipmentrecords`.
- Ensure wrappers delegate to existing legacy handlers/shared logic.
- Keep legacy writes primary and unchanged.

### Phase 15: broaden coverage with telemetry gates

- Expand to existing API-write families:
  - `servicerecords`, `gasrecords`, `reminders`, `odometerrecords`, `planrecords`, `taxrecords`, `repairrecords`, `upgraderecords`, `supplyrecords`.
- Roll out one family at a time with canary + rollback criteria.

### Phase 16: deferred families and deprecation planning

- Evaluate introducing API write contracts for currently MVC-only families (`health`, `vetvisit`, `vaccination`, `medication`, `licensing`, `petexpense`) only after side-effect parity tests exist.
- Publish deprecation timeline for legacy writes only after full parity and downstream contract readiness.

---

## 11. Do not implement first

Do **not** start with:
- Persistence/schema renames (`VehicleId` removal, column/key migration).
- Webhook event-name changes or dual emission by default.
- Browser storage/query canonicalization in same release as write-route migration.
- Health-family write migration first (`health`, `vetvisit`, `vaccination`, `medication`, `licensing`) due to linked-record/reminder side effects.
- Broad all-family v2 write rollout without per-family canary and rollback controls.

Most dangerous early anti-patterns:
- Implementing one-off v2 write routes without shared handler parity.
- Accepting both ids without strict conflict rejection.
- Introducing dual-write persistence semantics before contract parity is proven.

---

## Appendix A - Write rollout options

### Option A: legacy-authoritative writes with v2 alias parsing only

- Safety: Highest
- Complexity: Low
- Rollback ease: Highest
- Observability need: Moderate
- Notes: Minimal change, but does not deliver real v2 write routes.

### Option B: shared write handler with dual contract acceptance (recommended)

- Safety: High (if strict conflict/auth parity enforced)
- Complexity: Medium
- Rollback ease: High (disable v2 route flags)
- Observability need: High
- Notes: Best balance of migration progress and risk control.

### Option C: full v2 write shadowing with legacy delegation and broader contract evolution

- Safety: Medium (more moving parts)
- Complexity: High
- Rollback ease: Medium
- Observability need: Very high
- Notes: Appropriate only after Option B families are stable.

Recommendation:
- Execute Option B as primary plan, with Option A as pre-step on families not yet ready.

