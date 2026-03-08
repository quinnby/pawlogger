# PawLogger Phase 6: Vehicle-Era Eradication Migration Plan

Status: Design/Planning only (no runtime changes in this phase)  
Date: 2026-03-08

## 1. Executive Summary
This document defines the safe, staged migration required to remove remaining vehicle-era naming (`VehicleId`, `vehicleId`, `/Vehicle/*`, `/api/vehicle/*`, `vehicle.*`, `LUBELOGGER_*`) without breaking existing data, links, integrations, or security boundaries.

Core approach:
- Additive-first migration with dual-read/dual-write and compatibility aliases.
- Version external contracts (API/webhooks/import artifacts) before removals.
- Separate high-risk data/auth changes from lower-risk UI/route/documentation changes.
- Keep rollback possible at each release boundary.

## 2. Current Preserved Legacy Surfaces
Compatibility-critical surfaces currently preserved by design:
- Persistence identifiers: `VehicleId`/`vehicleId` across models and storage.
- Public API routes: `/api/vehicle/*` (many endpoints in `Controllers/API/*.cs` and `wwwroot/defaults/api.json`).
- MVC routes/actions: `/Vehicle/*` and `/animals/*` aliasing to `VehicleController` (`Program.cs`).
- Auth/access controls keyed by vehicle ids (`Filter/CollaboratorFilter.cs`, `Filter/StrictCollaboratorFilter.cs`, `Logic/UserLogic.cs`, `Models/User/UserAccess.cs`).
- Browser URL compatibility: `animalId` <-> `vehicleId` mirroring (`wwwroot/js/shared.js:setBrowserHistory`).
- Browser storage keys prefixed by `${vehicleId}_...` (`wwwroot/js/reports.js`, `wwwroot/js/shared.js`, `wwwroot/js/planrecord.js`).
- Import/export and CSV headers (`Controllers/Vehicle/ImportController.cs`, `Models/Shared/ImportModel.cs`, `MapProfile/ImportMappers.cs`, `Helper/StaticHelper.cs`).
- Backup/restore data path and legacy config path handling (`Helper/FileHelper.cs`, `Helper/StaticHelper.cs`).
- Webhook/event naming and payload fields (`Logic/EventLogic.cs`, `Models/Shared/WebHookPayload.cs`).
- Config/env compatibility (`Program.cs`, `Helper/ConfigHelper.cs`, `Models/Settings/ServerConfig.cs`).
- Calendar metadata compatibility (`Helper/StaticHelper.cs` uses `PRODID:lubelogger.com`).
- Auth redirect handling specifically for `/Vehicle/Index` deep links (`Middleware/Authen.cs`).

## 3. Target End State
Canonical future naming (internal and external):
- Entity concept: `PetProfile` (or `Profile` as API-level stable term).
- Identifier key: `PetProfileId` (internal and external).
- API routes: `/api/v2/profiles/*` (or `/api/profiles/*` in a versioned API boundary).
- MVC preferred route: `/animals/*` (legacy `/Vehicle/*` eventually removed after deprecation window).
- Webhook event names: `profile.*` (with typed record events continuing under neutral naming).
- Config/env keys: `PAWLOGGER_*` (legacy `LUBELOGGER_*` accepted only during compatibility window).
- Browser state keys: `${petProfileId}_...` with read compatibility for `${vehicleId}_...`.

## 4. Surface-by-Surface Migration Inventory

### 4.1 Database/Schema
| Legacy | Target | Where | Depends On | Risk | Strategy | Rollback |
|---|---|---|---|---|---|---|
| `vehicleId` SQL column | `petProfileId` | Postgres record tables in `External/Implementations/Postgres/*DataAccess.cs` | All record queries, deletes, imports, API, auth joins | Critical | Add new nullable column first; dual-write; backfill; then switch reads; later enforce non-null and drop legacy | Keep legacy column populated until final cut; rollback by switching reads to `vehicleId` |
| `useraccessrecords(userId, vehicleId)` | `useraccessrecords(userId, petProfileId)` | `PGUserAccessDataAccess` | Authorization/ownership checks | Critical | Add `petProfileId`; dual-write both keys; add dual-lookup indexes; migrate PK in final phase | Retain old PK until full confidence; rollback by reverting to old lookup path |
| Table names such as `vehicles`, `servicerecords` with vehicle semantics | neutral table names optional | Postgres/LiteDB | Backup/restore and tooling may assume names | High | Do not rename table/collection names in same phase as key rename; defer table renames to post-contract hardening | Keep old table names for at least one major cycle |
| LiteDB BSON key `VehicleId` | `PetProfileId` | `External/Implementations/Litedb/*` via `nameof(...VehicleId)` | Query/delete/filter logic, restore/import | Critical | Introduce model-level aliasing and custom serialization migration pass; dual-read key name in deserialization | Preserve original BSON keys until migration validation complete |

### 4.2 Persistence Models
| Legacy | Target | Where | Depends On | Risk | Strategy | Rollback |
|---|---|---|---|---|---|---|
| `GenericRecord.VehicleId` | `GenericRecord.PetProfileId` (canonical), keep alias while migrating | `Models/Shared/GenericRecord.cs` and inheritors | All record serialization and business logic | Critical | Add canonical property; dual serialize/deserialize; map both ways; update internal consumers gradually | Keep `VehicleId` as source of truth until read/write migration complete |
| `*RecordInput.VehicleId` | `PetProfileId` | multiple `Models/*Input.cs` | API and MVC POST payloads | High | Accept both fields; precedence rules; emit canonical in v2 only | Keep accepting legacy field for deprecation window |
| `Vehicle` model naming | `PetProfile` model | `Models/Vehicle/Vehicle.cs` and consumers | Broad internal compile/runtime surface | High | Introduce parallel type alias/adapter before full rename; avoid hard rename during schema migration | Keep type alias in place until full controller/service switchover |

### 4.3 API Routes
| Legacy | Target | Where | Depends On | Risk | Strategy | Rollback |
|---|---|---|---|---|---|---|
| `/api/vehicle/*` | `/api/v2/profiles/*` | `Controllers/API/*.cs`, `wwwroot/defaults/api.json` | External API clients, scripts, automations | Critical | Route shadowing: keep `/api/vehicle/*` as compatibility routes; launch versioned v2 routes first | Disable v2 routing and keep v1 unchanged |
| `/api/vehicle/info?vehicleId=` | `/api/v2/profiles/info?petProfileId=` | `APIController` | Integrations and docs | High | v2 route + query alias support + explicit deprecation headers on v1 | Remove deprecation headers; continue v1 only |

### 4.4 API Request/Response Payloads
| Legacy | Target | Where | Depends On | Risk | Strategy | Rollback |
|---|---|---|---|---|---|---|
| `VehicleId`, `DueOdometer`, `FuelConsumed` | `PetProfileId`, `DueDistance`/domain-neutral naming | `Models/Shared/ImportModel.cs`, API controllers | API clients, CSV tools, webhook consumers | Critical | Payload versioning: v1 unchanged, v2 canonical fields; v2 may include legacy aliases during transition | Keep v1 default; stop emitting v2 fields if regression detected |
| Validation text "vehicle id" | profile-neutral text | API controller responses | Client error parsing (if brittle) | Medium | Keep status codes stable; text changes only in v2 | Revert to legacy messages |

### 4.5 Auth/Ownership/Security
| Legacy | Target | Where | Depends On | Risk | Strategy | Rollback |
|---|---|---|---|---|---|---|
| `UserCanEditVehicle`, filter args `vehicleId` | `UserCanEditPetProfile`, args `petProfileId` | `Logic/UserLogic.cs`, `Filter/*.cs` | Every authorize gate | Critical | Security-first dual lookup: resolve both ids to same canonical internal id and enforce single authorization decision path | Keep legacy-only authorization path available until parity tests pass |
| `QueryParamFilter` requiring `vehicleId` | accept `petProfileId` + legacy fallback | `Filter/QueryParamFilter.cs` | API add/update calls | High | Add param alias map; reject ambiguous mismatched values | Roll back to legacy-only param parsing |
| SignalR group `vehicleId_{id}` | `petProfileId_{id}` | `Logic/EventLogic.cs`, JS subscribers | Realtime updates and multi-tab sync | High | Publish to both groups during transition; subscribe both in clients | Keep old group only |

### 4.6 Browser Storage/Session/Query State
| Legacy | Target | Where | Depends On | Risk | Strategy | Rollback |
|---|---|---|---|---|---|---|
| `${vehicleId}_selectedReportColumns`, `${vehicleId}_csvExportParameters_*`, `${vehicleId}_selectedPlanTab` | `${petProfileId}_...` | `wwwroot/js/reports.js`, `shared.js`, `planrecord.js` | User preferences and UX state | High | Read-both/write-new strategy after parity release; optionally write-both for one release | Continue reading legacy keys and writing legacy |
| URL query `vehicleId` | `animalId` or `petProfileId` | `wwwroot/js/shared.js`, `kiosk.js`, server handlers | Bookmarks/backlinks | High | Keep mirrored params during transition, then prefer canonical param in generated links | Continue mirroring both |
| `globalSearchSettings` structure may contain vehicle-specific semantics | profile-neutral schema | `wwwroot/js/vehicle.js` | Existing localStorage user setting | Medium | Versioned storage object (`schemaVersion`), migration-on-read | Fallback to previous schema version parser |

### 4.7 Import/Export/Backup/Restore
| Legacy | Target | Where | Depends On | Risk | Strategy | Rollback |
|---|---|---|---|---|---|---|
| `ImportToVehicleIdFromCsv` endpoint | canonical import endpoint (v2) | `Controllers/Vehicle/ImportController.cs` | Existing automation and UI callers | Critical | Keep old endpoint; add new endpoint using same implementation adapters | Keep old endpoint as primary |
| CSV/API export headers (`VehicleId`, `FuelConsumed`, etc.) | canonical headers in versioned exports | `Helper/StaticHelper.cs`, `Models/Shared/ImportModel.cs` | External CSV pipelines | Critical | Format versioning (`schemaVersion`) + header alias parser; export option for legacy format | Default exporter back to legacy format |
| Backup db filename `data/cartracker.db` and legacy config paths | canonical paths with legacy fallback | `Helper/StaticHelper.cs`, `Helper/FileHelper.cs` | Backup/restore and deployment scripts | Critical | Backups include manifest + format version; restore accepts both old/new paths and validates checksum | Keep old path resolution and old archive writer |

### 4.8 Webhooks/Events
| Legacy | Target | Where | Depends On | Risk | Strategy | Rollback |
|---|---|---|---|---|---|---|
| `vehicle.add`, `vehicle.update`, `vehicle.delete` | `profile.add`, `profile.update`, `profile.delete` | `VehicleController`, `APIController`, `WebHookPayload` | Third-party webhook processors | Critical | Dual event emission window (or v2 webhook endpoint); document deprecation timeline | Disable new event names; continue legacy only |
| Webhook top-level `VehicleId` and data `vehicleId` | `PetProfileId`/`petProfileId` | `Models/Shared/WebHookPayload.cs` | Downstream parsers | Critical | Version payload contract; include both fields during transition in v2 | Revert to legacy-only field set |

### 4.9 Background Jobs / Scheduled Tasks
| Legacy | Target | Where | Depends On | Risk | Strategy | Rollback |
|---|---|---|---|---|---|---|
| No server `BackgroundService`/scheduler; recurring updates are request-triggered on vehicle endpoints | keep behavior but canonical naming | Controllers and JS-triggered flows (`CheckRecurringTaxRecords`, reminder pushback, etc.) | Existing timing assumptions | Medium | Do not introduce scheduler in same release as naming migration; keep trigger semantics unchanged | Keep current endpoint-triggered behavior |

### 4.10 Config/Env/Settings
| Legacy | Target | Where | Depends On | Risk | Strategy | Rollback |
|---|---|---|---|---|---|---|
| `LUBELOGGER_*` env/config keys | `PAWLOGGER_*` keys | `Program.cs`, `Helper/ConfigHelper.cs`, `Models/Settings/ServerConfig.cs` | Deployments, Helm/Compose env files, docs | Critical | Config aliasing: read new keys first, fallback to legacy; emit warnings when legacy used | Disable new-key priority and continue legacy lookup |
| Serialized server config JsonProperty names using `LUBELOGGER_*` | canonical property names in new schema | `ServerConfig.cs` | Existing config files and backup bundles | High | Versioned config schema with importer for legacy names | Keep legacy serializer/deserializer as default |

### 4.11 Docs/External Integrations
| Legacy | Target | Where | Depends On | Risk | Strategy | Rollback |
|---|---|---|---|---|---|---|
| `wwwroot/defaults/api.json` only `/api/vehicle/*` | add v2 docs, keep v1 docs | API docs UI and external docs | API consumers onboarding | High | Publish parallel docs and migration guide with exact date-based deprecation milestones | Keep v1 docs as primary |
| `PRODID:lubelogger.com` | `PRODID:pawlogger.com` (or neutral) | `Helper/StaticHelper.cs` calendar export | Calendar clients that fingerprint source | High | Add compatibility option or dual-feed endpoint before changing default PRODID | Keep old PRODID default |

### 4.12 Route/Link Compatibility
| Legacy | Target | Where | Depends On | Risk | Strategy | Rollback |
|---|---|---|---|---|---|---|
| `/Vehicle/*` | `/animals/*` (then canonical `/profiles/*` if introduced) | MVC routes, JS links, auth challenge | Bookmarks, backlinks, login redirect flows | Critical | Keep `/Vehicle/*` as compatibility routes through full deprecation window; add telemetry and redirect strategy | Keep serving `/Vehicle/*` directly |
| Auth challenge special-case for `/Vehicle/Index` | canonical deep-link handling for both routes | `Middleware/Authen.cs` | Login redirects with preserved query string | High | Expand logic to all canonical profile routes before removing `/Vehicle/Index` special case | Restore explicit `/Vehicle/Index` handling |

## 5. Proposed Staged Migration Phases

### Phase A: Additive Foundations (no behavior break)
- Introduce canonical naming adapters in models/services without changing persisted schema yet.
- Introduce API v2 route skeletons and payload DTOs.
- Add config key aliasing (`PAWLOGGER_*` read first, `LUBELOGGER_*` fallback).
- Add observability: metric tags for legacy route/key usage.

Release boundary: `vNext-minor.1` (safe additive release).

### Phase B: Read Migration
- Switch server/business logic reads to canonical fields with fallback reads from legacy fields.
- Client reads storage from canonical key first then legacy key.
- API v2 reads canonical query/payload params, accepts legacy aliases for compatibility.

Release boundary: `vNext-minor.2`.

### Phase C: Write Migration
- Start dual-write for DB (`vehicleId` + `petProfileId`) and serialized payloads where needed.
- Client writes canonical storage keys; optionally keep one-release write-both for safety.
- Webhook dual-emission (`vehicle.*` + `profile.*`) or dual payload fields.

Release boundary: `vNext-minor.3`.

### Phase D: Backfill/Data Conversion
- Execute idempotent backfill jobs for Postgres and LiteDB (schema/key copies, integrity checks).
- Validate row/document parity and authorization join parity.
- Update backup manifest version and restore converters.

Release boundary: controlled migration release with maintenance window if needed.

### Phase E: Deprecation Window
- Keep legacy routes/fields/events active but deprecated.
- Emit warnings in response headers/logs when legacy surfaces are used.
- Publish explicit deprecation dates and final removal criteria.

Release boundary: at least one full minor cycle (recommended 60-90 days).

### Phase F: Legacy Removal
- Remove `/api/vehicle/*` and `/Vehicle/*` only after usage is near-zero and customer sign-off.
- Remove legacy fields from active writes, then from schema in final migration.
- Remove `LUBELOGGER_*` fallback only after deployment inventory confirms replacement.

Release boundary: major version bump.

## 6. Test and Validation Plan
Pre-migration gates:
- Full contract inventory snapshot (routes, payload fields, webhook samples, CSV headers).
- Golden backups from both LiteDB and Postgres installations.
- Integration fixtures for legacy clients and links.

During migration gates:
- DB parity tests: `vehicleId` and `petProfileId` equality across all migrated tables.
- Auth parity tests: old and new id paths return identical authorize/deny decisions.
- API compatibility tests: v1 unchanged, v2 canonical, legacy aliases accepted per phase.
- Browser migration tests: existing session/local storage remains intact after upgrade.
- Import/export tests: legacy CSV imports, legacy exports, new schema exports, round-trip restore.
- Webhook tests: old and new payload/event names both accepted by integration harness.
- Route tests: `/Vehicle/*` and `/animals/*` and query aliases continue to open identical profile.

Post-migration gates:
- No unauthorized-access regressions from renamed ids.
- Error-rate and webhook-delivery regressions below baseline threshold.
- Restore tests from backups created before migration and after migration.

## 7. Rollback Strategy
Rollback must be phase-scoped:
- Phase A/B rollback: disable new readers/routes via feature flags, keep legacy paths primary.
- Phase C rollback: stop canonical writes, continue legacy writes, preserve dual-write columns.
- Phase D rollback: idempotent backfill markers allow re-run; do not drop legacy columns/keys yet.
- Phase E rollback: cancel deprecation enforcement, keep legacy headers/routes active.
- Phase F rollback (highest risk): requires retained snapshot backups and reversible schema scripts; do not execute irreversible drops without validated restore drill.

Operational controls:
- Feature flags for API v2, dual-write, dual-emission webhooks, canonical storage writes.
- Snapshot before each migration step (DB backup + config backup + schema hash).
- Roll forward preferred over roll back after partial data conversion, unless security/data-corruption event occurs.

## 8. Risks and Blockers
Top risks:
- Security regression during ownership-key migration (`useraccessrecords` and filters).
- External API/webhook breakage if route/payload removals happen before deprecation window.
- Browser-state loss if key migration is write-only without legacy reads.
- Backup/restore incompatibility across legacy/new path and key names.
- LiteDB BSON-key migration complexity (key rename in document stores is higher-risk than SQL dual columns).

Blockers to resolve before execution phase:
- Confirm canonical naming decision (`PetProfile` vs `Profile`) and keep it fixed across DB/API/UI.
- Decide API versioning path (`/api/v2/...` recommended).
- Define telemetry success thresholds for legacy usage near-zero before removal.
- Prepare migration scripts and dry-run environment for both storage backends.

## 9. Recommended Order of Implementation
Safest-first sequence:
1. Additive API/config/model aliases and telemetry only (no destructive change).
2. Read-path migration with fallback (server + browser + import parsers).
3. Dual-write and dual-emission (DB, webhooks, storage keys).
4. Backfill + parity validation + restore validation.
5. Deprecation communication and enforcement window.
6. Final removals in major release.

Highest-risk steps:
- Auth ownership key migration (`vehicleId` -> `petProfileId` joins/filters).
- Physical schema/key removals (dropping legacy columns/keys/routes/events).

Prerequisites:
- Feature flag framework for migration toggles.
- Contract test suite for v1 + v2.
- Backup/restore dry-run automation.

Rollback gates:
- No step proceeds unless previous step has green parity checks and rollback artifacts verified.

Suggested release boundaries:
- Minor releases for Phases A-E.
- Major release for Phase F (legacy removals).

## 10. What Should NOT Be Migrated First
Do not migrate these first:
- Do not rename/remove `VehicleId` in persistence before dual-read/dual-write and auth parity tests exist.
- Do not remove `/api/vehicle/*` before shipping stable versioned replacement and deprecation window.
- Do not remove `/Vehicle/*` links before bookmark/backlink telemetry confirms safe redirect-only behavior.
- Do not switch webhook payload/event names without dual emission and downstream validation.
- Do not change backup format/path semantics before restore can import both old and new artifacts.
- Do not remove `LUBELOGGER_*` config support before deployment inventory confirms migration to new keys.

## 11. Phase 7 Implementation Snapshot (2026-03-08)
Implemented in this additive phase:
- Added config/env alias readers so `PAWLOGGER_*` keys are accepted with `LUBELOGGER_*` fallback in startup/config helpers (`Program.cs`, `Helper/ConfigHelper.cs`), while preserving legacy write schema.
- Added safe API v2 scaffolding endpoints that shadow existing behavior without replacing v1:
  - `GET /api/v2/profiles` -> existing vehicles handler.
  - `GET /api/v2/profiles/info` -> existing vehicle info handler.
  - `GET /api/v2/profiles/adjustedodometer` -> existing adjusted odometer handler.
- Added narrow read-fallback support for identifier aliases:
  - Existing `GET /api/vehicle/info` and `GET /api/vehicle/adjustedodometer` now accept `petProfileId` as an alias for `vehicleId`.
  - Existing JSON/query param parsing via `QueryParamFilter` now reads `petProfileId` when `vehicleId` is expected.
- Added migration telemetry logs for:
  - v2 route usage.
  - `petProfileId` alias fallback usage.
  - canonical-vs-legacy config key resolution.

Explicitly deferred to later phases:
- Any DB/LiteDB key/schema rename (`vehicleId`/`VehicleId` authority unchanged).
- Any write-path migration or dual-write behavior for persistence/browser storage/webhooks.
- Any auth ownership model migration beyond current `vehicleId` checks.
- Any route removals (`/Vehicle/*`, `/api/vehicle/*`) or webhook rename/removal (`vehicle.*`).
- Import/export/backup schema format migration and backfill/conversion jobs.

Risks discovered in this phase:
- Alias mismatch inputs (`vehicleId` and `petProfileId` both supplied with different values) can occur; handlers now reject mismatches on touched endpoints to avoid ambiguous authorization targets.
- Alias telemetry is log-based and not yet metric-counter based; quantitative thresholds are still deferred.

## 12. Phase 8 Implementation Snapshot (2026-03-08)
Implemented in this additive phase:
- Added high-value read-route v2 shadow coverage that delegates to existing behavior:
  - `GET /api/v2/profiles/servicerecords/all`, `GET /api/v2/profiles/servicerecords`
  - `GET /api/v2/profiles/gasrecords/all`, `GET /api/v2/profiles/gasrecords`
  - `GET /api/v2/profiles/reminders/all`, `GET /api/v2/profiles/reminders`
  - `GET /api/v2/profiles/odometerrecords/all`, `GET /api/v2/profiles/odometerrecords`, `GET /api/v2/profiles/odometerrecords/latest`
- Expanded safe `petProfileId` alias reads on touched legacy read endpoints:
  - `GET /api/vehicle/servicerecords`
  - `GET /api/vehicle/gasrecords`
  - `GET /api/vehicle/reminders`
  - `GET /api/vehicle/odometerrecords`
  - `GET /api/vehicle/odometerrecords/latest`
- Added explicit conflict guards for id ambiguity (`vehicleId` + `petProfileId` mismatch):
  - controller-level mismatch failures on touched endpoints
  - `CollaboratorFilter` mismatch failure for guarded routes using both ids
  - `QueryParamFilter` mismatch failure for query/body alias parsing paths
- Added lightweight deprecation/migration observability on touched API reads:
  - response headers identifying legacy vs v2 contract (`X-PawLogger-Api-Contract`)
  - response header when legacy route is used (`X-PawLogger-Legacy-Route`)
  - response headers/logs for id alias/legacy-id usage (`X-PawLogger-Alias-Id`, `X-PawLogger-Legacy-Id`)
  - structured logs for legacy route usage, v2 route usage, and alias fallback usage
- Added contract verification tests:
  - route-contract tests validating legacy + v2 shadow route templates exist for touched endpoints
  - alias contract tests validating `petProfileId` parameter support on touched legacy read routes
  - id-resolution unit tests validating conflict and fallback behavior

Explicitly deferred to later phases:
- Any destructive schema/key mutation, backfill, or persistence-field authority changes.
- Any auth ownership model redesign beyond current vehicle-id authorization semantics.
- Any legacy route removals (`/api/vehicle/*`, `/Vehicle/*`), webhook renames, or browser-storage key migrations.
- Any broad write-path v2 shadowing/dual-write behavior.

## 13. Phase 9 Implementation Snapshot (2026-03-08)
Implemented in this additive phase:
- Added authenticated HTTP integration tests (in-memory host) for touched Phase 8 read endpoints, validating:
  - legacy + v2 shadow route success
  - `vehicleId` and `petProfileId` alias behavior
  - conflict guard failures for mismatched ids
  - auth gate behavior (unauthenticated challenge and collaborator denial)
  - observability headers on legacy/v2/alias/legacy-id paths
- Extended the same read-only v2 shadowing + alias + conflict + observability pattern to next high-value families:
  - `GET /api/v2/profiles/planrecords/all`, `GET /api/v2/profiles/planrecords`
  - `GET /api/v2/profiles/taxrecords/all`, `GET /api/v2/profiles/taxrecords`
  - `GET /api/v2/profiles/repairrecords/all`, `GET /api/v2/profiles/repairrecords`
- Expanded legacy read alias handling on newly touched endpoints:
  - `GET /api/vehicle/planrecords`
  - `GET /api/vehicle/taxrecords`
  - `GET /api/vehicle/repairrecords`
- Extended route contract tests for new legacy/v2 read templates and alias parameter presence.

Remaining high-value read families not yet shadowed in v2:
- `notes`
- `upgraderecords`
- `inspectionrecords`
- `equipmentrecords`
- `supplyrecords`
- pet-health families (`healthrecords`, `vetvisitrecords`, `vaccinationrecords`, `medicationrecords`, `licensingrecords`, `petexpenserecords`)

Still blocking write-path migration:
- write endpoint parity and compatibility contract tests
- explicit write-path versioning/deprecation policy
- dual-write/read-switch rollout plan and rollback controls
- webhook/event payload versioning for write-side contract evolution

## 14. Phase 10 Implementation Snapshot (2026-03-08)
Implemented in this additive phase:
- Extended read-only v2 shadow-route coverage for the next highest-value API read families present in this repo:
  - `GET /api/v2/profiles/notes/all`, `GET /api/v2/profiles/notes`
  - `GET /api/v2/profiles/upgraderecords/all`, `GET /api/v2/profiles/upgraderecords`
  - `GET /api/v2/profiles/equipmentrecords/all`, `GET /api/v2/profiles/equipmentrecords`
  - `GET /api/v2/profiles/supplyrecords/all`, `GET /api/v2/profiles/supplyrecords`
- Added safe `petProfileId` alias reads and explicit mismatch conflict guards on newly touched legacy read endpoints:
  - `GET /api/vehicle/notes`
  - `GET /api/vehicle/upgraderecords`
  - `GET /api/vehicle/equipmentrecords`
  - `GET /api/vehicle/supplyrecords`
- Applied established observability behavior to touched Phase 10 endpoints:
  - contract headers (`X-PawLogger-Api-Contract`, `X-PawLogger-Legacy-Route`)
  - alias/legacy-id headers (`X-PawLogger-Alias-Id`, `X-PawLogger-Legacy-Id`)
  - existing structured contract/alias usage logs through shared helpers
- Expanded authenticated HTTP integration coverage for Phase 10 families:
  - legacy route success
  - v2 route success
  - `vehicleId` success
  - `petProfileId` success
  - conflict mismatch failure
  - auth semantics continuity
  - observability header presence
- Extended route contract tests for Phase 10 legacy/v2 templates and alias parameter acceptance.

Remaining uncovered high-value read families:
- `inspectionrecords` (no existing `/api/vehicle/inspectionrecords*` read family in current API surface to shadow additively without introducing a new contract family)
- pet-health families (`healthrecords`, `vetvisitrecords`, `vaccinationrecords`, `medicationrecords`, `licensingrecords`, `petexpenserecords`)

Remaining blockers before any write migration:
- write-route v1/v2 compatibility matrix and authenticated contract tests across all migrated families
- explicit write-side id alias precedence/deprecation policy with mismatch behavior guarantees
- staged dual-write/read-switch design with rollback controls and observability thresholds
- webhook and import/export write-side versioning plan that preserves legacy payload/schema compatibility

Recommended write-path design entry criteria:
- complete read parity across selected high-value families with stable telemetry indicating low legacy-id fallback and no alias-mismatch regression
- approved write contract for v2 (request/response/body semantics, auth behavior, and error-shape compatibility)
- non-destructive rollout controls defined (feature flags, rollback toggles, and phase gates)
- pre-validated compatibility plan for webhooks, backups, and browser state keys before any write-side canonicalization

## 15. Phase 11 Implementation Snapshot (2026-03-08)
Implemented in this additive phase:
- Extended read-only v2 shadow-route coverage to high-value pet-health read families present in this repo:
  - `GET /api/v2/profiles/healthrecords/all`, `GET /api/v2/profiles/healthrecords`
  - `GET /api/v2/profiles/vetvisitrecords/all`, `GET /api/v2/profiles/vetvisitrecords`
  - `GET /api/v2/profiles/vaccinationrecords/all`, `GET /api/v2/profiles/vaccinationrecords`
  - `GET /api/v2/profiles/medicationrecords/all`, `GET /api/v2/profiles/medicationrecords`
  - `GET /api/v2/profiles/licensingrecords/all`, `GET /api/v2/profiles/licensingrecords`
  - `GET /api/v2/profiles/petexpenserecords/all`, `GET /api/v2/profiles/petexpenserecords`
- Added safe `petProfileId` alias reads and explicit mismatch conflict guards on newly touched legacy read endpoints:
  - `GET /api/vehicle/healthrecords`
  - `GET /api/vehicle/vetvisitrecords`
  - `GET /api/vehicle/vaccinationrecords`
  - `GET /api/vehicle/medicationrecords`
  - `GET /api/vehicle/licensingrecords`
  - `GET /api/vehicle/petexpenserecords`
- Applied the existing observability behavior on all touched Phase 11 endpoints:
  - contract headers (`X-PawLogger-Api-Contract`, `X-PawLogger-Legacy-Route`)
  - alias/legacy-id headers (`X-PawLogger-Alias-Id`, `X-PawLogger-Legacy-Id`)
  - existing structured contract/alias logs through shared helpers
- Expanded authenticated HTTP integration coverage for Phase 11 families:
  - legacy route success
  - v2 route success
  - `vehicleId` success
  - `petProfileId` success
  - conflict mismatch failure
  - auth semantics continuity
  - observability header presence
- Extended route contract tests for Phase 11 legacy/v2 templates and alias parameter acceptance.

Remaining uncovered high-value read families:
- `inspectionrecords` (still no existing `/api/vehicle/inspectionrecords*` API read family in the current API surface to shadow without introducing a new non-shadow contract family)

Read-side migration completeness assessment:
- Read-side additive shadow coverage is now sufficiently complete across the major existing profile/pet record API families to begin Phase 12 write-path design, provided write-path scope remains non-destructive and preserves v1 contract compatibility.
