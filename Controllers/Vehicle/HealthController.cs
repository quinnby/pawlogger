using CarCareTracker.Filter;
using CarCareTracker.Helper;
using CarCareTracker.Models;
using Microsoft.AspNetCore.Mvc;

namespace CarCareTracker.Controllers
{
    public partial class VehicleController
    {
        // -----------------------------------------------------------------------
        // Phase 3 – HealthRecord controller actions
        // NOTE: All "vehicleId" parameters here represent the PetId.
        // The parameter name uses legacy infrastructure terminology;
        // VehicleId == PetId throughout this controller.
        // Do not introduce new uses of "serviceRecord" terminology in this file.
        // -----------------------------------------------------------------------

        [TypeFilter(typeof(CollaboratorFilter))]
        [HttpGet]
        public IActionResult GetHealthRecordsByVehicleId(int vehicleId)
        {
            var result = _healthRecordDataAccess.GetHealthRecordsByVehicleId(vehicleId);
            bool _useDescending = _config.GetUserConfig(User).UseDescending;
            if (_useDescending)
            {
                result = result.OrderByDescending(x => x.Date).ToList();
            }
            else
            {
                result = result.OrderBy(x => x.Date).ToList();
            }
            return PartialView("Health/_HealthRecords", result);
        }

        [HttpGet]
        public IActionResult GetAddHealthRecordPartialView(int category = -1)
        {
            var input = new HealthRecordInput();
            // Allow callers to pre-seed a specific category (e.g. category=7 for WeightCheck).
            // This lets the "Log Weight Check" shortcut open the modal with the correct
            // category + weight fields already visible, without a separate action.
            if (category >= 0 && Enum.IsDefined(typeof(HealthRecordCategory), category))
            {
                input.Category = (HealthRecordCategory)category;
            }
            return PartialView("Health/_HealthRecordModal", input);
        }

        [HttpGet]
        public IActionResult GetHealthRecordForEditById(int healthRecordId)
        {
            var result = _healthRecordDataAccess.GetHealthRecordById(healthRecordId);
            if (!_userLogic.UserCanEditVehicle(GetUserID(), result.VehicleId, HouseholdPermission.View))
            {
                return Redirect("/Error/Unauthorized");
            }
            var convertedResult = new HealthRecordInput
            {
                Id = result.Id,
                VehicleId = result.VehicleId,
                Date = result.Date.ToShortDateString(),
                Category = result.Category,
                Title = result.Title,
                Description = result.Description,
                Cost = result.Cost,
                Notes = result.Notes,
                Provider = result.Provider,
                FollowUpRequired = result.FollowUpRequired,
                FollowUpDate = result.FollowUpDate,
                Status = result.Status,
                Files = result.Files,
                Tags = result.Tags,
                ExtraFields = result.ExtraFields,
                // Phase 7 fields
                WeightValue = result.WeightValue,
                WeightUnit = result.WeightUnit,
                Severity = result.Severity,
                AllergyType = result.AllergyType,
                Trigger = result.Trigger,
                ReminderEnabled = result.ReminderEnabled,
                ReminderDueDate = result.ReminderDueDate
            };
            return PartialView("Health/_HealthRecordModal", convertedResult);
        }

        [HttpPost]
        public IActionResult SaveHealthRecordToVehicleId(HealthRecordInput healthRecord)
        {
            if (!_userLogic.UserCanEditVehicle(GetUserID(), healthRecord.VehicleId, HouseholdPermission.Edit))
            {
                return Json(OperationResponse.Failed("Access Denied"));
            }
            // Move any newly uploaded files out of temp storage
            healthRecord.Files = healthRecord.Files
                .Select(x => new UploadedFiles
                {
                    Name = x.Name,
                    Location = _fileHelper.MoveFileFromTemp(x.Location, "documents/")
                }).ToList();

            var convertedRecord = healthRecord.ToHealthRecord();
            var result = _healthRecordDataAccess.SaveHealthRecord(convertedRecord);
            if (result)
            {
                if (healthRecord.Category == HealthRecordCategory.PreventiveCare)
                {
                    // Phase 7 – Sync a date-based reminder for preventive care records
                    SyncReminderFromLinkedRecord(
                        petId: convertedRecord.VehicleId,
                        reminderEnabled: healthRecord.ReminderEnabled,
                        dueDateString: healthRecord.ReminderDueDate,
                        description: $"Preventive Care Due: {healthRecord.Title}",
                        petReminderType: PetReminderType.Custom,
                        linkedRecordType: ReminderLinkedRecordType.HealthRecord,
                        linkedRecordId: convertedRecord.Id);
                }

                // Phase 5.1 – Follow-up reminder: runs for all categories when FollowUpRequired is set.
                // Uses a distinct PetReminderType so it coexists safely with the PreventiveCare
                // reminder above on the same HealthRecord without overwriting it.
                // When FollowUpRequired is cleared or FollowUpDate removed, the reminder is deleted.
                // NOTE: This sync path is only triggered via the HealthRecord modal (SaveHealthRecordToVehicleId).
                // When a specialized record (VetVisit, Vaccination, etc.) projects into a HealthRecord
                // via SyncLinkedHealthRecord, this sync does NOT run automatically; the follow-up reminder
                // for that specialized record is managed by its own controller instead.
                SyncReminderFromLinkedRecord(
                    petId: convertedRecord.VehicleId,
                    reminderEnabled: healthRecord.FollowUpRequired && !string.IsNullOrWhiteSpace(healthRecord.FollowUpDate),
                    dueDateString: healthRecord.FollowUpDate,
                    description: $"Follow-up: {healthRecord.Title}",
                    petReminderType: PetReminderType.FollowUpReminder,
                    linkedRecordType: ReminderLinkedRecordType.HealthRecord,
                    linkedRecordId: convertedRecord.Id);

                // Weight Check sync: keep CurrentWeight in step with the most recent
                // Weight Check record so the pet profile always shows a useful value.
                // Manual edits to CurrentWeight remain valid until the next Weight Check is saved.
                if (healthRecord.Category == HealthRecordCategory.WeightCheck && healthRecord.WeightValue > 0)
                {
                    TrySyncCurrentWeightFromLatestWeightCheck(convertedRecord.VehicleId);
                }
            }
            return Json(OperationResponse.Conditional(result, string.Empty, StaticHelper.GenericErrorMessage));
        }

        [TypeFilter(typeof(CollaboratorFilter))]
        [HttpGet]
        public IActionResult GetWeightHistoryByVehicleId(int vehicleId)
        {
            var records = _healthRecordDataAccess
                .GetHealthRecordsByVehicleId(vehicleId)
                .Where(x => x.Category == HealthRecordCategory.WeightCheck && x.WeightValue > 0)
                .OrderBy(x => x.Date)
                .Select(x => new
                {
                    date = x.Date.ToShortDateString(),
                    value = x.WeightValue,
                    unit = string.IsNullOrWhiteSpace(x.WeightUnit) ? "lbs" : x.WeightUnit,
                    title = x.Title
                })
                .ToList();
            return Json(records);
        }

        [HttpGet]
        public IActionResult GetAddQuickHealthNotePartialView()
        {
            // Note: the JS caller passes vehicleId as a query param but this action does
            // not consume it — vehicleId is embedded on the client side during save.
            // Pre-populate with Informational status and IllnessSymptom category as sensible defaults
            var model = new HealthRecordInput
            {
                Category = HealthRecordCategory.IllnessSymptom,
                Status = HealthRecordStatus.Informational
            };
            return PartialView("Health/_QuickHealthNoteModal", model);
        }

        [HttpPost]
        public IActionResult DeleteHealthRecordById(int healthRecordId)
        {
            var existingRecord = _healthRecordDataAccess.GetHealthRecordById(healthRecordId);
            if (!_userLogic.UserCanEditVehicle(GetUserID(), existingRecord.VehicleId, HouseholdPermission.Delete))
            {
                return Json(OperationResponse.Failed("Access Denied"));
            }
            // Phase 4.2 – If this HealthRecord is a projection of a specialized record, clear the
            // back-reference on that specialized record so it does not hold a stale LinkedHealthRecordId.
            // On the next save of that specialized record a fresh projection will be created automatically.
            // If LinkedSpecializedRecordType is empty this is a standalone HealthRecord; no cleanup needed.
            if (existingRecord.LinkedSpecializedRecordId > 0 &&
                !string.IsNullOrWhiteSpace(existingRecord.LinkedSpecializedRecordType))
            {
                ClearLinkedHealthRecordId(
                    existingRecord.LinkedSpecializedRecordType,
                    existingRecord.LinkedSpecializedRecordId);
            }
            // Phase 5.1 – Delete any auto-linked reminders (FollowUp and/or PreventiveCare) before
            // removing this HealthRecord so they do not become silent orphans in the reminder list.
            DeleteAllLinkedReminders(existingRecord.VehicleId, ReminderLinkedRecordType.HealthRecord, existingRecord.Id);
            var result = _healthRecordDataAccess.DeleteHealthRecordById(existingRecord.Id);
            return Json(OperationResponse.Conditional(result, string.Empty, StaticHelper.GenericErrorMessage));
        }

        // -----------------------------------------------------------------------
        // Phase 4 integration – shared helpers used by specialized record controllers
        // -----------------------------------------------------------------------

        /// <summary>
        /// Creates a new HealthRecord linked to a specialized record, or updates the
        /// existing one if a prior link already exists.
        ///
        /// Call this AFTER the specialized record has been saved so that
        /// <paramref name="specializedRecordId"/> is the DB-assigned Id.
        ///
        /// ONE-WAY SYNC: data always flows specialized record → HealthRecord.
        /// Any manual edits made directly to the HealthRecord via the timeline UI
        /// WILL be overwritten the next time the owning specialized record is saved.
        /// This is intentional for Phase 4. Bidirectional merge is deferred to a later phase.
        ///
        /// STALE-LINK RECOVERY: if <paramref name="existingLinkedHealthRecordId"/> is non-zero
        /// but the referenced HealthRecord no longer exists in the DB (e.g. deleted directly
        /// via the timeline), this method silently falls through to the create-path and inserts
        /// a fresh projection. The caller then persists the new Id back onto the specialized
        /// record so future saves use the correct link.
        ///
        /// Returns the HealthRecord.Id (> 0 on success; 0 if the save failed).
        /// </summary>
        private int SyncLinkedHealthRecord(
            HealthRecord projected,
            int existingLinkedHealthRecordId,
            string specializedRecordType,
            int specializedRecordId)
        {
            projected.LinkedSpecializedRecordType = specializedRecordType;
            projected.LinkedSpecializedRecordId = specializedRecordId;

            if (existingLinkedHealthRecordId > 0)
            {
                // Edit-sync path: overwrite the existing linked HealthRecord with projected values.
                // See "ONE-WAY SYNC" note above – manual timeline edits will be lost here.
                var existing = _healthRecordDataAccess.GetHealthRecordById(existingLinkedHealthRecordId);
                if (existing != null && existing.Id > 0)
                {
                    existing.Category = projected.Category;
                    existing.Title = projected.Title;
                    existing.Date = projected.Date;
                    existing.Provider = projected.Provider;
                    existing.Notes = projected.Notes;
                    existing.Cost = projected.Cost;
                    existing.FollowUpRequired = projected.FollowUpRequired;
                    existing.FollowUpDate = projected.FollowUpDate;
                    existing.Status = projected.Status;
                    existing.LinkedSpecializedRecordType = specializedRecordType;
                    existing.LinkedSpecializedRecordId = specializedRecordId;
                    _healthRecordDataAccess.SaveHealthRecord(existing);
                    return existing.Id;
                }
                // Stale-link recovery: linked HealthRecord was not found (deleted externally or
                // via the timeline before Phase 4.2 cleanup was in place). Fall through to the
                // create-path to re-project a new entry and heal the broken link.
            }

            // Create-path: insert a brand-new HealthRecord projection.
            _healthRecordDataAccess.SaveHealthRecord(projected);
            return projected.Id;
        }

        /// <summary>
        /// Phase 4.2 – Clears <c>LinkedHealthRecordId</c> on the given specialized record so
        /// it does not hold a stale reference after its projected HealthRecord is deleted.
        /// On the next save of that specialized record a fresh projection will be created
        /// automatically via <see cref="SyncLinkedHealthRecord"/>.
        /// </summary>
        /// <remarks>
        /// This is the reverse of the create-path in <see cref="SyncLinkedHealthRecord"/>.
        /// It intentionally does NOT delete the specialized record itself.
        /// </remarks>
        private void ClearLinkedHealthRecordId(string specializedType, int specializedId)
        {
            switch (specializedType)
            {
                case "Vaccination":
                    var vacc = _vaccinationRecordDataAccess.GetVaccinationRecordById(specializedId);
                    if (vacc != null && vacc.Id > 0)
                    {
                        vacc.LinkedHealthRecordId = 0;
                        _vaccinationRecordDataAccess.SaveVaccinationRecord(vacc);
                    }
                    break;
                case "Medication":
                    var med = _medicationRecordDataAccess.GetMedicationRecordById(specializedId);
                    if (med != null && med.Id > 0)
                    {
                        med.LinkedHealthRecordId = 0;
                        _medicationRecordDataAccess.SaveMedicationRecord(med);
                    }
                    break;
                case "VetVisit":
                    var vet = _vetVisitRecordDataAccess.GetVetVisitRecordById(specializedId);
                    if (vet != null && vet.Id > 0)
                    {
                        vet.LinkedHealthRecordId = 0;
                        _vetVisitRecordDataAccess.SaveVetVisitRecord(vet);
                    }
                    break;
                case "Licensing":
                    var lic = _licensingRecordDataAccess.GetLicensingRecordById(specializedId);
                    if (lic != null && lic.Id > 0)
                    {
                        lic.LinkedHealthRecordId = 0;
                        _licensingRecordDataAccess.SaveLicensingRecord(lic);
                    }
                    break;
                default:
                    // Unknown type – no cleanup performed.
                    // The stale link will self-heal on the next specialized record save via
                    // SyncLinkedHealthRecord's create-path fallback.
                    _logger.LogWarning(
                        "Phase 4.2 ClearLinkedHealthRecordId: unknown specialized type '{Type}' (id={Id}). Stale link left to self-heal on next save.",
                        specializedType, specializedId);
                    break;
            }
        }

        /// <summary>
        /// Reads all Weight Check records for the given pet, finds the most recent one with a
        /// valid WeightValue, and writes a formatted string ("{value} {unit}") to the pet
        /// profile's CurrentWeight field.  Silently no-ops if the pet cannot be found or if
        /// no qualifying Weight Check records exist, so it can never break the outer save.
        /// </summary>
        private void TrySyncCurrentWeightFromLatestWeightCheck(int petId)
        {
            try
            {
                var latestWeightCheck = _healthRecordDataAccess
                    .GetHealthRecordsByVehicleId(petId)
                    .Where(x => x.Category == HealthRecordCategory.WeightCheck && x.WeightValue > 0)
                    .OrderByDescending(x => x.Date)
                    .FirstOrDefault();

                if (latestWeightCheck == null)
                    return;

                var pet = _dataAccess.GetVehicleById(petId);
                if (pet == null || pet.Id == 0)
                    return;

                var unit = string.IsNullOrWhiteSpace(latestWeightCheck.WeightUnit)
                    ? "lbs"
                    : latestWeightCheck.WeightUnit.Trim();

                pet.CurrentWeight = $"{latestWeightCheck.WeightValue} {unit}";
                _dataAccess.SaveVehicle(pet);
            }
            catch (Exception ex)
            {
                // Non-fatal: log and continue so the original HealthRecord save result is unaffected.
                _logger.LogWarning(ex, "TrySyncCurrentWeightFromLatestWeightCheck failed for petId={PetId}", petId);
            }
        }
    }
}
