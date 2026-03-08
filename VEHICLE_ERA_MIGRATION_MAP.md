# Vehicle-Era Migration Map
## Comprehensive Audit of Remaining Car/Vehicle Domain Concepts

**Generated:** 2026-03-07  
**Purpose:** Complete inventory of all vehicle/car-era concepts to support controlled, phased migration to pet health domain

---

## Executive Summary

This document catalogs every remaining vehicle-era concept in the PawLogger codebase following earlier pet health migration phases. Items are classified by risk level and migration complexity to enable systematic, safe cleanup.

**Key Findings:**
- **1 critical compatibility identifier** (`VehicleId`) used throughout the system
- **6 legacy record types** (Gas, Odometer, Upgrade, Tax, Equipment, Inspection) still functional
- **150+ controller actions** using vehicle-oriented routes
- **30+ model classes** containing vehicle terminology
- **15+ user settings** with fuel/gas/odometer legacy flags
- **20+ JavaScript files** with vehicle-related functions
- **50+ view files** with vehicle/odometer/fuel terminology

---

## 1. COMPATIBILITY-CRITICAL (DO NOT RENAME YET)

### 1.1 Core Persistence Identifier
**Risk Level:** 🔴 **CRITICAL** - Requires database migration

| Item | Type | Location | Usage Scope |
|------|------|----------|-------------|
| `VehicleId` | Property | ALL record models | Primary foreign key linking all records to pet profiles |
| `Vehicle` class | Model | `Models/Vehicle/Vehicle.cs` | Core entity representing both legacy vehicles and pet profiles |
| `VehicleController` | Controller | `Controllers/VehicleController.cs` | Main controller handling both `/Vehicle` and `/animals` routes |
| `IVehicleDataAccess` | Interface | `External/Interfaces/IVehicleDataAccess.cs` | Data access abstraction |
| `IVehicleLogic` | Interface | `Logic/VehicleLogic.cs` | Business logic layer |

**Notes:**
- `VehicleId` appears in **31+ model classes** as the foreign key
- Used in database tables, JSON serialization, API contracts, webhooks
- Renaming would break data migration, imports/exports, and API compatibility
- Must remain until a formal data migration strategy is implemented

### 1.2 Critical Route Compatibility
**Risk Level:** 🔴 **CRITICAL** - Public-facing compatibility layer

| Route Pattern | Maps To | Purpose |
|---------------|---------|---------|
| `/Vehicle/*` | `VehicleController` | Legacy compatibility routes |
| `/animals/*` | `VehicleController` | New pet-friendly routes (alias) |

**Implementation:** [Program.cs](Program.cs#L230-237)
```csharp
app.MapControllerRoute(
    name: "animals",
    pattern: "animals/{action=Index}/{id?}",
    defaults: new { controller = "Vehicle" });
```

---

## 2. LEGACY RECORD TYPES & WORKFLOWS

### 2.1 Vehicle-Specific Record Types
**Risk Level:** 🟡 **MEDIUM** - Can be deprecated/hidden from UI first

| Record Type | Models | Controllers | Views | Data Access | Still Visible? |
|-------------|--------|-------------|-------|-------------|----------------|
| **GasRecord** | `GasRecord`, `GasRecordInput`, `GasRecordViewModel` | `Vehicle/GasController`, `API/GasController` | `Views/Vehicle/Gas/*` | `IGasRecordDataAccess` | Yes (Legacy tab) |
| **OdometerRecord** | `OdometerRecord`, `OdometerRecordInput` | `Vehicle/OdometerController`, `API/OdometerController` | `Views/Vehicle/Odometer/*` | `IOdometerRecordDataAccess` | Yes (Legacy tab) |
| **UpgradeRecord** | `UpgradeRecord`, `UpgradeReportInput` | `Vehicle/UpgradeController`, `API/UpgradeController` | `Views/Vehicle/Upgrade/*` | `IUpgradeRecordDataAccess` | Yes (Legacy tab) |
| **TaxRecord** | `TaxRecord`, `TaxRecordInput` | `Vehicle/TaxController`, `API/TaxController` | `Views/Vehicle/Tax/*` | `ITaxRecordDataAccess` | Yes |
| **EquipmentRecord** | `EquipmentRecord`, `EquipmentRecordInput` | `Vehicle/EquipmentController`, `API/EquipmentController` | `Views/Vehicle/Equipment/*` | `IEquipmentRecordDataAccess` | Yes |
| **InspectionRecord** | `InspectionRecord`, `InspectionRecordInput` | `Vehicle/InspectionController` | `Views/Vehicle/Inspection/*` | `IInspectionRecordDataAccess` | Yes |

**Status:**
- All record types still functional and accessible
- Users can enable/disable via "Legacy Vehicle Tabs" setting
- Default: Gas and Odometer hidden for new profiles
- Can be deprecated in future phase once data migration strategy exists

### 2.2 VehicleRecords Aggregate Model
**Risk Level:** 🟢 **LOW** - Internal model only

**Location:** `Models/Shared/VehicleRecords.cs`
```csharp
public class VehicleRecords
{
    public List<ServiceRecord> ServiceRecords { get; set; }
    public List<CollisionRecord> CollisionRecords { get; set; }
    public List<UpgradeRecord> UpgradeRecords { get; set; }
    public List<GasRecord> GasRecords { get; set; }
    public List<TaxRecord> TaxRecords { get; set; }
    public List<OdometerRecord> OdometerRecords { get; set; }
}
```

---

## 3. USER-FACING TERMINOLOGY

### 3.1 UI Labels and Tab Names
**Risk Level:** 🟢 **LOW** - User-facing only, can be renamed safely

| Visible Item | Location | Translation Key | Current Status |
|--------------|----------|-----------------|----------------|
| "Fuel" tab | Settings, Vehicle Index | `"Fuel"` | Marked (Legacy) |
| "Odometer" tab | Settings, Vehicle Index | `"Odometer"` | Marked (Legacy) |
| "Upgrades" tab | Garage context menu | `"Upgrades"` | Marked (Legacy) |
| "Gas Records" | Views | `"Gas Records"` | Legacy UI only |
| "Mileage" | Reports, forms | `"Mileage"` | Vehicle-specific |
| "MPG" / "Fuel Economy" | Settings, reports | Various | Legacy calculation |

**Locations:**
- `Views/Home/_Settings.cshtml` - Legacy vehicle settings section
- `Views/Home/_GarageDisplay.cshtml` - Context menu items
- `Views/Vehicle/Index.cshtml` - Tab labels
- Translation keys throughout views

### 3.2 User Settings (Legacy Flags)
**Risk Level:** 🟡 **MEDIUM** - Persisted JSON settings

**Location:** `Models/Settings/UserConfig.cs`

| Setting | Purpose | Marked Legacy? | Used Active? |
|---------|---------|----------------|--------------|
| `UseMPG` | Fuel economy in MPG | Yes | Gas records only |
| `UseUKMPG` | UK imperial gallons | Yes | Gas records only |
| `UseThreeDecimalGasCost` | Gas cost precision | Yes | Gas records only |
| `UseThreeDecimalGasConsumption` | Gas consumption precision | Yes | Gas records only |
| `PreferredGasUnit` | Gas volume unit | Yes | Gas records only |
| `PreferredGasMileageUnit` | MPG/L per 100km | Yes | Gas/reports |
| `UseUnitForFuelCost` | Cost per unit vs total | Yes | Gas records only |
| `EnableAutoOdometerInsert` | Auto-create odometer records | Yes | Service/Gas workflows |
| `EnableAutoFillOdometer` | Pre-fill odometer input | Yes | Forms |
| `HideSoldVehicles` | Hide inactive pets | No | Active (reused) |
| `ShowVehicleThumbnail` | Show pet photo in header | No | Active (reused) |

**Notes:**
- These settings are JSON-serialized to database
- Renaming requires migration of stored user preferences
- Most are already marked as "(Legacy)" in UI

### 3.3 Import/Export Labels
**Risk Level:** 🟢 **LOW** - Translation strings only

**Locations:**
- `Views/Home/_ExtraFields.cshtml` - Import mode dropdown
- CSV import/export labels
- Translation files

---

## 4. INTERNAL NAMING (SAFE TO RENAME)

### 4.1 Helper Classes
**Risk Level:** 🟢 **LOW** - Internal only, no persistence

| Class | Location | Purpose | Rename Safe? |
|-------|----------|---------|--------------|
| `GasHelper` | `Helper/GasHelper.cs` | Fuel economy calculations | ✅ Yes |
| `EquipmentHelper` | `Helper/EquipmentHelper.cs` | Equipment record formatting | ✅ Yes |
| `OdometerLogic` | `Logic/OdometerLogic.cs` | Odometer record logic | ✅ Yes |

### 4.2 JavaScript Functions
**Risk Level:** 🟡 **MEDIUM** - Internal but widely used

**High-frequency vehicle references:**
- `GetVehicleId()` - Used in **50+ places** across JS files
- `getVehicleHealthRecords()` - Health record fetching
- `saveHealthRecordToVehicle()` - Save health record
- `viewVehicleWithTab()` - Navigation
- `selectAllVehicles()` / `clearSelectedVehicles()` - Garage selection

**File locations:**
- `wwwroot/js/healthrecord.js`
- `wwwroot/js/vetvisitrecord.js`
- `wwwroot/js/medicationrecord.js`
- `wwwroot/js/vaccinationrecord.js`
- `wwwroot/js/vehicle.js`
- `wwwroot/js/garage.js`
- `wwwroot/js/shared.js`

### 4.3 View Model Properties
**Risk Level:** 🟢 **LOW** - Internal view models only

Examples:
- `VehicleViewModel` - Can stay as wrapper
- `KioskVehicleViewModel` - Used in kiosk mode
- `VehicleImageMap` - Image location mapping

---

## 5. ODOMETER/MILEAGE CONCEPTS

### 5.1 Core Odometer Infrastructure
**Risk Level:** 🔴 **HIGH** - Deeply integrated

| Component | Usage | Replacement Strategy |
|-----------|-------|---------------------|
| `OdometerRecord` model | Tracks distance over time | Phase out for pets, keep for legacy |
| `Mileage` property | All records with distance | Conditional: hide for pets |
| `LastReportedMileage` | Vehicle summary | Conditional display |
| `ReminderMetric.Odometer` enum | Distance-based reminders | Keep for backward compat |
| `ReminderMileageInterval` enum | Predefined intervals | Rename to distance/interval |

**Affected Areas:**
- Service records, repair records, gas records, upgrade records
- Reminder system (distance-based triggers)
- Reports (cost per mile calculations)
- Dashboard metrics

**Pet-Specific Adaptations Already Implemented:**
- Reports auto-hide odometer column if `!string.IsNullOrWhiteSpace(vehicle.PetName)`
- Forms allow odometer to be optional via `OdometerOptional` flag
- Distance metrics conditional in UI

### 5.2 Odometer-Related Fields
**Risk Level:** 🟡 **MEDIUM** - Used in multiple record types

| Field | Found In | Purpose | Pet Relevance |
|-------|----------|---------|---------------|
| `Odometer` / `Mileage` | ServiceRecord, GasRecord, UpgradeRecord, etc. | Distance at time of record | Not applicable for pets |
| `InitialOdometer` | OdometerRecord | Starting distance | Legacy only |
| `OdometerOptional` | Vehicle | Allow records without distance | Used for pets |
| `OdometerMultiplier` | Vehicle | Unit conversion | Legacy only |
| `OdometerDifference` | Vehicle | Odometer adjustment | Legacy only |
| `HasOdometerAdjustment` | Vehicle | Flag for adjustments | Legacy only |

---

## 6. FUEL/GAS CONCEPTS

### 6.1 Fuel Record Infrastructure
**Risk Level:** 🟡 **MEDIUM** - Complete feature set, can be deprecated

**Components:**
- **Models:** `GasRecord`, `GasRecordInput`, `GasRecordViewModel`, `GasRecordInputContainer`
- **Controllers:** `Vehicle/GasController`, `API/GasController`
- **Views:** `Views/Vehicle/Gas/_Gas.cshtml`, `_GasRecordModal.cshtml`
- **JS:** `wwwroot/js/gasrecord.js`
- **Data Access:** `IGasRecordDataAccess`, implementations in Litedb/Postgres
- **Helper:** `GasHelper` - MPG/fuel economy calculations

**User Settings Affected:**
- `UseMPG`, `UseUKMPG`
- `UseThreeDecimalGasCost`, `UseThreeDecimalGasConsumption`
- `PreferredGasUnit`, `PreferredGasMileageUnit`
- `UseUnitForFuelCost`

**Import/Export:**
- CSV import supports Fuelly format
- MapProfile includes `fuelup_date`, `gallons`, `liters`, `partial_fuelup`, `missed_fuelup` mappings

### 6.2 Fuel Economy Calculations
**Risk Level:** 🟢 **LOW** - Isolated logic

**Location:** `Helper/GasHelper.cs`
- MPG calculations
- UK gallon conversions
- Fuel cost per unit calculations
- Can be deprecated once gas records are phased out

---

## 7. VEHICLE-SPECIFIC REPORT CONCEPTS

### 7.1 Report Calculations (Vehicle-Oriented)
**Risk Level:** 🟡 **MEDIUM** - Logic layer, can be adapted

**File:** `Controllers/Vehicle/ReportController.cs`

**Vehicle-Specific Metrics:**
- `GetVehicleHistory()` - Aggregates all record types including gas, odometer, upgrades
- Total distance calculations
- Cost per mile metrics
- MPG/fuel economy displays
- Depreciation calculations (purchase price vs sold price)

**Already Pet-Aware:**
- Detects pet profiles via `!string.IsNullOrWhiteSpace(vehicle.PetName)`
- Hides odometer column for pets
- Conditionally displays distance-related metrics

### 7.2 Report View Models
**Risk Level:** 🟢 **LOW** - View models only

**Files:**
- `Models/Report/MPGForVehicleByMonth.cs`
- `Models/Report/CostForVehicleByMonth.cs`
- `Models/Report/CostMakeUpForVehicle.cs`
- `Models/Report/ReminderMakeUpForVehicle.cs`

**Notes:**
- Names contain "Vehicle" but represent pet profiles now
- Can be renamed safely (internal DTOs)

---

## 8. ENUMS WITH VEHICLE CONCEPTS

### 8.1 Import/Record Type Enum
**Risk Level:** 🔴 **HIGH** - Persisted in database/settings

**File:** `Enum/ImportMode.cs`
```csharp
public enum ImportMode
{
    ServiceRecord = 0,
    RepairRecord = 1,
    GasRecord = 2,           // Legacy
    TaxRecord = 3,
    UpgradeRecord = 4,       // Legacy
    ReminderRecord = 5,
    NoteRecord = 6,
    SupplyRecord = 7,
    Dashboard = 8,
    PlanRecord = 9,
    OdometerRecord = 10,     // Legacy
    VehicleRecord = 11,
    InspectionRecord = 12,
    EquipmentRecord = 13,
    HealthRecord = 14
}
```

**Usage:**
- User settings: `VisibleTabs`, `DefaultTab`, `TabOrder`
- Import/export mode selection
- UI navigation and routing
- Cannot rename without breaking stored preferences

### 8.2 Reminder System Enums
**Risk Level:** 🟡 **MEDIUM** - Used in reminder logic

| Enum | Values | Vehicle-Specific? |
|------|--------|-------------------|
| `ReminderMetric` | Date, Odometer, Both | "Odometer" is vehicle concept |
| `ReminderMileageInterval` | 50 miles, 1000 miles, 10000 miles, etc. | All mileage-based |

**File Locations:**
- `Enum/ReminderMetric.cs`
- `Enum/ReminderMileageInterval.cs`

**Usage Examples:**
- `ReminderHelper.cs` - Reminder urgency calculations based on distance
- Reminder forms - User selects interval
- Can be renamed to "DistanceInterval" but breaks stored reminders

---

## 9. DATABASE TABLES & DATA ACCESS

### 9.1 Vehicle-Named Tables
**Risk Level:** 🔴 **CRITICAL** - Database schema

**Tables (Implied from DataAccess):**
- `vehicle` - Core pet/vehicle table (dual-purpose)
- `gas_records` - Fuel records
- `odometer_records` - Distance tracking records
- `upgrade_records` - Vehicle upgrades
- `equipment_records` - Equipment tracking
- `inspection_records` - Inspection records
- `tax_records` - Tax/licensing records

**Data Access Interfaces:**
- `IVehicleDataAccess`
- `IGasRecordDataAccess`
- `IOdometerRecordDataAccess`
- `IUpgradeRecordDataAccess`
- `IEquipmentRecordDataAccess`
- `IInspectionRecordDataAccess`
- `ITaxRecordDataAccess`

**Implementations:**
- LiteDB: `External/Implementations/Litedb/*`
- PostgreSQL: `External/Implementations/Postgres/*`

---

## 10. IMPORT/EXPORT & COMPATIBILITY

### 10.1 CSV Import Mappings
**Risk Level:** 🟡 **MEDIUM** - Breaking change for users

**File:** `MapProfile/ImportMappers.cs`

**Vehicle-Era Mappings:**
```csharp
Map(m => m.Odometer).Name(["odometer", "odo"]);
Map(m => m.FuelConsumed).Name(["gallons", "liters", "litres", "consumption", "fuelconsumed"]);
Map(m => m.PartialFuelUp).Name(["partial_fuelup", "partial tank"]);
Map(m => m.MissedFuelUp).Name(["missed_fuelup", "missedfuelup"]);
Map(m => m.InitialOdometer).Name(["initialodometer"]);
```

**Notes:**
- Supports Fuelly import format (vehicle tracking app)
- Maintains backward compatibility with LubeLogger exports
- Changing these breaks CSV import for migrating users

### 10.2 API Contracts
**Risk Level:** 🔴 **HIGH** - Public API

**Controllers:** `Controllers/API/*`
- `GasController`, `OdometerController`, `UpgradeController`, `TaxController`, `EquipmentController`
- All use `VehicleId` in payloads
- JSON contracts include vehicle-era field names

**WebHook Payloads:**
**File:** `Models/Shared/WebHookPayload.cs`
- All payloads include `VehicleId` property
- Sent to external integrations
- Breaking change if renamed

---

## 11. DOCUMENTATION & README

### 11.1 Public-Facing Documentation
**Risk Level:** 🟢 **LOW** - Documentation only

**Files:**
- `README.md` - References legacy `/Vehicle` routes, LubeLogger origin
- `docs/screenshots.md` - "Track Gas Records", "Track Service Records / Repairs / Upgrades"
- `.github/CONTRIBUTING.md` - "LubeLogger is a Vehicle Maintenance and Fuel Mileage Tracker"
- `PET_REPORTS_INVENTORY.md` - Phase 7 documentation with vehicle terminology

**Action:** Update documentation in user-facing cleanup phase

---

## 12. CLASSIFICATION SUMMARY

### By Risk Level

#### 🔴 CRITICAL - Requires Database Migration
- `VehicleId` property (31+ model classes)
- `Vehicle` class/table
- Database table names
- `ImportMode` enum values (persisted in settings)
- API contracts and webhooks
- `/Vehicle/*` route compatibility

#### 🟡 MEDIUM - Requires Data Migration or Careful Planning
- Legacy record type models (Gas, Odometer, Upgrade, Tax, Equipment, Inspection)
- User settings (`UserConfig` JSON fields)
- JavaScript `GetVehicleId()` function (50+ usages)
- Reminder enums (`ReminderMetric`, `ReminderMileageInterval`)
- CSV import field mappings

#### 🟢 LOW - Safe to Rename (Internal Only)
- Helper classes (`GasHelper`, `EquipmentHelper`, `OdometerLogic`)
- View models (`VehicleViewModel`, report models)
- UI labels and translation keys
- JavaScript function names (internal)
- Documentation files

---

## 13. PHASED MIGRATION PLAN

### Phase A: User-Facing Terminology Cleanup ✅ **SAFE TO START**
**Goal:** Remove vehicle terminology from UI without touching persistence or logic

**Actions:**
1. Update translation keys:
   - "Fuel" → "Nutrition" (or hide completely)
   - "Odometer" → "Timeline" (or hide completely)
   - "Upgrades" → deprecate or repurpose
   - "Mileage" → "Date" (conditional display)
   - "Vehicle" → "Pet" in user-visible text

2. Update setting labels:
   - Mark all gas/fuel settings as "(Deprecated)"
   - Consider hiding legacy settings section by default
   - Update tooltips and help text

3. Update documentation:
   - `README.md` - Emphasize pet health focus
   - `CONTRIBUTING.md` - Clarify LubeLogger heritage
   - Screenshots - Replace vehicle images with pet examples

4. Update context menu labels:
   - Garage right-click menu
   - Tab dropdown selections

**Impact:** User-facing only, no breaking changes  
**Time Estimate:** 1-2 days  
**Risk:** Very Low

---

### Phase B: Deprecate Legacy Record Types 🟡 **MEDIUM EFFORT**
**Goal:** Remove legacy vehicle-specific record types from normal workflows

**Actions:**
1. Hide by default:
   - Gas Records tab (already possible via settings)
   - Odometer Records tab (already possible via settings)
   - Upgrade Records tab (already possible via settings)
   - Equipment Records tab (keep or repurpose?)
   - Inspection Records tab (keep or repurpose?)

2. Add deprecation warnings:
   - Show banner when accessing legacy tabs
   - Prompt users to migrate data to appropriate pet record types
   - Provide migration tool/guide

3. Create data migration utilities:
   - Gas → Pet Expense
   - Odometer → Timeline/Date-based tracking
   - Upgrade → Service Record or deprecate
   - Tax → Pet Expense or deprecate

4. Update default `UserConfig.VisibleTabs`:
   - Remove `GasRecord`, `OdometerRecord`, `UpgradeRecord` from defaults
   - Keep for existing users who have data

**Impact:** Existing users with legacy data still have access, but new users don't see vehicle tabs  
**Time Estimate:** 3-5 days  
**Risk:** Medium (requires user communication)

---

### Phase C: Internal Naming Cleanup 🟢 **LOW HANGING FRUIT**
**Goal:** Rename internal classes/functions without breaking persistence

**Actions:**
1. Rename helper classes:
   - `GasHelper` → deprecate or `FuelCalculationHelper` (keep internal)
   - `EquipmentHelper` → `EquipmentFormatter` or similar
   - `OdometerLogic` → `DistanceRecordLogic` or deprecate

2. Rename JavaScript functions:
   - `GetVehicleId()` → `GetPetId()` (wrap for backward compat)
   - `getVehicleHealthRecords()` → `getPetHealthRecords()`
   - `saveHealthRecordToVehicle()` → `saveHealthRecordToPet()`
   - Keep old function names as aliases initially

3. Rename view models:
   - `VehicleViewModel` → `PetProfileViewModel`
   - `KioskVehicleViewModel` → `KioskPetViewModel`
   - Report view models with "Vehicle" → "Pet"

4. Refactor conditional logic:
   - Replace `!string.IsNullOrWhiteSpace(vehicle.PetName)` checks
   - Add explicit `IsPetProfile` property to Vehicle/Pet model
   - Simplify pet vs vehicle detection

**Impact:** Internal refactoring, no user-facing changes  
**Time Estimate:** 2-3 days  
**Risk:** Low (test thoroughly)

---

### Phase D: Database & Persistence Migration 🔴 **HIGH RISK - PLAN CAREFULLY**
**Goal:** Rename core persistence identifiers and database schemas

**Prerequisites:**
- All earlier phases complete
- Comprehensive test coverage
- Data backup/migration scripts tested
- User communication plan
- Rollback plan

**Actions:**
1. Create migration strategy:
   - Option A: Create new `PetId` field, dual-write temporarily, migrate data, deprecate `VehicleId`
   - Option B: Rename `VehicleId` → `PetId` via database migration (breaking change)
   - Option C: Keep `VehicleId` permanently as internal implementation detail

2. If proceeding with rename:
   - Update all model classes (`public int PetId { get; set; }`)
   - Update database schemas (table columns)
   - Update data access interfaces
   - Update JSON serialization contracts
   - Update API contracts (versioned endpoints)
   - Update webhook payloads

3. Migration scripts:
   - LiteDB migration
   - PostgreSQL migration
   - Export/import tool to migrate data between versions

4. Update routing:
   - Deprecate `/Vehicle/*` routes
   - Make `/animals/*` primary
   - Maintain redirect compatibility for old bookmarks

**Impact:** Major breaking change, requires careful version management  
**Time Estimate:** 1-2 weeks + extensive testing  
**Risk:** High

---

### Phase E: Remove Dead Code & Legacy Features 🧹 **FINAL CLEANUP**
**Goal:** Remove unused vehicle-era code after migration complete

**Prerequisites:**
- All earlier phases complete
- Users migrated off legacy record types
- Analytics confirm no usage of deprecated features

**Actions:**
1. Remove legacy record type infrastructure:
   - Delete `GasRecord` models, controllers, views, JS
   - Delete `OdometerRecord` models, controllers, views, JS
   - Delete `UpgradeRecord` models, controllers, views, JS
   - Archive or repurpose Equipment/Inspection/Tax as needed

2. Remove legacy settings:
   - Delete unused `UserConfig` properties
   - Clean up user settings migration

3. Remove legacy enums:
   - Remove `GasRecord`, `OdometerRecord`, `UpgradeRecord` from `ImportMode`
   - Renumber enum or mark as obsolete

4. Remove legacy helpers:
   - Delete `GasHelper` (if deprecated)
   - Delete `OdometerLogic` (if deprecated)

5. Update dependency injection:
   - Remove unused data access registrations in `Program.cs`

**Impact:** Code cleanup, reduces maintenance burden  
**Time Estimate:** 2-3 days  
**Risk:** Low (if earlier phases complete)

---

## 14. RECOMMENDED EXECUTION ORDER

1. ✅ **Phase A: User-Facing Terminology Cleanup** (Start immediately, safest)
2. 🟡 **Phase C: Internal Naming Cleanup** (Parallel with A, low risk)
3. 🟡 **Phase B: Deprecate Legacy Record Types** (After A complete)
4. 🔴 **Phase D: Database & Persistence Migration** (Requires extensive planning)
5. 🧹 **Phase E: Remove Dead Code** (Only after D + user migration complete)

**Critical Path:**
- Phase A can start immediately with no dependencies
- Phase C can run in parallel with minimal risk
- Phase B requires user communication but no breaking changes
- Phase D is the most complex and should be last
- Phase E is cleanup after everything else is stable

---

## 15. ITEMS TO KEEP (NOT VEHICLE-ERA)

**These items contain "Vehicle" terminology but are actually pet-aware or dual-purpose:**

| Item | Reason to Keep |
|------|----------------|
| `Vehicle` class | Core entity, already supports pet fields |
| `VehicleController` | Handles both routes, dual-purpose |
| `VehicleId` | Primary key, compatibility-critical |
| `VehicleViewModel` | View model, can be renamed but not urgent |
| `/Vehicle/*` routes | Compatibility layer for existing integrations |
| `ImportMode.VehicleRecord` | Import mode for bulk pet/vehicle import |

---

## 16. TESTING STRATEGY

### 16.1 Pre-Migration Testing
- Export all test data (pets + legacy vehicles)
- Verify all record types load correctly
- Test CSV import/export for legacy records
- Test API endpoints with vehicle terminology

### 16.2 During Migration Testing
- Run migration scripts on test database copies
- Verify data integrity after each phase
- Test backward compatibility with old routes
- Test forward compatibility with new names

### 16.3 Post-Migration Testing
- Full regression test suite
- User acceptance testing with real pet profiles
- Performance testing with large datasets
- API contract validation

---

## 17. NOTES & RECOMMENDATIONS

### Immediate Actions ✅
1. Start with Phase A (terminology cleanup) - safest and most visible improvement
2. Document any new vehicle-era additions to prevent regression
3. Add linting rules to flag new "vehicle" terminology in PRs
4. Create this migration map as living document

### Strategic Recommendations 💡
1. **Keep `VehicleId` long-term:** Renaming is not worth the migration risk. It's an internal implementation detail.
2. **Deprecate, don't delete:** Keep legacy record types accessible for existing users with historical data
3. **Dual-write strategy:** If renaming persistence, support both old and new field names temporarily
4. **API versioning:** Version API endpoints before any breaking changes to contracts
5. **User communication:** Announce deprecations clearly, provide migration tools

### Risk Mitigation 🛡️
1. Never rename database fields without comprehensive migration plan
2. Always maintain backward compatibility for at least one major version
3. Provide export/import tools for users to migrate their own data
4. Test on real user data snapshots (anonymized)
5. Have rollback plan for every breaking change

---

## Conclusion

This audit identifies **200+ locations** with vehicle-era terminology across the PawLogger codebase. The most critical finding is the universal use of `VehicleId` as the foreign key linking all records to pet profiles - this should **not** be renamed without a formal database migration strategy.

The safest migration path is:
1. Clean up user-facing terminology (Phase A)
2. Deprecate legacy features from normal workflows (Phase B)
3. Rename internal implementation details (Phase C)
4. Only then consider database schema changes (Phase D)

**The app is stable and functional in its current state.** All vehicle-era concepts are either:
- Already marked as "(Legacy)" in the UI
- Hidden by default for new pet profiles
- Still useful for users with historical vehicle data
- Too risky to rename without breaking compatibility

Further migration should be driven by user needs and strategic goals, not just terminology aesthetics.
