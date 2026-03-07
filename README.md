# PawLogger

PawLogger is a self-hosted, open-source pet health and care tracking application.

It is a fork/refactor of Lubelogger that now centers user-facing workflows around pets and care history rather than vehicles, while retaining some legacy internals for compatibility during phased migration.

## Project Overview

PawLogger is designed to keep each pet’s health and care history in one place, including:

* Pet profiles and lifecycle status
* A HealthRecord-based timeline/history
* Structured pet-health records for vaccinations, medications, vet visits, and licensing
* Centralized reminders and pet expenses
* Weight tracking, allergy tracking, and quick health notes
* Printable pet health summaries and reports

## Feature Summary

Only currently implemented features are listed below.

### Pet Profiles

* Track one or more pets per account
* Store profile details such as name, species, breed, date of birth / estimated age, sex, color, microchip, spayed/neutered status, vet/contact fields, and profile image
* Pet statuses include Active, Archived, Rehomed, and Deceased

### HealthRecord Timeline / History Backbone

* HealthRecord is the central timeline model for pet care history
* Supports categorized health events, including Weight Check and Allergy / Reaction
* Timeline entries support notes, tags, costs, and attachments where applicable

### Specialized Pet-Health Records

* Vaccinations
* Medications
* Vet Visits
* Licensing

These specialized flows support richer structured fields and can project/link into HealthRecord timeline entries.

### Reminders

* Centralized, pet-care-oriented reminder flow
* Reminders can be manual or created from linked/specialized record workflows when enabled

### Pet Expense Tracking

* Centralized pet expense tracking
* Supports categories such as vet, medication, vaccination, grooming, food, supplies, licensing, insurance, boarding/daycare, training, preventive care, and other

### Weight Tracking and Trends

* Weight tracking via Weight Check HealthRecord entries
* Weight trend/history chart is available
* Weight entries can include explicit units

### Allergy Tracking

* Allergy events are tracked through HealthRecord entries

### Quick Health Notes

* Quick health notes are available for fast care observations

### Pet Health Summary

* Pet Health Summary view is available
* Includes printable summary/report workflow

### Multi-Pet Support

* Per-pet profiles, records, reminders, and expenses

### Attachments

* File/document attachments are available in relevant record workflows

### Legacy Compatibility

* Some Lubelogger-era internals and some legacy tabs/sections may still remain for compatibility
* User-facing navigation now prefers the `/animals` route prefix

## Main Workflows

1. **My Pets**

   * Manage active pets and visibility of archived/inactive pets

2. **Pet Profile**

   * Edit identity and status fields, including Archived, Rehomed, and Deceased states

3. **Health Records**

   * Use the timeline as the primary longitudinal care history

4. **Specialized Health Records**

   * Create and update vaccination, medication, vet visit, and licensing records
   * Link/project specialized entries into HealthRecord history

5. **Reminders**

   * Track upcoming and past-due care reminders and linked follow-ups

6. **Expenses**

   * Track pet-care costs through the centralized pet expense flow

7. **Pet Health Summary / Print**

   * Review or print a consolidated pet health summary/report

## Tech Stack

| Layer              | Technology                    |
| ------------------ | ----------------------------- |
| Runtime            | .NET 10 (ASP.NET Core MVC)    |
| Default database   | LiteDB (embedded, file-based) |
| Optional database  | PostgreSQL (via Npgsql)       |
| UI framework       | Bootstrap                     |
| Charts             | Chart.js                      |
| Date picker        | Bootstrap-DatePicker          |
| Alerts / modals    | SweetAlert2                   |
| CSV processing     | CsvHelper                     |
| Markdown rendering | Drawdown                      |
| Email              | MailKit                       |
| Layout utilities   | Masonry                       |
| QR support         | QRCode-Generator              |

## Setup and Installation

### Prerequisites

* [Docker](https://docs.docker.com/get-docker/) for containerized deployment, or
* [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) for local development

### Option 1 — Docker (LiteDB, default)

```bash
docker compose up -d
```

* Uses [`docker-compose.yml`](docker-compose.yml)
* App binds to port **8080**
* Persists app data and ASP.NET data-protection keys via Docker volumes (`data`, `keys`)

### Option 2 — Docker with PostgreSQL

```bash
docker compose -f docker-compose.postgresql.yml up -d
```

* Uses [`docker-compose.postgresql.yml`](docker-compose.postgresql.yml)
* Starts both PostgreSQL and app containers
* Configure the database connection with `POSTGRES_CONNECTION`, for example:

```text
Host=<host>:5432;Username=<user>;Password=<password>;Database=<dbname>;
```

### Option 3 — Local Development (.NET CLI)

```bash
dotnet restore
dotnet run
```

Default local URLs are typically `https://localhost:5001` and `http://localhost:5000` unless overridden by local configuration.

### Additional Compose Variants

* [`docker-compose.dev.yml`](docker-compose.dev.yml): local development profile with Postgres health checks and local image build
* [`docker-compose.traefik.yml`](docker-compose.traefik.yml): example Traefik integration profile

## Configuration

Configuration is managed through [`appsettings.json`](appsettings.json), optional user/server config files loaded at startup, and environment variables.

Key settings include:

| Key                       | Default   | Description                                   |
| ------------------------- | --------- | --------------------------------------------- |
| `EnableAuth`              | `false`   | Enable login/password authentication          |
| `DisableRegistration`     | `false`   | Disable new user self-registration            |
| `EnableRootUserOIDC`      | `false`   | Enable OIDC/SSO for root user                 |
| `EnableCsvImports`        | `true`    | Enable CSV import flows for supported records |
| `ShowCalendar`            | `true`    | Show calendar UI                              |
| `UserLanguage`            | `en_US`   | Default UI language                           |
| `UseMarkDownOnSavedNotes` | `false`   | Render markdown in notes                      |
| `DefaultReminderEmail`    | `""`      | Default email for reminders                   |
| `POSTGRES_CONNECTION`     | *(unset)* | Switch persistence from LiteDB to PostgreSQL  |

If `POSTGRES_CONNECTION` is not set, the app uses LiteDB by default.

## Compatibility Notes

* User-facing pet workflows now prefer `/animals` routes.
* Legacy `/Vehicle` routes still work for backward compatibility.
* Some Lubelogger-era internal identifiers remain in code and schema, including `Vehicle` / `VehicleId`.
* Some legacy tabs/features may still remain available for compatibility and may be marked as **Legacy** in the UI.
* The project/assembly naming (`CarCareTracker`) still exists internally and does not affect normal end-user use.

## Known Limitations / Current Caveats

* Legacy naming and compatibility layers are still present internally while migration continues.
* Some legacy vehicle-era sections may still be visible in certain configurations.
* Reminder behavior for linked projected-record workflows can depend on per-record reminder flags and existing data state.
* `Current Weight` and structured Weight Check history can be edited through different flows; use Weight Check entries as the authoritative trend/history source.

## Optional Roadmap

Potential future improvements include:

* Further reducing legacy internal naming and compatibility layers
* Further simplifying or hiding remaining legacy tabs as migration continues
* Additional pet-health-first UX/reporting polish

## License

MIT (see [LICENSE](LICENSE)).

## Credits and Attribution

PawLogger is a fork of [LubeLogger](https://github.com/hargata/lubelog) by hargata.

* Upstream project: self-hosted vehicle maintenance and fuel mileage tracker
* This repository: pet health and care tracking adaptation built on that codebase
* Licensing: both projects are MIT licensed

### Core Dependencies

* [Bootstrap](https://github.com/twbs/bootstrap)
* [LiteDB](https://github.com/mbdavid/litedb)
* [Npgsql](https://github.com/npgsql/npgsql)
* [Bootstrap-DatePicker](https://github.com/uxsolutions/bootstrap-datepicker)
* [SweetAlert2](https://github.com/sweetalert2/sweetalert2)
* [CsvHelper](https://github.com/JoshClose/CsvHelper)
* [Chart.js](https://github.com/chartjs/Chart.js)
* [Drawdown](https://github.com/adamvleggett/drawdown)
* [MailKit](https://github.com/jstedfast/MailKit)
* [Masonry](https://github.com/desandro/masonry)
* [QRCode-Generator](https://github.com/kazuhikoarase/qrcode-generator)
