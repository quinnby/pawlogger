# PawLogger Phase 1 Audit: Remaining LubeLogger-Era Remnants

## Scope and method
- Phase: `Phase 1 — Final Remnant Audit Only`
- Runtime behavior changes: none (documentation-only audit)
- Search basis: repository-wide `rg` scan for terms including `lubelogger`, `lube`, `vehicle`, `garage`, `odometer`, `mileage`, `fuel`, `oil`, `tire`, `maintenance`, `service interval`
- Raw first-party hit volume (after excluding vendor libs/minified assets): ~5,882 line hits
- This inventory is deduplicated into **finding groups** for migration planning.

## User-facing text
| file path | symbol/string | brief context | risk | action |
|---|---|---|---|---|
| `Views/Shared/_Layout.cshtml` | `lubelogger_icon_*`, `lubelogger_launch.png`, `lubelogger-body-container` | App shell still references LubeLogger-named icons/classes visible in HTML output. | medium | Rename safely now |
| `Views/Login/*.cshtml` and `Views/Admin/Index.cshtml` and `Views/API/Index.cshtml` | CSS class `lubelogger-logo`, `lubelogger-navbar*` | Visible UI class names leak legacy brand in markup and styling hooks. | low | Rename safely now |
| `Views/Home/_Settings.cshtml` | "Legacy Vehicle Tabs (Compatibility)", "Fuel (Legacy)", "Odometer (Legacy)" | Settings UI intentionally exposes vehicle-era compatibility toggles. | medium | Preserve for compatibility |
| `Views/Vehicle/Report/_CareHistory.cshtml` | "Last Reported Odometer Reading", "Average Fuel Economy", "Total Spent on Fuel" | Report templates still render vehicle-specific language when legacy data present. | medium | Alias and deprecate |
| `Views/Kiosk/_Kiosk.cshtml` and `Views/Kiosk/_KioskVehicleInfo.cshtml` | `Fuel`, `Odometer`, `VehicleData` display labels | Kiosk still surfaces multiple automotive metrics/labels. | medium | Alias and deprecate |
| `Views/Shared/401.cshtml` | "Return to Garage" | Unauthorized screen still uses garage terminology. | low | Rename safely now |
| `docs/index.html` | `<title>LubeLogger</title>`, vehicle/fuel marketing copy | Public docs landing page is still effectively upstream LubeLogger branding/content. | low | Remove now |
| `SECURITY.md` | "LubeLogger ..." statements | Security doc is still fully LubeLogger-branded text. | low | Rename safely now |

## Internal code identifiers
| file path | symbol/string | brief context | risk | action |
|---|---|---|---|---|
| `Models/Vehicle/Vehicle.cs` | class `Vehicle`, `VehicleIdentifier`, `Odometer*`, `FuelType` semantics | Core domain object remains vehicle-first with pet fields layered on top. | high | Defer to migration v2 |
| `Logic/VehicleLogic.cs` | interface/class `IVehicleLogic`/`VehicleLogic` | Central business logic namespace/type names remain vehicle-centric. | medium | Alias and deprecate |
| `Logic/OdometerLogic.cs` | `IOdometerLogic`, `IMileageLogic`, `AutoInsertOdometerRecord` | Core logic still built around odometer lifecycle assumptions. | medium | Alias and deprecate |
| `Controllers/VehicleController.cs` | controller/type name and many `*Vehicle*` actions | Main app controller still named for vehicle domain. | medium | Alias and deprecate |
| `External/Interfaces/IVehicleDataAccess.cs` | `IVehicleDataAccess`, `SaveVehicle`, `GetVehicleById` | Interface contracts preserve legacy naming used widely in DI graph. | high | Preserve for compatibility |
| `Models/Report/*Vehicle*.cs` | `CostForVehicleByMonth`, `CostTableForVehicle`, etc. | Reporting DTO names remain vehicle-centric despite pet-facing output. | low | Rename safely now |
| `wwwroot/js/vehicle.js` | global functions `saveVehicle`, `deleteVehicle`, `GetVehicleId` usage | Frontend module names and helpers still built on vehicle naming. | medium | Alias and deprecate |
| `Program.cs` | `IVehicleDataAccess`, `IVehicleLogic` service bindings | Composition root exposes legacy naming at system boundary. | medium | Alias and deprecate |

## API/contracts
| file path | symbol/string | brief context | risk | action |
|---|---|---|---|---|
| `Controllers/API/*.cs` | routes under `/api/vehicle/...` | Public API path prefix is vehicle-based; likely client-integrated. | high | Preserve for compatibility |
| `Models/Shared/WebHookPayload.cs` | payload key `vehicleId` | Webhook and event payload shape includes vehicle identifier key. | high | Preserve for compatibility |
| `Models/Shared/ImportModel.cs` | many export DTO fields `VehicleId`, `DueOdometer`, `FuelConsumed` | External contract for API/exports uses legacy field names. | high | Preserve for compatibility |
| `Controllers/API/OdometerController.cs` | messages like "Must provide a valid vehicle id" | API response text and validation language still vehicle-centric. | low | Rename safely now |
| `wwwroot/defaults/api.json` | API docs content references vehicle endpoints/types | Generated/served API docs still describe vehicle-era concepts. | medium | Alias and deprecate |
| `Logic/EventLogic.cs` + `Models/Shared/WebHookPayload.cs` | action types `vehicle.add`, `vehicle.update`, `vehicle.delete` | Event naming contract consumed by integrations/webhooks. | high | Preserve for compatibility |

## Persistence/database/schema
| file path | symbol/string | brief context | risk | action |
|---|---|---|---|---|
| `Models/Shared/GenericRecord.cs` | property `VehicleId` | Base record key inherited by many record types and persisted as-is. | high | Preserve for compatibility |
| `External/Implementations/Postgres/*DataAccess.cs` | SQL columns `vehicleId`, table `vehicles` | PostgreSQL schema/queries are built around `vehicleId` and vehicle tables. | high | Preserve for compatibility |
| `External/Implementations/Litedb/*DataAccess.cs` | collection names like `vehicles`, queries on `VehicleId` | LiteDB collections and lookup keys depend on legacy names. | high | Preserve for compatibility |
| `Models/User/UserAccess.cs` | `UserVehicle` compound key (`UserId`, `VehicleId`) | Access-control linkage persistence key depends on `VehicleId`. | high | Preserve for compatibility |
| `Models/Settings/ServerConfig.cs` | `[JsonPropertyName("LUBELOGGER_...")]` | Persisted server config JSON/env key mapping uses LUBELOGGER names. | high | Preserve for compatibility |
| `Helper/StaticHelper.cs` | `DbName = data/cartracker.db` | Database filename still reflects car tracker origin. | medium | Alias and deprecate |
| `Models/Vehicle/Vehicle.cs` | `OdometerOptional`, `OdometerMultiplier`, `OdometerDifference` | Persisted vehicle-era state still present on profile model. | high | Defer to migration v2 |

## Auth/security mappings
| file path | symbol/string | brief context | risk | action |
|---|---|---|---|---|
| `Filter/CollaboratorFilter.cs` | action args `vehicleId`/`vehicleIds` (with `animalId` fallback) | Authorization guard logic maps all access checks through vehicle IDs. | high | Preserve for compatibility |
| `Filter/StrictCollaboratorFilter.cs` | strict auth checks on `vehicleId` semantics | Delete/edit enforcement depends on vehicle-key ownership logic. | high | Preserve for compatibility |
| `Logic/UserLogic.cs` | `UserCanEditVehicle`, `AddUserAccessToVehicle`, `FilterUserVehicles` | Core permission and tenancy mapping model remains vehicle-based. | high | Preserve for compatibility |
| `Logic/EventLogic.cs` | SignalR group key `vehicleId_{id}` | Realtime authorization/update partitioning keyed by legacy ID naming. | medium | Alias and deprecate |
| `Filter/QueryParamFilter.cs` | required query key `vehicleId` | Request validation enforces vehicle-named parameter contracts. | high | Preserve for compatibility |

## Browser/local storage
| file path | symbol/string | brief context | risk | action |
|---|---|---|---|---|
| `wwwroot/js/reports.js` | session keys `${vehicleId}_selectedReportColumns`, `${vehicleId}_yearMetric`, etc. | Per-profile report state persisted under vehicle-id-prefixed keys. | high | Preserve for compatibility |
| `wwwroot/js/shared.js` | session key `${vehicleId}_csvExportParameters_${mode}` | CSV export UI state depends on existing storage key format. | high | Preserve for compatibility |
| `wwwroot/js/planrecord.js` | session key `${vehicleId}_selectedPlanTab` | Planner mobile tab memory uses vehicle-based key namespace. | medium | Alias and deprecate |
| `wwwroot/js/vehicle.js` | local key `globalSearchSettings` + `/Vehicle/*` requests | Frontend state/actions still bound to vehicle routes/helpers. | medium | Alias and deprecate |

## Import/export/backup
| file path | symbol/string | brief context | risk | action |
|---|---|---|---|---|
| `MapProfile/ImportMappers.cs` | CSV aliases: `fuelup_date`, `odometer`, `fuelconsumed`, `partial_fuelup`, `missed_fuelup` | Import parser intentionally supports legacy automotive CSV headers. | high | Preserve for compatibility |
| `Controllers/Vehicle/ImportController.cs` | methods `ExportFromVehicleToCsv`, `GenerateCsvSample`, `ImportMode.GasRecord/OdometerRecord` | Import/export routes and flow names still vehicle-centric. | high | Preserve for compatibility |
| `Helper/StaticHelper.cs` | CSV writers with columns `Odometer`, `FuelConsumed`, `VehicleId` | Export schema retains legacy field names used by downstream tools. | high | Preserve for compatibility |
| `Models/Shared/ImportModel.cs` | `VehicleImportModel` with `LicensePlate`, `FuelType`, `OdometerOptional` | Backup/import payload models still include vehicle-era fields. | high | Preserve for compatibility |
| `Views/Home/_Settings.cshtml` | backup/restore entry points adjacent to legacy tab toggles | User backup workflows still include legacy tab/state payload assumptions. | medium | Preserve for compatibility |
| `wwwroot/defaults/demo_default.zip` | bundled demo dataset | Demo backup likely encodes legacy schema identifiers. | high | Defer to migration v2 |

## Tests/fixtures
| file path | symbol/string | brief context | risk | action |
|---|---|---|---|---|
| repository-wide | N/A | No dedicated automated test/fixture project was found in this repo snapshot, so no test-fixture remnant inventory exists yet. | medium | Defer to migration v2 |

## Docs/assets
| file path | symbol/string | brief context | risk | action |
|---|---|---|---|---|
| `README.md` | multiple references to LubeLogger and `/Vehicle` compatibility | Expected upstream attribution + explicit compatibility notes. | low | Preserve for compatibility |
| `docs/index.html` | LubeLogger brand, vehicle/fuel screenshots/copy, `docs.lubelogger.com` links | Public docs site remains entirely upstream-oriented. | low | Remove now |
| `docs/screenshots.md` | sections like "Garage", "Track Gas Records", "Vehicle" | Screenshot docs remain vehicle-era. | low | Remove now |
| `docs/lubelogger_logo.png` | asset filename includes legacy brand | Legacy-branded docs asset remains published. | low | Rename safely now |
| `wwwroot/defaults/lubelogger_*` | app icons/logos/startup image names | Runtime static assets still use LubeLogger filenames. | medium | Rename safely now |
| `wwwroot/defaults/garage.png`, `garage_narrow.png`, `addnew_vehicle.png` | asset filenames/imagery are vehicle-era | PWA screenshots and defaults still reference garage/vehicle visuals. | medium | Rename safely now |
| `wwwroot/manifest.json` | icon refs `lubelogger_*`, screenshots `garage*.png` | Web app manifest still points to legacy-branded assets. | medium | Rename safely now |

## Conclusion
### Total findings by category (deduplicated groups)
- User-facing text: 8
- Internal code identifiers: 8
- API/contracts: 6
- Persistence/database/schema: 7
- Auth/security mappings: 5
- Browser/local storage: 4
- Import/export/backup: 6
- Tests/fixtures: 1
- Docs/assets: 7
- Total finding groups: 52

### Highest-risk remnants
- Persistence keys and schema identifiers centered on `VehicleId` (`GenericRecord`, Postgres `vehicleId`, LiteDB `VehicleId` queries).
- Public API and webhook contract surfaces (`/api/vehicle/*`, JSON keys like `vehicleId`, event actions like `vehicle.add`).
- Auth/authorization mapping keyed to vehicle access (`CollaboratorFilter`, `StrictCollaboratorFilter`, `UserLogic`).
- Browser/session state keys keyed by `vehicleId` prefixes.
- Import/export schema and CSV alias compatibility for odometer/fuel fields.

### Recommended Phase 2 cleanup scope
- Focus only on **low-risk user-facing and docs/assets remnants**:
  - UI labels/messages that can be renamed without changing persisted fields or route contracts.
  - CSS class names and static asset filenames with compatibility aliases.
  - Public docs site (`docs/index.html`, `docs/screenshots.md`) to PawLogger messaging.
- Introduce adapter/alias layers where needed (do not break existing `/Vehicle` and `/api/vehicle/*` consumers yet).

### Must not be touched without a migration plan
- `VehicleId`/`vehicleId` persistence fields, LiteDB collection contracts, Postgres SQL schema columns/tables.
- API response/request contract fields used externally (`VehicleId`, `DueOdometer`, `FuelConsumed`, etc.).
- Auth/tenant permission joins and ownership checks bound to vehicle IDs.
- Browser storage keys currently used by active UI flows unless dual-read/write compatibility is implemented.
- Import/export and backup schema compatibility surfaces without versioned migration tooling.

## Phase 3 boundary update (2026-03-08)
- Added additive service aliases:
  - `IPetProfileLogic` over `IVehicleLogic`
  - `IProfileAccessLogic` over `IUserLogic`
- Added additive helper aliases:
  - `StaticHelper.GetPetProfileIdentifier(...)` wrappers over existing `GetVehicleIdentifier(...)`
- Added non-serialized domain aliases for internal code:
  - `GenericRecord.PetProfileId` (maps to `VehicleId`)
  - `UserVehicle.PetProfileId` (maps to `VehicleId`)
  - `Vehicle.ProfileName` and `VehicleViewModel.ProfileName`
- Updated high-level UI/service usage to consume pet/profile aliases in:
  - `HomeController`, `KioskController`, garage card display, reminder email template substitution.

### Explicitly preserved compatibility boundaries in Phase 3
- Database schema, LiteDB/BSON keys, and persisted `VehicleId` fields remain unchanged.
- API routes and contract shapes (including `/api/vehicle/*`, payload keys, and webhook/event names) remain unchanged.
- Browser storage key namespaces remain unchanged.

## Phase 4 boundary update (2026-03-08)
- Compatibility hardening only (no destructive migration or contract renames).
- Added additive backup-restore path tolerance in `FileHelper.RestoreBackup(...)`:
  - Database file is now resolved from either `data/cartracker.db` or `cartracker.db` inside backup zips.
  - Widgets file is now resolved from either `data/widgets.html` or `widgets.html`.
  - User/server config files are now resolved from either `config/*.json` or `data/config/*.json`.
- Added additive CSV import header aliases in `ImportMapper` to tolerate both existing legacy headers and newer profile-friendly column names for distance/consumption fields.
- Added browser URL-state compatibility sync in `setBrowserHistory(...)`:
  - when `animalId` is set/cleared, `vehicleId` is mirrored;
  - when `vehicleId` is set/cleared, `animalId` is mirrored.
  - This preserves deep-link compatibility for old and new URLs without changing route contracts.

### Intentionally unchanged in Phase 4
- Persisted key names (`VehicleId`, `vehicleId`, database column names, BSON keys) remain unchanged by design.
- API/webhook compatibility identifiers remain unchanged (`/api/vehicle/*`, `vehicle.add`, payload `vehicleId`).
- Browser storage keys remain unchanged (`${vehicleId}_*`, `globalSearchSettings`) to avoid session/preference loss.
- Calendar export `PRODID:lubelogger.com` remains unchanged to avoid downstream iCal integration churn in this phase.

### Deferred migration notes for future phases
- If calendar metadata branding is changed, ship additive compatibility strategy and regression-test subscriber clients against both old and new feeds.
- If browser storage namespaces are renamed, implement explicit dual-read migration and staged write strategy before removing old keys.
- If `VehicleId` storage contracts are ever migrated, require versioned API/contracts, dual-write period, and verified rollback path across LiteDB and PostgreSQL.
