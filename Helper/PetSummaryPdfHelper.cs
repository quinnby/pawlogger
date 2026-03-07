using CarCareTracker.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CarCareTracker.Helper
{
    /// <summary>
    /// Helper for generating PDF exports of Pet Health Summary reports.
    /// Phase 7 – PDF export pass (polished visual layout).
    /// </summary>
    public static class PetSummaryPdfHelper
    {
        // Consistent color scheme
        private static readonly string PrimaryColor = "#2563eb"; // Blue
        private static readonly string AccentColor = "#0284c7"; // Sky blue
        private static readonly string WarningColor = "#dc2626"; // Red
        private static readonly string AlertColor = "#ea580c"; // Orange
        private static readonly string NeutralDark = "#374151"; // Gray-700
        private static readonly string NeutralMedium = "#6b7280"; // Gray-500
        private static readonly string NeutralLight = "#e5e7eb"; // Gray-200

        // Embedded vector assets for PDF-safe section icons.
        private static readonly string ProfileIconSvg = BuildIconSvg(
            "<circle cx='8' cy='8' r='2.2' /><circle cx='16' cy='8' r='2.2' /><circle cx='5.4' cy='13.8' r='1.8' /><circle cx='18.6' cy='13.8' r='1.8' /><ellipse cx='12' cy='15.8' rx='3' ry='2.2' />");

        private static readonly string AllergyIconSvg = BuildIconSvg(
            "<path d='M12 3l9 16H3L12 3z' /><path d='M12 9v5' /><circle cx='12' cy='16.2' r='0.8' />",
            "#dc2626");

        private static readonly string VaccinationIconSvg = BuildIconSvg(
            "<path d='M4 20l5.5-5.5' /><path d='M8.5 15.5l4-4' /><path d='M11 9l4 4' /><path d='M14.5 5.5l4 4' /><path d='M17.8 2.2l4 4-2.6 2.6-4-4z' /><path d='M6.5 17.5l-1.8 1.8' />");

        private static readonly string MedicationIconSvg = BuildIconSvg(
            "<rect x='4' y='8' width='16' height='8' rx='4' ry='4' /><path d='M12 8v8' />");

        private static readonly string UpcomingCareIconSvg = BuildIconSvg(
            "<rect x='3' y='5' width='18' height='16' rx='2' ry='2' /><path d='M3 9h18' /><path d='M8 3v4' /><path d='M16 3v4' />");

        private static readonly string HealthHistoryIconSvg = BuildIconSvg(
            "<path d='M8 3h8l3 3v15H5V3z' /><path d='M16 3v3h3' /><path d='M12 10v6' /><path d='M9 13h6' />");

        private static readonly string WeightHistoryIconSvg = BuildIconSvg(
            "<path d='M6 5h12a3 3 0 013 3v8a3 3 0 01-3 3H6a3 3 0 01-3-3V8a3 3 0 013-3z' /><path d='M8.5 12a3.5 3.5 0 017 0' /><path d='M12 12l1.8-1.6' />");

        /// <summary>
        /// Generates a PDF document from a PetSummaryViewModel.
        /// Returns the PDF as a byte array ready for download.
        /// </summary>
        public static byte[] GeneratePetSummaryPdf(PetSummaryViewModel model, IFileHelper fileHelper)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.Margin(0.5f, Unit.Inch);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial").FontColor(NeutralDark));

                    page.Header().Element(c => Header(c, model));
                    page.Content().Element(c => Content(c, model, fileHelper));
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span($"Generated: {model.GeneratedDate}").FontSize(8).FontColor(NeutralMedium);
                        text.Span(" | Page ").FontSize(8).FontColor(NeutralMedium);
                        text.CurrentPageNumber().FontSize(8).FontColor(NeutralMedium);
                        text.Span(" of ").FontSize(8).FontColor(NeutralMedium);
                        text.TotalPages().FontSize(8).FontColor(NeutralMedium);
                    });
                });
            });

            return document.GeneratePdf();
        }

        private static void Header(IContainer container, PetSummaryViewModel model)
        {
            container.BorderBottom(2).BorderColor(PrimaryColor).PaddingBottom(6).Text("Pet Health Summary")
                .FontSize(13).Bold().FontColor(AccentColor);
        }

        private static void Content(IContainer container, PetSummaryViewModel model, IFileHelper fileHelper)
        {
            container.Column(column =>
            {
                // Keep pet identity details in content flow to avoid brittle repeating header constraints.
                column.Item().Element(c => PetIdentitySection(c, model, fileHelper));

                // Pet Profile Section
                column.Item().PaddingTop(8).Element(c => PetProfileSection(c, model));

                // Known Allergies Section - Keep prominent due to medical importance
                if (model.KnownAllergies.Any())
                {
                    column.Item().PaddingTop(12).Element(c => AllergiesSection(c, model));
                }

                // Vaccinations Section
                if (model.Vaccinations.Any())
                {
                    column.Item().PaddingTop(12).Element(c => VaccinationsSection(c, model));
                }

                // Active Medications Section
                if (model.ActiveMedications.Any())
                {
                    column.Item().PaddingTop(12).Element(c => MedicationsSection(c, model));
                }

                // Upcoming Reminders Section
                if (model.UpcomingReminders.Any())
                {
                    column.Item().PaddingTop(12).Element(c => RemindersSection(c, model));
                }

                // Health History Section
                if (model.RecentHealthRecords.Any())
                {
                    column.Item().PaddingTop(12).Element(c => HealthHistorySection(c, model));
                }

                // Weight History Section
                if (model.WeightHistory.Any())
                {
                    column.Item().PaddingTop(12).Element(c => WeightHistorySection(c, model));
                }

                // Empty state message
                if (!model.Vaccinations.Any() && !model.ActiveMedications.Any() &&
                    !model.KnownAllergies.Any() && !model.UpcomingReminders.Any() &&
                    !model.RecentHealthRecords.Any())
                {
                    column.Item().PaddingTop(20).AlignCenter().Text("No health records found for this pet yet.")
                        .FontSize(11).FontColor(NeutralMedium);
                }
            });
        }

        private static void PetIdentitySection(IContainer container, PetSummaryViewModel model, IFileHelper fileHelper)
        {
            container.Column(column =>
            {
                var petName = GetPetDisplayName(model);
                var subtitle = GetPetSubtitle(model);
                var imagePath = ResolvePetImagePath(model, fileHelper);

                if (!string.IsNullOrWhiteSpace(imagePath))
                {
                    // Constrained and isolated image container to keep layout predictable.
                    column.Item().PaddingBottom(5).AlignLeft().Width(72).Height(72).Image(imagePath);
                }

                column.Item().Text(petName).FontSize(18).Bold().FontColor(PrimaryColor);

                if (!string.IsNullOrWhiteSpace(subtitle))
                {
                    column.Item().PaddingTop(2).Text(subtitle).FontSize(10).FontColor(NeutralMedium);
                }

                if (model.PetData.PetStatus != PetStatus.Active)
                {
                    column.Item().PaddingTop(3).Text($"Status: {model.PetData.PetStatus}")
                        .FontSize(9).Bold().FontColor(WarningColor);
                }
            });
        }

        private static void PetProfileSection(IContainer container, PetSummaryViewModel model)
        {
            container.Column(column =>
            {
                column.Item().PaddingTop(8).Element(c => SectionHeader(c, "Pet Profile", ProfileIconSvg, NeutralDark));

                column.Item().PaddingTop(3).PaddingBottom(3).BorderBottom(1).BorderColor(NeutralLight);

                var pet = model.PetData;

                // Calculate age
                string ageStr = "";
                if (!string.IsNullOrWhiteSpace(pet.DateOfBirth) && DateTime.TryParse(pet.DateOfBirth, out DateTime dob))
                {
                    var today = DateTime.Today;
                    int years = today.Year - dob.Year;
                    int months = today.Month - dob.Month;
                    if (months < 0) { years--; months += 12; }
                    if (today.Day < dob.Day) months--;
                    ageStr = years > 0 ? $"{years} yr{(years != 1 ? "s" : "")} {months} mo" : $"{months} mo";
                    if (pet.IsEstimatedAge) ageStr += " (est.)";
                }

                // Keep profile details in a single vertical flow for robust paging.
                column.Item().PaddingTop(6).Column(col =>
                {
                    if (!string.IsNullOrWhiteSpace(pet.PetSex))
                    {
                        var sexText = pet.PetSex;
                        if (pet.IsSpayedNeutered)
                            sexText += " (Spayed/Neutered)";
                        AddProfileField(col, "Sex", sexText);
                    }

                    if (!string.IsNullOrWhiteSpace(ageStr))
                        AddProfileField(col, "Age", ageStr);

                    if (!string.IsNullOrWhiteSpace(pet.DateOfBirth))
                        AddProfileField(col, "DOB", pet.DateOfBirth);

                    if (!string.IsNullOrWhiteSpace(pet.CurrentWeight))
                        AddProfileField(col, "Current Weight", pet.CurrentWeight);

                    if (model.WeightHistory.Any())
                    {
                        var lastWeight = model.WeightHistory.First();
                        var weightUnit = string.IsNullOrWhiteSpace(lastWeight.WeightUnit) ? "lbs" : lastWeight.WeightUnit;
                        AddProfileField(col, "Last Recorded Weight", $"{lastWeight.WeightValue} {weightUnit} on {lastWeight.Date.ToShortDateString()}");
                    }

                    if (!string.IsNullOrWhiteSpace(pet.MicrochipNumber))
                        AddProfileField(col, "Microchip", pet.MicrochipNumber);

                    if (!string.IsNullOrWhiteSpace(pet.LicenseNumber))
                        AddProfileField(col, "License/Tag", pet.LicenseNumber);

                    if (!string.IsNullOrWhiteSpace(pet.PrimaryVet))
                        AddProfileField(col, "Primary Vet", pet.PrimaryVet);

                    if (!string.IsNullOrWhiteSpace(pet.EmergencyContact))
                        AddProfileField(col, "Emergency Contact", pet.EmergencyContact);
                });
            });
        }

        private static void AllergiesSection(IContainer container, PetSummaryViewModel model)
        {
            container.Column(column =>
            {
                column.Item().Element(c => SectionHeader(c, "Known Allergies / Reactions", AllergyIconSvg, WarningColor));

                column.Item().PaddingTop(3).PaddingBottom(3).BorderBottom(1).BorderColor(NeutralLight);

                column.Item().PaddingTop(6).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(1);
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("Allergen / Trigger").Bold().FontSize(8);
                        header.Cell().Element(CellStyle).Text("Type").Bold().FontSize(8);
                        header.Cell().Element(CellStyle).Text("Severity").Bold().FontSize(8);
                        header.Cell().Element(CellStyle).Text("Notes").Bold().FontSize(8);
                        header.Cell().Element(CellStyle).Text("Date").Bold().FontSize(8);

                        static IContainer CellStyle(IContainer container)
                        {
                            return container.Background("#fee2e2")
                                .BorderBottom(1).BorderColor(WarningColor)
                                .Padding(4);
                        }
                    });

                    // Rows
                    foreach (var allergy in model.KnownAllergies)
                    {
                        table.Cell().Element(CellStyle).Text(!string.IsNullOrWhiteSpace(allergy.Trigger) ? allergy.Trigger : allergy.Title).FontSize(8);
                        table.Cell().Element(CellStyle).Text(string.IsNullOrWhiteSpace(allergy.AllergyType) ? "Unknown" : allergy.AllergyType).FontSize(8);
                        table.Cell().Element(CellStyle).Text(string.IsNullOrWhiteSpace(allergy.Severity) ? "—" : allergy.Severity).FontSize(8);
                        table.Cell().Element(CellStyle).Text(allergy.Notes ?? "").FontSize(8);
                        table.Cell().Element(CellStyle).Text(allergy.Date.ToShortDateString()).FontSize(8);

                        static IContainer CellStyle(IContainer container)
                        {
                            return container.BorderBottom(1).BorderColor(NeutralLight).Padding(4);
                        }
                    }
                });
            });
        }

        private static void VaccinationsSection(IContainer container, PetSummaryViewModel model)
        {
            container.Column(column =>
            {
                column.Item().Element(c => SectionHeader(c, "Vaccination History", VaccinationIconSvg, NeutralDark));

                column.Item().PaddingTop(3).PaddingBottom(3).BorderBottom(1).BorderColor(NeutralLight);

                column.Item().PaddingTop(6).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(2);
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("Vaccine").Bold().FontSize(8);
                        header.Cell().Element(CellStyle).Text("Date").Bold().FontSize(8);
                        header.Cell().Element(CellStyle).Text("Next Due").Bold().FontSize(8);
                        header.Cell().Element(CellStyle).Text("Clinic").Bold().FontSize(8);

                        static IContainer CellStyle(IContainer container)
                        {
                            return container.Background("#dbeafe")
                                .BorderBottom(1).BorderColor(AccentColor)
                                .Padding(4);
                        }
                    });

                    // Rows
                    foreach (var vacc in model.Vaccinations)
                    {
                        table.Cell().Element(CellStyle).Text(vacc.VaccineName ?? "").FontSize(8);
                        table.Cell().Element(CellStyle).Text(vacc.Date.ToShortDateString()).FontSize(8);

                        // Next Due with overdue highlighting
                        table.Cell().Element(CellStyle).Text(text =>
                        {
                            if (!string.IsNullOrWhiteSpace(vacc.NextDueDate))
                            {
                                bool overdue = DateTime.TryParse(vacc.NextDueDate, out DateTime nd) && nd < DateTime.Today;
                                if (overdue)
                                    text.Span(vacc.NextDueDate).Bold().FontColor(WarningColor).FontSize(8);
                                else
                                    text.Span(vacc.NextDueDate).FontSize(8);
                            }
                            else
                            {
                                text.Span("—").FontSize(8);
                            }
                        });

                        table.Cell().Element(CellStyle).Text(string.IsNullOrWhiteSpace(vacc.Clinic) ? (vacc.AdministeredBy ?? "") : vacc.Clinic).FontSize(8);

                        static IContainer CellStyle(IContainer container)
                        {
                            return container.BorderBottom(1).BorderColor(NeutralLight).Padding(4);
                        }
                    }
                });
            });
        }

        private static void MedicationsSection(IContainer container, PetSummaryViewModel model)
        {
            container.Column(column =>
            {
                column.Item().Element(c => SectionHeader(c, "Current Medications", MedicationIconSvg, NeutralDark));

                column.Item().PaddingTop(3).PaddingBottom(3).BorderBottom(1).BorderColor(NeutralLight);

                column.Item().PaddingTop(6).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("Medication").Bold().FontSize(8);
                        header.Cell().Element(CellStyle).Text("Dose / Frequency").Bold().FontSize(8);
                        header.Cell().Element(CellStyle).Text("Route").Bold().FontSize(8);
                        header.Cell().Element(CellStyle).Text("Purpose").Bold().FontSize(8);
                        header.Cell().Element(CellStyle).Text("Started").Bold().FontSize(8);

                        static IContainer CellStyle(IContainer container)
                        {
                            return container.Background("#f3f4f6")
                                .BorderBottom(1).BorderColor(NeutralMedium)
                                .Padding(4);
                        }
                    });

                    // Rows
                    foreach (var med in model.ActiveMedications)
                    {
                        table.Cell().Element(CellStyle).Text(med.MedicationName ?? "").Bold().FontSize(8);
                        table.Cell().Element(CellStyle).Text($"{med.Dosage} {med.Unit} — {med.Frequency}").FontSize(8);
                        table.Cell().Element(CellStyle).Text(med.Route ?? "").FontSize(8);
                        table.Cell().Element(CellStyle).Text(med.Purpose ?? "").FontSize(8);
                        table.Cell().Element(CellStyle).Text(med.Date.ToShortDateString()).FontSize(8);

                        static IContainer CellStyle(IContainer container)
                        {
                            return container.BorderBottom(1).BorderColor(NeutralLight).Padding(4);
                        }
                    }
                });
            });
        }

        private static void RemindersSection(IContainer container, PetSummaryViewModel model)
        {
            container.Column(column =>
            {
                column.Item().Element(c => SectionHeader(c, "Upcoming Care (next 90 days)", UpcomingCareIconSvg, NeutralDark));

                column.Item().PaddingTop(3).PaddingBottom(3).BorderBottom(1).BorderColor(NeutralLight);

                column.Item().PaddingTop(6).Column(col =>
                {
                    foreach (var reminder in model.UpcomingReminders)
                    {
                        bool urgent = reminder.Date <= DateTime.Today.AddDays(7);
                        col.Item().PaddingBottom(3).Row(row =>
                        {
                            row.AutoItem().PaddingRight(5).Width(3).Height(9).Background(urgent ? AlertColor : NeutralMedium);
                            row.ConstantItem(70).Text(reminder.Date.ToShortDateString())
                                .Bold()
                                .FontSize(9)
                                .FontColor(urgent ? AlertColor : NeutralDark);
                            row.RelativeItem().Text(reminder.Description ?? "")
                                .FontSize(9)
                                .FontColor(urgent ? AlertColor : NeutralDark);
                        });
                    }
                });
            });
        }

        private static void HealthHistorySection(IContainer container, PetSummaryViewModel model)
        {
            container.Column(column =>
            {
                column.Item().Element(c => SectionHeader(c, "Health History", HealthHistoryIconSvg, NeutralDark));

                column.Item().PaddingTop(3).PaddingBottom(3).BorderBottom(1).BorderColor(NeutralLight);

                column.Item().PaddingTop(6).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(3);
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("Date").Bold().FontSize(8);
                        header.Cell().Element(CellStyle).Text("Category").Bold().FontSize(8);
                        header.Cell().Element(CellStyle).Text("Entry").Bold().FontSize(8);
                        header.Cell().Element(CellStyle).Text("Provider").Bold().FontSize(8);
                        header.Cell().Element(CellStyle).Text("Notes").Bold().FontSize(8);

                        static IContainer CellStyle(IContainer container)
                        {
                            return container.Background("#f3f4f6")
                                .BorderBottom(1).BorderColor(NeutralMedium)
                                .Padding(4);
                        }
                    });

                    // Rows
                    foreach (var health in model.RecentHealthRecords)
                    {
                        table.Cell().Element(CellStyle).Text(health.Date.ToShortDateString()).FontSize(8);
                        table.Cell().Element(CellStyle).Text(FormatHealthRecordCategory(health.Category)).FontSize(8);
                        table.Cell().Element(CellStyle).Text(health.Title ?? "").FontSize(8);
                        table.Cell().Element(CellStyle).Text(health.Provider ?? "").FontSize(8);
                        table.Cell().Element(CellStyle).Text(TruncateNotes(health.Notes, 80)).FontSize(8);

                        static IContainer CellStyle(IContainer container)
                        {
                            return container.BorderBottom(1).BorderColor(NeutralLight).Padding(4);
                        }
                    }
                });
            });
        }

        private static void WeightHistorySection(IContainer container, PetSummaryViewModel model)
        {
            container.Column(column =>
            {
                column.Item().Element(c => SectionHeader(c, "Weight History", WeightHistoryIconSvg, NeutralDark));

                column.Item().PaddingTop(3).PaddingBottom(3).BorderBottom(1).BorderColor(NeutralLight);

                column.Item().PaddingTop(6).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("Date").Bold().FontSize(8);
                        header.Cell().Element(CellStyle).Text("Weight").Bold().FontSize(8);
                        header.Cell().Element(CellStyle).Text("Unit").Bold().FontSize(8);

                        static IContainer CellStyle(IContainer container)
                        {
                            return container.Background("#f3f4f6")
                                .BorderBottom(1).BorderColor(NeutralMedium)
                                .Padding(4);
                        }
                    });

                    // Rows
                    foreach (var weight in model.WeightHistory)
                    {
                        table.Cell().Element(CellStyle).Text(weight.Date.ToShortDateString()).FontSize(8);
                        table.Cell().Element(CellStyle).Text(weight.WeightValue.ToString()).FontSize(8);
                        table.Cell().Element(CellStyle).Text(string.IsNullOrWhiteSpace(weight.WeightUnit) ? "lbs" : weight.WeightUnit).FontSize(8);

                        static IContainer CellStyle(IContainer container)
                        {
                            return container.BorderBottom(1).BorderColor(NeutralLight).Padding(4);
                        }
                    }
                });
            });
        }

        private static string TruncateNotes(string notes, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(notes))
                return "";
            if (notes.Length <= maxLength)
                return notes;
            return notes.Substring(0, maxLength) + "...";
        }

        private static void AddProfileField(ColumnDescriptor column, string label, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            column.Item().PaddingBottom(2).Text(text =>
            {
                text.Span($"{label}: ").Bold().FontSize(9);
                text.Span(value).FontSize(9);
            });
        }

        private static void SectionHeader(IContainer container, string title, string iconSvg, string titleColor)
        {
            container.Row(row =>
            {
                row.AutoItem().PaddingRight(6).Width(12).Height(12).Element(icon =>
                {
                    if (string.IsNullOrWhiteSpace(iconSvg))
                    {
                        icon.Border(1).BorderColor(NeutralMedium);
                        return;
                    }

                    icon.Svg(iconSvg);
                });

                row.RelativeItem().Text(title).FontSize(12).Bold().FontColor(titleColor);
            });
        }

        private static string BuildIconSvg(string body, string strokeColor = "#374151")
        {
            return $"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='{strokeColor}' stroke-width='1.8' stroke-linecap='round' stroke-linejoin='round'>{body}</svg>";
        }

        private static string GetPetDisplayName(PetSummaryViewModel model)
        {
            return !string.IsNullOrWhiteSpace(model.PetData.PetName)
                ? model.PetData.PetName
                : $"{model.PetData.Year} {model.PetData.Make} {model.PetData.Model}";
        }

        private static string GetPetSubtitle(PetSummaryViewModel model)
        {
            var subtitle = model.PetData.Species;

            if (!string.IsNullOrWhiteSpace(model.PetData.Breed))
                subtitle += $" · {model.PetData.Breed}";

            if (!string.IsNullOrWhiteSpace(model.PetData.Color))
                subtitle += $" · {model.PetData.Color}";

            return subtitle;
        }

        private static string ResolvePetImagePath(PetSummaryViewModel model, IFileHelper fileHelper)
        {
            var imageLocation = model.PetData.ImageLocation;

            if (string.IsNullOrWhiteSpace(imageLocation) || imageLocation == "/defaults/noimage.png")
                return string.Empty;

            try
            {
                var fullImagePath = fileHelper.GetFullFilePath(imageLocation, mustExist: true);
                if (!string.IsNullOrWhiteSpace(fullImagePath) && System.IO.File.Exists(fullImagePath))
                    return fullImagePath;
            }
            catch
            {
                // If image fails to resolve, continue without it.
            }

            return string.Empty;
        }

        /// <summary>
        /// Formats HealthRecordCategory enum values as human-readable labels for PDF display.
        /// </summary>
        private static string FormatHealthRecordCategory(HealthRecordCategory category)
        {
            return category switch
            {
                HealthRecordCategory.VetVisit => "Vet Visit",
                HealthRecordCategory.Vaccination => "Vaccination",
                HealthRecordCategory.Medication => "Medication",
                HealthRecordCategory.IllnessSymptom => "Illness / Symptom",
                HealthRecordCategory.ProcedureSurgery => "Procedure / Surgery",
                HealthRecordCategory.Dental => "Dental",
                HealthRecordCategory.Grooming => "Grooming",
                HealthRecordCategory.WeightCheck => "Weight Check",
                HealthRecordCategory.AllergyReaction => "Allergy / Reaction",
                HealthRecordCategory.LabResult => "Lab Result",
                HealthRecordCategory.Licensing => "Licensing",
                HealthRecordCategory.PreventiveCare => "Preventive Care",
                HealthRecordCategory.BehavioralNote => "Behavioral Note",
                HealthRecordCategory.MiscellaneousCare => "Miscellaneous Care",
                _ => category.ToString()
            };
        }
    }
}
