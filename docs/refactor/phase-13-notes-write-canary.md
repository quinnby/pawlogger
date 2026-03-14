# Phase 13 - Notes Write Canary (Feature-Gated)

## Scope implemented

- Added additive API v2 write routes for `notes` only:
  - `POST /api/v2/profiles/notes/add`
  - `PUT /api/v2/profiles/notes/update`
  - `DELETE /api/v2/profiles/notes/delete`
- Preserved legacy API note write routes and behavior:
  - `POST /api/vehicle/notes/add`
  - `PUT /api/vehicle/notes/update`
  - `DELETE /api/vehicle/notes/delete`
- Reused existing note write persistence/event logic (`VehicleId` remains authoritative).
- Added route/query/body identifier conflict guards for v2 notes writes.
- Added notes-write contract/integration tests and feature-flag-off coverage.

## Feature flags added

- `LUBELOGGER_WRITE_V2_ROUTES` / `PAWLOGGER_WRITE_V2_ROUTES`
  - Global v2 write hard gate.
- `LUBELOGGER_WRITE_V2_FAMILY_NOTES` / `PAWLOGGER_WRITE_V2_FAMILY_NOTES`
  - Per-family notes v2 write gate.
- `LUBELOGGER_WRITE_V2_ALIAS_PARSING` / `PAWLOGGER_WRITE_V2_ALIAS_PARSING`
  - Allows `vehicleId` legacy id alias on v2 notes write routes.
- `LUBELOGGER_WRITE_V2_STRICT_ID_CONFLICT_REJECT` / `PAWLOGGER_WRITE_V2_STRICT_ID_CONFLICT_REJECT`
  - Controls strict conflict checks (default `true`).

Default behavior remains safe/off for v2 writes because global and family gates default to disabled.

## Telemetry / observability added

- Write contract usage headers/logging on note write responses:
  - `X-PawLogger-Api-Contract=legacy-vehicle-v1` for legacy.
  - `X-PawLogger-Api-Contract=v2-profiles-shadow` for v2.
- Existing alias headers/logging reused:
  - `X-PawLogger-Alias-Id=petProfileId` when alias resolves.
  - `X-PawLogger-Legacy-Id=vehicleId` when legacy id is used on v2 route.
- Added lightweight Phase 13 logs for:
  - legacy/v2 note write route usage (`add|update|delete`)
  - v2 disabled-by-flag rejections
  - v2 alias-disabled rejections
  - id conflict rejections

## Identifier behavior

- Internal authoritative id remains `vehicleId` / `VehicleId`.
- `petProfileId` accepted on v2 notes writes.
- `vehicleId` accepted on v2 notes writes only when alias parsing flag is enabled.
- Conflicting id input (`vehicleId` vs `petProfileId`) returns `400`.
- For update/delete, ownership/auth continues to resolve from existing record `VehicleId`.

## Explicitly unchanged in Phase 13

- No schema/key renames (`VehicleId`/`vehicleId` unchanged).
- No LiteDB/BSON or Postgres column changes.
- No legacy route removals.
- No auth ownership model changes.
- No import/export/backup/restore contract changes.
- No webhook/event name changes.
- No browser storage/state key changes.
- No write rollout beyond `notes`.

## Deferred to later phases

- v2 write migration for all other write families.
- API v2 writes for MVC-only families.
- Any persistence identifier migration/cutover work.
- Any event contract versioning or dual-emission strategy.
- Broader canary cohorts and rollout automation.
