# PawLogger

Self-hosted, open-source, web-based pet health and care tracking app.

PawLogger helps you maintain a complete health history for your pets — tracking vet visits, vaccinations, medications, licensing, reminders, expenses, weight, allergies, and other care events — all in one place.

---

## Features

### Pet Profiles
- Create profiles for one or more pets
- Fields: name, species, breed, sex, date of birth (with estimated-age flag), color, microchip number, spayed/neutered status, primary vet, emergency contact, adoption date, source
- Pet status lifecycle: **Active**, **Archived**, **Rehomed**, **Deceased**
- Profile image support

### Health Record Timeline
A unified care timeline for each pet, with categorized health events:
- Vet Visit
- Vaccination
- Medication
- Illness / Symptom
- Procedure / Surgery
- Dental
- Grooming
- Weight Check
- Allergy Reaction
- Lab Result
- Licensing
- Preventive Care
- Behavioral Note
- Miscellaneous Care

Each record supports cost tracking, notes, file/document attachments, tags, and custom extra fields.

### Specialized Record Types
Structured sub-records that integrate with the health timeline:

- **Vaccination records** — vaccine name, lot number, clinic, administering vet, next due date, optional renewal reminder
- **Medication records** — medication name, dosage, unit, frequency, route of administration, prescribing vet, purpose, end date, refill date, active/inactive status, optional refill reminder
- **Vet visit records** — clinic, veterinarian, reason for visit, symptoms reported, diagnosis, treatment provided, follow-up date, optional follow-up reminder
- **Licensing records** — license number, issuing authority, expiry date, optional renewal reminder

### Weight Tracking
Weight check events on the health timeline store a numeric weight value and unit (e.g. lbs, kg). Weight history is available per pet.

> **Note:** The `Current Weight` field on the pet profile is a manually set free-form string. It does not automatically sync from the most recent Weight Check entry.

### Allergy Tracking
Allergy Reaction events capture severity (Mild / Moderate / Severe / Life-threatening), allergy type (Food / Medication / Environmental / Contact / Unknown), and specific trigger.

### Reminders
Date-based reminders per pet, with the following types:
- Custom
- Vaccination Due
- Medication Refill / Dose Schedule
- License Renewal
- Annual Checkup
- Flea & Tick Prevention
- Heartworm Prevention
- Deworming
- Grooming
- Dental Cleaning
- Weight Check
- Follow-Up (linked to a vet visit or health record follow-up date)

Reminders can be created manually or generated automatically from specialized records when reminder options are enabled on those records.

### Pet Expense Tracking
Centralized expense log per pet with categories:
Vet, Medication, Vaccination, Grooming, Food, Supplies, Licensing, Insurance, Boarding/Daycare, Training, Preventive Care, Other.

Expenses support vendor tracking, recurring flags, cost totals, notes, file attachments, and tags.

### Quick Notes
Free-form notes attached to a pet, with pinning, tags, and file attachments. Useful for quick observations that do not fit a formal health record.

### File and Document Attachments
Most record types support uploading and associating files (receipts, lab results, vet reports, etc.).

### Multi-Pet Support
All features are scoped to individual pet profiles. Manage as many pets as needed from a single instance.

### Archived and Inactive Pet Support
Pets can be marked Archived, Rehomed, or Deceased and optionally hidden from the active pet list while preserving their full history.

### CSV Import Support
CSV import support is available for supported record types (configurable via `EnableCsvImports`).

### Email Reminders
Optional email notification support for reminders via MailKit / SMTP (configured in `appsettings.json`).

---

## User Workflow Overview

1. **My Pets** — Landing page showing all active pet profiles. Archived/inactive pets can be shown or hidden.
2. **Pet Profile** — View and edit a pet's details, profile image, and status.
3. **Health Records** — The main care timeline for a pet. Add, filter, and review categorized health events.
4. **Specialized Records** — Dedicated tabs for Vaccinations, Medications, Vet Visits, and Licensing with richer structured fields.
5. **Reminders** — View upcoming and overdue reminders for a pet. Mark complete or dismiss.
6. **Expenses** — Log and review all costs associated with a pet's care.
7. **Notes** — Quick notes and observations attached to a pet.
8. **Summary / Print View** — A printable health summary for a pet (report view).

---

## Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 10 (ASP.NET Core MVC) |
| Default database | LiteDB (embedded, file-based, no external server required) |
| Optional database | PostgreSQL (via Npgsql) |
| UI framework | Bootstrap |
| Charts | Chart.js |
| Date picker | Bootstrap-DatePicker |
| Alerts / modals | SweetAlert2 |
| CSV processing | CsvHelper |
| Markdown rendering | Drawdown |
| Email | MailKit |
| UI layout | Masonry |
| QR codes | QRCode-Generator |

> **Developer note:** The internal .NET namespace, project file, and assembly name are still `CarCareTracker` — a legacy artifact of the upstream codebase. The database schema likewise uses `VehicleId` as the column name for what is semantically the `PetId`. These internal identifiers do not affect user-facing behavior and will be migrated in a future phase.

---

## Setup and Installation

### Prerequisites
- [Docker](https://docs.docker.com/get-docker/) (recommended), **or**
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) for local development

---

### Option 1 — Docker (LiteDB, default)

```bash
docker compose up -d
```

Uses [`docker-compose.yml`](docker-compose.yml). The app runs on port **8080**.

Data is persisted in Docker volumes (`data` and `keys`).

---

### Option 2 — Docker with PostgreSQL

```bash
docker compose -f docker-compose.postgresql.yml up -d
```

Uses [`docker-compose.postgresql.yml`](docker-compose.postgresql.yml). Starts both a PostgreSQL 18 container and the application. Configure the connection string via the `POSTGRES_CONNECTION` environment variable:

```
Host=<host>:5432;Username=<user>;Password=<password>;Database=<dbname>;
```

---

### Option 3 — Local Development (.NET CLI)

```bash
dotnet restore
dotnet run
```

The app will be available at `https://localhost:5001` or `http://localhost:5000` by default.

---

## Configuration

Configuration is managed through [`appsettings.json`](appsettings.json) or environment variable overrides.

Key settings:

| Key | Default | Description |
|---|---|---|
| `EnableAuth` | `false` | Enable login/password authentication |
| `DisableRegistration` | `false` | Prevent new user self-registration |
| `EnableRootUserOIDC` | `false` | Enable OIDC/SSO for the root user |
| `EnableCsvImports` | `true` | Allow CSV-based record imports |
| `ShowCalendar` | `true` | Show calendar view |
| `UserLanguage` | `en_US` | UI language |
| `UseMarkDownOnSavedNotes` | `false` | Render Markdown in saved notes |
| `DefaultReminderEmail` | `""` | Email address for reminder notifications |
| `POSTGRES_CONNECTION` | _(not set)_ | Set this environment variable to switch from LiteDB to PostgreSQL |

When `POSTGRES_CONNECTION` is not set, LiteDB is used automatically.

---

## Known Limitations

- The internal project namespace, assembly, and database schema retain legacy `CarCareTracker` / `VehicleId` naming from the upstream Lubelogger codebase. This is an internal implementation detail only and does not affect the user-facing interface.
- The `Current Weight` field on the pet profile is free-form and must be updated manually; it does not auto-populate from the latest Weight Check health record.
- Some legacy vehicle-specific record types (service records, gas logs, odometer entries, inspections, tax records, etc.) remain in the codebase from the upstream project. These are not part of the standard pet-health workflow and some tabs/sections may still appear depending on tab visibility configuration.
- Reminder auto-generation from specialized records (vaccinations, medications, vet visit follow-ups) depends on the `ReminderEnabled` flag being set when the record is saved. Existing records saved before that option was introduced will not have reminders created retroactively.
- The Docker Compose files still reference the upstream `lubelogger` image tag. Update the `image:` value to your local or published PawLogger image when deploying a custom build.

---

## Roadmap

- Full schema rename from `VehicleId` / `CarCareTracker` to `PetId` / `PawLogger`
- Auto-sync `Current Weight` from the latest Weight Check health record
- Printable / exportable per-pet health summary
- OIDC/SSO improvements
- Mobile-optimized views

---

## License

MIT

---

## Credits and Attribution

PawLogger is a fork of [LubeLogger](https://github.com/hargata/lubelog) by hargata, which is also MIT licensed.
The original project is a self-hosted vehicle maintenance and fuel mileage tracker.
PawLogger is an independent fork that repurposes the architecture for pet health and care tracking.

### Dependencies
- [Bootstrap](https://github.com/twbs/bootstrap)
- [LiteDB](https://github.com/mbdavid/litedb)
- [Npgsql](https://github.com/npgsql/npgsql)
- [Bootstrap-DatePicker](https://github.com/uxsolutions/bootstrap-datepicker)
- [SweetAlert2](https://github.com/sweetalert2/sweetalert2)
- [CsvHelper](https://github.com/JoshClose/CsvHelper)
- [Chart.js](https://github.com/chartjs/Chart.js)
- [Drawdown](https://github.com/adamvleggett/drawdown)
- [MailKit](https://github.com/jstedfast/MailKit)
- [Masonry](https://github.com/desandro/masonry)
- [QRCode-Generator](https://github.com/kazuhikoarase/qrcode-generator)