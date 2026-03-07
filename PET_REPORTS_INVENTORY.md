# Pet Health Summary & Care History Report Implementation Inventory

## Overview
Two major report/summary features have been implemented for pet profiles (Phase 7):
1. **Pet Health Summary** – Modal view showing consolidated pet medical/health info
2. **Care History Report** – Comprehensive timeline export of all pet care records with filtering

---

## 1. CONTROLLER ACTIONS

### File: [Controllers/Vehicle/ReportController.cs](Controllers/Vehicle/ReportController.cs)

#### A. Pet Health Summary Action
**Method:** `GetPetSummaryData(int vehicleId)` [L742-789]
- **HTTP Method:** GET
- **Filter:** `[TypeFilter(typeof(CollaboratorFilter))]`
- **Returns:** `PartialView("Report/_PetSummary", PetSummaryViewModel)`
- **Data Aggregated:**
  - Vaccinations (ordered by date, most recent first)
  - Active medications (filtered where `IsActive == true`)
  - Known allergies (HealthRecords with `AllergyReaction` category)
  - Recent health records (last 12 months, excluding allergies)
  - Weight history (HealthRecords with `WeightCheck` category, last 10 entries)
  - Upcoming reminders (next 90 days, sorted by date)
  - Pet profile data
  - Generated date timestamp

#### B. Care History Report Action
**Method:** `GetVehicleHistory(int vehicleId, ReportParameter reportParameter)` [L532-738]
- **HTTP Method:** POST/GET
- **Filter:** `[TypeFilter(typeof(CollaboratorFilter))]`
- **Returns:** `PartialView("Report/_CareHistory", CareHistoryViewModel)`
- **Capabilities:**
  - Filters by date range (optional)
  - Filters by tags with exclude/include modes
  - Removes odometer column for pet profiles
  - Aggregates all record types: ServiceRecords, CollisionRecords, UpgradeRecords, TaxRecords, **HealthRecords** (Phase 7)
  - Calculates totals: distance, cost, gas cost, per-mile metrics, depreciation
  - Supports MPG/fuel economy conversions
- **Data Aggregated:**
  - Service, repair, upgrade, tax, and health records
  - Gas costs and mileage
  - Days owned and depreciation metrics
  - Pet-specific fields: weight, provider, category

#### C. Supporting Report Actions
**GetReportParameters(int vehicleId)** [L493-530]
- Returns report configuration UI (_ReportParameters partial view)
- Detects pet profiles and removes odometer column for pets
- Lists available extra fields from record types

**GetReportPartialView(int vehicleId)** [L14-202]
- Main report dashboard that displays both Pet Health Summary and Care History buttons

---

## 2. VIEW FILES (.cshtml)

### File: [Views/Vehicle/Report/_PetSummary.cshtml](Views/Vehicle/Report/_PetSummary.cshtml)
**Model:** `PetSummaryViewModel`
- **Modal Header:** "Pet Health Summary" with print button
- **Pet Profile Section:**
  - Name, species, breed, color
  - Age (calculated from DOB), sex, spaying status
  - Current weight, microchip, license number
  - Primary vet, emergency contact
  - Pet image (if available)
  - Generated timestamp

- **Content Sections:**
  1. **Known Allergies / Reactions** – Table with allergen, type, severity, notes, date
  2. **Vaccination History** – Table with vaccine, date, next due, clinic
  3. **Active Medications** – Table with medication, dose/frequency, route, purpose, started date
  4. **Upcoming Care (next 90 days)** – List of upcoming reminders with urgency highlighting
  5. **Recent Health History (last 12 months)** – Table with date, category, entry, provider, notes
  6. **Weight History** – Last 10 weight check entries (if available)

- **Empty State:** Shows message when no records exist

**Rendering:** Modal with print-friendly styling (print button, d-print-none hiding)

---

### File: [Views/Vehicle/Report/_CareHistory.cshtml](Views/Vehicle/Report/_CareHistory.cshtml)
**Model:** `CareHistoryViewModel`
- **Report Header:**
  - Pet/vehicle name and status badge
  - Ownership metrics: days owned, distance traveled, total cost
  - MPG/fuel economy information
  - Depreciation metrics (if applicable)

- **Timeline Table:**
  - Columns: Date, Type (badge), Odometer (hidden for pets), Description, Provider, Cost, Notes, Weight (pet-specific)
  - Includes: ServiceRecords, CollisionRecords, UpgradeRecords, TaxRecords, **HealthRecords**
  - Rows ordered by date, then odometer/mileage
  - Print-friendly formatting with individual record printing option

---

## 3. UI EXPOSURE & NAVIGATION

### A. Report Tab ([Views/Vehicle/Report/_Report.cshtml](Views/Vehicle/Report/_Report.cshtml))
**Primary Button Location:** [L102-109] Control panel in right sidebar
```
Button 1: "Care History Report" (onclick="generateCareHistoryReport()")
Button 2: "Pet Health Summary" (onclick="showPetSummaryModal()")
```

**Secondary Menu:** [L166-167] Three-dots dropdown menu
```
<a href="#" onclick="generateCareHistoryReport()">Care History Report</a>
<a href="#" onclick="showPetSummaryModal()">Pet Health Summary</a>
```

**Modal Container:** [L188-194] Pet Summary modal definition
```html
<div class="modal fade" id="petSummaryModal" tabindex="-1">
    <div class="modal-dialog modal-xl">
        <div class="modal-content" id="petSummaryModalContent"></div>
    </div>
</div>
```

---

### B. Health Records Tab ([Views/Vehicle/Health/_HealthRecords.cshtml](Views/Vehicle/Health/_HealthRecords.cshtml))
**Dropdown Menu:** [L100] Record action menu
```
<a class="dropdown-item" href="#" onclick="showAddQuickHealthNoteModal()">
    Quick Observation
</a>
<a class="dropdown-item" href="#" onclick="showPetSummaryModal()">
    Pet Health Summary
</a>
```

---

### C. Main Vehicle Page ([Views/Vehicle/Index.cshtml](Views/Vehicle/Index.cshtml))
**Modal Container:** [L334] Pet Health Summary modal (at page level)
```html
<div class="modal fade" id="petSummaryModal" tabindex="-1">
    <div class="modal-dialog modal-xl">
        <div class="modal-content" id="petSummaryModalContent"></div>
    </div>
</div>
```

**Quick Health Note Modal:** [L326] Per-pet observation entry point
```html
<div class="modal fade" id="quickHealthNoteModal" tabindex="-1">
    <div class="modal-dialog modal-md">
        <div class="modal-content" id="quickHealthNoteModalContent"></div>
    </div>
</div>
```

---

## 4. JAVASCRIPT FUNCTIONS

### File: [wwwroot/js/reports.js](wwwroot/js/reports.js)

**Function:** `generateCareHistoryReport()` [L92]
- **Flow:**
  1. Gets vehicleId from `GetVehicleId()`
  2. Calls `/Vehicle/GetReportParameters?vehicleId=` to fetch report configuration UI
  3. Opens SweetAlert2 modal for column selection, tag filtering, date range filtering
  4. On confirmation, POSTs to `/Vehicle/GetVehicleHistory` with report parameters
  5. Calls `printContainer(data)` to render and print the timeline

- **Saved Settings:** Uses sessionStorage for `${vehicleId}_selectedReportColumns`

---

### File: [wwwroot/js/healthrecord.js](wwwroot/js/healthrecord.js)

**Function:** `showPetSummaryModal()` [L257]
```javascript
function showPetSummaryModal() {
    var vehicleId = GetVehicleId().vehicleId;
    $.get('/Vehicle/GetPetSummaryData?vehicleId=' + vehicleId, function (data) {
        if (data) {
            $('#petSummaryModalContent').html(data);
            $('#petSummaryModal').modal('show');
        }
    });
}
```
- GETs from `/Vehicle/GetPetSummaryData`
- Loads partial view into `#petSummaryModalContent`
- Shows modal with Bootstrap

---

**Function:** `showAddQuickHealthNoteModal()` [L191]
```javascript
function showAddQuickHealthNoteModal() {
    var vehicleId = GetVehicleId().vehicleId;
    $.get('/Vehicle/GetAddQuickHealthNotePartialView?vehicleId=' + vehicleId, function (data) {
        if (data) {
            $('#quickHealthNoteModalContent').html(data);
            $('#quickHealthNoteModal').modal('show');
        }
    });
}
```
- Fetches `/Vehicle/GetAddQuickHealthNotePartialView`
- Shows modal for rapid health observation entry

---

**Function:** `saveQuickHealthNote()` [L211]
- Validates date and title
- POSTs to `/Vehicle/SaveHealthRecordToVehicleId` with HealthRecordInput payload
- Defaults category to `IllnessSymptom` (3), status to `Informational` (2)
- Refreshes health records table on success

---

## 5. KEY DATA MODELS & SERVICES

### View Models

**[Models/Report/PetSummaryViewModel.cs](Models/Report/PetSummaryViewModel.cs)**
```csharp
public class PetSummaryViewModel
{
    public Vehicle PetData { get; set; }                // Pet profile (Vehicle reused)
    public List<VaccinationRecord> Vaccinations { get; set; }
    public List<MedicationRecord> ActiveMedications { get; set; }
    public List<HealthRecord> KnownAllergies { get; set; }
    public List<ReminderRecord> UpcomingReminders { get; set; }
    public List<HealthRecord> RecentHealthRecords { get; set; }
    public List<HealthRecord> WeightHistory { get; set; }
    public string GeneratedDate { get; set; }
}
```

**[Models/Report/CareHistoryViewModel.cs](Models/Report/CareHistoryViewModel.cs)**
```csharp
public class CareHistoryViewModel
{
    public Vehicle VehicleData { get; set; }
    public List<GenericReportModel> CareHistory { get; set; }
    public ReportParameter ReportParameters { get; set; }
    public string Odometer { get; set; }
    public string MPG { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalGasCost { get; set; }
    public string DaysOwned { get; set; }
    public string DistanceTraveled { get; set; }
    public decimal TotalCostPerMile { get; set; }
    public string DistanceUnit { get; set; }
    // Depreciation, date filtering, etc.
}
```

**[Models/Report/GenericReportModel.cs](Models/Report/GenericReportModel.cs)**
```csharp
public class GenericReportModel
{
    public ImportMode DataType { get; set; }           // ServiceRecord, HealthRecord, etc.
    public DateTime Date { get; set; }
    public int Odometer { get; set; }
    public string Description { get; set; }
    public string Notes { get; set; }
    public decimal Cost { get; set; }
    public List<UploadedFiles> Files { get; set; }
    public List<ExtraField> ExtraFields { get; set; }
    
    // Pet-specific fields (Phase 7)
    public decimal WeightValue { get; set; }
    public string WeightUnit { get; set; }
    public string Provider { get; set; }
    public string Category { get; set; }              // Human-readable category label
}
```

**[Models/Report/ReportParameter.cs](Models/Report/ReportParameter.cs)**
```csharp
public class ReportParameter
{
    public List<string> VisibleColumns { get; set; }
    public List<string> ExtraFields { get; set; }
    public TagFilter TagFilter { get; set; }          // Exclude/IncludeOnly
    public List<string> Tags { get; set; }
    public bool FilterByDateRange { get; set; }
    public string StartDate { get; set; }
    public string EndDate { get; set; }
    public bool PrintIndividualRecords { get; set; }
}
```

---

### Data Access Services Used

**VehicleController Constructor Injection:**
- `IHealthRecordDataAccess _healthRecordDataAccess` – Queries pet health timeline
- `IVaccinationRecordDataAccess _vaccinationRecordDataAccess` – Vaccinations list
- `IMedicationRecordDataAccess _medicationRecordDataAccess` – Active medications
- `IReminderRecordDataAccess _reminderRecordDataAccess` – Upcoming care reminders
- `IVehicleLogic _vehicleLogic` – Vehicle/pet aggregation logic
- `IServiceRecordDataAccess`, `IGasRecordDataAccess`, `ICollisionRecordDataAccess`, etc. – Vehicle record types
- `IConfigHelper _config` – User preferences, locale
- `IFileHelper _fileHelper` – File/export handling
- `IGasHelper _gasHelper` – Fuel economy calculations

---

## 6. CURRENT UI FLOW DIAGRAM

```
Pet/Vehicle Main Page (Vehicle/Index.cshtml)
    ↓
[Report Tab] → _Report.cshtml
    ├─→ "Care History Report" button
    │   ├─ JS: generateCareHistoryReport()
    │   ├─ GET /Vehicle/GetReportParameters
    │   ├─ SweetAlert2: Column/Filter Selection Modal
    │   ├─ POST /Vehicle/GetVehicleHistory
    │   └─ View: _CareHistory.cshtml (printable timeline)
    │
    └─→ "Pet Health Summary" button
        ├─ JS: showPetSummaryModal()
        ├─ GET /Vehicle/GetPetSummaryData
        ├─ Modal: #petSummaryModal (modal-xl)
        └─ View: _PetSummary.cshtml (printable summary)

[Health Records Tab] → _HealthRecords.cshtml
    ├─→ Dropdown → "Quick Observation"
    │   ├─ JS: showAddQuickHealthNoteModal()
    │   └─ Modal: Quick entry form
    │
    └─→ Dropdown → "Pet Health Summary"
        └─ JS: showPetSummaryModal() [same as above]
```

---

## 7. LINKING & EXPOSURE SUMMARY

| Feature | Controller Action | View | Primary UI Trigger | Secondary Trigger |
|---------|-------------------|------|-------------------|-------------------|
| **Pet Health Summary** | `GetPetSummaryData()` | `_PetSummary.cshtml` | Report tab "Pet Health Summary" button | Health Records dropdown, Index.cshtml modal |
| **Care History Report** | `GetVehicleHistory()` | `_CareHistory.cshtml` | Report tab "Care History Report" button | Report tab 3-dots menu dropdown |
| **Quick Health Note** | `GetAddQuickHealthNotePartialView()` | `_QuickHealthNoteModal.cshtml` | Health Records dropdown | Index.cshtml modal |

---

## 8. PHASE 7 INTEGRATION NOTES

- **HealthRecord Timeline Integration:** `GetVehicleHistory()` now includes HealthRecords in the care history timeline (L687-705)
  - Filters by date range if applied
  - Includes pet-specific fields: weight, provider, category
  - Ordered chronologically with all other care records

- **Modal Registration:**
  - Page-level modals in `Index.cshtml` (L326, L334) to prevent ID collisions
  - Tab-level fallback modals in Report view for standalone use

- **Legacy Vehicle Support:**
  - Pet profiles automatically hide odometer/mileage columns in reports
  - Detection: `!string.IsNullOrWhiteSpace(vehicle.PetName)`

---

## 9. KEY FILES REFERENCE

| Purpose | File Path |
|---------|-----------|
| Report Actions | `Controllers/Vehicle/ReportController.cs` |
| Report Layout | `Views/Vehicle/Report/_Report.cshtml` |
| Pet Summary View | `Views/Vehicle/Report/_PetSummary.cshtml` |
| Care History View | `Views/Vehicle/Report/_CareHistory.cshtml` |
| Health Records | `Views/Vehicle/Health/_HealthRecords.cshtml` |
| Main Pet Page | `Views/Vehicle/Index.cshtml` |
| Report JavaScript | `wwwroot/js/reports.js` |
| Health JS | `wwwroot/js/healthrecord.js` |
| Pet Summary Model | `Models/Report/PetSummaryViewModel.cs` |
| Care History Model | `Models/Report/CareHistoryViewModel.cs` |
| Report Models | `Models/Report/` |

---

## 10. PRODUCTION DEPENDENCIES

- **HealthRecord table** – Must have records for summaries to populate
- **VaccinationRecord, MedicationRecord** – Linked by vehicleId (petId)
- **ReminderRecord** – For upcoming care section
- **ImportMode enum** – DataType determination
- **HealthRecordCategory enum** – Filtering allergies, weight checks
- **CollaboratorFilter** – Access control on report endpoints
- **SessionStorage** – Caching report parameters client-side
- **SweetAlert2** – Modal UI for report configuration
- **Bootstrap 5** – Modal and print styling
